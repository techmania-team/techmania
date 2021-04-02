using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.Events;
using System;

public static class Paths
{
    public const string kTrackFilename = "track.tech";
    public const string kSkinFilename = "skin.json";

    #region Things in working directory
    private static string GetWorkingDirectory()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Android
        string current = Application.persistentDataPath;
#else
        // Does not end with separator
        string current = Directory.GetCurrentDirectory();
#endif

        string buildsFolder = Path.Combine(current, "Builds");
        if (Directory.Exists(buildsFolder))
        {
            return buildsFolder;
        }

        return current;
    }

    public static string GetTrackFolder()
    {
        string tracks = Path.Combine(GetWorkingDirectory(), "Tracks");
        Directory.CreateDirectory(tracks);  // No error if already exists
        return tracks;
    }

    public static string GetSkinFolder()
    {
        return Path.Combine(GetWorkingDirectory(), "Skins");
    }

    public static string GetNoteSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), "Note");
    }

    public static string GetNoteSkinFolder(string name)
    {
        return Path.Combine(GetNoteSkinRootFolder(), name);
    }

    public static string GetVfxSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), "VFX");
    }

    public static string GetVfxSkinFolder(string name)
    {
        return Path.Combine(GetVfxSkinRootFolder(), name);
    }

    public static string GetComboSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), "Combo");
    }

    public static string GetComboSkinFolder(string name)
    {
        return Path.Combine(GetComboSkinRootFolder(), name);
    }
    #endregion

    #region Things in document folder
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
        Directory.CreateDirectory(folder);
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
    #endregion

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
}
