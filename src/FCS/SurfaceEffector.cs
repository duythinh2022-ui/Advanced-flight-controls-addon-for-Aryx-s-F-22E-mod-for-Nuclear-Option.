using NuclearOption.Jobs;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal sealed class SurfaceEffector : Effector
	{
		public readonly ControlSurface cs;

		public readonly AeroPart part;

		public readonly SurfaceKind kind;

		public readonly AirfoilTable foil;

		public readonly float stallAoA;

		public float origPitchRange;

		public float origRollRange;

		public float origYawRange;

		public bool overriding;

		public float localAoANow;

		public float localAoACmd;

		public bool linearPitch;

		public bool noPitch;

		public float pitchFrac = 1f;

		public float branchCool;

		private float[] rawY;

		public float honestGate;

		private float[] ySave;

		private bool honestOn;

		// 1.8.40 roll-pitch pass: swap in the surface's true pitch curve (flaperons: their pitch moment, otherwise held
		// at the current value; stabilators: the raw curve, past the lift peak included, instead of the monotone envelope).
		public void HonestPitchOn()
		{
			float[] src = noPitch ? pitchRaw : (linearPitch ? rawY : null);
			if (!active || src == null || src.Length != N || N < 2)
			{
				honestOn = false;
				return;
			}
			if (ySave == null || ySave.Length != N)
			{
				ySave = new float[N];
			}
			for (int k = 0; k < N; k++)
			{
				ySave[k] = M[k].y;
				M[k].y = src[k];
			}
			honestOn = true;
		}

		public float HonestPitchAt(float c)
		{
			float[] src = noPitch ? pitchRaw : (linearPitch ? rawY : null);
			if (src == null || src.Length != N || N < 2)
			{
				return MAt(c).y;
			}
			float f = (c - min) / base.Step;
			if (f <= 0f)
			{
				return src[0];
			}
			if (f >= (float)(N - 1))
			{
				return src[N - 1];
			}
			int i = (int)f;
			return src[i] + (src[i + 1] - src[i]) * (f - (float)i);
		}

		public void HonestPitchOff()
		{
			if (!honestOn)
			{
				return;
			}
			for (int k = 0; k < N; k++)
			{
				M[k].y = ySave[k];
			}
			honestOn = false;
		}

		public bool branchOn;

		public float branchFail;

		public float pitchSlope;

		public float localAoA0;

		public float[] DbgRaw;

		public float[] pitchRaw;

		public bool revFlow;

		public float teUp;

		internal static float RevFlowOn = 120f;

		internal static float RevFlowOff = 110f;

		internal static bool RevFlowEnabled = true;

		private Quaternion visRot;

		private Quaternion lRel;

		private float deltaVis;

		private Vector3 com;

		private Vector3 col;

		private Vector3 v;

		private Vector3 vhat;

		private float qS;

		private float v2;

		public bool Valid
		{
			get
			{
				if (cs != null && part != null && Acc.CS_visible(cs) != null && Acc.AP_liftNormal(part) != null && part.rb != null && Acc.JobCreated(cs))
				{
					return !part.IsDetached();
				}
				return false;
			}
		}

		public SurfaceEffector(ControlSurface cs, AeroPart part, SurfaceKind kind, AirfoilTable foil)
		{
			this.cs = cs;
			this.part = part;
			this.kind = kind;
			this.foil = foil;
			name = cs.gameObject.name.Replace("Aryx_KingRaptor_", "");
			origPitchRange = Acc.CS_pitchRange(cs);
			origRollRange = Acc.CS_rollRange(cs);
			origYawRange = Acc.CS_yawRange(cs);
			float num = Mathf.Abs(origPitchRange) + Mathf.Abs(origRollRange) + Mathf.Abs(origYawRange);
			if (num < 1f)
			{
				num = 20f;
			}
			regNorm = ((RegOrigSurf >= 2 || (RegOrigSurf == 1 && kind == SurfaceKind.Stabilator)) ? num : 0f);
			if (kind == SurfaceKind.Rudder && Plugin.RudderRange > 0f)
			{
				num = Plugin.RudderRange;
			}
			else if ((kind == SurfaceKind.Aileron || kind == SurfaceKind.Flaperon) && Plugin.FlaperonRange > 0f)
			{
				num = Plugin.FlaperonRange;
			}
			else if (kind == SurfaceKind.Stabilator && (Plugin.StabRangeTEUp > 0f || Plugin.StabRangeTEDown > 0f))
			{
				rangeUp = ((Plugin.StabRangeTEUp > 0f) ? Plugin.StabRangeTEUp : num);
				rangeDown = ((Plugin.StabRangeTEDown > 0f) ? Plugin.StabRangeTEDown : num);
				asymRange = Mathf.Abs(rangeUp - rangeDown) > 0.01f;
				num = (asymRange ? Mathf.Min(rangeUp, rangeDown) : rangeUp);
				gridSpan = Mathf.Max(rangeUp, rangeDown);
			}
			min = 0f - num;
			max = num;
			lo = min;
			hi = max;
			stallAoA = foil.StallAoADeg();
			int num2 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(num, gridSpan)) + 1, 9, 31);
			AllocGrid(num2);
			for (int i = 0; i < num2; i++)
			{
				grid[i] = min + (float)i * (max - min) / (float)(num2 - 1);
			}
		}

		public override void Evaluate(ref EvalContext ctx)
		{
			active = false;
			if (!Valid)
			{
				return;
			}
			ref ControlSurfaceFields reference = ref Acc.Job(cs);
			if (reference.IsDetached)
			{
				return;
			}
			Transform transform = Acc.CS_visible(cs).transform;
			Transform transform2 = Acc.AP_liftNormal(part);
			visRot = transform.rotation;
			lRel = Quaternion.Inverse(visRot) * transform2.rotation;
			Quaternion quaternion = Quaternion.Inverse(reference.restingRotation) * transform.localRotation;
			deltaVis = 2f * Mathf.Atan2(quaternion.x, quaternion.w) * 57.29578f;
			if (deltaVis > 180f)
			{
				deltaVis -= 360f;
			}
			else if (deltaVis < -180f)
			{
				deltaVis += 360f;
			}
			now = reference.currentPitch + reference.currentRoll + reference.currentYaw;
			rate = Mathf.Max(1f, reference.servoSpeed);
			com = part.rb.worldCenterOfMass;
			col = Acc.AP_centerOfLift(part);
			v = part.rb.velocity - ctx.wind;
			v2 = v.sqrMagnitude;
			if (v2 < 1f)
			{
				v2 = 0f;
				vhat = Vector3.forward;
			}
			else
			{
				vhat = v / Mathf.Sqrt(v2);
			}
			qS = 0.5f * ctx.rho * v2 * Acc.AP_wingArea(part) * Acc.AP_wingEff(part);
			if (teUp == 0f && v2 > 100f)
			{
				teUp = ((LocalAoAAt(5f) < LocalAoAAt(-5f)) ? 1f : (-1f));
				if (asymRange)
				{
					ApplyAsymRange();
				}
			}
			for (int i = 0; i < N; i++)
			{
				M[i] = Eval(grid[i], ref ctx, out var _);
			}
			Mnow = Eval(now, ref ctx, out localAoANow);
			if (noPitch)
			{
				if (pitchRaw == null || pitchRaw.Length != N)
				{
					pitchRaw = new float[N];
				}
				for (int j = 0; j < N; j++)
				{
					pitchRaw[j] = M[j].y;
					M[j].y = Mnow.y;
				}
			}
			if (linearPitch)
			{
				float num = 0f - Mathf.Sign((origPitchRange == 0f) ? (-1f) : origPitchRange);
				float num2 = Mathf.Abs(LocalAoAAt(0f));
				if (!revFlow && num2 > RevFlowOn)
				{
					revFlow = true;
				}
				else if (revFlow && num2 < RevFlowOff)
				{
					revFlow = false;
				}
				if (revFlow && RevFlowEnabled)
				{
					num = 0f - num;
				}
				if (DbgRaw != null && DbgRaw.Length >= N)
				{
					for (int k = 0; k < N; k++)
					{
						DbgRaw[k] = M[k].y;
					}
				}
				if (rawY == null || rawY.Length != N)
				{
					rawY = new float[N];
				}
				for (int k2 = 0; k2 < N; k2++)
				{
					rawY[k2] = M[k2].y;
				}
				float num3 = float.MaxValue;
				float num4 = float.MinValue;
				for (int l = 0; l < N; l++)
				{
					num3 = Mathf.Min(num3, M[l].y);
					num4 = Mathf.Max(num4, M[l].y);
				}
				if (num > 0f)
				{
					for (int m = 1; m < N; m++)
					{
						M[m].y = Mathf.Max(M[m].y, M[m - 1].y);
					}
				}
				else
				{
					for (int num5 = N - 2; num5 >= 0; num5--)
					{
						M[num5].y = Mathf.Max(M[num5].y, M[num5 + 1].y);
					}
				}
				float num6 = Mnow.y - MAt(now).y;
				for (int n = 0; n < N; n++)
				{
					M[n].y += num6;
				}
				float num7 = float.MaxValue;
				float num8 = float.MinValue;
				for (int num9 = 0; num9 < N; num9++)
				{
					num7 = Mathf.Min(num7, M[num9].y);
					num8 = Mathf.Max(num8, M[num9].y);
				}
				float num10 = num4 - num3;
				pitchFrac = ((num10 > 0.001f) ? Mathf.Clamp01((num8 - num7 - 0.25f * num10) / (0.75f * num10)) : 1f);
				localAoA0 = LocalAoAAt(0f);
				if (honestGate > 0.001f)
				{
					bool flag = localAoA0 >= 0f;
					float hg = Mathf.Clamp01(honestGate);
					for (int k3 = 0; k3 < N; k3++)
					{
						float hv = (flag ? Mathf.Max(M[k3].y, rawY[k3]) : Mathf.Min(M[k3].y, rawY[k3]));
						M[k3].y += (hv - M[k3].y) * hg;
					}
				}
			}
			active = true;
		}

		public Vector3 Eval(float delta, ref EvalContext ctx, out float localAoADeg)
		{
			Quaternion quaternion = visRot * Quaternion.AngleAxis(delta - deltaVis, Vector3.right) * lRel;
			Vector3 vector = Quaternion.Inverse(quaternion) * v;
			float num = Mathf.Atan2(vector.y, vector.z);
			localAoADeg = (0f - num) * 57.29578f;
			if (v2 <= 0f)
			{
				return Vector3.zero;
			}
			foil.Get(num, out var CL, out var CD);
			Vector3 rhs = quaternion * Vector3.right;
			Vector3 vector2 = Vector3.Cross(v, rhs);
			float magnitude = vector2.magnitude;
			if (magnitude < 0.0001f)
			{
				return Vector3.zero;
			}
			vector2 /= magnitude;
			Vector3 rhs2 = -vector2 * (CL * qS) - vhat * (CD * qS);
			Vector3 vector3 = com + quaternion * col;
			return Effector.ToAero(ctx.rootInv, Vector3.Cross(vector3 - ctx.cg, rhs2));
		}

		public Vector3 EvalFlow(float delta, Vector3 vFlow, float rho, ref EvalContext ctx)
		{
			float sqrMagnitude = vFlow.sqrMagnitude;
			if (sqrMagnitude < 1f)
			{
				return Vector3.zero;
			}
			Quaternion quaternion = visRot * Quaternion.AngleAxis(delta - deltaVis, Vector3.right) * lRel;
			Vector3 vector = Quaternion.Inverse(quaternion) * vFlow;
			foil.Get(Mathf.Atan2(vector.y, vector.z), out var CL, out var CD);
			Vector3 rhs = quaternion * Vector3.right;
			Vector3 vector2 = Vector3.Cross(vFlow, rhs);
			float magnitude = vector2.magnitude;
			if (magnitude < 0.0001f)
			{
				return Vector3.zero;
			}
			float num = 0.5f * rho * sqrMagnitude * Acc.AP_wingArea(part) * Acc.AP_wingEff(part);
			Vector3 rhs2 = -vector2 / magnitude * (CL * num) - vFlow / Mathf.Sqrt(sqrMagnitude) * (CD * num);
			return Effector.ToAero(ctx.rootInv, Vector3.Cross(com + quaternion * col - ctx.cg, rhs2));
		}

		public float PitchRawAt(float c)
		{
			if (pitchRaw == null || N < 2)
			{
				return 0f;
			}
			float num = (c - min) / base.Step;
			if (num <= 0f)
			{
				return pitchRaw[0];
			}
			if (num >= (float)(N - 1))
			{
				return pitchRaw[N - 1];
			}
			int num2 = (int)num;
			float num3 = num - (float)num2;
			return pitchRaw[num2] + (pitchRaw[num2 + 1] - pitchRaw[num2]) * num3;
		}

		public static int RegOrigSurf = 0;

		public float rangeUp;

		public float rangeDown;

		public bool asymRange;

		private float gridSpan;

		public float Span => Mathf.Max(max, 0f - min);

		public float LimitFor(float c)
		{
			return (c >= 0f) ? max : (0f - min);
		}

		private void ApplyAsymRange()
		{
			if (teUp > 0f)
			{
				max = rangeUp;
				min = 0f - rangeDown;
			}
			else
			{
				max = rangeDown;
				min = 0f - rangeUp;
			}
			for (int i = 0; i < N; i++)
			{
				grid[i] = min + (float)i * (max - min) / (float)(N - 1);
			}
			asymRange = false;
			Plugin.Log.LogInfo($"F-22E FCS: {name} travel {min:0}/+{max:0} deg (trailing edge up is {(teUp > 0f ? "+" : "-")}).");
		}

		public float LocalAoAAt(float delta)
		{
			Vector3 vector = Quaternion.Inverse(visRot * Quaternion.AngleAxis(delta - deltaVis, Vector3.right) * lRel) * v;
			return (0f - Mathf.Atan2(vector.y, vector.z)) * 57.29578f;
		}
	}
}
