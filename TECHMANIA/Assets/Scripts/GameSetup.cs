using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Pass information from MainMenu scene to Game scene.
public class GameSetup
{
    public static Track track;
    // Full path to the track.tech file.
    public static string trackPath;
    public static string trackFolder
    {
        get
        {
            // FileInfo.DirectoryName turns "jar:file:///" into
            // "/jar:file:/", so we need to correct it.
            return new FileInfo(trackPath).DirectoryName.Replace(
                "/jar:file:/data", "jar:file:///data");
        }
    }
    public static PerTrackOptions trackOptions;

    public static Pattern pattern;
    public static Pattern patternBeforeApplyingModifier;
    public static int beginningScanInEditorPreview;
}