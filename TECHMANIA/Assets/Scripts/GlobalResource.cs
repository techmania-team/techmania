using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.IO;
using System;
using static GlobalResource;

public enum SkinType
{
    Note,
    Vfx,
    Combo,
    GameUI
}

[MoonSharpUserData]
public static class GlobalResource
{
    #region Skins
    public static NoteSkin noteSkin;
    public static VfxSkin vfxSkin;
    public static ComboSkin comboSkin;
    public static GameUISkin gameUiSkin;

    public static List<Tuple<AnimationCurve, string>> 
        comboAnimationCurvesAndAttributes;

    public static List<string> GetSkinList(SkinType type)
    {
        return AllSkinsInFolder(
            Paths.GetSkinRootFolderForType(type),
            Paths.GetSkinRootFolderForType(type, streamingAssets: true));
    }

    private static List<string> AllSkinsInFolder(string skinFolder,
        string streamingSkinFolder)
    {
        List<string> skinNames = new List<string>();

        // Enumerate skins in the skin folder.
        try
        {
            foreach (string folder in
                Directory.EnumerateDirectories(skinFolder))
            {
                // folder does not end in directory separator.
                string skinName = Path.GetFileName(folder);
                skinNames.Add(skinName);
            }
        }
        catch (DirectoryNotFoundException)
        {
            // Silently ignore.
        }

        // Enumerate skins in the streaming assets folder.
        if (BetterStreamingAssets.DirectoryExists(
            Paths.RelativePathInStreamingAssets(streamingSkinFolder)))
        {
            foreach (string relativeFilename in
                BetterStreamingAssets.GetFiles(
                Paths.RelativePathInStreamingAssets(
                    streamingSkinFolder),
                Paths.kSkinFilename,
                SearchOption.AllDirectories))
            {
                string folder = Path.GetDirectoryName(
                    relativeFilename);
                string skinName = Path.GetFileName(folder);
                skinNames.Add(skinName);
            }
        }

        skinNames.Sort();
        return skinNames;
    }
    #endregion

    #region Track list
    // Shared by tracks and setlists.
    //
    // Subfolders in streaming assets are seen as subfolders of
    // Paths.GetTrack/SetlistRootFolder().
    [MoonSharpUserData]
    public class Subfolder
    {
        public string name;
        public string fullPath;
        public DateTime modifiedTime;
        public string eyecatchFullPath;

        [MoonSharpHidden]
        public void FindEyecatch()
        {
            string pngEyecatch = Path.Combine(fullPath,
                Paths.kSubfolderEyecatchPngFilename);
            if (File.Exists(pngEyecatch))
            {
                eyecatchFullPath = pngEyecatch;
                return;
            }

            string jpgEyecatch = Path.Combine(fullPath,
                Paths.kSubfolderEyecatchJpgFilename);
            if (File.Exists(jpgEyecatch))
            {
                eyecatchFullPath = jpgEyecatch;
            }
        }

        [MoonSharpHidden]
        public void FindStreamingEyecatch(string relativePath)
        {
            string pngEyecatch = Path.Combine(relativePath,
                Paths.kSubfolderEyecatchPngFilename);
            if (BetterStreamingAssets.FileExists(pngEyecatch))
            {
                eyecatchFullPath = Paths.
                    AbsolutePathInStreamingAssets(pngEyecatch);
                return;
            }
            string jpgEyecatch = Path.Combine(relativePath,
                Paths.kSubfolderEyecatchJpgFilename);
            if (BetterStreamingAssets.FileExists(jpgEyecatch))
            {
                eyecatchFullPath = Paths.
                    AbsolutePathInStreamingAssets(jpgEyecatch);
            }
        }
    }
    [MoonSharpUserData]
    public class TrackInFolder
    {
        // The folder that track.tech is in.
        public string folder;
        // The last modified time of the folder.
        // Newly unzipped folders will have the modified time be set
        // to the time of unzipping.
        public DateTime modifiedTime;
        // Minimized to save RAM; does not contain notes or time events.
        public Track minimizedTrack;
    }
    // Shared by tracks and setlists.
    [MoonSharpUserData]
    public class ResourceWithError
    {
        public enum Type
        {
            Load,
            Upgrade
        }
        public Type typeEnum;
        public string type => typeEnum.ToString();
        public Status status;
    }

    // Cached, keyed by track folder's parent folder.
    public static Dictionary<string, List<Subfolder>>
        trackSubfolderList;
    public static Dictionary<string, List<TrackInFolder>>
        trackList;
    public static Dictionary<string, List<ResourceWithError>>
        trackWithErrorList;

    #region Lua accessor
    // DEPRECATED; new code should use GetTrackSubfolders.
    public static List<Subfolder> GetSubfolders(string parent)
    {
        Debug.LogWarning("GlobalResource.GetSubfolders is deprecated. Call GlobalResource.GetTrackSubfolders instead.");
        return GetTrackSubfolders(parent);
    }

    public static List<Subfolder> GetTrackSubfolders(string parent)
    {
        if (trackSubfolderList.ContainsKey(parent))
        {
            return trackSubfolderList[parent];
        }
        else
        {
            return new List<Subfolder>();
        }
    }

