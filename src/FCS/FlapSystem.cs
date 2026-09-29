using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal sealed class FlapSystem
	{
		private sealed class MovingPart
		{
			public Transform t;

			public bool move;

			public bool rotate;

			public Vector3 pR;

			public Vector3 pD;

			public Vector3 aR;

			public Vector3 aD;

			public void Animate(float x)
			{
				if (!(t == null))
				{
					if (move)
					{
						t.localPosition = Vector3.Lerp(pR, pD, x);
					}
					if (rotate)
					{
						t.localEulerAngles = Vector3.Lerp(aR, aD, x);
					}
				}
			}
		}

		private sealed class Hld
		{
			public HighLiftDevice d;

			public AeroPart part;

			public readonly List<MovingPart> parts = new List<MovingPart>();

			public float pos;
		}

		private sealed class Drive
		{
			public Component resp;

			public Transform rotator;

			public Quaternion baseRot;

			public SurfaceEffector surf;

			public bool flap;

			public Hld hl;

			public float hlAngle;

			public float angle;

			public bool Functional
			{
				get
				{
					if (resp != null)
					{
						if (!(fFunctional == null))
						{
							return (bool)fFunctional.GetValue(resp);
						}
						return true;
					}
					return false;
				}
			}
		}

		public static readonly Type ResponderType = AccessTools.TypeByName("Aryx_F22E_StrikeRaptor.AryxAlphaResponder");

		private static readonly FieldInfo fRotator = ((ResponderType != null) ? AccessTools.Field(ResponderType, "rotator") : null);

		private static readonly FieldInfo fFunctional = ((ResponderType != null) ? AccessTools.Field(ResponderType, "isFunctional") : null);

		private static readonly FieldInfo fDeflection = ((ResponderType != null) ? AccessTools.Field(ResponderType, "deflection") : null);

		private static readonly FieldInfo fMovingParts = AccessTools.Field(typeof(HighLiftDevice), "movingParts");

		private readonly List<Hld> hlds = new List<Hld>();

		private readonly List<Drive> drives = new List<Drive>();

		public readonly HashSet<int> ownedIds = new HashSet<int>();

		public bool Active;

		public float flapTarget;

		public float flapBias;

		public float hlPos;

		public float relief;

		public float ailBias;

		public bool Found
		{
			get
			{
				if (drives.Count <= 0)
				{
					return hlds.Count > 0;
				}
				return true;
			}
		}

		public FlapSystem(Aircraft ac, List<SurfaceEffector> surfaces)
		{
			foreach (HighLiftDevice item in Acc.Collect<HighLiftDevice>(ac))
			{
				Hld hld = new Hld
				{
					d = item,
					part = Acc.HL_part(item),
					pos = Acc.HL_position(item)
				};
				if (fMovingParts != null && fMovingParts.GetValue(item) is Array array)
				{
					foreach (object item2 in array)
					{
						if (item2 != null)
						{
							Traverse traverse = Traverse.Create(item2);
							hld.parts.Add(new MovingPart
							{
								t = traverse.Field("transform").GetValue<Transform>(),
								move = traverse.Field("move").GetValue<bool>(),
								rotate = traverse.Field("rotate").GetValue<bool>(),
								pR = traverse.Field("positionRetracted").GetValue<Vector3>(),
								pD = traverse.Field("positionDeployed").GetValue<Vector3>(),
								aR = traverse.Field("anglesRetracted").GetValue<Vector3>(),
								aD = traverse.Field("anglesDeployed").GetValue<Vector3>()
							});
						}
					}
				}
				hlds.Add(hld);
				ownedIds.Add(item.GetInstanceID());
			}
			if (!(ResponderType != null) || !(fRotator != null))
			{
				return;
			}
			foreach (Component r in Acc.Collect(ac, ResponderType))
			{
				Transform transform = fRotator.GetValue(r) as Transform;
				if (transform == null)
				{
					continue;
				}
				SurfaceEffector surfaceEffector = surfaces.Find((SurfaceEffector s) => s.cs != null && s.cs.gameObject == r.gameObject);
				if (surfaceEffector == null || (surfaceEffector.kind != SurfaceKind.Flaperon && surfaceEffector.kind != SurfaceKind.Aileron))
				{
					continue;
				}
				Drive drive = new Drive
				{
					resp = r,
					rotator = transform,
					baseRot = Quaternion.identity,
					surf = surfaceEffector,
					flap = (surfaceEffector.kind == SurfaceKind.Flaperon)
				};
				float num = transform.localEulerAngles.x;
				if (num > 180f)
				{
					num -= 360f;
				}
				drive.angle = num;
				foreach (Hld hld2 in hlds)
				{
					foreach (MovingPart part in hld2.parts)
					{
						if (part.t != null && part.rotate && part.t.IsChildOf(transform) && part.t != transform)
						{
							drive.hl = hld2;
							drive.hlAngle = part.aD.x - part.aR.x;
						}
					}
				}
				drives.Add(drive);
				ownedIds.Add(r.GetInstanceID());
			}
		}

		public string Describe()
		{
			string text = "";
			if (hlds.Count > 0 && hlds[0].d != null)
			{
				HighLiftDevice d = hlds[0].d;
				text = $" (stock schedule: deploy<{Acc.HL_speedDep(d):F0} m/s, retract>{Acc.HL_speedRet(d):F0}, alphaMin {Acc.HL_alphaMin(d):F0} deg, alphaFactor {Acc.HL_alphaFactor(d):F2}; " + $"part area {Acc.HL_areaRet(d):F1} -> {Acc.HL_areaDep(d):F1} m^2 - that area is the device's ONLY aero effect)";
			}
			return $"{hlds.Count} high-lift devices{text}, {drives.FindAll((Drive drive) => drive.flap).Count} flaperon drives, {drives.FindAll((Drive drive) => !drive.flap).Count} outer-flaperon drives";
		}

		private static float Wrap(float x)
		{
			x %= 360f;
			if (x > 180f)
			{
				x -= 360f;
			}
			else if (x < -180f)
			{
				x += 360f;
			}
			return x;
		}

		public void Update(float dt, FcsController c, bool ground, float speed, float stickRoll, float gndBrake)
		{
			Active = true;
			float alpha = c.alpha;
			float nz = c.nz;
			float num = 1f;
			float num2 = 1f;
			if (hlds.Count > 0)
			{
				float num3 = Acc.HL_speedDep(hlds[0].d);
				float num4 = Acc.HL_speedRet(hlds[0].d);
				num2 = Mathf.Clamp01(1f - Mathf.Max(0f, speed - num3) / Mathf.Max(1f, num4 - num3));
			}
			float num5 = (ground ? 1f : (Mathf.Clamp01(alpha / 4f) * Mathf.Clamp01((nz - 0.2f) / 0.5f)));
			float num6 = (ground ? 1f : (1f - Mathf.Clamp01((alpha - 20f) / 10f)));
			float num7 = (ground ? 0f : (8f * Mathf.Clamp01(alpha / 10f) * num6 * num5));
			float num8 = num2 * num5 * num6 * num;
			float num9 = 15f * num8 + num7;
			float num10 = 1f - Mathf.Clamp01((Mathf.Abs(alpha) - (Plugin.HighLiftMaxAoA - 10f)) / 10f);
			float b = Mathf.Clamp01((Mathf.Abs(alpha) - Plugin.SlatAoAStart) / Mathf.Max(1f, Plugin.SlatAoAFull - Plugin.SlatAoAStart));
			float value = (ground ? num2 : (num5 * Mathf.Max(num2 * num6, Plugin.HighLiftHighAoA * num10 * Mathf.Max(num2, b))));
			float num11 = 99f;
			float a = 0f;
			foreach (Drive drife in drives)
			{
				if (drife.flap && drife.surf != null && drife.surf.active)
				{
					a = Mathf.Max(a, Mathf.Max(0f, 0f - drife.surf.cmd));
				}
			}
			float num12 = Mathf.Max(a, 12f * Mathf.Abs(stickRoll));
			foreach (Drive drife2 in drives)
			{
				if (drife2.flap && drife2.surf != null && drife2.surf.active && drife2.Functional)
				{
					float num13 = 0f - (drife2.angle + ((drife2.hl != null) ? (drife2.hlAngle * drife2.hl.pos) : 0f));
					float num14 = drife2.surf.localAoANow - num13 + drife2.surf.now;
					num11 = Mathf.Min(num11, drife2.surf.stallAoA - 5f - num14 - num12);
				}
			}
			if (ground)
			{
				num11 = 99f;
			}
			float num15 = (flapTarget = Mathf.Clamp(Mathf.Min(num9, num11), 0f, 23f));
			flapBias = Mathf.MoveTowards(flapBias, num15, ((num15 > flapBias) ? 20f : 60f) * dt);
			float num16 = Mathf.Max(Mathf.Min(num8, flapBias / 15f), Mathf.Clamp01(value));
			if (gndBrake > 0.01f)
			{
				num16 = Mathf.Max(num16, gndBrake);
				flapBias = Mathf.MoveTowards(flapBias, 0f, 60f * dt);
				ailBias = Mathf.MoveTowards(ailBias, 0f, 40f * dt);
				relief = 0f;
			}
			foreach (Hld hld in hlds)
			{
				if (hld.d == null)
				{
					continue;
				}
				hld.pos = Mathf.MoveTowards(hld.pos, Mathf.Clamp01(num16), dt);
				Acc.HL_position(hld.d) = hld.pos;
				if (hld.part != null)
				{
					Acc.AP_wingArea(hld.part) = Mathf.Lerp(Acc.HL_areaRet(hld.d), Acc.HL_areaDep(hld.d), hld.pos);
				}
				foreach (MovingPart part in hld.parts)
				{
					part.Animate(hld.pos);
				}
				hlPos = hld.pos;
			}
			float a2 = Plugin.OuterFlapShare * num9;
			float num17 = 99f;
			float a3 = 0f;
			foreach (Drive drife3 in drives)
			{
				if (!drife3.flap && drife3.surf != null && drife3.surf.active)
				{
					a3 = Mathf.Max(a3, Mathf.Max(0f, 0f - drife3.surf.cmd));
				}
			}
			float num18 = Mathf.Max(a3, 18f * Mathf.Abs(stickRoll));
			foreach (Drive drife4 in drives)
			{
				if (!drife4.flap && drife4.surf != null && drife4.surf.active && drife4.Functional)
				{
					float num19 = drife4.surf.localAoANow + drife4.angle + drife4.surf.now;
					num17 = Mathf.Min(num17, drife4.surf.stallAoA - 5f - num19 - num18);
				}
			}
			if (ground)
			{
				num17 = 99f;
			}
			float num20 = Mathf.Clamp(Mathf.Min(a2, num17), 0f, 15f);
			ailBias = Mathf.MoveTowards(ailBias, num20, ((num20 > ailBias) ? 15f : 40f) * dt);
			relief = (ground ? 0f : (9.6f * Mathf.Clamp01((nz - 3f) / 6f) * (1f - Mathf.Clamp01((alpha - 16f) / 8f))));
			float num21 = 70f * Plugin.ActuatorScale * dt;
			foreach (Drive drife5 in drives)
			{
				if (!(drife5.rotator == null) && drife5.Functional)
				{
					float value2 = (drife5.flap ? (0f - flapBias - ((drife5.hl != null) ? (drife5.hlAngle * drife5.hl.pos) : 0f)) : (relief - ailBias));
					value2 = Mathf.Clamp(value2, -25f, 20f);
					drife5.angle = Mathf.MoveTowards(drife5.angle, value2, drife5.flap ? num21 : Mathf.Min(num21, 12f * dt));
					drife5.rotator.localRotation = drife5.baseRot * Quaternion.Euler(drife5.angle, 0f, 0f);
					if (fDeflection != null)
					{
						fDeflection.SetValue(drife5.resp, drife5.angle);
					}
				}
			}
		}

		public void Release()
		{
			Active = false;
		}
	}
}
