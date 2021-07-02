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

    private void OnEnable()
    {
        StartCoroutine(Refresh());
    }

    protected override bool ShowNewTrackCard()
    {
        return true;
    }

    protected override void OnTrackCardClick(GameObject o)
    {
        EditorContext.Reset();
        EditorContext.trackPath = Path.Combine(cardToTrack[o].folder, 
            Paths.kTrackFilename);
        EditorContext.track = cardToTrack[o].track;
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
        string filteredTitle = Paths.FilterString(title);
        string filteredArtist = Paths.FilterString(artist);
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
                Locale.GetStringAndFormat(
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
            messageDialog.Show(Locale.GetStringAndFormat(
                "new_track_error_format",
                    filename,
                    e.Message));
            return;
        }

        RemoveCachedListsAtCurrentLocation();

        EditorContext.Reset();
        EditorContext.trackPath = filename;
        EditorContext.track = track;
        PanelTransitioner.TransitionTo(trackSetupPanel, 
            TransitionToPanel.Direction.Right);
    }
}
