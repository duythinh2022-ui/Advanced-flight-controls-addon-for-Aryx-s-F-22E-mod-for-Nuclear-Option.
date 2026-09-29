using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal sealed class AirfoilTable
	{
		private readonly float[] cl = new float[128];

		private readonly float[] cd = new float[128];

		private readonly bool generic;

		public AirfoilTable(Airfoil af)
		{
			if (af == null || af.liftCoef == null || af.dragCoef == null)
			{
				generic = true;
				return;
			}
			for (int i = 0; i < 128; i++)
			{
				float time = (float)(i - 64) * 0.04908734f;
				cl[i] = af.liftCoef.Evaluate(time);
				cd[i] = af.dragCoef.Evaluate(time);
			}
		}

		private static float Read(float[] t, float idx)
		{
			if (idx <= 0f)
			{
				return t[0];
			}
			if (idx >= 127f)
			{
				return t[127];
			}
			int num = (int)idx;
			float num2 = idx - (float)num;
			return t[num] + (t[num + 1] - t[num]) * num2;
		}

		public void Get(float a, out float CL, out float CD)
		{
			if (generic)
			{
				CL = 1.8f * Mathf.Sin(5f * a);
				CD = 1.5f * (1f - Mathf.Cos(2f * a)) + 0.02f;
			}
			else
			{
				float idx = a * 20.37185f + 64f;
				CL = Read(cl, idx);
				CD = Read(cd, idx);
			}
		}

		public float StallAoADeg()
		{
			if (generic)
			{
				return 18f;
			}
			int num = 64;
			for (int i = 64; i < 96; i++)
			{
				if (cl[i] > cl[num])
				{
					num = i;
				}
			}
			return (float)(num - 64) * 0.04908734f * 57.29578f;
		}
	}
}
