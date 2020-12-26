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
        Refresh();
    }

    protected override bool ShowNewTrackCard()
    {
        return true;
    }

    protected override bool ShowUpgradeNoticeOnOutdatedTracks()
    {
        return true;
    }

    protected override void OnClickCard(GameObject o)
    {
        EditorContext.Reset();
        EditorContext.trackPath = 
            $"{cardToTrack[o].folder}\\{Paths.kTrackFilename}";
        EditorContext.track = cardToTrack[o].track;
        PanelTransitioner.TransitionTo(trackSetupPanel, 
            TransitionToPanel.Direction.Right);
    }

    protected override void OnClickNewTrackCard()
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

        string newDir = $"{Paths.GetTrackFolder()}\\{filteredArtist} - {filteredTitle} - {timestamp}";
        try
        {
            Directory.CreateDirectory(newDir);
        }
        catch (Exception e)
        {
            messageDialog.Show($"An error occurred when " +
                $"creating {newDir}:\n\n{e.Message}");
            return;
        }

        // Create empty track.
        Track track = new Track(title, artist);
        string filename = $"{newDir}\\{Paths.kTrackFilename}";
        try
        {
            track.SaveToFile(filename);
        }
        catch (Exception e)
        {
            messageDialog.Show($"An error occurred when " +
                $"writing to {filename}:\n\n{e.Message}");
            return;
        }

        EditorContext.Reset();
        EditorContext.trackPath = filename;
        EditorContext.track = track;
        PanelTransitioner.TransitionTo(trackSetupPanel, 
            TransitionToPanel.Direction.Right);
    }

    protected override void OnClickCardWithUpgradeNotice(GameObject o)
    {
        messageDialog.Show("This track was created with an outdated version of TECHMANIA.\n\nTECHMANIA will upgrade the track, and overwrite the previous version upon the first save. Please double check all resources, metadata and patterns before saving.", () =>
        {
            OnClickCard(o);
        });
    }
}
