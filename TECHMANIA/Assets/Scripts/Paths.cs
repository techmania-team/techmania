using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.Events;
using System;

public static class Paths
{
    public static string GetTrackFolder()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Android
        string current = Application.persistentDataPath;
#else
        // Does not end with separator
        string current = Directory.GetCurrentDirectory();
#endif
        
        // If there's a "Builds" folder, assume we are running from
        // Unity editor.
        if (Directory.Exists(Path.Combine(current, "Builds")))
        {
            current = Path.Combine(current, "Builds");
        }

        string tracks = Path.Combine(current, "Tracks");
        if (!Directory.Exists(tracks))
        {
            Directory.CreateDirectory(tracks);
        }
        return tracks;
    }

    private static string GetDataFolder()
    {

#if UNITY_ANDROID || UNITY_IOS
        // Android
        string folder = Path.Combine(
            Application.persistentDataPath,
            "TECHMANIA");
#else
        string folder = Path.Combine(
            System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.MyDocuments),
            "TECHMANIA");
#endif
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return folder;
    }

    public static string GetOptionsFilePath()
    {
        return Path.Combine(GetDataFolder(), "options.json");
    }

    public static string GetRulesetFilePath()
    {
        return Path.Combine(GetDataFolder(), "ruleset.json");
    }

    private static List<string> GetAllMatchingFiles(string folder, 
        List<string> patterns)
    {
        List<string> files = new List<string>();
        foreach (string pattern in patterns)
        {
            foreach (string file in
                Directory.EnumerateFiles(folder, pattern))
            {
                files.Add(file);
            }
        }
        return new List<string>(NumericSort.Sort(files));
    }

    public static List<string> GetAllAudioFiles(string folder)
    {
        return GetAllMatchingFiles(folder, new List<string>()
            { "*.wav", "*.ogg"}
        );
    }

    public static List<string> GetAllImageFiles(string folder)
    {
        return GetAllMatchingFiles(folder, new List<string>()
            { "*.png", "*.jpg"}
        );
    }

    public static List<string> GetAllVideoFiles(string folder)
    {
        return GetAllMatchingFiles(folder, new List<string>()
            { "*.mp4", "*.wmv"}
        );
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

    public static string FullPathToUri(string fullPath)
    {
        return "file://" + fullPath.Replace("#", "%23")
            .Replace("$", "%24")
            .Replace("&", "%26")
            .Replace("+", "%2b")
            .Replace(",", "%2c")
            .Replace(";", "%3b")
            .Replace("=", "%3d")
            .Replace("?", "%3f")
            .Replace("@", "%40");
    }

    public const string kTrackFilename = "track.tech";
}
