using System;
using System.Collections;
using System.Collections.Concurrent;
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
    public Button undoButton;
    public Button redoButton;

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
    private LimitedStack<Track> undoStack;
    private LimitedStack<Track> redoStack;

    // Start is called before the first frame update
    void Start()
    {
        undoStack = new LimitedStack<Track>(100);
        redoStack = new LimitedStack<Track>(100);
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
        Navigation instance = GetInstance();
        instance.currentTrack = track;
        instance.currentTrackPath = path;
        instance.dirty = false;
        instance.undoStack.Clear();
        instance.redoStack.Clear();
    }

    public static Track GetCurrentTrack()
    {
        return GetInstance().currentTrack;
    }

    // Call this before making any change to currentTrack.
    public static void PrepareForChange()
    {
        Debug.Log("Cloning in-memory track.");
        Navigation instance = GetInstance();
        instance.dirty = true;
        instance.undoStack.Push(instance.currentTrack.Clone() as Track);
        instance.redoStack.Clear();
    }

    // Call this after making any change to currentTrack.
    public static void DoneWithChange()
    {
        GetInstance().RefreshNavigationPanel();
    }

    private void MemoryToUI()
    {
        switch (location)
        {
            case Location.Track:
                trackPanel.MemoryToUI();
                break;
        }
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
        this.location = location;

        RefreshNavigationPanel();
    }

    private void RefreshNavigationPanel()
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

        editButtons.SetActive(location != Location.SelectTrack);
        undoButton.interactable = !undoStack.Empty();
        redoButton.interactable = !redoStack.Empty();
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
        RefreshNavigationPanel();
    }

    public void Undo()
    {
        if (undoStack.Empty()) return;
        redoStack.Push(currentTrack.Clone() as Track);
        currentTrack = undoStack.Pop();
        RefreshNavigationPanel();
        MemoryToUI();
    }

    public void Redo()
    {
        if (redoStack.Empty()) return;
        undoStack.Push(currentTrack.Clone() as Track);
        currentTrack = redoStack.Pop();
        RefreshNavigationPanel();
        MemoryToUI();
    }
}
