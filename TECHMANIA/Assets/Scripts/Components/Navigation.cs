using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Navigation : MonoBehaviour
{
    private static Navigation instance;
    private static Navigation GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<Navigation>();
        }
        return instance;
    }

    public SelectTrackPanel selectTrackPanel;
    public TrackPanel trackPanel;

    public Text backButtonText;
    public Text title;
    public GameObject editButtons;

    public enum Location
    {
        SelectTrack,
        Track,
        Pattern,
        PatternMetadata
    }
    private Location location;

    private Track currentTrack;
    private string currentTrackPath;
    private bool dirty;

    // Start is called before the first frame update
    void Start()
    {
        InternalGoTo(Location.SelectTrack);
        currentTrack = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                Save();
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }

    public static void SetCurrentTrack(Track track, string path)
    {
        GetInstance().currentTrack = track;
        GetInstance().currentTrackPath = path;
        GetInstance().dirty = false;
    }

    public static Track GetCurrentTrack()
    {
        return GetInstance().currentTrack;
    }

    public static void SetDirty()
    {
        GetInstance().dirty = true;
        GetInstance().RefreshBackAndLocationText();
    }

    public void GoBack()
    {
        switch (location)
        {
            case Location.SelectTrack:
                Debug.LogError("There is no main menu to go to.");
                break;
            case Location.Track:
                if (dirty)
                {
                    StartCoroutine(ConfirmCloseTrackWithoutSave());
                }
                else
                {
                    GoTo(Location.SelectTrack);
                }
                break;
        }
    }

    private IEnumerator ConfirmCloseTrackWithoutSave()
    {
        ConfirmDialog.Show("Returning to track list without saving. " +
            "All unsaved changes will be lost. Continue?");
        yield return new WaitUntil(() => { return ConfirmDialog.IsResolved(); });
        if (ConfirmDialog.GetResult() == ConfirmDialog.Result.Cancelled)
        {
            yield break;
        }

        dirty = false;
        GoTo(Location.SelectTrack);
    }

    public static void GoTo(Location location)
    {
        GetInstance().InternalGoTo(location);
    }

    private void InternalGoTo(Location location)
    {
        selectTrackPanel.gameObject.SetActive(location == Location.SelectTrack);
        trackPanel.gameObject.SetActive(location == Location.Track);
        editButtons.SetActive(location != Location.SelectTrack);
        this.location = location;

        RefreshBackAndLocationText();
    }

    private void RefreshBackAndLocationText()
    {
        switch (location)
        {
            case Location.SelectTrack:
                backButtonText.text = "< Main Menu";
                title.text = "TECHMANIA Editor";
                break;
            case Location.Track:
                backButtonText.text = "< All tracks";
                title.text = currentTrack.trackMetadata.title;
                break;
        }
        if (dirty)
        {
            title.text += "*";
        }
    }

    public void Save()
    {
        try
        {
            currentTrack.SaveToFile(currentTrackPath);
            dirty = false;
        }
        catch (Exception e)
        {
            MessageDialog.Show(e.Message);
        }
        RefreshBackAndLocationText();
    }

    public void Undo()
    {

    }

    public void Redo()
    {

    }
}
