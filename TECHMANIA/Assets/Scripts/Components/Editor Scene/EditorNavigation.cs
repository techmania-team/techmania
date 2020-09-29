using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// TODO: deprecate this.
public class EditorNavigation : MonoBehaviour
{
    private static EditorNavigation instance;
    private static EditorNavigation GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<EditorNavigation>();
        }
        return instance;
    }

    public EditorSelectTrackPanel selectTrackPanel;
    public TrackPanel trackPanel;
    public ResourcePanel resourcePanel;
    public PatternMetadataPanel patternMetadataPanel;
    public PatternPanel patternPanel;

    public Text backButtonText;
    public Text title;
    public GameObject editButtons;
    public Button undoButton;
    public Button redoButton;

    public enum Location
    {
        SelectTrack,
        Track,
        PatternMetadata,
        Pattern
    }
    private Location location;

    private Track currentTrack;
    private int currentPatternIndex;
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }

    public static void SetCurrentTrack(Track track, string path)
    {
        EditorNavigation instance = GetInstance();
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

    public static string GetCurrentTrackPath()
    {
        return GetInstance().currentTrackPath;
    }

    public static void SetCurrentPattern(int index)
    {
        GetInstance().currentPatternIndex = index;
    }

    public static Pattern GetCurrentPattern()
    {
        EditorNavigation instance = GetInstance();
        return instance.currentTrack.patterns[instance.currentPatternIndex];
    }

    // Call this before making any change to currentTrack.
    public static void PrepareForChange()
    {
        // Debug.Log("Cloning in-memory track.");
        EditorNavigation instance = GetInstance();
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
                trackPanel.RefreshDropdowns();
                break;
            case Location.PatternMetadata:
                patternMetadataPanel.RefreshDropdowns();
                break;
            case Location.Pattern:
                patternPanel.MemoryToUI();
                break;
        }
    }

    public void GoBack()
    {
        switch (location)
        {
            case Location.SelectTrack:
                SceneManager.LoadScene("Main Menu");
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
            case Location.PatternMetadata:
                GoTo(Location.Track);
                break;
            case Location.Pattern:
                GoTo(Location.PatternMetadata);
                break;
        }
    }

    private IEnumerator ConfirmCloseTrackWithoutSave()
    {
        // ConfirmDialog.Show("Returning to track list without saving. " +
        //     "All unsaved changes will be lost. Continue?");
        // yield return new WaitUntil(() => { return ConfirmDialog.IsResolved(); });
        // if (ConfirmDialog.GetResult() == ConfirmDialog.Result.Cancelled)
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
        resourcePanel.gameObject.SetActive(location == Location.Track ||
            location == Location.PatternMetadata);
        patternMetadataPanel.gameObject.SetActive(location == Location.PatternMetadata);
        patternPanel.gameObject.SetActive(location == Location.Pattern);
        this.location = location;

        RefreshNavigationPanel();
        MemoryToUI();
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
            case Location.PatternMetadata:
                backButtonText.text = "< All patterns";
                title.text = $"{currentTrack.trackMetadata.title} - " +
                    $"{currentTrack.patterns[currentPatternIndex].patternMetadata.patternName}";
                break;
            case Location.Pattern:
                backButtonText.text = "< Pattern setup";
                title.text = $"{currentTrack.trackMetadata.title} - " +
                    $"{currentTrack.patterns[currentPatternIndex].patternMetadata.patternName}";
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
            // MessageDialog.Show(e.Message);
        }
        RefreshNavigationPanel();
    }

    public void Undo()
    {
        if (undoStack.Empty()) return;
        redoStack.Push(currentTrack.Clone() as Track);
        currentTrack = undoStack.Pop();
        dirty = true;
        RefreshNavigationPanel();
        MemoryToUI();
    }

    public void Redo()
    {
        if (redoStack.Empty()) return;
        undoStack.Push(currentTrack.Clone() as Track);
        currentTrack = redoStack.Pop();
        dirty = true;
        RefreshNavigationPanel();
        MemoryToUI();
    }
}
