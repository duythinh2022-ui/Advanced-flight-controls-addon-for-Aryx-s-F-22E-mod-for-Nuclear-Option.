using System.Collections.Generic;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal sealed class TvcEffector : Effector
	{
		public readonly List<Turbofan> engines = new List<Turbofan>();

		public float totalThrust;

		public float speedFade = 1f;

		public static float CutoffFadeBand = 0.05f;

		public float NozzleLimit => max;

		public TvcEffector(IEnumerable<Turbofan> fans)
		{
			name = "TVC";
			engines.AddRange(fans);
			float num = 10f;
			foreach (Turbofan engine in engines)
			{
				num = Mathf.Max(0.1f, Mathf.Abs(Acc.TF_vec(engine).x));
			}
			min = 0f - num;
			max = num;
			lo = min;
			hi = max;
			regNorm = 10f;
			mu = 5E-05f;
			lam = 2E-05f;
			AllocGrid(11);
			for (int i = 0; i < N; i++)
			{
				grid[i] = min + (float)i * (max - min) / (float)(N - 1);
			}
		}

		public override void Evaluate(ref EvalContext ctx)
		{
			active = false;
			for (int i = 0; i < N; i++)
			{
				M[i] = Vector3.zero;
			}
			Mnow = Vector3.zero;
			totalThrust = 0f;
			bool flag = false;
			float num = 0f;
			int num2 = 0;
			speedFade = 1f;
			foreach (Turbofan engine in engines)
			{
				if (engine == null || !Acc.TF_operable(engine))
				{
					continue;
				}
				Transform[] array = Acc.TF_vecT(engine);
				if (array == null || array.Length == 0)
				{
					continue;
				}
				Aircraft aircraft = Acc.TF_aircraft(engine);
				if ((Object)(object)aircraft != null && aircraft.speed > Acc.TF_vecMaxSpeed(engine))
				{
					continue;
				}
				if ((Object)(object)aircraft != null && CutoffFadeBand > 0f)
				{
					float vmax = Acc.TF_vecMaxSpeed(engine);
					speedFade = Mathf.Min(speedFade, Mathf.Clamp01((vmax - aircraft.speed) / Mathf.Max(1f, CutoffFadeBand * vmax)));
				}
				Transform transform = array[0];
				if (transform == null)
				{
					continue;
				}
				Quaternion quaternion = ((transform.parent != null) ? transform.parent.rotation : Quaternion.identity);
				Vector3 vector = Acc.TF_nozzleAngles(engine);
				float y = vector.y;
				num += vector.x;
				num2++;
				JetNozzle[] array2 = Acc.TF_nozzles(engine);
				if (array2 == null)
				{
					continue;
				}
				JetNozzle[] array3 = array2;
				foreach (JetNozzle jetNozzle in array3)
				{
					Transform transform2 = ((jetNozzle != null) ? Acc.JN_thrustT(jetNozzle) : null);
					if (transform2 == null)
					{
						continue;
					}
					float num3 = Acc.JN_totalThrust(jetNozzle);
					if (num3 <= 1f)
					{
						continue;
					}
					totalThrust += num3;
					Quaternion rotation = transform.rotation;
					Quaternion quaternion2 = Quaternion.Inverse(rotation) * transform2.rotation;
					Vector3 vector2 = Quaternion.Inverse(rotation) * (transform2.position - transform.position);
					for (int k = 0; k <= N; k++)
					{
						float value = ((k < N) ? Mathf.Clamp(grid[k], min * speedFade, max * speedFade) : vector.x);
						Quaternion quaternion3 = quaternion * Quaternion.Euler(Mathf.Clamp(value, -20f, 20f), y, 0f);
						Vector3 vector3 = quaternion3 * quaternion2 * Vector3.forward;
						Vector3 vector4 = transform.position + quaternion3 * vector2;
						Vector3 vector5 = Effector.ToAero(ctx.rootInv, Vector3.Cross(vector4 - ctx.cg, vector3 * num3));
						if (k < N)
						{
							M[k] += vector5;
						}
						else
						{
							Mnow += vector5;
						}
					}
					flag = true;
				}
			}
			now = ((num2 > 0) ? (num / (float)num2) : 0f);
			rate = Plugin.TvcSlew;
			active = flag;
			lo = min * speedFade;
			hi = max * speedFade;
			if (!active)
			{
				cmd = 0f;
			}
		}
	}
}
