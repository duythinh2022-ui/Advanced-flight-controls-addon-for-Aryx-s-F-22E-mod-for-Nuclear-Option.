using System;
using System.IO;
using BepInEx;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal sealed class Telemetry
	{
		private StreamWriter w;

		private readonly FcsController c;

		private float lastFlush;

		public Telemetry(FcsController c)
		{
			this.c = c;
			try
			{
				string text = Path.Combine(Paths.BepInExRootPath, "F22E_FCS_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
				w = new StreamWriter(text, append: false);
				c.WriteHeader(w);
				Plugin.Log.LogInfo("F-22E FCS telemetry: " + text);
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("Telemetry disabled: " + ex.Message);
				w = null;
			}
		}

		public void Write(float t, float sp, float sr, float sy, float rho)
		{
			if (w != null)
			{
				c.WriteRow(w, t, sp, sr, sy, rho);
				if (t - lastFlush > 1f)
				{
					w.Flush();
					lastFlush = t;
				}
			}
		}

		public void Close()
		{
			try
			{
				w?.Flush();
				w?.Dispose();
			}
			catch
			{
			}
			w = null;
		}
	}
}
