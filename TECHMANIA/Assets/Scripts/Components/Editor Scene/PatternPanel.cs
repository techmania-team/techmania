using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PatternPanel : MonoBehaviour
{
    [Header("Workspace")]
    public ScrollRect workspace;
    public RectTransform workspaceContent;
    public ScanlineInEditor scanline;

    [Header("Lanes")]
    public RectTransform hiddenLaneBackground;
    public RectTransform header;
    public RectTransform laneDividerParent;

    [Header("Markers")]
    public Transform markerContainer;
    public GameObject scanMarkerTemplate;
    public GameObject beatMarkerTemplate;
    public GameObject bpmMarkerTemplate;

    [Header("Notes")]
    public Transform noteContainer;
    public NoteObject noteCursor;
    public GameObject basicNotePrefab;
    public GameObject hiddenNotePrefab;

    [Header("Audio")]
    public AudioSource backingTrackSource;
    public List<AudioSource> keysoundSources;

    [Header("Options")]
    public TextMeshProUGUI beatSnapDividerDisplay;
    public TMP_Dropdown hiddenLanesDropdown;
    public Toggle applyNoteTypeToSelectionToggle;
    public Toggle applyKeysoundToSelectionToggle;
    public Toggle showKeysoundToggle;

    [Header("UI")]
    public KeysoundSideSheet keysoundSheet;
    public GameObject playButton;
    public GameObject stopButton;
    public Slider scanlinePositionSlider;
    public MessageDialog messageDialog;
    public BpmEventDialog bpmEventDialog;
    public Dialog shortcutDialog;

    #region Internal Data Structures
    // All note objects sorted by pulse. This allows fast lookups
    // of whether any location is occupied when moving notes.
    //
    // This data structure must be updated alongside
    // EditorContext.Pattern at all times.
    private SortedNoteObjects sortedNoteObjects;

    private GameObject lastSelectedNoteObjectWithoutShift;
    private HashSet<GameObject> selectedNoteObjects;

    private class NoteWithSound
    {
        public Note note;
        public string sound;
        public static NoteWithSound FromNoteObject(GameObject o)
        {
            NoteObject n = o.GetComponent<NoteObject>();
            return new NoteWithSound()
            {
                note = n.note,
                sound = n.sound
            };
        }
    }
    // Clipboard stores notes and sounds instead of GameObjects,
    // so we are free of Unity stuff such as MonoBehaviors and
    // Instantiating.
    //
    // The clipboard is intentionally not initialized in OnEnabled,
    // so it is preserved between editing sessions, and across
    // patterns.
    private List<NoteWithSound> clipboard;
    private int minPulseInClipboard;
    #endregion

    #region Vertical Spacing
    public static int PlayableLanes => 4;
    public static int HiddenLanes { get; private set; }
    public const int MaxHiddenLanes = 8;
    public static int TotalLanes => PlayableLanes + HiddenLanes;
    public static int MaxTotalLanes => PlayableLanes + MaxHiddenLanes;
    public static float AllLaneTotalHeight { get; private set; }
    public static float LaneHeight => AllLaneTotalHeight / TotalLanes;
    #endregion

    #region Horizontal Spacing
    private int numScans;
    private static int zoom;
    private int beatSnapDivisor;
    public static float ScanWidth => 10f * zoom;
    private float WorkspaceContentWidth => numScans * ScanWidth;
    #endregion

    #region Outward Events
    public static event UnityAction RepositionNeeded;
    public static event UnityAction<HashSet<GameObject>> SelectionChanged;
    public static event UnityAction<bool> KeysoundVisibilityChanged;
    #endregion

    #region MonoBehavior APIs
    private void OnEnable()
    {
        // Vertical spacing
        HiddenLanes = int.Parse(
            hiddenLanesDropdown.options[hiddenLanesDropdown.value].text);
        Canvas.ForceUpdateCanvases();
        AllLaneTotalHeight = laneDividerParent.rect.height;

        // Horizontal spacing
        zoom = 100;
        beatSnapDivisor = 2;

        // Scanline
        scanline.floatPulse = 0f;
        scanline.GetComponent<SelfPositioner>().Reposition();

        // UI and options
        UpdateBeatSnapDivisorDisplay();
        keysoundSheet.Initialize();

        // Playback
        playButton.GetComponent<Button>().interactable = false;
        ResourceLoader.CacheAudioResources(EditorContext.trackFolder,
            cacheAudioCompleteCallback: OnResourceLoadComplete);
        isPlaying = false;

        Refresh();
        OnKeysoundVisibilityChanged(showKeysoundToggle.isOn);
        EditorContext.UndoneOrRedone += Refresh;
        NoteInEditor.LeftClicked += OnNoteObjectLeftClick;
        NoteInEditor.RightClicked += OnNoteObjectRightClick;
        NoteInEditor.BeginDrag += OnNoteObjectBeginDrag;
        NoteInEditor.Drag += OnNoteObjectDrag;
        NoteInEditor.EndDrag += OnNoteObjectEndDrag;
        KeysoundSideSheet.selectedKeysoundsUpdated += OnSelectedKeysoundsUpdated;
    }

    private void OnDisable()
    {
        StopPlayback();
        EditorContext.UndoneOrRedone -= Refresh;
        NoteInEditor.LeftClicked -= OnNoteObjectLeftClick;
        NoteInEditor.RightClicked -= OnNoteObjectRightClick;
        NoteInEditor.BeginDrag -= OnNoteObjectBeginDrag;
        NoteInEditor.Drag -= OnNoteObjectDrag;
        NoteInEditor.EndDrag -= OnNoteObjectEndDrag;
        KeysoundSideSheet.selectedKeysoundsUpdated -= OnSelectedKeysoundsUpdated;
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlaying)
        {
            UpdatePlayback();
        }
        if (messageDialog.gameObject.activeSelf ||
            bpmEventDialog.gameObject.activeSelf ||
            shortcutDialog.gameObject.activeSelf)
        {
            return;
        }

        bool mouseInWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            workspace.GetComponent<RectTransform>(),
            Input.mousePosition);
        bool mouseInHeader = RectTransformUtility.RectangleContainsScreenPoint(
            header, Input.mousePosition);
        if (Input.mouseScrollDelta.y != 0)
        {
            HandleMouseScroll(Input.mouseScrollDelta.y,
                mouseInWorkspace);
        }

        if (mouseInWorkspace && !mouseInHeader)
        {
            noteCursor.gameObject.SetActive(true);
            SnapNoteCursor();
        }
        else
        {
            noteCursor.gameObject.SetActive(false);
        }

        if (Input.GetMouseButton(0) &&
            mouseInWorkspace &&
            mouseInHeader &&
            !isPlaying)
        {
            MoveScanlineToMouse();
        }

        HandleKeyboardShortcuts();
    }
    #endregion

    #region Mouse and Keyboard Update
    private void HandleMouseScroll(float y, bool mouseInWorkspace)
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);

        // Is the cursor inside the workspace?
        if (mouseInWorkspace && !alt)
        {
            if (ctrl)
            {
                // Adjust zoom
                zoom += Mathf.FloorToInt(y * 5f);
                zoom = Mathf.Clamp(zoom, 10, 500);
                float horizontal = workspace.horizontalNormalizedPosition;
                ResizeWorkspace();
                RepositionNeeded?.Invoke();
                workspace.horizontalNormalizedPosition = horizontal;
            }
            else
            {
                // Scroll workspace
                workspace.horizontalNormalizedPosition += y * 100f / WorkspaceContentWidth;
                workspace.horizontalNormalizedPosition =
                    Mathf.Clamp01(workspace.horizontalNormalizedPosition);
            }
        }

        // Alt+scroll to change beat snap divisor
        if (alt)
        {
            OnBeatSnapDivisorChanged(y < 0f ? -1 : 1);
        }
    }

    private void SnapNoteCursor()
    {
        Vector2 pointInContainer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            noteContainer.GetComponent<RectTransform>(),
            Input.mousePosition,
            cam: null,
            out pointInContainer);

        int bps = EditorContext.Pattern.patternMetadata.bps;
        float cursorScan = pointInContainer.x / ScanWidth;
        float cursorPulse = cursorScan * bps * Pattern.pulsesPerBeat;
        int snappedCursorPulse = SnapPulse(cursorPulse);

        int snappedLane = Mathf.FloorToInt(-pointInContainer.y / LaneHeight);

        noteCursor.note = new Note();
        noteCursor.note.pulse = snappedCursorPulse;
        noteCursor.note.lane = snappedLane;
        noteCursor.GetComponent<SelfPositioner>().Reposition();
    }

    private void MoveScanlineToMouse()
    {
        Vector2 pointInHeader;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            header, Input.mousePosition,
            cam: null, out pointInHeader);

        int bps = EditorContext.Pattern.patternMetadata.bps;
        float cursorScan = pointInHeader.x / ScanWidth;
        float cursorPulse = cursorScan * bps * Pattern.pulsesPerBeat;
        int snappedCursorPulse = SnapPulse(cursorPulse);

        scanline.floatPulse = snappedCursorPulse;
        scanline.GetComponent<SelfPositioner>().Reposition();
        RefreshScanlinePositionSlider();
    }

    private void HandleKeyboardShortcuts()
    {
        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                SelectAll();
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                CutSelection();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                CopySelection();
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                PasteAtScanline();
            }
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteSelection();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying)
            {
                StopPlayback();
            }
            else
            {
                StartPlayback();
            }
        }
    }
    #endregion

    #region Events From Workspace
    public void OnNoteContainerClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        if ((eventData as PointerEventData).button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }
        if (!noteCursor.gameObject.activeSelf) return;
        if (sortedNoteObjects.HasAt(
            noteCursor.note.pulse, noteCursor.note.lane))
        {
            return;
        }
        if (isPlaying) return;

        // Add note to pattern
        string sound = keysoundSheet.UpcomingKeysound();
        keysoundSheet.AdvanceUpcoming();
        Note n = new Note();
        n.pulse = noteCursor.note.pulse;
        n.lane = noteCursor.note.lane;
        n.type = NoteType.Basic;
        EditorContext.PrepareForChange();
        EditorContext.Pattern.AddNote(n, sound);
        EditorContext.DoneWithChange();

        // Add note to UI
        SpawnNoteObject(n, sound);
        UpdateNumScansAndRelatedUI();
    }

    public void OnNoteContainerDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData p = eventData as PointerEventData;
        if (p.button != PointerEventData.InputButton.Middle) return;

        float viewPortWidth = workspace.GetComponent<RectTransform>().rect.width;
        if (WorkspaceContentWidth < viewPortWidth) return;
        float horizontal = workspace.horizontalNormalizedPosition *
            (WorkspaceContentWidth - viewPortWidth);
        horizontal -= p.delta.x;
        workspace.horizontalNormalizedPosition = Mathf.Clamp01(
            horizontal /
            (WorkspaceContentWidth - viewPortWidth));
    }

    public void OnNoteObjectLeftClick(GameObject o)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        if (shift)
        {
            if (lastSelectedNoteObjectWithoutShift == null)
            {
                lastSelectedNoteObjectWithoutShift = sortedNoteObjects.GetFirst();
            }
            List<GameObject> range = sortedNoteObjects.GetRange(
                    lastSelectedNoteObjectWithoutShift, o);
            if (ctrl)
            {
                // Add [prev, o] to current selection.
                foreach (GameObject oInRange in range)
                {
                    selectedNoteObjects.Add(oInRange);
                }
            }
            else  // !ctrl
            {
                // Overwrite current selection with [prev, o].
                selectedNoteObjects.Clear();
                foreach (GameObject oInRange in range)
                {
                    selectedNoteObjects.Add(oInRange);
                }
            }
        }
        else  // !shift
        {
            lastSelectedNoteObjectWithoutShift = o;
            if (ctrl)
            {
                // Toggle o in current selection.
                ToggleSelection(o);
            }
            else  // !ctrl
            {
                if (selectedNoteObjects.Count > 1)
                {
                    selectedNoteObjects.Clear();
                    selectedNoteObjects.Add(o);
                }
                else if (selectedNoteObjects.Count == 1)
                {
                    if (selectedNoteObjects.Contains(o))
                    {
                        selectedNoteObjects.Remove(o);
                    }
                    else
                    {
                        selectedNoteObjects.Clear();
                        selectedNoteObjects.Add(o);
                    }
                }
                else  // Count == 0
                {
                    selectedNoteObjects.Add(o);
                }
            }
        }

        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    private void ToggleSelection(GameObject o)
    {
        if (selectedNoteObjects.Contains(o))
        {
            selectedNoteObjects.Remove(o);
        }
        else
        {
            selectedNoteObjects.Add(o);
        }
    }

    public void OnNoteObjectRightClick(GameObject o)
    {
        if (isPlaying) return;

        // Delete note from pattern
        NoteObject n = o.GetComponent<NoteObject>();
        EditorContext.PrepareForChange();
        EditorContext.Pattern.DeleteNote(n.note, n.sound);
        EditorContext.DoneWithChange();

        // Delete note from UI
        sortedNoteObjects.Delete(o);
        if (lastSelectedNoteObjectWithoutShift == o)
        {
            lastSelectedNoteObjectWithoutShift = null;
        }
        selectedNoteObjects.Remove(o);
        Destroy(o);
        UpdateNumScansAndRelatedUI();
    }
    #endregion

    #region UI Events And Updates
    public void OnBeatSnapDivisorChanged(int direction)
    {
        do
        {
            beatSnapDivisor += direction;
            if (beatSnapDivisor <= 0 && direction < 0)
            {
                beatSnapDivisor = Pattern.pulsesPerBeat;
            }
            if (beatSnapDivisor > Pattern.pulsesPerBeat && direction > 0)
            {
                beatSnapDivisor = 1;
            }
        }
        while (Pattern.pulsesPerBeat % beatSnapDivisor != 0);
        UpdateBeatSnapDivisorDisplay();
    }

    private void UpdateBeatSnapDivisorDisplay()
    {
        beatSnapDividerDisplay.text = beatSnapDivisor.ToString();
    }

    public void OnBpmEventButtonClick()
    {
        int scanlineIntPulse = (int)scanline.floatPulse;
        BpmEvent currentEvent = EditorContext.Pattern.bpmEvents.
            Find((BpmEvent e) =>
        {
            return e.pulse == scanlineIntPulse;
        });
        bpmEventDialog.Show(currentEvent, (double? newBpm) =>
        {
            if (currentEvent == null && newBpm == null)
            {
                // No change.
                return;
            }
            if (currentEvent != null && newBpm.HasValue &&
                currentEvent.bpm == newBpm.Value)
            {
                // No change.
                return;
            }

            EditorContext.PrepareForChange();
            // Delete event.
            EditorContext.Pattern.bpmEvents.RemoveAll((BpmEvent e) =>
            {
                return e.pulse == scanlineIntPulse;
            });
            // Add event if there is one.
            if (newBpm.HasValue)
            {
                EditorContext.Pattern.bpmEvents.Add(new BpmEvent()
                {
                    pulse = scanlineIntPulse,
                    bpm = newBpm.Value
                });
            }
            EditorContext.DoneWithChange();

            DestroyAndRespawnAllMarkers();
            RepositionNeeded?.Invoke();
        });
    }

    public void OnHiddenLaneNumberChanged(int newValue)
    {
        HiddenLanes = newValue * 4;

        // Update background
        hiddenLaneBackground.anchorMin = Vector2.zero;
        hiddenLaneBackground.anchorMax = new Vector2(
            1f, (float)HiddenLanes / TotalLanes);

        // Update lane dividers
        for (int i = 0; i < laneDividerParent.childCount; i++)
        {
            laneDividerParent.GetChild(i).gameObject.SetActive(
                i < TotalLanes);
        }

        RepositionNeeded?.Invoke();
    }

    public void OnShortcutButtonClick()
    {
        shortcutDialog.FadeIn();
    }

    public void OnScanlinePositionSliderValueChanged(float newValue)
    {
        if (isPlaying) return;

        int totalPulses = numScans
            * EditorContext.Pattern.patternMetadata.bps
            * Pattern.pulsesPerBeat;
        float scanlineRawPulse = totalPulses * newValue;
        scanline.floatPulse = SnapPulse(scanlineRawPulse);
        scanline.GetComponent<SelfPositioner>().Reposition();
        ScrollScanlineIntoView();
    }

    public void OnKeysoundVisibilityChanged(bool visible)
    {
        KeysoundVisibilityChanged?.Invoke(visible);
    }

    private void OnSelectedKeysoundsUpdated(List<string> keysounds)
    {
        if (selectedNoteObjects == null ||
            selectedNoteObjects.Count == 0) return;
        if (!applyKeysoundToSelectionToggle.isOn) return;
        if (keysounds.Count == 0)
        {
            keysounds.Add("");
        }

        // Sort selected note objects, first by pulse, then by lane.
        List<NoteObject> sortedSelection = new List<NoteObject>();
        foreach (GameObject o in selectedNoteObjects)
        {
            sortedSelection.Add(o.GetComponent<NoteObject>());
        }
        sortedSelection.Sort((NoteObject e1, NoteObject e2) =>
        {
            Note n1 = e1.note;
            Note n2 = e2.note;
            if (n1.pulse != n2.pulse) return n1.pulse - n2.pulse;
            return n1.lane - n2.lane;
        });

        // Apply keysound.
        EditorContext.PrepareForChange();
        int keysoundIndex = 0;
        foreach (NoteObject n in sortedSelection)
        {
            EditorContext.Pattern.ModifyNoteKeysound(
                n.note, n.sound, keysounds[keysoundIndex]);
            n.sound = keysounds[keysoundIndex];
            n.GetComponent<NoteInEditor>().SetKeysoundText();
            n.GetComponent<NoteInEditor>().SetKeysoundVisibility(showKeysoundToggle.isOn);
            keysoundIndex = (keysoundIndex + 1) % keysounds.Count;
        }
        EditorContext.DoneWithChange();
    }
    #endregion

    #region Dragging And Dropping Notes
    private GameObject draggedNoteObject;
    private void OnNoteObjectBeginDrag(GameObject o)
    {
        if (isPlaying) return;

        draggedNoteObject = o;
        lastSelectedNoteObjectWithoutShift = o;
        if (!selectedNoteObjects.Contains(o))
        {
            selectedNoteObjects.Clear();
            selectedNoteObjects.Add(o);

            SelectionChanged?.Invoke(selectedNoteObjects);
        }
    }

    private void OnNoteObjectDrag(Vector2 delta)
    {
        if (isPlaying) return;

        foreach (GameObject o in selectedNoteObjects)
        {
            // This is only visual. Notes are only really moved
            // in OnNoteObjectEndDrag.
            o.GetComponent<RectTransform>().anchoredPosition += delta;
        }
    }

    private void OnNoteObjectEndDrag()
    {
        if (isPlaying) return;

        // Calculate delta pulse and delta lane
        NoteObject noteObject = draggedNoteObject.GetComponent<NoteObject>();
        int oldPulse = noteObject.note.pulse;
        int oldLane = noteObject.note.lane;
        int deltaPulse = noteCursor.note.pulse - oldPulse;
        int deltaLane = noteCursor.note.lane - oldLane;

        // Is the movement applicable to all notes?
        // Movement will fail if:
        // - there's collision with existing notes,
        // - notes would go out of playable & hidden lanes, or
        // - notes would have negative pulses.
        bool movable = true;
        foreach (GameObject o in selectedNoteObjects)
        {
            Note n = o.GetComponent<NoteObject>().note;
            int newPulse = n.pulse + deltaPulse;
            int newLane = n.lane + deltaLane;

            if (sortedNoteObjects.HasAt(newPulse, newLane))
            {
                GameObject collision = sortedNoteObjects.GetAt(newPulse, newLane);
                if (!selectedNoteObjects.Contains(collision))
                {
                    movable = false;
                    break;
                }
            }
            if (newPulse < 0)
            {
                movable = false;
                break;
            }
            if (newLane < 0 || newLane >= TotalLanes)
            {
                movable = false;
                break;
            }
        }

        if (movable)
        {
            // Apply move. We need to delete and respawn note
            // objects, because they may have been moved between
            // playable and hidden lanes.
            EditorContext.PrepareForChange();
            HashSet<GameObject> replacedSelection = new HashSet<GameObject>();
            foreach (GameObject o in selectedNoteObjects)
            {
                sortedNoteObjects.Delete(o);
            }
            foreach (GameObject o in selectedNoteObjects)
            {
                Note n = o.GetComponent<NoteObject>().note;
                n.pulse += deltaPulse;
                n.lane += deltaLane;
                GameObject newO = SpawnNoteObject(
                    n, o.GetComponent<NoteObject>().sound);
                Destroy(o);
                replacedSelection.Add(newO);
            }
            EditorContext.DoneWithChange();
            selectedNoteObjects = replacedSelection;
            SelectionChanged?.Invoke(selectedNoteObjects);

            // Add scans if needed.
            UpdateNumScansAndRelatedUI();
        }

        foreach (GameObject o in selectedNoteObjects)
        {
            o.GetComponent<SelfPositioner>().Reposition();
        }
    }
    #endregion

    #region Refreshing
    private void Refresh()
    {
        DestroyAndRespawnExistingNotes();
        UpdateNumScans();
        DestroyAndRespawnAllMarkers();
        ResizeWorkspace();
        RefreshScanlinePositionSlider();
    }

    // Returns whether the number changed.
    private bool UpdateNumScans()
    {
        int numScansBackup = numScans;

        GameObject o = sortedNoteObjects.GetLast();
        if (o == null)
        {
            numScans = 1;
            return numScans != numScansBackup;
        }

        int lastPulse = o.GetComponent<NoteObject>().note.pulse;
        int lastScan = lastPulse / Pattern.pulsesPerBeat
            / EditorContext.Pattern.patternMetadata.bps;
        numScans = lastScan + 2;  // 1 empty scan at the end

        return numScans != numScansBackup;
    }

    private void UpdateNumScansAndRelatedUI()
    {
        if (UpdateNumScans())
        {
            DestroyAndRespawnAllMarkers();
            ResizeWorkspace();
        }
    }

    private void ResizeWorkspace()
    {
        workspaceContent.sizeDelta = new Vector2(
            WorkspaceContentWidth,
            workspaceContent.sizeDelta.y);
        workspace.horizontalNormalizedPosition =
                Mathf.Clamp01(workspace.horizontalNormalizedPosition);
    }

    private void RefreshScanlinePositionSlider()
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;
        float scanlineNormalizedPosition = scanline.floatPulse /
            (numScans * bps * Pattern.pulsesPerBeat);
       
        scanlinePositionSlider.SetValueWithoutNotify(scanlineNormalizedPosition);
    }
    #endregion

    #region Spawning
    private enum MarkerPriority
    {
        Bpm,
        Other
    }

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

        EditorContext.Pattern.PrepareForTimeCalculation();
        int bps = EditorContext.Pattern.patternMetadata.bps;
        // Value in KeyValuePairs is priority: 1 for BPM events, 0 for others.
        List<KeyValuePair<Transform, MarkerPriority>> allMarkers =
            new List<KeyValuePair<Transform, MarkerPriority>>();
        for (int scan = 0; scan < numScans; scan++)
        {
            GameObject marker = Instantiate(scanMarkerTemplate, markerContainer);
            marker.SetActive(true);  // This calls OnEnabled
            Marker m = marker.GetComponent<Marker>();
            m.pulse = scan * bps * Pattern.pulsesPerBeat;
            m.SetTimeDisplay();
            m.GetComponent<SelfPositioner>().Reposition();
            allMarkers.Add(new KeyValuePair<Transform, MarkerPriority>(
                marker.transform, MarkerPriority.Other));

            for (int beat = 1; beat < bps; beat++)
            {
                marker = Instantiate(beatMarkerTemplate, markerContainer);
                marker.SetActive(true);
                m = marker.GetComponent<Marker>();
                m.pulse = (scan * bps + beat) * Pattern.pulsesPerBeat;
                m.SetTimeDisplay();
                m.GetComponent<SelfPositioner>().Reposition();
                allMarkers.Add(new KeyValuePair<Transform, MarkerPriority>(
                marker.transform, MarkerPriority.Other));
            }
        }

        foreach (BpmEvent e in EditorContext.Pattern.bpmEvents)
        {
            GameObject marker = Instantiate(bpmMarkerTemplate, markerContainer);
            marker.SetActive(true);
            Marker m = marker.GetComponent<Marker>();
            m.pulse = e.pulse;
            m.SetBpmText(e.bpm);
            m.GetComponent<SelfPositioner>().Reposition();
            allMarkers.Add(new KeyValuePair<Transform, MarkerPriority>(
                marker.transform, MarkerPriority.Bpm));
        }

        // Sort all markers so they are drawn from left to right.
        allMarkers.Sort((
            KeyValuePair<Transform, MarkerPriority> p1,
            KeyValuePair<Transform, MarkerPriority> p2) =>
        {
            float deltaX = p1.Key.position.x - p2.Key.position.x;
            if (deltaX < 0) return -1;
            if (deltaX > 0) return 1;
            // At the same position, BPM markers should be drawn later.
            if (p1.Value == MarkerPriority.Bpm) return 1;
            if (p2.Value == MarkerPriority.Bpm) return -1;
            return 0;
        });
        for (int i = 0; i < allMarkers.Count; i++)
        {
            allMarkers[i].Key.SetSiblingIndex(i);
        }
    }

    private GameObject SpawnNoteObject(Note n, string sound)
    {
        GameObject prefab = basicNotePrefab;
        if (n.lane >= PlayableLanes)
        {
            prefab = hiddenNotePrefab;
        }
        NoteObject noteObject = Instantiate(prefab,
            noteContainer).GetComponent<NoteObject>();
        noteObject.note = n;
        noteObject.sound = sound;
        noteObject.GetComponent<NoteInEditor>().SetKeysoundText();
        noteObject.GetComponent<NoteInEditor>().SetKeysoundVisibility(showKeysoundToggle.isOn);
        noteObject.GetComponent<SelfPositioner>().Reposition();

        sortedNoteObjects.Add(noteObject.gameObject);

        // Binary search the appropriate sibling index of
        // new note, so all notes are drawn from right to left.
        //
        // More specifically, we are looking for the smallest-index
        // sibling that's located on the left of the new note.
        if (noteContainer.childCount == 1)
        {
            return noteObject.gameObject;
        }
        float targetX = noteObject.transform.position.x;
        int first = 0;
        int last = noteContainer.childCount - 2;
        while (true)
        {
            float firstX = noteContainer.GetChild(first).position.x;
            float lastX = noteContainer.GetChild(last).position.x;
            if (firstX <= targetX)
            {
                noteObject.transform.SetSiblingIndex(first);
                break;
            }
            if (lastX >= targetX)
            {
                noteObject.transform.SetSiblingIndex(last + 1);
                break;
            }
            // Now we know for sure that lastX < targetX < firstX.
            int middle = (first + last) / 2;
            float middleX = noteContainer.GetChild(middle).position.x;
            if (middleX == targetX)
            {
                noteObject.transform.SetSiblingIndex(middle);
                break;
            }
            if (middleX < targetX)
            {
                last = middle - 1;
            }
            else  // middleX > targetX
            {
                first = middle + 1;
            }
        }

        return noteObject.gameObject;
    }

    private void DestroyAndRespawnExistingNotes()
    {
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            Destroy(noteContainer.GetChild(i).gameObject);
        }
        sortedNoteObjects = new SortedNoteObjects();
        lastSelectedNoteObjectWithoutShift = null;
        selectedNoteObjects = new HashSet<GameObject>();

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

    #region Selection And Clipboard
    public void SelectAll()
    {
        selectedNoteObjects.Clear();
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            selectedNoteObjects.Add(noteContainer.GetChild(i).gameObject);
        }
        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    public void CutSelection()
    {
        if (selectedNoteObjects.Count == 0) return;

        CopySelection();
        DeleteSelection();
    }

    public void CopySelection()
    {
        if (selectedNoteObjects.Count == 0) return;

        if (clipboard == null)
        {
            clipboard = new List<NoteWithSound>();
        }
        clipboard.Clear();
        minPulseInClipboard = int.MaxValue;
        foreach (GameObject o in selectedNoteObjects)
        {
            NoteWithSound n = NoteWithSound.FromNoteObject(o);
            if (n.note.pulse < minPulseInClipboard)
            {
                minPulseInClipboard = n.note.pulse;
            }
            clipboard.Add(n);
        }
    }

    public void PasteAtScanline()
    {
        if (clipboard == null) return;
        if (clipboard.Count == 0) return;
        if (isPlaying) return;

        int scanlinePulse = (int)scanline.floatPulse;
        int deltaPulse = scanlinePulse - minPulseInClipboard;

        // Does the paste conflict with any existing note?
        foreach (NoteWithSound n in clipboard)
        {
            int newPulse = n.note.pulse + deltaPulse;
            if (sortedNoteObjects.HasAt(newPulse, n.note.lane))
            {
                messageDialog.Show("Cannot paste here because some pasted notes would overwrite existing notes.");
                return;
            }
        }

        // OK to paste. Add scans if needed.
        UpdateNumScansAndRelatedUI();
        RepositionNeeded?.Invoke();

        // Paste.
        EditorContext.PrepareForChange();
        foreach (NoteWithSound n in clipboard)
        {
            Note noteClone = n.note.Clone();
            noteClone.pulse += deltaPulse;

            // Add note to pattern.
            EditorContext.Pattern.AddNote(noteClone, n.sound);

            // Add note to UI.
            SpawnNoteObject(noteClone, n.sound);
        }
        EditorContext.DoneWithChange();
    }

    public void DeleteSelection()
    {
        if (selectedNoteObjects.Count == 0) return;
        if (isPlaying) return;

        // Delete notes from pattern.
        EditorContext.PrepareForChange();
        foreach (GameObject o in selectedNoteObjects)
        {
            NoteObject e = o.GetComponent<NoteObject>();
            EditorContext.Pattern.DeleteNote(e.note, e.sound);
        }
        EditorContext.DoneWithChange();

        // Delete notes from UI.
        foreach (GameObject o in selectedNoteObjects)
        {
            sortedNoteObjects.Delete(o);
            Destroy(o);
        }
        lastSelectedNoteObjectWithoutShift = null;
        selectedNoteObjects.Clear();
        UpdateNumScansAndRelatedUI();
    }
    #endregion

    #region Playback
    // During playback, the following features are disabled:
    // - Adding or deleting notes, including by clicking, dragging
    //   and cut/copy/paste
    // - Applying note types and/or keysounds to selection, if
    //   specified in options (TODO: implement)
    // - Moving the scanline, including by clicking the header
    //   and dragging the scanline position slider.
    private bool isPlaying;
    private float playbackStartingPulse;
    private float playbackStartingTime;
    private DateTime systemTimeOnPlaybackStart;
    private List<Queue<NoteWithSound>> notesInLanes;

    private void OnResourceLoadComplete(string error)
    {
        if (error == null)
        {
            playButton.GetComponent<Button>().interactable = true;
        }
        else
        {
            messageDialog.Show(error + "\n\n" +
                "You can continue to edit this pattern, but playback and preview will be disabled.");
        }
    }

    private void UpdateUIOnPlaybackStartOrStop()
    {
        playButton.SetActive(!isPlaying);
        stopButton.SetActive(isPlaying);
        scanlinePositionSlider.interactable = !isPlaying;
    }

    public void StartPlayback()
    {
        if (isPlaying) return;
        if (!playButton.GetComponent<Button>().interactable) return;
        isPlaying = true;
        UpdateUIOnPlaybackStartOrStop();

        Pattern pattern = EditorContext.Pattern;
        pattern.PrepareForTimeCalculation();
        pattern.CalculateTimeOfAllNotes();
        playbackStartingPulse = scanline.floatPulse;
        playbackStartingTime = pattern.PulseToTime((int)playbackStartingPulse);

        // Put notes into queues, each corresponding to a lane.
        // Use MaxTotalLanes instead of TotalLanes, so that, for
        // example, when user sets hidden lanes to 4, lanes
        // 8~11 are still played.
        notesInLanes = new List<Queue<NoteWithSound>>();
        for (int i = 0; i < MaxTotalLanes; i++)
        {
            notesInLanes.Add(new Queue<NoteWithSound>());
        }
        for (int pulse = (int)playbackStartingPulse;
            pulse <= sortedNoteObjects.GetMaxPulse();
            pulse++)
        {
            List<GameObject> noteObjectsAtThisPulse =
                sortedNoteObjects.GetAt(pulse);
            if (noteObjectsAtThisPulse == null) continue;
            foreach (GameObject o in noteObjectsAtThisPulse)
            {
                NoteObject n = o.GetComponent<NoteObject>();
                notesInLanes[n.note.lane].Enqueue(NoteWithSound.FromNoteObject(o));
            }
        }

        systemTimeOnPlaybackStart = DateTime.Now;
        PlaySound(backingTrackSource,
            ResourceLoader.GetCachedClip(
                pattern.patternMetadata.backingTrack),
            playbackStartingTime);
    }

    public void StopPlayback()
    {
        if (!isPlaying) return;
        if (!stopButton.GetComponent<Button>().interactable) return;
        isPlaying = false;
        UpdateUIOnPlaybackStartOrStop();

        backingTrackSource.Stop();
        scanline.floatPulse = playbackStartingPulse;
        scanline.GetComponent<SelfPositioner>().Reposition();
        ScrollScanlineIntoView();
        RefreshScanlinePositionSlider();
    }

    public void UpdatePlayback()
    {
        // Calculate time.
        float elapsedTime = (float)(DateTime.Now - systemTimeOnPlaybackStart).TotalSeconds;
        float playbackCurrentTime = playbackStartingTime + elapsedTime;
        float playbackCurrentPulse = EditorContext.Pattern.TimeToPulse(playbackCurrentTime);

        // Stop playback after the last scan.
        int totalPulses = numScans * EditorContext.Pattern.patternMetadata.bps * Pattern.pulsesPerBeat;
        if (playbackCurrentPulse > totalPulses)
        {
            StopPlayback();
            return;
        }

        // Debug.Log($"frame: {Time.frameCount} time: {time} timeFromSamples: {timeFromSamples} systemTime: {systemTime} unityTime: {unityTime} pulse: {pulse}");

        // Play keysounds if it's their time.
        for (int i = 0; i < notesInLanes.Count; i++)
        {
            if (notesInLanes[i].Count == 0) continue;
            NoteWithSound nextNote = notesInLanes[i].Peek();
            if (playbackCurrentTime >= nextNote.note.time)
            {
                AudioClip clip = ResourceLoader.GetCachedClip(nextNote.sound);
                AudioSource source = keysoundSources[i];
                float startTime = playbackCurrentTime - nextNote.note.time;
                PlaySound(source, clip, startTime);

                notesInLanes[i].Dequeue();
            }
        }

        // Move scanline.
        scanline.floatPulse = playbackCurrentPulse;
        scanline.GetComponent<SelfPositioner>().Reposition();
        ScrollScanlineIntoView();
        RefreshScanlinePositionSlider();
    }

    private void PlaySound(AudioSource source, AudioClip clip, float startTime)
    {
        if (clip == null) return;

        int startSample = Mathf.FloorToInt(startTime * clip.frequency);
        source.clip = clip;
        source.timeSamples = startSample;
        source.Play();
    }
    #endregion

    #region Utilities
    private int SnapPulse(float rawPulse)
    {
        int pulsesPerDivision = Pattern.pulsesPerBeat / beatSnapDivisor;
        int snappedPulse = Mathf.RoundToInt(rawPulse / pulsesPerDivision)
            * pulsesPerDivision;
        return snappedPulse;
    }

    private void ScrollScanlineIntoView()
    {
        float viewPortWidth = workspace.GetComponent<RectTransform>().rect.width;
        if (WorkspaceContentWidth <= viewPortWidth) return;

        float scanlinePosition = scanline.GetComponent<RectTransform>().anchoredPosition.x;

        float xAtViewPortLeft = (WorkspaceContentWidth - viewPortWidth)
            * workspace.horizontalNormalizedPosition;
        float xAtViewPortRight = xAtViewPortLeft + viewPortWidth;

        if (scanlinePosition >= xAtViewPortLeft &&
            scanlinePosition < xAtViewPortRight)
        {
            // No need to scroll.
            return;
        }

        float desiredXAtLeft;
        if (scanlinePosition < xAtViewPortLeft)
        {
            // Scrolling left: put scanline at right side.
            desiredXAtLeft = scanlinePosition - viewPortWidth * 0.8f;
        }
        else
        {
            // Scrolling right: put scanline at left side.
            desiredXAtLeft = scanlinePosition - viewPortWidth * 0.2f;
        }
        float normalizedPosition =
            desiredXAtLeft / (WorkspaceContentWidth - viewPortWidth);
        workspace.horizontalNormalizedPosition =
            Mathf.Clamp01(normalizedPosition);
    }
    #endregion
}
