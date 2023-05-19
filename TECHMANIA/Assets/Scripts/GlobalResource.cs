using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.IO;

[MoonSharpUserData]
public static class GlobalResource
{
    #region Skins
    public static NoteSkin noteSkin;
    public static VfxSkin vfxSkin;
    public static ComboSkin comboSkin;
    public static GameUISkin gameUiSkin;

    public static List<string> AllNoteSkins()
    {
        return AllSkinsInFolder(
            Paths.GetNoteSkinRootFolder(),
            Paths.GetStreamingNoteSkinRootFolder());
    }

    public static List<string> AllVfxSkins()
    {
        return AllSkinsInFolder(
            Paths.GetVfxSkinRootFolder(),
            Paths.GetStreamingVfxSkinRootFolder());
    }

    public static List<string> AllComboSkins()
    {
        return AllSkinsInFolder(
            Paths.GetComboSkinRootFolder(),
            Paths.GetStreamingComboSkinRootFolder());
    }

    public static List<string> AllGameUiSkins()
    {
        return AllSkinsInFolder(
            Paths.GetGameUiSkinRootFolder(),
            Paths.GetStreamingGameUiSkinRootFolder());
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
    [MoonSharpUserData]
    // Note: subfolders in streaming assets are seen as
    // subfolders for Paths.GetTrackRootFolder().
    public class TrackSubfolder
    {
        public string name;
        public string fullPath;
        public string eyecatchFullPath;
    }
    [MoonSharpUserData]
    public class TrackInFolder
    {
        // The folder that track.tech is in.
        public string folder;
        // Minimized to save RAM; does not contain notes or time events.
        public Track minimizedTrack;
    }
    [MoonSharpUserData]
    public class TrackWithError
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
    public static Dictionary<string, List<TrackSubfolder>>
        trackSubfolderList;
    public static Dictionary<string, List<TrackInFolder>>
        trackList;
    public static Dictionary<string, List<TrackWithError>>
        trackWithErrorList;

    #region Lua accessor
    public static List<TrackSubfolder> GetSubfolders(string parent)
    {
        if (trackSubfolderList.ContainsKey(parent))
        {
            return trackSubfolderList[parent];
        }
        else
        {
            return null;
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
            return null;
        }
    }

    public static List<TrackWithError> GetTracksWithError(
        string parent)
    {
        if (trackWithErrorList.ContainsKey(parent))
        {
            return trackWithErrorList[parent];
        }
        else
        {
            return null;
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

    #region Theme
    public static Dictionary<string, Object> themeContent;

    /// <summary>
    /// Returns null if the specified file doesn't exist, or isn't
    /// the correct type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T GetThemeContent<T>(string name) where T : Object
    {
        name = name.ToLower();
        if (!themeContent.ContainsKey(name))
        {
            Debug.LogError($"The asset {name} does not exist in the current theme.");
            return null;
        }
        Object asset = themeContent[name];
        if (asset.GetType() != typeof(T))
        {
            Debug.LogError($"The asset {name} exists in the current theme, but is not of the expected type. Expected type: {typeof(T).Name}; actual type: {asset.GetType().Name}");
            return null;
        }
        return asset as T;
    }
    #endregion
}
