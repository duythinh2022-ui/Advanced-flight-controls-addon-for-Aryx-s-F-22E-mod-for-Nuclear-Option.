using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal struct RateTraj
	{
		public float x;

		public float v;

		public void Reset(float x0)
		{
			x = x0;
			v = 0f;
		}

		public void Step(float target, float A, float J, float dt)
		{
			Step(target, A, J, J, dt);
		}

		public void Step(float target, float A, float J, float Jend, float dt)
		{
			float num = target - x;
			if (num * v < 0f)
			{
				v = 0f;
			}
			float num2 = J * dt;
			float num4 = Jend * dt;
			float target2 = Mathf.Sign(num) * Mathf.Min(A, Mathf.Sqrt(0.25f * num4 * num4 + 2f * Jend * Mathf.Abs(num)) - 0.5f * num4);
			v = Mathf.MoveTowards(v, target2, J * dt);
			float num3 = x + v * dt;
			if ((target - num3) * num <= 0f)
			{
				num3 = target;
				v = 0f;
			}
			x = num3;
		}
	}
}
