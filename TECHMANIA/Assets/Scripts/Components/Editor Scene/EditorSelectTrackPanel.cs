using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EditorSelectTrackPanel : SelectTrackPanel
{
    public NewTrackDialog newTrackDialog;
    public Panel trackSetupPanel;

    protected override bool ShowNewTrackCard()
    {
        return true;
    }

    protected override void OnTrackCardClick(GameObject o)
    {
        EditorContext.Reset();
        EditorContext.trackPath = Path.Combine(cardToTrack[o].folder,
            Paths.kTrackFilename);
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

        string newDir = Path.Combine(currentLocation,
            $"{filteredArtist} - {filteredTitle} - {timestamp}");
        try
        {
            Directory.CreateDirectory(newDir);
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
        string filename = Path.Combine(newDir, Paths.kTrackFilename);
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
        GlobalResource.trackList[currentLocation].Add(new GlobalResource.TrackInFolder()
        {
            folder = newDir,
            minimizedTrack = track
        });

        EditorContext.Reset();
        EditorContext.trackPath = filename;
        EditorContext.track = track;
        PanelTransitioner.TransitionTo(trackSetupPanel, 
            TransitionToPanel.Direction.Right);
    }
}
