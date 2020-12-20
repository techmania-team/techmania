using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Pass information from MainMenu scene to Game scene.
public class GameSetup
{
    public static TrackV1 track;
    public static string trackPath;
    public static string trackFolder
    {
        get
        {
            return new FileInfo(trackPath).DirectoryName;
        }
    }
    
    public static PatternV1 pattern;
    public static bool noFail;
    public static bool autoPlay;
}
