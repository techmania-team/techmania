using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class GlobalResource
{
    #region Skins
    public static NoteSkin noteSkin;
    public static VfxSkin vfxSkin;
    public static ComboSkin comboSkin;
    public static GameUISkin gameUiSkin;
    #endregion

    #region Track list
    [MoonSharpUserData]
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
        // Minimized to save RAM.
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
        public Type type;
        public string trackFile;
        public string message;
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
        if (!themeContent.ContainsKey(name)) return null;
        Object content = themeContent[name];
        if (content.GetType() != typeof(T)) return null;
        return content as T;
    }
    #endregion
}
