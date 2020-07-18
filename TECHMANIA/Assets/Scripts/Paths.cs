using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class Paths
{
    public static string GetSongFolder()
    {
        string current = Directory.GetCurrentDirectory();  // Does not end with \
        // If there's a "Builds" folder, assume we are running from
        // Unity editor.
        if (Directory.Exists(current + "\\Builds"))
        {
            current += "\\Builds";
        }

        string songs = current + "\\Songs";
        if (!Directory.Exists(songs))
        {
            Directory.CreateDirectory(songs);
        }
        return songs;
    }
}
