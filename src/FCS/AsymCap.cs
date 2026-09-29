using System;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal static class AsymCap
	{
		public static float Find(float[] alphas, float[] u, int n, int side, float limit, float floor, float uMax)
		{
			if (n < 2)
			{
				return limit;
			}
			float num = Math.Abs(limit);
			float val = Math.Min(Math.Abs(floor), num);
			float a = 0f;
			float u2 = Interp(alphas, u, n, 0f);
			if (side > 0)
			{
				for (int i = 0; i < n; i++)
				{
					float num2 = alphas[i];
					if (!(num2 <= 0f))
					{
						if (num2 > num)
						{
							num2 = num;
						}
						float num3 = ((num2 == alphas[i]) ? u[i] : Interp(alphas, u, n, num2));
						if (num3 > uMax)
						{
							return Math.Max(val, Cross(a, u2, num2, num3, uMax));
						}
						a = num2;
						u2 = num3;
						if (num2 >= num)
						{
							break;
						}
					}
				}
				return num;
			}
			for (int num4 = n - 1; num4 >= 0; num4--)
			{
				float num5 = 0f - alphas[num4];
				if (!(num5 <= 0f))
				{
					if (num5 > num)
					{
						num5 = num;
					}
					float num6 = ((num5 == 0f - alphas[num4]) ? u[num4] : Interp(alphas, u, n, 0f - num5));
					if (num6 > uMax)
					{
						return 0f - Math.Max(val, Cross(a, u2, num5, num6, uMax));
					}
					a = num5;
					u2 = num6;
					if (num5 >= num)
					{
						break;
					}
				}
			}
			return 0f - num;
		}

		private static float Cross(float a0, float u0, float a1, float u1, float thr)
		{
			if (u0 >= thr)
			{
				return a0;
			}
			float val = (thr - u0) / Math.Max(1E-06f, u1 - u0);
			return a0 + (a1 - a0) * Math.Min(1f, Math.Max(0f, val));
		}

		public static float Interp(float[] alphas, float[] u, int n, float a)
		{
			if (n == 0)
			{
				return 0f;
			}
			if (a <= alphas[0])
			{
				return u[0];
			}
			if (a >= alphas[n - 1])
			{
				return u[n - 1];
			}
			for (int i = 1; i < n; i++)
			{
				if (a <= alphas[i])
				{
					float num = (a - alphas[i - 1]) / (alphas[i] - alphas[i - 1]);
					return u[i - 1] + (u[i] - u[i - 1]) * num;
				}
			}
			return u[n - 1];
		}

		public static float Slew(float eff, float target, int side, float downRate, float upRate, float dt)
		{
			float num = (((target - eff) * (float)side > 0f) ? upRate : downRate) * dt;
			if (Math.Abs(target - eff) <= num)
			{
				return target;
			}
			return eff + (float)Math.Sign(target - eff) * num;
		}
	}
}
