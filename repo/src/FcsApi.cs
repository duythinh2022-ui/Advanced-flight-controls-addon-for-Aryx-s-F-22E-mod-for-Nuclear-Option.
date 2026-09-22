// Small public read-only API so other plugins (e.g. the flight data readout) can show what the F-22E FCS is
// commanding without a compile-time dependency: they look this type up by name and call it through reflection.
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
    public static class FcsApi
    {
        public const int Version = 1;

        /// <summary>
        /// Fills v (length >= 10) and returns true when the FCS is flying this aircraft.
        /// v[0] pitch law: 0 off, 1 ground (direct), 2 gear-down rate command, 3 AoA command, 4 G command, 5 MPO rate command,
        ///      6 stick neutral: pitch-rate (attitude) hold
        /// v[1] commanded AoA (deg)   v[2] commanded load factor (g)   v[3] commanded pitch rate (deg/s, after limiters)
        /// v[4] AoA (deg)             v[5] load factor (g)              v[6] AoA protection active (0/1)
        /// v[7] unloading (0/1)       v[8] AoA limit (deg)              v[9] positive g limit
        /// </summary>
        public static bool GetPitchCommand(Aircraft aircraft, float[] v)
        {
            if (v == null || v.Length < 10) return false;
            FcsController c = Registry.Find(aircraft);
            if (c == null || c.Failed || !c.Engaged) return false;
            float law;
            switch (c.Mode)
            {
                case FcsMode.Ground: law = 1f; break;
                case FcsMode.GearDownRate: law = 2f; break;
                case FcsMode.AoAG: law = Mathf.Abs(c.sp) < 0.04f && !c.prot ? 6f : (c.betaG < 0.5f ? 3f : 4f); break;
                case FcsMode.MPO: law = 5f; break;
                default: law = 0f; break;
            }
            v[0] = law; v[1] = c.alphaCmd; v[2] = c.nCmd; v[3] = c.qT;
            v[4] = c.alpha; v[5] = c.nz; v[6] = c.prot ? 1f : 0f; v[7] = c.unload ? 1f : 0f;
            v[8] = Plugin.AoAPos; v[9] = c.Mode == FcsMode.MPO ? Plugin.MpoGPos : Plugin.GPos;
            return true;
        }
    }
}
