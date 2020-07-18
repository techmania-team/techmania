using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class Paths
{
    public static string GetTrackFolder()
    {
        string current = Directory.GetCurrentDirectory();  // Does not end with \
        // If there's a "Builds" folder, assume we are running from
        // Unity editor.
        if (Directory.Exists(current + "\\Builds"))
        {
            current += "\\Builds";
        }

        string tracks = current + "\\Tracks";
        if (!Directory.Exists(tracks))
        {
            Directory.CreateDirectory(tracks);
        }
        return tracks;
    }
}