    public static List<TrackInFolder> GetTracksInFolder(string parent)
    {
        if (trackList.ContainsKey(parent))
        {
            return trackList[parent];
        }
        else
        {
            return new List<TrackInFolder>();
        }
    }

    public static List<ResourceWithError> GetTracksWithError(
        string parent)
    {
        if (trackWithErrorList.ContainsKey(parent))
        {
            return trackWithErrorList[parent];
        }
        else
        {
            return new List<ResourceWithError>();
        }
    }

    public static void ClearTrackList()
    {
        trackSubfolderList.Clear();
        trackList.Clear();
        trackWithErrorList.Clear();
    }
    #endregion

    public static bool anyOutdatedTrack;
    #endregion

    #region Setlist list
    [MoonSharpUserData]
    public class SetlistInFolder
    {
        // The folder that setlist.tech is in.
        public string folder;
        // The last modified time of the folder.
        // Newly unzipped folders will have the modified time be set
        // to the time of unzipping.
        public DateTime modifiedTime;
        // Unlike tracks, here we can keep the entire setlist in RAM.
        public Setlist setlist;
    }

    // Cached, keyed by setlist folder's parent folder.
    public static Dictionary<string, List<Subfolder>>
        setlistSubfolderList;
    public static Dictionary<string, List<SetlistInFolder>>
        setlistList;
    public static Dictionary<string, List<ResourceWithError>>
        setlistWithErrorList;

    #region Lua accessor
    public static List<Subfolder> GetSetlistSubfolders(string parent)
    {
        if (setlistSubfolderList.ContainsKey(parent))
        {
            return setlistSubfolderList[parent];
        }
        else
        {
            return new List<Subfolder>();
        }
    }

    public static List<SetlistInFolder> GetSetlistsInFolder(
        string parent)
    {
        if (setlistList.ContainsKey(parent))
        {
            return setlistList[parent];
        }
        else
        {
            return new List<SetlistInFolder>();
        }
    }

    public static List<ResourceWithError> GetSetlistsWithError(
        string parent)
    {
        if (setlistWithErrorList.ContainsKey(parent))
        {
            return setlistWithErrorList[parent];
        }
        else
        {
            return new List<ResourceWithError>();
        }
    }

    public static void ClearSetlistList()
    {
        setlistSubfolderList.Clear();
        setlistList.Clear();
        setlistWithErrorList.Clear();
    }
    #endregion

    public static bool anyOutdatedSetlist;

    public static Status SearchForPatternReference(
        Setlist.PatternReference reference,
        out TrackInFolder trackInFolder, out Pattern minimizedPattern)
    {
        foreach (KeyValuePair<string, List<TrackInFolder>> pair in
            trackList)
        {
            foreach (TrackInFolder t in pair.Value)
            {
                string trackGuid = t.minimizedTrack.trackMetadata.guid;
                if (trackGuid != reference.trackGuid) continue;

                foreach (Pattern p in t.minimizedTrack.patterns)
                {
                    string patternGuid = p.patternMetadata.guid;
                    if (patternGuid != reference.patternGuid) continue;

                    trackInFolder = t;
                    minimizedPattern = p;
                    return Status.OKStatus();
                }
            }
        }

        trackInFolder = null;
        minimizedPattern = null;
        return Status.Error(Status.Code.NotFound);
    }
    #endregion

    #region Theme
    public static List<string> GetThemeList()
    {
        List<string> themeNames = new List<string>();
        string searchPattern = "*" + Paths.kThemeExtension;

        // Enumerate themes in the theme folder.
        foreach (string filename in
            Directory.EnumerateFiles(Paths.GetThemeFolder(), 
            searchPattern))
        {
            themeNames.Add(Path.GetFileNameWithoutExtension(filename));
        }

        // Enumerate themes in the streaming assets folder.
        string relativeStreamingThemesFolder =
            Paths.RelativePathInStreamingAssets(
                Paths.GetThemeFolder(streamingAssets: true));
        if (BetterStreamingAssets.DirectoryExists(
            relativeStreamingThemesFolder))
        {
            foreach (string relativeFilename in
                BetterStreamingAssets.GetFiles(
                    relativeStreamingThemesFolder,
                    searchPattern,
                    SearchOption.AllDirectories))
            {
                themeNames.Add(Path.GetFileNameWithoutExtension(
                    relativeFilename));
            }
        }

        themeNames.Sort();
        return themeNames;
    }

    public static Dictionary<string, object> themeContent;

    /// <summary>
    /// Returns null if the specified file doesn't exist, or isn't
    /// the correct type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T GetThemeContent<T>(string name)
        where T : class
    {
        name = name.ToLower();
        if (!themeContent.ContainsKey(name))
        {
            Debug.LogError($"The asset {name} does not exist in the current theme.");
            return null;
        }
        object asset = themeContent[name];
        if (asset.GetType() != typeof(T))
        {
            Debug.LogError($"The asset {name} exists in the current theme, but is not of the expected type. Expected type: {typeof(T).Name}; actual type: {asset.GetType().Name}");
            return null;
        }
        return asset as T;
    }
    #endregion
}
