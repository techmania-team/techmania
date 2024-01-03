using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DebugWindow : MonoBehaviour
{
    public bool display;

    private Rect debugOverlayRect = new Rect(10, 10, 300, 200);
    private float lastDebugUpdate = 0f;
    private string lastDebugText = "";

    private void OnGUI()
    {
        if (!display) return;

        // Copied from FMODUnity.RuntimeManager.DrawDebugOverlay
        debugOverlayRect = GUI.Window(0, debugOverlayRect,
        (int windowID) =>
        {
            if (Time.unscaledTime - lastDebugUpdate >= 0.25f)
            {
                StringBuilder debug = new StringBuilder();

                // FMOD stats
                int fmodCurrentMemoryBytes;
                int fmodTotalMemoryBytes;
                int totalSounds;
                int fmodRealChannels;
                int fmodTotalChannels;
                FmodManager.instance.GetStats(
                    out fmodCurrentMemoryBytes,
                    out fmodTotalMemoryBytes,
                    out totalSounds,
                    out fmodRealChannels,
                    out fmodTotalChannels);
                debug.Append("========= FMOD =========\n");
                debug.AppendFormat("Memory usage: current {0}MB, max {1}MB\n", fmodCurrentMemoryBytes >> 20, fmodTotalMemoryBytes >> 20);
                debug.AppendFormat("Total sounds loaded: {0}\n", totalSounds);
                debug.AppendFormat("Channels: {0} real, {1} total\n", fmodRealChannels, fmodTotalChannels);

                // Asset stats
                debug.Append("\n");
                debug.Append("===== Assets from files =====\n");
                debug.AppendFormat("Textures: {0}\nSounds: {1}\nVideos: {2}\n",
                    ThemeApi.IO.numTexturesFromFile,
                    ThemeApi.IO.numSoundsFromFile,
                    ThemeApi.IO.numVideosFromFile);

                lastDebugText = debug.ToString();
                lastDebugUpdate = Time.unscaledTime;
            }

            GUI.Label(new Rect(10, 20, 290, 200), lastDebugText);
            GUI.DragWindow();

            if (GUI.Button(new Rect(10, 170, 50, 20), "Close"))
            {
                display = false;
            }
        }, "Debug information");
    }
}
