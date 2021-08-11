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
    public const string kSubfolderEyecatchPngFilename = "eyecatch.png";
    public const string kSubfolderEyecatchJpgFilename = "eyecatch.jpg";

    #region Important folders
    public static string workingDirectory { get; private set; }
    public static string trackRootFolder { get; private set; }
    public static string skinFolder { get; private set; }
    public static string dataFolder { get; private set; }

    public static void PrepareFolders()
    {
        // Working directory
#if UNITY_ANDROID || UNITY_IOS
        string current = Application.persistentDataPath;
#else
        // Does not end with separator
        string current = Directory.GetCurrentDirectory();
#endif
        string buildsFolder = Path.Combine(current, "Builds");
        if (Directory.Exists(buildsFolder))
        {
            workingDirectory = buildsFolder;
        }
        else
        {
            workingDirectory = current;
        }

        // Track root folder
        trackRootFolder = Path.Combine(workingDirectory, "Tracks");
        Directory.CreateDirectory(trackRootFolder);

        // Skin folder
        skinFolder = Path.Combine(workingDirectory, "Skins");
        Directory.CreateDirectory(skinFolder);

        // Data folder
#if UNITY_ANDROID || UNITY_IOS
        dataFolder = Path.Combine(
            Application.persistentDataPath,
            "TECHMANIA");
#else
        dataFolder = Path.Combine(
            System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.MyDocuments),
            "TECHMANIA");
#endif
        Directory.CreateDirectory(dataFolder);
    }
    #endregion

    #region Things in working directory
    public static string GetTrackRootFolder()
    {
        return trackRootFolder;
    }

    public static string GetSkinFolder()
    {
        return skinFolder;
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

    public static string GetGameUiSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), "Game UI");
    }

    public static string GetGameUiSkinFolder(string name)
    {
        return Path.Combine(GetGameUiSkinRootFolder(), name);
    }
    #endregion

    #region Things in document folder
    public static string GetOptionsFilePath()
    {
        return Path.Combine(dataFolder, "options.json");
    }

    public static string GetRulesetFilePath()
    {
        return Path.Combine(dataFolder, "ruleset.json");
    }

    public static string GetRecordsFilePath()
    {
        return Path.Combine(dataFolder, "records.json");
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
