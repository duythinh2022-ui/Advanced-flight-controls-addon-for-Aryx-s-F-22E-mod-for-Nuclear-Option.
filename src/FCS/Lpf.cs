namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal struct Lpf
	{
		public float y;

		public bool init;

		public float Step(float x, float tau, float dt)
		{
			if (!init)
			{
				y = x;
				init = true;
				return y;
			}
			y += (x - y) * (dt / (tau + dt));
			return y;
		}
	}
}
