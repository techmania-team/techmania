using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalResource
{
    #region Skins
    public static NoteSkin noteSkin;
    public static VfxSkin vfxSkin;
    public static ComboSkin comboSkin;
    public static GameUISkin gameUiSkin;
    #endregion

    #region Track list
    public class TrackSubfolder
    {
        public string path;
        public string eyecatchFullPath;
    }
    public class TrackInFolder
    {
        // The folder that track.tech is in.
        public string folder;
        // Minimized to save RAM.
        public Track minimizedTrack;
    }
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
