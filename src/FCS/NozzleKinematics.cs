using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	// 1.8.40 nozzle flap kinematics (pure math, upper-flap frame: y out, z forward, metres, degrees; the lower flap is the
	// mirror image). Pieces: root (blue, fixed), cowl skin (green, the flap's outer skin panel only), flap (purple, the end
	// panel plus the flap's inner wall back from the throat).
	//   Area: MIL = flap + skin rotated 7.7 deg in about the skin's front edge (skin flush); idle = flap moved out 4 cm, skin
	//   ramps from the root's edge up onto it; AB = idle + 1.5 deg flare about the throat.
	//   Vectoring, both directions: the flap rotates about the throat (its inner surface at the root joint).
	//     outward: the whole flap, root included, is pulled in 0.47 m * tan(o/2), forward and 8 deg outward, and the skin
	//     rotates with the flap - its front edge swings forward over the root's top, tilted up slightly about its back
	//     edge (front +0.7 cm at 20 deg) so it rides over the flush root instead of cutting into it.
	//     inward: the skin floats - centred between the root's edge and the flap's front, at the area angle plus half the
	//     flap's rotation (two even gaps).
	internal static class NozzleKinematics
	{
		public const float ERest = 0.45f;

		private const float MilAngle = -7.7f;
		private const float IdleOut = 0.04f;
		private const float AbFlare = 1.5f;
		// outward swing: the whole flap, root included, is pulled in by HingeSpan * tan(o/2) (8.3 cm at 20 deg) - the offset
		// that keeps the exit width across the turned flow (mitred bend). It slides forward and 8 deg outward, close to the
		// sidewall's top edge (~10 deg next to the root) so the flush root stays within ~0.25 cm of the wall; steeper puts the
		// root's front side corner through the top of Aryx's thin shroud lip. Past ~2 cm the root and the skin's front edge
		// overlap the inside of the shroud (hidden: the root corner stays >= 0.5 cm under the shroud's outer surface).
		private const float HingeSpan = 0.47f;    // distance between the two flaps' throat pivots
		private const float PullDirY = 0.1392f;   // sin 8 deg
		private const float PullDirZ = 0.9903f;   // cos 8 deg
		private const float SkinLift = -16f;      // deg of skin tilt about its back edge per metre of pull (front up ~0.7 cm at 20 deg)

		// skin front / back outer corners at rest, throat pivot, skin front hinge (y, z)
		private static readonly Vector2 AF = new Vector2(0.500f, -0.23f);
		private static readonly Vector2 AB = new Vector2(0.435f, -0.51f);
		private static readonly Vector2 Throat = new Vector2(0.235f, -0.158f);
		private static readonly Vector2 H1 = new Vector2(0.500f, -0.23f);

		// 2D rigid transform in the (y, z) plane: rotate by a (deg, + = aft end outward) then translate
		public struct T2
		{
			public float a;
			public Vector2 t;

			public Vector2 Ap(Vector2 p)
			{
				float r = a * (Mathf.PI / 180f);
				float c = Mathf.Cos(r), s = Mathf.Sin(r);
				return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y) + t;
			}

			public static T2 operator *(T2 l, T2 r)
			{
				T2 o;
				o.a = l.a + r.a;
				o.t = l.Ap(r.t);
				return o;
			}

			public static T2 Id => new T2 { a = 0f, t = Vector2.zero };

			public static T2 Tr(float y, float z) => new T2 { a = 0f, t = new Vector2(y, z) };

			public static T2 RotAbout(Vector2 p, float deg)
			{
				T2 r = new T2 { a = deg, t = Vector2.zero };
				r.t = p - r.Ap(p);
				return r;
			}
		}

		private static float Ang(Vector2 v) => Mathf.Atan2(v.x, -v.y) * (180f / Mathf.PI);

		private static T2 Skin(Vector2 b, Vector2 p, float th)
		{
			T2 q = new T2 { a = th, t = Vector2.zero };
			Vector2 m0 = 0.5f * (AF + AB);
			Vector2 m1 = 0.5f * (b + p);
			q.t = m1 - q.Ap(m0);
			return q;
		}

		public static T2 Area(float e)
		{
			if (e <= ERest)
			{
				float u = Mathf.Clamp01(e / ERest);
				return T2.Tr(IdleOut * u, 0f) * T2.RotAbout(H1, MilAngle * (1f - u));
			}
			float w = Mathf.Clamp01((e - ERest) / (1f - ERest));
			return T2.Tr(IdleOut, 0f) * T2.RotAbout(Throat, AbFlare * w);
		}

		// o = outward swing of this flap (deg); returns root, skin, flap transforms in the upper-flap frame
		public static void Flap(float e, float o, out T2 root, out T2 skin, out T2 flap)
		{
			T2 A = Area(e);
			Vector2 pA = A.Ap(AB);
			float thA = Ang(pA - AF) - Ang(AB - AF);
			T2 skinA = Skin(AF, pA, thA);
			T2 r = T2.RotAbout(Throat, o);
			float pp = o > 0f ? HingeSpan * (float)System.Math.Tan(0.5 * o * System.Math.PI / 180.0) : 0f;
			T2 pull = T2.Tr(pp * PullDirY, pp * PullDirZ);
			root = pull;
			flap = pull * r * A;
			if (o >= 0f)
			{
				skin = pull * r * skinA * T2.RotAbout(AB, SkinLift * pp);
			}
			else
			{
				skin = Skin(AF, flap.Ap(AB), thA + 0.5f * o);
			}
		}
	}
}
