using System;
using UnityEngine;
using UnityEngine.UI;

public class EditorSelectTrackPanel : SelectTrackPanel
{
    public NewTrackDialog newTrackDialog;
    public Panel trackSetupPanel;

    protected override bool ShowNewTrackCard()
    {
#if UNITY_ANDROID
        // Creating new file doesn't work with this 
        if (Options.instance.customDataLocation)
        {
            return false;
        }
#endif
        return true;
    }

    protected override void OnTrackCardClick(GameObject o)
    {
        EditorContext.Reset();
        EditorContext.trackPath = UniversalIO.PathCombine(cardToTrack[o].folder,
            Paths.kTrackFilename);
        EditorContext.trackFolder = cardToTrack[o].folder;
        PanelTransitioner.TransitionTo(trackSetupPanel, 
            TransitionToPanel.Direction.Right);
    }

    protected override void OnNewTrackCardClick()
    {
        newTrackDialog.Show(createCallback:
            (string title, string artist) =>
        {
            OnCreateButtonClick(title, artist);
        });
    }

    private void OnCreateButtonClick(string title, string artist)
    {
        // Attempt to create track directory. Contains timestamp
        // so collisions are very unlikely.
        string filteredTitle = Paths.
            RemoveCharsNotAllowedOnFileSystem(title);
        string filteredArtist = Paths
            .RemoveCharsNotAllowedOnFileSystem(artist);
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        string newDirName = $"{filteredArtist} - {filteredTitle} - {timestamp}";
        string newDir = UniversalIO.PathCombine(currentLocation, newDirName);
        try
        {
            UniversalIO.DirectoryCreateDirectoryCSharp(newDirName);
        }
        catch (Exception e)
        {
            messageDialog.Show(
                Locale.GetStringAndFormatIncludingPaths(
                    "new_track_error_format",
                    newDir,
                    e.Message));
            return;
        }

        // Create empty track.
        Track track = new Track(title, artist);
        string filename = UniversalIO.PathCombine(newDir, Paths.kTrackFilename);
        try
        {
            track.SaveToFile(filename);
        }
        catch (Exception e)
        {
            messageDialog.Show(Locale.GetStringAndFormatIncludingPaths(
                "new_track_error_format",
                    filename,
                    e.Message));
            return;
        }

        // Update in-memory track list.
        trackList[currentLocation].Add(new TrackInFolder()
        {
            folder = newDir,
            track = track
        });

        EditorContext.Reset();
        EditorContext.trackPath = filename;
        EditorContext.trackFolder = newDir;
        EditorContext.track = track;
        PanelTransitioner.TransitionTo(trackSetupPanel, 
            TransitionToPanel.Direction.Right);
    }
}
