using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        string current = UniversalIO.DirectoryGetCurrentDirectory();
#endif
        string buildsFolder = UniversalIO.PathCombine(current, "Builds");
        if (UniversalIO.FileExists(buildsFolder))
        {
            workingDirectory = buildsFolder;
        }
        else
        {
            workingDirectory = current;
        }

        // Track root folder
        trackRootFolder = UniversalIO.PathCombine(workingDirectory,
            kTrackFolderName);
        UniversalIO.DirectoryCreateDirectory(trackRootFolder);
        streamingTrackRootFolder = UniversalIO.PathCombine(
            streamingAssetsFolder, kTrackFolderName);

        // Skin folder
        skinFolder = UniversalIO.PathCombine(workingDirectory,
            kSkinFolderName);
        UniversalIO.DirectoryCreateDirectory(skinFolder);
        streamingSkinFolder = UniversalIO.PathCombine(
            streamingAssetsFolder, kSkinFolderName);

        UniversalIO.DirectoryCreateDirectory(GetNoteSkinRootFolder());
        UniversalIO.DirectoryCreateDirectory(GetVfxSkinRootFolder());
        UniversalIO.DirectoryCreateDirectory(GetComboSkinRootFolder());
        UniversalIO.DirectoryCreateDirectory(GetGameUiSkinRootFolder());

        // Data folder
#if UNITY_ANDROID || UNITY_IOS
        dataFolder = UniversalIO.PathCombine(
            Application.persistentDataPath,
            "TECHMANIA");
#else
        dataFolder = UniversalIO.PathCombine(
            System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.MyDocuments),
            "TECHMANIA");
#endif
        UniversalIO.DirectoryCreateDirectory(dataFolder);
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
            trackRootFolder = UniversalIO.PathCombine(workingDirectory,
                kTrackFolderName);
            skinFolder = UniversalIO.PathCombine(workingDirectory,
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
        return UniversalIO.PathCombine(GetSkinFolder(), kNoteSkinFolderName);
    }
    public static string GetStreamingNoteSkinRootFolder()
    {
        return UniversalIO.PathCombine(GetStreamingSkinFolder(),
            kNoteSkinFolderName);
    }
    public static string GetNoteSkinFolder(string name)
    {
        // If there's a name collision between a skin in the
        // working directory and streaming assets, prioritize the
        // former. This is the same behavior as SelectSkinPanel.
        string temp = UniversalIO.PathCombine(GetNoteSkinRootFolder(), name);

        bool isExist = UniversalIO.FileExists(temp);
        Debug.Log("Folder: " + temp + " is " + (!isExist ? "not " : "") + "exist");
        return isExist ?
            temp :
            UniversalIO.PathCombine(GetStreamingNoteSkinRootFolder(), name);
    }

    public static string GetVfxSkinRootFolder()
    {
        return UniversalIO.PathCombine(GetSkinFolder(), kVfxSkinFolderName);
    }
    public static string GetStreamingVfxSkinRootFolder()
    {
        return UniversalIO.PathCombine(GetStreamingSkinFolder(),
            kVfxSkinFolderName);
    }
    public static string GetVfxSkinFolder(string name)
    {
        string temp = UniversalIO.PathCombine(GetVfxSkinRootFolder(), name);
        return UniversalIO.FileExists(temp) ?
            temp :
            UniversalIO.PathCombine(GetStreamingVfxSkinRootFolder(), name);
    }

    public static string GetComboSkinRootFolder()
    {
        return UniversalIO.PathCombine(GetSkinFolder(), kComboSkinFolderName);
    }
    public static string GetStreamingComboSkinRootFolder()
    {
        return UniversalIO.PathCombine(GetStreamingSkinFolder(),
            kComboSkinFolderName);
    }
    public static string GetComboSkinFolder(string name)
    {
        string temp = UniversalIO.PathCombine(GetComboSkinRootFolder(), name);
        return UniversalIO.FileExists(temp) ?
            temp :
            UniversalIO.PathCombine(GetStreamingComboSkinRootFolder(), name);
    }

    public static string GetGameUiSkinRootFolder()
    {
        return UniversalIO.PathCombine(GetSkinFolder(), kGameUiFolderName);
    }
    public static string GetStreamingGameUiSkinRootFolder()
    {
        return UniversalIO.PathCombine(GetStreamingSkinFolder(),
            kGameUiFolderName);
    }
    public static string GetGameUiSkinFolder(string name)
    {
        string temp = UniversalIO.PathCombine(GetGameUiSkinRootFolder(), name);
        return UniversalIO.FileExists(temp) ?
            temp :
            UniversalIO.PathCombine(GetStreamingGameUiSkinRootFolder(), name);
    }
    #endregion

    #region Things in document folder
    public static string GetOptionsFilePath()
    {
        return UniversalIO.PathCombine(dataFolder, "options.json");
    }

    public static string GetRulesetFilePath()
    {
        return UniversalIO.PathCombine(dataFolder, "ruleset.json");
    }

    public static string GetRecordsFilePath()
    {
        return UniversalIO.PathCombine(dataFolder, "records.json");
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
                UniversalIO.DirectoryEnumerateFiles(folder, pattern, System.IO.SearchOption.AllDirectories))
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

    // Returns the path of absolutePath relative to reference.
    public static string RelativePath(string reference,
        string absolutePath)
    {
        if (reference.StartsWith(UniversalIO.ANDROID_CONTENT_URI))
        {
            return absolutePath.Replace(reference, "").Replace("%2F", "");
        }
        return absolutePath
            .Substring(reference.Length + 1)
#if UNITY_WSA || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            .Replace("\\", "/")
#endif
            ;
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
        string absolutePath = UniversalIO.PathCombine(streamingAssetsFolder,
            relativePath);
#if UNITY_WSA || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        absolutePath = absolutePath.Replace('/', '\\');
#endif
        return absolutePath;
    }
    #endregion
}
