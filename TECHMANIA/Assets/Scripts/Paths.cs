using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

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

    // Removes characters that aren't allowed in file names.
    public static string FilterString(string input)
    {
        StringBuilder builder = new StringBuilder();
        const string invalidChars = "\\/*:?\"<>|";
        foreach (char c in input)
        {
            if (!invalidChars.Contains(c.ToString()))
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    public const string kTrackFilename = "track.tech";
}
