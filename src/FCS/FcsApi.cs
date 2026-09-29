using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	public static class FcsApi
	{
		public const int Version = 2; // 2 (1.9.1): code 7 = high-AoA pitch-rate command, v[3] = commanded pitch rate

		public static bool GetPitchCommand(Aircraft aircraft, float[] v)
		{
			if (v == null || v.Length < 10)
			{
				return false;
			}
			FcsController fcsController = Registry.Find(aircraft);
			if (fcsController == null || fcsController.Failed || !fcsController.Engaged)
			{
				return false;
			}
			v[0] = fcsController.Mode switch
			{
				FcsMode.Ground => 1f, 
				FcsMode.GearDownRate => 2f, 
				FcsMode.AoAG => (Mathf.Abs(fcsController.sp) < 0.04f && !fcsController.prot) ? 6f : ((fcsController.hiRate && fcsController.rateW > 0.5f) ? 7f : ((fcsController.betaG < 0.5f) ? 3f : 4f)), 
				FcsMode.MPO => 5f, 
				FcsMode.AIRate => 2f, 
				_ => 0f, 
			};
			v[1] = fcsController.alphaCmd;
			v[2] = fcsController.nCmd;
			v[3] = fcsController.qT;
			v[4] = fcsController.alpha;
			v[5] = fcsController.nz;
			v[6] = (fcsController.prot ? 1f : 0f);
			v[7] = (fcsController.unload ? 1f : 0f);
			v[8] = Plugin.AoAPos;
			v[9] = ((fcsController.Mode == FcsMode.MPO) ? Plugin.MpoGPos : Plugin.GPos);
			return true;
		}
	}
}
