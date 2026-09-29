using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	// 1.8.37: two-flap nozzle animation (visual only).
	// Aryx's TVC mesh holds the two flaps as separate pieces. Each flap is split (along existing edges) into a root
	// piece under the shroud, a forward segment and an end segment; seam strips close the joints. Every vertex follows
	// exactly one bone, so nothing stretches. The game keeps rotating the vectoring transform for the thrust physics;
	// the skinned mesh ignores that transform and follows the bones instead. Geometry is in the TVC rest frame
	// (z fwd, y up, m).
	// 1.8.40: kinematics in NozzleKinematics (F119 footage and Tom's sketches). The forward segment is split: its outer
	// skin is the flap's own cowl panel (floats between root and flap), its inner wall belongs to the flap, its side panels
	// are removed (open, as on the real nozzle). The overlapping-panel look is kept; the inside of the gaps is dark:
	// as on the F119: a box beam bolted under each side edge of the skin (rigid with the skin, from 10 cm aft of its front
	// edge to its back edge, two bolt tabs), a black liner under the skin, black backdrop plates at |x| 0.37 (one on the
	// skin, one on the flap running aft under the gusset) so the cavity is dark and closed, a black floor over the flap's
	// inner wall, black end caps on the root and the flap front, and the end flaps' sides as dark grey gussets with two
	// black lightening holes. 148 tris added in all. The cavity parts render with plain dark URP/Lit materials (submeshes
	// 1-3), the rest with Aryx's skin material (submesh 0).
	// Also: the root's outer side edge is raised to run flush with the sidewall's top line (one vertex per side, just under
	// the shroud lip), and the skin's front bend-down is tapered - Aryx's full bend at the centre, flat at the side edges -
	// so the skin's front edge rides over the flush root when the flap swings out.
	internal class NozzleRig : MonoBehaviour
	{
		// bind pivots: root and cowl skin (top surface at the root/skin joint), flap (outer crease)
		private const float Za = -0.23f;
		private const float Ya = 0.50f;
		private const float Zb = -0.51f;
		private const float Yb = 0.435f;

		// Area schedule e: 0 = MIL (closed), ERest = idle, 1 = full AB.
		private const float ERest = NozzleKinematics.ERest;

		private static Mesh sharedMesh;
		private static bool dataFailed;
		private static bool loggedOnce;
		private static readonly FieldInfo fAfterburners = AccessTools.Field(typeof(JetNozzle), "afterburners");
		private static FieldInfo fAbAmount;

		private Transform tvc;
		private Transform rig;
		private Transform oL, rL, oU, rU, bL, bU;
		private Turbofan engine;

		private Aircraft aircraft;

		private SkinnedMeshRenderer smr;

		private Mesh origMesh;

		private Transform[] origBones;

		private Transform origRoot;

		private Bounds origBounds;

		private SkinQuality origQuality;

		private bool origOffscreen;

		private Material[] origMats;

		// 1.8.40: the cavity parts (black liners/backdrop/floor/caps/holes, dark grey beams and end-flap side gussets, grey
		// bolt tabs) get their own plain URP/Lit materials: in Aryx's atlas those spots come out mid grey through the
		// livery layer and pick up the skin's metallic/smoothness sheen. Built once from a clone of the URP/Lit material
		// Aryx's bundle already uses (no keyword changes, so no shader variant the game may not have).
		private static Material[] darkMats;
		private static bool darkTried;
		private static readonly Color[] DarkColors = new Color[3]
		{
			new Color(0.035f, 0.035f, 0.037f, 1f),
			new Color(0.085f, 0.085f, 0.09f, 1f),
			new Color(0.2f, 0.2f, 0.21f, 1f)
		};
		private const float DarkSmoothness = 0.15f;

		// 1.8.42: the seam bands (the two gaps between root, floating cowl and end flap) used Aryx's atlas grey, which reads
		// mid grey through the livery. They now get their own plain material, a shadow grey (Visuals / NozzleSeamShade).
		private static Color SeamShadeColor()
		{
			float g = Mathf.Clamp01(Plugin.NozzleSeamShade);
			return new Color(g, g, g * 1.04f, 1f);
		}

		private bool rigged;

		private float playerCheck;
		private object afterburner;
		private float e = -1f;

		public static void Attach(Turbofan tf, Aircraft a)
		{
			if (tf == null || !Plugin.NozzleAnimation)
			{
				return;
			}
			Transform[] vts = Acc.TF_vecT(tf);
			if (vts == null)
			{
				return;
			}
			JetNozzle[] nozzles = Acc.TF_nozzles(tf);
			foreach (Transform t in vts)
			{
				if (t == null || t.GetComponent<NozzleRig>() != null)
				{
					continue;
				}
				SkinnedMeshRenderer smr = t.GetComponent<SkinnedMeshRenderer>();
				Mesh m0 = smr != null ? smr.sharedMesh : null;
				if (m0 == null || m0.vertexCount != NozzleMeshData.SourceVertexCount || !m0.name.StartsWith("Aryx_KingRaptor_TVC"))
				{
					if (!loggedOnce)
					{
						loggedOnce = true;
						Plugin.Log.LogWarning("F-22E nozzle rig: vectoring transform '" + t.name + "' does not carry the expected Aryx TVC mesh (" + (m0 != null ? (m0.name + ", " + m0.vertexCount + " verts") : "none") + "); nozzle animation left stock.");
					}
					continue;
				}
				Mesh mesh = GetMesh();
				if (mesh == null)
				{
					return;
				}
				JetNozzle jn = null;
				if (nozzles != null)
				{
					foreach (JetNozzle n in nozzles)
					{
						if (n != null && (n.transform == t || Acc.JN_thrustT(n) == t))
						{
							jn = n;
						}
					}
					if (jn == null && nozzles.Length > 0)
					{
						jn = nozzles[0];
					}
				}
				t.gameObject.AddComponent<NozzleRig>().Init(t, smr, mesh, tf, jn, a);
			}
		}

		private static Transform Bone(string name, Transform parent, Vector3 p)
		{
			Transform b = new GameObject(name).transform;
			b.SetParent(parent, false);
			b.localPosition = p;
			b.localRotation = Quaternion.identity;
			b.localScale = Vector3.one;
			return b;
		}

		private void Init(Transform t, SkinnedMeshRenderer r, Mesh mesh, Turbofan tf, JetNozzle jn, Aircraft a)
		{
			tvc = t;
			engine = tf;
			aircraft = a;
			smr = r;
			origMesh = r.sharedMesh;
			origBones = r.bones;
			origRoot = r.rootBone;
			origBounds = r.localBounds;
			origQuality = r.quality;
			origOffscreen = r.updateWhenOffscreen;
			rig = new GameObject("FCS_NozzleRig").transform;
			rig.SetParent(t.parent, false);
			rig.localPosition = t.localPosition;
			rig.localRotation = Quaternion.identity;
			rig.localScale = t.localScale;
			// bone order must match NozzleMeshData.Bone: 0 lower root, 1 lower forward segment, 2 lower end segment,
			// 3 upper root, 4 upper forward segment, 5 upper end segment. All are posed directly under the rig.
			oL = Bone("FlapLowerRoot", rig, new Vector3(0f, -Ya, Za));
			rL = Bone("FlapLowerSkin", rig, new Vector3(0f, -Ya, Za));
			bL = Bone("FlapLowerFlap", rig, new Vector3(0f, -Yb, Zb));
			oU = Bone("FlapUpperRoot", rig, new Vector3(0f, Ya, Za));
			rU = Bone("FlapUpperSkin", rig, new Vector3(0f, Ya, Za));
			bU = Bone("FlapUpperFlap", rig, new Vector3(0f, Yb, Zb));
			rigMesh = mesh;
			try
			{
				if (jn != null && fAfterburners != null && fAfterburners.GetValue(jn) is Array arr && arr.Length > 0 && arr.GetValue(0) != null)
				{
					afterburner = arr.GetValue(0);
					if (fAbAmount == null)
					{
						fAbAmount = AccessTools.Field(afterburner.GetType(), "afterburnerAmount");
					}
				}
			}
			catch
			{
				afterburner = null;
			}
			SetRigged(WantRig());
		}

		private Mesh rigMesh;

		// 1.8.40: AI jets keep Aryx's one-piece nozzle (NozzleAnimationAI = false) - no skinning or per-frame posing for them.
		private bool WantRig()
		{
			if (Plugin.NozzleAnimationAI)
			{
				return true;
			}
			try
			{
				return aircraft == null || (UnityEngine.Object)(object)aircraft.Player != null;
			}
			catch
			{
				return true;
			}
		}

		private void SetRigged(bool on)
		{
			if (smr == null || on == rigged)
			{
				return;
			}
			if (on)
			{
				smr.sharedMesh = rigMesh;
				origMats = smr.sharedMaterials;
				Material baseMat = origMats != null && origMats.Length > 0 ? origMats[0] : null;
				Material[] dm = DarkMats(smr.transform);
				smr.sharedMaterials = new Material[5]
				{
					baseMat,
					dm != null ? dm[0] : baseMat,
					dm != null ? dm[1] : baseMat,
					dm != null ? dm[2] : baseMat,
					dm != null ? dm[3] : baseMat
				};
				smr.bones = new Transform[6] { oL, rL, bL, oU, rU, bU };
				smr.rootBone = rig;
				smr.quality = SkinQuality.Bone1;
				smr.updateWhenOffscreen = false;
				smr.localBounds = new Bounds(new Vector3(0f, 0f, -0.5f), new Vector3(1.4f, 2.1f, 2.5f));
				rigged = true;
				Apply(e < 0f ? Target() : e);
			}
			else
			{
				smr.sharedMesh = origMesh;
				if (origMats != null)
				{
					smr.sharedMaterials = origMats;
				}
				smr.bones = origBones;
				smr.rootBone = origRoot;
				smr.localBounds = origBounds;
				smr.quality = origQuality;
				smr.updateWhenOffscreen = origOffscreen;
				rigged = false;
			}
		}

		private static Mesh GetMesh()
		{
			if (sharedMesh != null)
			{
				return sharedMesh;
			}
			if (dataFailed)
			{
				return null;
			}
			try
			{
				int n = NozzleMeshData.VertexCount;
				float[] pos = Floats(NozzleMeshData.Pos, n * 3);
				float[] nrm = Floats(NozzleMeshData.Nrm, n * 3);
				float[] tan = Floats(NozzleMeshData.Tan, n * 4);
				float[] uv = Floats(NozzleMeshData.Uv, n * 2);
				byte[] bone = Convert.FromBase64String(NozzleMeshData.Bone);
				byte[] tb = Convert.FromBase64String(NozzleMeshData.Tri);
				if (bone.Length != n || tb.Length != NozzleMeshData.TriangleCount * 6)
				{
					throw new Exception("mesh data size mismatch");
				}
				Vector3[] v = new Vector3[n];
				Vector3[] vn = new Vector3[n];
				Vector4[] vt = new Vector4[n];
				Vector2[] vu = new Vector2[n];
				Color32[] vc = new Color32[n];
				BoneWeight[] bw = new BoneWeight[n];
				for (int i = 0; i < n; i++)
				{
					v[i] = new Vector3(pos[i * 3], pos[i * 3 + 1], pos[i * 3 + 2]);
					vn[i] = new Vector3(nrm[i * 3], nrm[i * 3 + 1], nrm[i * 3 + 2]);
					vt[i] = new Vector4(tan[i * 4], tan[i * 4 + 1], tan[i * 4 + 2], tan[i * 4 + 3]);
					vu[i] = new Vector2(uv[i * 2], uv[i * 2 + 1]);
					vc[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
					if (bone[i] > 5)
					{
						throw new Exception("bad bone index");
					}
					BoneWeight bwi = default(BoneWeight);
					bwi.boneIndex0 = bone[i];
					bwi.weight0 = 1f;
					bw[i] = bwi;
				}
				int[] tri = new int[NozzleMeshData.TriangleCount * 3];
				for (int j = 0; j < tri.Length; j++)
				{
					tri[j] = tb[j * 2] | (tb[j * 2 + 1] << 8);
				}
				Mesh mesh = new Mesh();
				mesh.name = "Aryx_KingRaptor_TVC_FCSRig";
				mesh.vertices = v;
				mesh.normals = vn;
				mesh.tangents = vt;
				mesh.uv = vu;
				mesh.colors32 = vc;
				byte[] sub = Convert.FromBase64String(NozzleMeshData.Sub);
				if (sub.Length != NozzleMeshData.TriangleCount)
				{
					throw new Exception("material slot data size mismatch");
				}
				int[] cnt = new int[5];
				for (int j = 0; j < sub.Length; j++)
				{
					if (sub[j] > 4)
					{
						throw new Exception("bad material slot");
					}
					cnt[sub[j]]++;
				}
				int[][] subTri = new int[5][];
				for (int k = 0; k < 5; k++)
				{
					subTri[k] = new int[cnt[k] * 3];
					cnt[k] = 0;
				}
				for (int j = 0; j < sub.Length; j++)
				{
					int k = sub[j];
					subTri[k][cnt[k]++] = tri[j * 3];
					subTri[k][cnt[k]++] = tri[j * 3 + 1];
					subTri[k][cnt[k]++] = tri[j * 3 + 2];
				}
				mesh.subMeshCount = 5;
				for (int k = 0; k < 5; k++)
				{
					mesh.SetTriangles(subTri[k], k);
				}
				mesh.boneWeights = bw;
				Vector3 pl = new Vector3(0f, -Ya, Za);
				Vector3 pu = new Vector3(0f, Ya, Za);
				Vector3 ql = new Vector3(0f, -Yb, Zb);
				Vector3 qu = new Vector3(0f, Yb, Zb);
				mesh.bindposes = new Matrix4x4[6]
				{
					Matrix4x4.Translate(-pl),
					Matrix4x4.Translate(-pl),
					Matrix4x4.Translate(-ql),
					Matrix4x4.Translate(-pu),
					Matrix4x4.Translate(-pu),
					Matrix4x4.Translate(-qu)
				};
				mesh.RecalculateBounds();
				mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
				sharedMesh = mesh;
				Plugin.Log.LogInfo($"F-22E nozzle rig: two-flap nozzle mesh built (root, cowl skin, flap, beams, liners) ({n} verts, {NozzleMeshData.TriangleCount} tris; Aryx's is 468 / 400).");
				return mesh;
			}
			catch (Exception ex)
			{
				dataFailed = true;
				Plugin.Log.LogWarning("F-22E nozzle rig: mesh build failed (" + ex.Message + "); nozzle animation left stock.");
				return null;
			}
		}

		private static Material[] DarkMats(Transform near)
		{
			if (darkTried)
			{
				return darkMats;
			}
			darkTried = true;
			try
			{
				Material template = null;
				Transform top = near != null ? near.root : null;
				if (top != null)
				{
					foreach (Renderer rr in top.GetComponentsInChildren<Renderer>(true))
					{
						foreach (Material m in rr.sharedMaterials)
						{
							if (m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Lit")
							{
								template = m;
								break;
							}
						}
						if (template != null)
						{
							break;
						}
					}
				}
				Shader sh = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
				if (sh == null)
				{
					Plugin.Log.LogWarning("F-22E nozzle rig: no URP/Lit shader found; cavity parts keep Aryx's material.");
					return null;
				}
				Material[] mats = new Material[4];
				for (int k = 0; k < 4; k++)
				{
					Material m = template != null ? new Material(template) : new Material(sh);
					m.name = "FCS_NozzleCavity" + k;
					m.hideFlags = HideFlags.DontUnloadUnusedAsset;
					if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
					if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
					Color dc = (k < 3) ? DarkColors[k] : SeamShadeColor();
					if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", dc);
					if (m.HasProperty("_Color")) m.SetColor("_Color", dc);
					if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
					if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", DarkSmoothness);
					if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", DarkSmoothness);
					mats[k] = m;
				}
				darkMats = mats;
				Plugin.Log.LogInfo($"F-22E nozzle rig: cavity materials from {(template != null ? "the bundle's URP/Lit material" : "Shader.Find(URP/Lit)")}.");
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning("F-22E nozzle rig: cavity materials failed, keeping Aryx's material: " + ex.Message);
				darkMats = null;
			}
			return darkMats;
		}

		private static float[] Floats(string b64, int count)
		{
			byte[] b = Convert.FromBase64String(b64);
			if (b.Length != count * 4)
			{
				throw new Exception("float block size mismatch");
			}
			float[] f = new float[count];
			Buffer.BlockCopy(b, 0, f, 0, b.Length);
			return f;
		}

		private float Target()
		{
			float rpm = 0f;
			try
			{
				rpm = engine != null ? engine.GetRPMRatio() : 0f;
			}
			catch
			{
			}
			float ab = 0f;
			if (afterburner != null && fAbAmount != null)
			{
				try
				{
					ab = Mathf.Clamp01((float)fAbAmount.GetValue(afterburner));
				}
				catch
				{
					ab = 0f;
				}
			}
			float dry;
			if (rpm < 0.25f)
			{
				dry = ERest;
			}
			else if (Plugin.NozzleIdleOpen)
			{
				dry = ERest * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.95f, rpm)));
			}
			else
			{
				dry = 0f;
			}
			return Mathf.Lerp(dry, 1f, ab);
		}

		private void LateUpdate()
		{
			if (tvc == null || rig == null || oU == null || rU == null || oL == null || rL == null || bU == null || bL == null)
			{
				Destroy(this);
				return;
			}
			playerCheck -= Time.deltaTime;
			if (playerCheck <= 0f)
			{
				playerCheck = 1f;
				SetRigged(WantRig());
			}
			if (!rigged)
			{
				return;
			}
			float target = Target();
			e = (e < 0f) ? target : Mathf.MoveTowards(e, target, Mathf.Max(0.05f, Plugin.NozzleAreaRate) * Time.deltaTime);
			Apply(e);
		}

		private void Apply(float ee)
		{
			Vector3 eu = tvc.localEulerAngles;
			float d = Mathf.DeltaAngle(0f, eu.x);
			rig.localRotation = Quaternion.Euler(0f, eu.y, eu.z);
			Blade(1f, d, ee, oU, rU, bU);
			Blade(-1f, d, ee, oL, rL, bL);
		}

		private static void Pose(Transform b, NozzleKinematics.T2 m, float sg, Vector2 pivot)
		{
			Vector2 p = m.Ap(pivot);
			b.localPosition = new Vector3(0f, sg * p.x, p.y);
			b.localRotation = Quaternion.Euler(sg * m.a, 0f, 0f);
		}

		// sg = +1 upper flap, -1 lower (mirror image of the upper-flap kinematics)
		private static void Blade(float sg, float d, float ee, Transform root, Transform skin, Transform flap)
		{
			NozzleKinematics.Flap(ee, sg * d, out var mr, out var ms, out var mf);
			Vector2 pRoot = new Vector2(Ya, Za);
			Pose(root, mr, sg, pRoot);
			Pose(skin, ms, sg, pRoot);
			Pose(flap, mf, sg, new Vector2(Yb, Zb));
		}

		private void OnDestroy()
		{
			if (rigged)
			{
				SetRigged(false);
			}
			if (rig != null)
			{
				Destroy(rig.gameObject);
			}
		}
	}
}
