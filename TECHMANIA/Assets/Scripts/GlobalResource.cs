using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalResource
{
    public static NoteSkin noteSkin;
    // TODO: VFX and Combo skin

    // TODO: migrate these back to SelectTrackPanel.
    public class TrackInFolder
    {
        public string folder;
        public Track track;
    }
    public class ErrorInTrackFolder
    {
        public string trackFile;
        public string message;
    }

    public static List<TrackInFolder> allTracks;
    public static List<ErrorInTrackFolder> allTracksWithError;
}
