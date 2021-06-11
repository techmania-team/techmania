using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Pass information from MainMenu scene to Game scene.
public class GameSetup
{
    public static Track track;
    public static string trackPath;
    public static string trackFolder
    {
        get
        {
            return new FileInfo(trackPath).DirectoryName;
        }
    }
    public static PerTrackOptions trackOptions;

    public static Pattern pattern;
    public static Pattern patternBeforeApplyingModifier;
    public static int beginningScanInEditorPreview;
}
