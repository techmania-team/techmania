using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.Events;
using System;
using ThemeApi;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class Paths
{
    public const string kTrackFilename = "track.tech";
    public const string kSetlistFilename = "setlist.tech";
    public const string kSkinFilename = "skin.json";
    public const string kSubfolderEyecatchPngFilename = 
        "eyecatch.png";
    public const string kSubfolderEyecatchJpgFilename = 
        "eyecatch.jpg";
    public const string kThemeExtension = ".tmtheme";

    public const string kTrackFolderName = "Tracks";
    public const string kSetlistFolderName = "Setlists";
    public const string kSkinFolderName = "Skins";
    public const string kNoteSkinFolderName = "Note";
    public const string kVfxSkinFolderName = "VFX";
    public const string kComboSkinFolderName = "Combo";
    public const string kGameUiFolderName = "Game UI";
    public const string kThemeFolderName = "Themes";

    public const string kAssetBundleFolder = "Assets/AssetBundles";
    public const string kDefaultBundleName = "default";

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

    private static string workingDirectory;
    private static string dataFolder;

    private static string streamingAssetsFolder;
    private static string trackRootFolder;
    private static string streamingTrackRootFolder;
    private static string setlistRootFolder;
    private static string streamingSetlistRootFolder;
    private static string skinRootFolder;
    private static string streamingSkinRootFolder;
    private static string themeFolder;
    private static string streamingThemeFolder;

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

        // Setlist root folder
        setlistRootFolder = Path.Combine(workingDirectory,
            kSetlistFolderName);
        Directory.CreateDirectory(setlistRootFolder);
        streamingSetlistRootFolder = Path.Combine(
            streamingAssetsFolder, kSetlistFolderName);

        // Skin folder
        skinRootFolder = Path.Combine(workingDirectory, 
            kSkinFolderName);
        Directory.CreateDirectory(skinRootFolder);
        foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
        {
            Directory.CreateDirectory(GetSkinRootFolderForType(type));
        }
        streamingSkinRootFolder = Path.Combine(
            streamingAssetsFolder, kSkinFolderName);

        // Theme folder
        themeFolder = Path.Combine(workingDirectory,
            kThemeFolderName);
        Directory.CreateDirectory(themeFolder);
        streamingThemeFolder = Path.Combine(
            streamingAssetsFolder, kThemeFolderName);

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
        trackRootFolder = Path.Combine(workingDirectory,
            kTrackFolderName);
        setlistRootFolder = Path.Combine(workingDirectory,
            kSetlistFolderName);
        skinRootFolder = Path.Combine(workingDirectory,
            kSkinFolderName);
        themeFolder = Path.Combine(workingDirectory,
            kThemeFolderName);
        if (Options.instance.customDataLocation)
        {
            if (!string.IsNullOrEmpty(
                Options.instance.tracksFolderLocation))
            {
                trackRootFolder = Options.instance.tracksFolderLocation;
            }
            if (!string.IsNullOrEmpty(
                Options.instance.setlistsFolderLocation))
            {
                setlistRootFolder = Options.instance
                    .setlistsFolderLocation;
            }
            if (!string.IsNullOrEmpty(
                Options.instance.skinsFolderLocation))
            {
                skinRootFolder = Options.instance.skinsFolderLocation;
            }
            if (!string.IsNullOrEmpty(
                Options.instance.themesFolderLocation))
            {
                themeFolder = Options.instance.themesFolderLocation;
            }
        }
    }
    #endregion

    #region Things in working directory
    public static string GetTrackRootFolder(
        bool streamingAssets = false)
    {
        return streamingAssets ? streamingTrackRootFolder :
            trackRootFolder;
    }

    public static string GetSetlistRootFolder(
        bool streamingAssets = false)
    {
        return streamingAssets ? streamingSetlistRootFolder :
            setlistRootFolder;
    }

    public static string GetSkinRootFolder(
        bool streamingAssets = false)
    {
        return streamingAssets ? streamingSkinRootFolder : 
            skinRootFolder;
    }

    public static string GetSkinRootFolderForType(
        SkinType type, bool streamingAssets = false)
    {
        string rootFolder = GetSkinRootFolder(streamingAssets);
        string folderName = type switch
        {
            SkinType.Note => kNoteSkinFolderName,
            SkinType.Vfx => kVfxSkinFolderName,
            SkinType.Combo => kComboSkinFolderName,
            SkinType.GameUI => kGameUiFolderName,
            _ => ""
        };
        return Path.Combine(rootFolder, folderName);
    }

    public static string GetSkinFolder(SkinType type, string name)
    {
        // If there's a name collision between a skin in the
        // working directory and streaming assets, prioritize the
        // former. This is the same behavior as SelectSkinPanel.
        string folderInWorkingDir = Path.Combine(
            GetSkinRootFolderForType(type), name);
        if (Directory.Exists(folderInWorkingDir))
        {
            return folderInWorkingDir;
        }
        return Path.Combine(GetSkinRootFolderForType(
            type, streamingAssets: true), name);
    }

    public static string GetThemeFolder(bool streamingAssets = false)
    {
        return streamingAssets ? streamingThemeFolder : themeFolder;
    }

    public static string GetThemeFilePath(string name)
    {
        string filenameInFolder = name + kThemeExtension;
        string pathInWorkingDir = Path.Combine(
            GetThemeFolder(), filenameInFolder);
        if (File.Exists(pathInWorkingDir))
        {
            return pathInWorkingDir;
        }

        return Path.Combine(GetThemeFolder(streamingAssets: true),
            filenameInFolder);
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

    public static string GetStatisticsFilePath()
    {
        return Path.Combine(dataFolder, "stats.json");
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
        // Streaming assets on Android are not files, so they are
        // inaccessible with "file://".
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
            .Replace(Paths.GetTrackRootFolder(streamingAssets: true), 
                kTrackFolderName)
            .Replace(Paths.GetTrackRootFolder(streamingAssets: false), 
                kTrackFolderName)
            .Replace(Paths.GetSkinRootFolder(streamingAssets: true), 
                kSkinFolderName)
            .Replace(Paths.GetSkinRootFolder(streamingAssets: false), 
                kSkinFolderName)
            .Replace(Paths.GetThemeFolder(streamingAssets: true), 
                kThemeFolderName)
            .Replace(Paths.GetThemeFolder(streamingAssets: false), 
                kThemeFolderName)
            .Replace(dataFolder, "TECHMANIA");
#else
        return fullPath;
#endif
    }

    // Returns the path of absolutePath relative to reference.
    public static string RelativePath(string reference,
        string absolutePath)
    {
        return absolutePath
            .Substring(reference.Length + 1)
#if UNITY_WSA || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            .Replace("\\", "/")
#endif
            ;
    }

    public static string ForceEscapeBackslash(string path)
    {
        return path.Replace("\\", "\\\\");
    }

    public static string EscapeBackslash(string path)
    {
        // On API version 1 (Unity 2022.3.2f1), UI Toolkit parsed
        // escape sequences in strings, so this was necessary
        // for any element that has the potential to display
        // paths with backslashes.
        // On 2 and onward, the parsing is disabled by default,
        // but older theme code still called this, resulting in
        // double backslashes. To ensure no change in behavior,
        // we change this function to do nothing on version 1.
        if (ScriptSession.apiVersion == 1)
        {
            return path;
        }
        else
        {
            return ForceEscapeBackslash(path);
        }
    }

    // Similar to Path.GetDirectoryName but, if going up from
    // a subdirectory of streaming assets, this returns the non-streaming
    // track root instead of streaming track root.
    //
    // This will also not go further above the non-streaming track root.
    public static string GoUpFrom(string path)
    {
        if (path == GetTrackRootFolder(streamingAssets: false) ||
            path == GetTrackRootFolder(streamingAssets: true))
        {
            return GetTrackRootFolder();
        }
        if (path == GetSetlistRootFolder(streamingAssets: false) ||
            path == GetSetlistRootFolder(streamingAssets: true))
        {
            return GetSetlistRootFolder();
        }

        string up = Path.GetDirectoryName(path);
#if UNITY_ANDROID
        // On Android, the following variables / methods
        // take the following values:
        //
        // path
        // jar:file:///storage/emulated/0/Android/obb/com.TECHMANIATeam.TECHMANIA/main.1.com.TECHMANIATeam.TECHMANIA.obb!/assets/Tracks/Official Tracks
        //
        // GetTrackRootFolder(streamingAssets: true)
        // jar:file:///storage/emulated/0/Android/obb/com.TECHMANIATeam.TECHMANIA/main.1.com.TECHMANIATeam.TECHMANIA.obb!/assets/Tracks
        //
        // GetTrackRootFolder(streamingAssets: false)
        // /storage/emulated/0/Android/data/com.TECHMANIATeam.TECHMANIA/files/Tracks
        //
        // up = Path.GetDirectoryName(path)
        // jar:file:/storage/emulated/0/Android/obb/com.TECHMANIATeam.TECHMANIA/main.1.com.TECHMANIATeam.TECHMANIA.obb!/assets/Tracks
        //
        // Notice that GetDirectoryName turns "///" to "/" so we
        // restore it.

        up = up.Replace("jar:file:/", "jar:file:///");
#endif
        if (up == GetTrackRootFolder(streamingAssets: false) ||
            up == GetTrackRootFolder(streamingAssets: true))
        {
            return GetTrackRootFolder();
        }
        if (path == GetSetlistRootFolder(streamingAssets: false) ||
            path == GetSetlistRootFolder(streamingAssets: true))
        {
            return GetSetlistRootFolder();
        }
        return up;
    }

    public static string Combine(string path1, string path2)
    {
        if (path1 == null) path1 = "";
        if (path2 == null) path2 = "";
        return System.IO.Path.Combine(path1, path2);
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
        // In Path.Combine, if an argument other than the first
        // contains a rooted path, any previous path components
        // are ignored, and the returned string begins with that
        // rooted path component.
        //
        // To work around that, we trim slashes.
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
