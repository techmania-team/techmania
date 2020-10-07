using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PatternPanel : MonoBehaviour
{
    [Header("Workspace")]
    public ScrollRect workspace;
    public RectTransform workspaceContent;
    public RectTransform scanline;

    [Header("Lanes")]
    public RectTransform hiddenLaneBackground;
    public RectTransform laneDividerParent;

    [Header("Markers")]
    public Transform markerContainer;
    public GameObject scanMarkerTemplate;
    public GameObject beatMarkerTemplate;
    public GameObject bpmMarkerTemplate;

    [Header("Notes")]
    public Transform noteContainer;
    public GameObject basicNotePrefab;
    public GameObject hiddenNotePrefab;

    // All note objects sorted by pulse. This allows fast lookups
    // of whether any location is occupied when moving notes.
    //
    // This data structure must be updated alongside
    // EditorContext.Pattern at all times.
    private SortedNoteObjects sortedNoteObjects;

    #region Vertical Spacing
    public static int PlayableLanes
    {
        get
        {
            return 4;
        }
    }
    public static int HiddenLanes { get; private set; }
    public static int TotalLanes
    {
        get
        {
            return PlayableLanes + HiddenLanes;
        }
    }
    public static float AllLaneTotalHeight { get; private set; }
    public static float LaneHeight
    {
        get
        {
            return AllLaneTotalHeight / TotalLanes;
        }
    }
    #endregion

    #region Horizontal Spacing
    private int numScans;
    private static int zoom;
    private int beatSnapDivisor;
    public static float ScanWidth
    {
        get
        {
            return 10f * zoom;
        }
    }
    private float WorkspaceContentWidth
    {
        get
        {
            return numScans * ScanWidth;
        }
    }
    #endregion

    #region Events
    public static event UnityAction RepositionNeeded;
    #endregion

    private void OnEnable()
    {
        // Vertical spacing
        HiddenLanes = 8;
        Canvas.ForceUpdateCanvases();
        AllLaneTotalHeight = laneDividerParent.rect.height;

        // Horizontal spacing
        zoom = 100;
        beatSnapDivisor = 2;

        Refresh();
        EditorContext.UndoneOrRedone += Refresh;
    }

    private void OnDisable()
    {
        EditorContext.UndoneOrRedone -= Refresh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Refresh()
    {
        DestroyAndRespawnExistingNotes();
        UpdateNumScans();
        DestroyAndRespawnAllMarkers();
        RepositionNeeded?.Invoke();
        ResizeWorkspace();
    }

    private void UpdateNumScans()
    {
        GameObject o = sortedNoteObjects.GetLast();
        if (o == null)
        {
            numScans = 1;
            return;
        }

        int lastPulse = o.GetComponent<EditorElement>().note.pulse;
        int lastScan = lastPulse / Pattern.pulsesPerBeat
            / EditorContext.Pattern.patternMetadata.bps;
        numScans = lastScan + 2;  // 1 empty scan at the end
    }

    private void ResizeWorkspace()
    {
        workspaceContent.sizeDelta = new Vector2(
            WorkspaceContentWidth,
            workspaceContent.sizeDelta.y);
    }

    #region Spawning
    private void DestroyAndRespawnAllMarkers()
    {
        for (int i = 0; i < markerContainer.childCount; i++)
        {
            GameObject child = markerContainer.GetChild(i).gameObject;
            if (child == scanMarkerTemplate) continue;
            if (child == beatMarkerTemplate) continue;
            if (child == bpmMarkerTemplate) continue;
            Destroy(child.gameObject);
        }

        int bps = EditorContext.Pattern.patternMetadata.bps;
        for (int scan = 0; scan < numScans; scan++)
        {
            GameObject marker = Instantiate(scanMarkerTemplate, markerContainer);
            marker.SetActive(true);  // This calls OnEnabled
            EditorElement element = marker.GetComponent<EditorElement>();
            element.beat = scan * bps;
            element.SetTimeDisplay();

            for (int beat = 1; beat < bps; beat++)
            {
                marker = Instantiate(beatMarkerTemplate, markerContainer);
                marker.SetActive(true);
                element = marker.GetComponent<EditorElement>();
                element.beat = scan * bps + beat;
                element.SetTimeDisplay();
            }
        }

        EditorContext.Pattern.PrepareForTimeCalculation();
        foreach (BpmEvent e in EditorContext.Pattern.bpmEvents)
        {
            GameObject marker = Instantiate(bpmMarkerTemplate, markerContainer);
            marker.SetActive(true);
            EditorElement element = marker.GetComponent<EditorElement>();
            element.pulse = e.pulse;
            element.SetBpmText(e.bpm);
        }
    }

    private void SpawnNoteObject(Note n, string sound)
    {
        EditorElement noteObject = Instantiate(basicNotePrefab,
            noteContainer).GetComponent<EditorElement>();
        noteObject.note = n;
        noteObject.sound = sound;
        noteObject.SetKeysoundText();

        sortedNoteObjects.Add(noteObject.gameObject);
    }

    private void DestroyAndRespawnExistingNotes()
    {
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            Destroy(noteContainer.GetChild(i).gameObject);
        }
        sortedNoteObjects = new SortedNoteObjects();

        // For newly created patterns, there's no sound channel yet.
        EditorContext.Pattern.CreateListsIfNull();

        foreach (SoundChannel channel in EditorContext.Pattern.soundChannels)
        {
            foreach (Note n in channel.notes)
            {
                SpawnNoteObject(n, channel.name);
            }
        }
    }
    #endregion
}
