using System.Collections.Generic;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	// 1.8.38 (buffed build): drag relief applied as a force opposing each AeroPart's own drag, computed with the
	// game's exact per-part drag model (AeroJob_Math, decompiled 2026-09-24):
	//   parasitic  = 0.25 * rho * v^2 * (dragArea + (dragArea + 0.1 * wingArea) * (1 - eff))
	//   airfoil    = CD(alpha) * 0.5 * rho * v^2 * wingArea * eff
	//   total      = (parasitic + airfoil) * (1 + 0.15 * k^3), k = (0.2 - min(|1 - M|, 0.2)) / 0.2 for 0.8 < M < 1.2
	//   a = max(340 - 0.005 * y, 290) m/s
	// Wanted: (parasitic + airfoil - rA * (airfoil - airfoil at its minimum CD)) x transonic x (1 - rM(M)); the difference is pushed back along the part's airflow,
	// at the same point the game applies the part's aero force (rigidbody COM + rotated centre of lift).
	internal static class DragReliefMath
	{
		public static float SpeedOfSound(float y)
		{
			return Mathf.Max(340f - 0.005f * y, 290f);
		}

		public static float Transonic(float mach)
		{
			if (mach <= 0.8f || mach >= 1.2f)
			{
				return 1f;
			}
			float k = (0.2f - Mathf.Min(Mathf.Abs(1f - mach), 0.2f)) / 0.2f;
			return 1f + 0.15f * k * k * k;
		}

		private static float Smooth(float x)
		{
			x = Mathf.Clamp01(x);
			return x * x * (3f - 2f * x);
		}

		// Mach relief r(M) is built so the slope of drag vs Mach is the game's slope times g(M), with g going smoothly
		// from 1 (below Mach 0.2) to 1 - 1.1 * amount (above 0.7) and never back up: no dip below and no hump above the
		// game's curve. For drag ~ M^2 that means r(M) = 1 - (2 / M^2) * integral_0^M g(m) m dm.
		private const float MA = 0.2f;

		private const float MB = 0.7f;

		private const float MStep = 0.01f;

		private static float[] machTable;

		private static float machTableAmount = -1f;

		public static float Mach(float mach, float amount)
		{
			if (amount <= 0f || mach <= MA)
			{
				return 0f;
			}
			if (machTable == null || machTableAmount != amount)
			{
				BuildMachTable(amount);
			}
			float gEnd = 1f - 1.1f * amount;
			int n = machTable.Length;
			float top = (n - 1) * MStep;
			if (mach >= top)
			{
				float iTop = (1f - machTable[n - 1]) * top * top * 0.5f;
				return 1f - 2f / (mach * mach) * (iTop + gEnd * (mach * mach - top * top) * 0.5f);
			}
			float f = mach / MStep;
			int i = (int)f;
			return Mathf.Lerp(machTable[i], machTable[i + 1], f - i);
		}

		private static void BuildMachTable(float amount)
		{
			float gEnd = 1f - 1.1f * amount;
			int n = 301;
			float[] t = new float[n];
			double integ = 0.0;
			const int sub = 20;
			for (int i = 1; i < n; i++)
			{
				double m0 = (i - 1) * MStep;
				double h = MStep / sub;
				for (int k = 0; k < sub; k++)
				{
					double m = m0 + (k + 0.5) * h;
					double g = 1.0 - (1.0 - gEnd) * Smooth((float)((m - MA) / (MB - MA)));
					integ += g * m * h;
				}
				double M = i * MStep;
				t[i] = (float)(1.0 - 2.0 * integ / (M * M));
			}
			t[0] = 0f;
			machTable = t;
			machTableAmount = amount;
		}

		// drag force magnitude to give back for one part (>= 0). The AoA relief removes a fraction of the part's airfoil drag
		// above that airfoil's minimum, i.e. a pure scaling of the drag that grows with AoA - no hand-over band, so the
		// AoA drag curve keeps the game's shape (slope x (1 - amount)) at every AoA.
		public static float Relief(float parasitic, float airfoil, float airfoilMin, float mach)
		{
			float t = Transonic(mach);
			float game = (parasitic + airfoil) * t;
			float foilExcess = Mathf.Max(0f, airfoil - airfoilMin);
			float want = (parasitic + airfoil - Plugin.DragReliefAoA * foilExcess) * t * (1f - Mach(mach, Plugin.DragReliefTransonic));
			return Mathf.Max(0f, game - want);
		}
	}

	internal class DragRelief : MonoBehaviour
	{
		private Aircraft ac;

		private readonly List<AeroPart> parts = new List<AeroPart>();

		private readonly List<AirfoilTable> tables = new List<AirfoilTable>();

		private readonly List<float> cdMins = new List<float>();

		public static void Attach(Aircraft a)
		{
			if (a == null || (Plugin.DragReliefAoA <= 0f && Plugin.DragReliefTransonic <= 0f))
			{
				return;
			}
			GameObject go = ((Component)(object)a).gameObject;
			if (go.GetComponent<DragRelief>() != null)
			{
				return;
			}
			DragRelief r = go.AddComponent<DragRelief>();
			r.ac = a;
			AircraftParameters prm = a.GetAircraftParameters();
			foreach (UnitPart p in a.partLookup)
			{
				if (p is AeroPart ap)
				{
					Airfoil af = null;
					int idx = Acc.AP_airfoil(ap);
					if (idx >= 0 && prm != null && prm.airfoils != null && idx < prm.airfoils.Length)
					{
						af = prm.airfoils[idx];
					}
					r.parts.Add(ap);
					AirfoilTable tbl = new AirfoilTable(af);
					r.tables.Add(tbl);
					float mn = float.MaxValue;
					for (float aa = -0.35f; aa <= 0.35f; aa += 0.005f)
					{
						tbl.Get(aa, out _, out var cdv);
						mn = Mathf.Min(mn, cdv);
					}
					r.cdMins.Add(mn);
				}
			}
			Plugin.Log.LogInfo($"F-22E drag relief on {((UnityEngine.Object)(object)a).name}: {r.parts.Count} parts, AoA-dependent drag -{Plugin.DragReliefAoA * 100f:F0}%, Mach relief {Plugin.DragReliefTransonic * 100f:F0}%.");
		}

		private void FixedUpdate()
		{
			if ((UnityEngine.Object)(object)ac == null)
			{
				Destroy(this);
				return;
			}
			if (!ac.LocalSim || (Plugin.DragReliefAoA <= 0f && Plugin.DragReliefTransonic <= 0f))
			{
				return;
			}
			float rho = ac.airDensity;
			if (rho <= 0f)
			{
				return;
			}
			Vector3 wind = Vector3.zero;
			try
			{
				wind = ac.GetWindVelocity();
			}
			catch
			{
			}
			for (int i = 0; i < parts.Count; i++)
			{
				AeroPart ap = parts[i];
				if (ap == null || ap.rb == null || ap.IsDetached())
				{
					continue;
				}
				Transform lt = Acc.AP_liftNormal(ap);
				if (lt == null)
				{
					continue;
				}
				Rigidbody rb = ap.rb;
				Vector3 v = rb.velocity - wind;
				float v2 = v.sqrMagnitude;
				if (v2 < 25f)
				{
					continue;
				}
				Quaternion rot = lt.rotation;
				Vector3 vl = Quaternion.Inverse(rot) * v;
				float a = Mathf.Atan2(vl.y, vl.z);
				tables[i].Get(a, out _, out var cd);
				float cdMin = cdMins[i];
				float S = Acc.AP_wingArea(ap);
				float eff = Acc.AP_wingEff(ap);
				float dA = Acc.AP_dragArea(ap);
				float q = 0.5f * rho * v2;
				float par = 0.5f * q * (dA + (dA + 0.1f * S) * (1f - eff));
				float foil = cd * q * S * eff;
				float foilMin = Mathf.Min(cdMin, cd) * q * S * eff;
				Vector3 com = rb.worldCenterOfMass;
				float vm = Mathf.Sqrt(v2);
				float mach = vm / DragReliefMath.SpeedOfSound(com.y);
				float dF = DragReliefMath.Relief(par, foil, foilMin, mach);
				if (dF > 0f)
				{
					rb.AddForceAtPosition(v * (dF / vm), com + rot * Acc.AP_centerOfLift(ap));
				}
			}
		}
	}
}
