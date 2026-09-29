using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal abstract class Effector
	{
		public string name;

		public bool active;

		public float min;

		public float max;

		public float now;

		public float cmd;

		public float cmdOut;

		public float cmdLag;

		public RateTraj outTraj;

		public float rate;

		public float mu = 0.0001f;

		public float lam = 2E-05f;

		public float lo;

		public float hi;

		public Effector partner;

		public float pairSign = 1f;

		public float symCost;

		public float regNorm;

		public float RegN => (regNorm > 0f && Allocator.RegOrigRange) ? regNorm : Mathf.Max(1f, max);

		public float antiCost;

		// 1.8.32 nozzle budget: beyond +-band of anchor, each degree costs anchorK x (deg/RegN)^2 on top of mu/lam.
		public float anchor;

		public float band;

		public float anchorK;

		public int N;

		public float[] grid;

		public Vector3[] M;

		public Vector3 Mnow;

		// 1.8.41: roll-fight group for the Allocator: 1 = stabilators, 2 = outboard flaperons, 3 = inboard flaperons, 0 = none
		public int rollGroup;

		// 1.8.42: pitch-fight group: 1 = stabilators, 2 = TVC, 0 = none
		public int pitchGroup;

		public float Step => (max - min) / (float)(N - 1);

		protected void AllocGrid(int n)
		{
			N = n;
			grid = new float[n];
			M = new Vector3[n];
		}

		public Vector3 MAt(float c)
		{
			if (N < 2)
			{
				return Vector3.zero;
			}
			float num = (c - min) / Step;
			if (num <= 0f)
			{
				return M[0];
			}
			if (num >= (float)(N - 1))
			{
				return M[N - 1];
			}
			int num2 = (int)num;
			float num3 = num - (float)num2;
			return M[num2] + (M[num2 + 1] - M[num2]) * num3;
		}

		public abstract void Evaluate(ref EvalContext ctx);

		protected static Vector3 ToAero(Quaternion rootInv, Vector3 worldMoment)
		{
			Vector3 vector = rootInv * worldMoment;
			return new Vector3(0f - vector.z, 0f - vector.x, vector.y);
		}
	}
}
