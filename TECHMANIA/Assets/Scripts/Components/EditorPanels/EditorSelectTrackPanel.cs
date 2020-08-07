using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class EditorSelectTrackPanel : SelectTrackPanel
{
    public Button deleteButton;
    public Button openButton;

    private GameObject selectedTrackObject;

    private void OnEnable()
    {
        Refresh();
        selectedTrackObject = null;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        deleteButton.interactable = selectedTrackObject != null;
        openButton.interactable = selectedTrackObject != null;
    }

    protected override void OnClickTrackObject(GameObject trackObject)
    {
        if (selectedTrackObject != null)
        {
            selectedTrackObject.transform.Find("Selection").gameObject.SetActive(false);
        }
        if (!objectToTrack.ContainsKey(trackObject))
        {
            selectedTrackObject = null;
        }
        else
        {
            selectedTrackObject = trackObject;
            selectedTrackObject.transform.Find("Selection").gameObject.SetActive(true);
        }
        RefreshButtons();
    }

    public void New()
    {
        StartCoroutine(InternalNew());
    }

    private IEnumerator InternalNew()
    {
        // Get title and artist.
        NewTrackDialog.Show();
        yield return new WaitUntil(() => { return NewTrackDialog.IsResolved(); });
        if (NewTrackDialog.GetResult() == NewTrackDialog.Result.Cancelled)
        {
            yield break;
        }
        string title = NewTrackDialog.GetTitle();
        string artist = NewTrackDialog.GetArtist();
        string filteredTitle = Paths.FilterString(title);
        string filteredArtist = Paths.FilterString(artist);
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Create new directory. Contains timestamp so collisions are
        // very unlikely.
        string newDir = $"{Paths.GetTrackFolder()}\\{filteredArtist} - {filteredTitle} - {timestamp}";
        try
        {
            Directory.CreateDirectory(newDir);
        }
        catch (Exception e)
        {
            MessageDialog.Show(e.Message);
            yield break;
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
            MessageDialog.Show(e.Message);
            yield break;
        }

        Refresh();
    }

    public void Delete()
    {
        if (selectedTrackObject == null) return;
        StartCoroutine(InternalDelete());
    }

    private IEnumerator InternalDelete()
    {
        TrackInFolder trackInFolder = objectToTrack[selectedTrackObject];
        string title = trackInFolder.track.trackMetadata.title;
        string path = trackInFolder.folder;
        ConfirmDialog.Show($"Deleting {title}. This will permanently " +
            $"delele \"{path}\" and everything in it. Are you sure?");
        yield return new WaitUntil(() => { return ConfirmDialog.IsResolved(); });

        if (ConfirmDialog.GetResult() == ConfirmDialog.Result.Cancelled)
        {
            yield break;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception e)
        {
            MessageDialog.Show(e.Message);
        }

        Refresh();
    }

    public void Open()
    {
        if (selectedTrackObject == null) return;

        // Reload track from disk, just in case.
        try
        {
            string path = $"{objectToTrack[selectedTrackObject].folder}\\{Paths.kTrackFilename}";
            Track track = TrackBase.LoadFromFile(path) as Track;
            EditorNavigation.SetCurrentTrack(track, path);
        }
        catch (Exception e)
        {
            MessageDialog.Show(e.Message);
            return;
        }

        EditorNavigation.GoTo(EditorNavigation.Location.Track);
    }
}
