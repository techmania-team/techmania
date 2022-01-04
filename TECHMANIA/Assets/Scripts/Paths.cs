using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.Events;
using System;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public static class Paths
{
    public const string kTrackFilename = "track.tech";
    public const string kSkinFilename = "skin.json";
    public const string kSubfolderEyecatchPngFilename = 
        "eyecatch.png";
    public const string kSubfolderEyecatchJpgFilename = 
        "eyecatch.jpg";
    public const string kTrackFolderName = "Tracks";
    public const string kSkinFolderName = "Skins";
    public const string kNoteSkinFolderName = "Note";
    public const string kVfxSkinFolderName = "VFX";
    public const string kComboSkinFolderName = "Combo";
    public const string kGameUiFolderName = "Game UI";

    #region Important folders
    // "Streaming" refers to streaming assets. On PC this brings no
    // benefit, but on mobile, anything in the streaming assets gets
    // copied to a system location during app installation, so we take
    // advantage of this to install official tracks and skins without
    // requiring the user to copy files.
    //
    // Both tracks and skins read from 2 locations, the folder in
    // working directory and the streaming folder. SelectTrackPanel
    // displays contents of both directories, effectively merging
    // them into one. Skins are a bit problematic since they load
    // on startup and we don't store paths in options, so we add
    // special logic to try both the working directory and
    // streaming folder.

    private static string streamingAssetsFolder;
    private static string workingDirectory;
    private static string trackRootFolder;
    private static string streamingTrackRootFolder;
    private static string skinFolder;
    private static string streamingSkinFolder;
    private static string dataFolder;

    public static void PrepareFolders()
    {
        // Streaming assets folder
        streamingAssetsFolder = Application.streamingAssetsPath;
#if UNITY_WSA || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // Application.streamingAssetsPath gives '/' on Windows, which
        // causes problems when comparing paths.
        streamingAssetsFolder = streamingAssetsFolder.Replace(
            '/', '\\');
#endif

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
        trackRootFolder = Path.Combine(workingDirectory,
            kTrackFolderName);
        Directory.CreateDirectory(trackRootFolder);
        streamingTrackRootFolder = Path.Combine(
            streamingAssetsFolder, kTrackFolderName);

        // Skin folder
        skinFolder = Path.Combine(workingDirectory, 
            kSkinFolderName);
        Directory.CreateDirectory(skinFolder);
        streamingSkinFolder = Path.Combine(
            streamingAssetsFolder, kSkinFolderName);

        Directory.CreateDirectory(GetNoteSkinRootFolder());
        Directory.CreateDirectory(GetVfxSkinRootFolder());
        Directory.CreateDirectory(GetComboSkinRootFolder());
        Directory.CreateDirectory(GetGameUiSkinRootFolder());

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

    public static void ApplyCustomDataLocation()
    {
        if (Options.instance.customDataLocation)
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            }
#endif
            trackRootFolder = Options.instance.tracksFolderLocation;
            skinFolder = Options.instance.skinsFolderLocation;
        }
        else
        {
            trackRootFolder = Path.Combine(workingDirectory,
                kTrackFolderName);
            skinFolder = Path.Combine(workingDirectory,
                kSkinFolderName);
        }
    }
    #endregion

    #region Things in working directory
    public static string GetTrackRootFolder()
    {
        return trackRootFolder;
    }

    public static string GetStreamingTrackRootFolder()
    {
        return streamingTrackRootFolder;
    }

    public static string GetSkinFolder()
    {
        return skinFolder;
    }

    public static string GetStreamingSkinFolder()
    {
        return streamingSkinFolder;
    }

    public static string GetNoteSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), kNoteSkinFolderName);
    }
    public static string GetStreamingNoteSkinRootFolder()
    {
        return Path.Combine(GetStreamingSkinFolder(), 
            kNoteSkinFolderName);
    }
    public static string GetNoteSkinFolder(string name)
    {
        // If there's a name collision between a skin in the
        // working directory and streaming assets, prioritize the
        // former. This is the same behavior as SelectSkinPanel.
        string temp = Path.Combine(GetNoteSkinRootFolder(), name);
        return Directory.Exists(temp) ?
            temp :
            Path.Combine(GetStreamingNoteSkinRootFolder(), name);
    }

    public static string GetVfxSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), kVfxSkinFolderName);
    }
    public static string GetStreamingVfxSkinRootFolder()
    {
        return Path.Combine(GetStreamingSkinFolder(), 
            kVfxSkinFolderName);
    }
    public static string GetVfxSkinFolder(string name)
    {
        string temp = Path.Combine(GetVfxSkinRootFolder(), name);
        return Directory.Exists(temp) ?
            temp :
            Path.Combine(GetStreamingVfxSkinRootFolder(), name);
    }

    public static string GetComboSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), kComboSkinFolderName);
    }
    public static string GetStreamingComboSkinRootFolder()
    {
        return Path.Combine(GetStreamingSkinFolder(), 
            kComboSkinFolderName);
    }
    public static string GetComboSkinFolder(string name)
    {
        string temp = Path.Combine(GetComboSkinRootFolder(), name);
        return Directory.Exists(temp) ?
            temp :
            Path.Combine(GetStreamingComboSkinRootFolder(), name);
    }

    public static string GetGameUiSkinRootFolder()
    {
        return Path.Combine(GetSkinFolder(), kGameUiFolderName);
    }
    public static string GetStreamingGameUiSkinRootFolder()
    {
        return Path.Combine(GetStreamingSkinFolder(), 
            kGameUiFolderName);
    }
    public static string GetGameUiSkinFolder(string name)
    {
        string temp = Path.Combine(GetGameUiSkinRootFolder(), name);
        return Directory.Exists(temp) ? 
            temp :
            Path.Combine(GetStreamingGameUiSkinRootFolder(), name);
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

    #region Listing files of specific type
    private static List<string> GetAllMatchingFiles(string folder, 
        List<string> patterns)
    {
        List<string> files = new List<string>();
        foreach (string pattern in patterns)
        {
            foreach (string file in
                Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories))
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
    #endregion

    #region Path manipulation
    public static string RemoveCharsNotAllowedOnFileSystem(
        string input)
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
#if UNITY_ANDROID
        // Streaming assets on Android are not files, so they are inaccessible with "file://".
        if (fullPath.Contains(Application.streamingAssetsPath))
        {
            return fullPath;
        }
        else
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
#else
        return "file://" + fullPath.Replace("#", "%23")
            .Replace("$", "%24")
            .Replace("&", "%26")
            .Replace("+", "%2b")
            .Replace(",", "%2c")
            .Replace(";", "%3b")
            .Replace("=", "%3d")
            .Replace("?", "%3f")
            .Replace("@", "%40");
#endif
    }

    public static string HidePlatformInternalPath(string fullPath)
    {
#if UNITY_ANDROID || UNITY_IOS
        return fullPath
            .Replace(Paths.GetStreamingTrackRootFolder(), "Tracks")
            .Replace(Paths.GetTrackRootFolder(), "Tracks")
            .Replace(Paths.GetStreamingSkinFolder(), "Skins")
            .Replace(Paths.GetSkinFolder(), "Skins")
            .Replace(dataFolder, "TECHMANIA");
#else
        return fullPath;
#endif
    }
    #endregion

    #region Streaming assets
    public static bool IsInStreamingAssets(string path)
    {
        return path.StartsWith(streamingAssetsFolder);
    }

    public static string RelativePathInStreamingAssets(string 
        absolutePath)
    {
        return absolutePath.Substring(streamingAssetsFolder.Length);
    }

    public static string AbsolutePathInStreamingAssets(string
        relativePath)
    {
        // If an argument other than the first contains a rooted path,
        // any previous path components are ignored, and the
        // returned string begins with that rooted path component.
        relativePath = relativePath.TrimStart('/', '\\');
        string absolutePath = Path.Combine(streamingAssetsFolder,
            relativePath);
#if UNITY_WSA || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        absolutePath = absolutePath.Replace('/', '\\');
#endif
        return absolutePath;
    }
    #endregion
}
