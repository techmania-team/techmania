using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PatternPanel : MonoBehaviour
{
    public RectTransform rootCanvas;

    [Header("Workspace")]
    public ScrollRect workspaceScrollRect;
    public ScrollRect headerScrollRect;
    public RectTransform workspaceContent;
    public RectTransform headerContent;
    public ScanlineInEditor scanline;

    [Header("Lanes")]
    public RectTransform hiddenLaneBackground;
    public RectTransform header;
    public RectTransform laneDividerParent;

    [Header("Markers")]
    public Transform markerInHeaderContainer;
    public GameObject scanMarkerInHeaderTemplate;
    public GameObject beatMarkerInHeaderTemplate;
    public GameObject bpmMarkerTemplate;
    public Transform markerContainer;
    public GameObject scanMarkerTemplate;
    public GameObject beatMarkerTemplate;

    [Header("Notes")]
    public Transform noteContainer;
    public Transform noteCemetary;
    public NoteObject noteCursor;
    public GameObject basicNotePrefab;
    public GameObject chainHeadPrefab;
    public GameObject chainNodePrefab;
    public GameObject repeatHeadPrefab;
    public GameObject repeatHeadHoldPrefab;
    public GameObject repeatNotePrefab;
    public GameObject repeatHoldPrefab;
    public GameObject holdNotePrefab;
    public GameObject dragNotePrefab;

    [Header("Audio")]
    public AudioSourceManager audioSourceManager;

    [Header("Options")]
    public TextMeshProUGUI beatSnapDividerDisplay;
    public TMP_Dropdown hiddenLanesDropdown;
    public Toggle applyNoteTypeToSelectionToggle;
    public Toggle applyKeysoundToSelectionToggle;
    public Toggle showKeysoundToggle;

    [Header("UI")]
    public List<NoteTypeButton> noteTypeButtons;
    public KeysoundSideSheet keysoundSheet;
    public GameObject playButton;
    public GameObject stopButton;
    public GameObject audioLoadingIndicator;
    public Slider scanlinePositionSlider;
    public Snackbar snackbar;
    public MessageDialog messageDialog;
    public BpmEventDialog bpmEventDialog;
    public Dialog shortcutDialog;

    #region Internal Data Structures
    // Each NoteObject contains a reference to a Note, and this
    // dictionary is the reverse of that. Must be updated alongside
    // EditorContext.Pattern at all times.
    private Dictionary<Note, NoteObject> noteToNoteObject;

    private Note lastSelectedNoteWithoutShift;
    private HashSet<GameObject> selectedNoteObjects;

    private Note GetNoteFromGameObject(GameObject o)
    {
        return o.GetComponent<NoteObject>().note;
    }
    
    private GameObject GetGameObjectFromNote(Note n)
    {
        return noteToNoteObject[n].gameObject;
    }

    // Clipboard stores notes instead of GameObjects,
    // so we are free of Unity stuff such as MonoBehaviors and
    // Instantiating.
    //
    // The clipboard is intentionally not initialized in OnEnabled,
    // so it is preserved between editing sessions, and across
    // patterns.
    private List<Note> clipboard;
    private int minPulseInClipboard;

    private NoteType noteType;
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
    public static float PulseWidth
    {
        get
        {
            return ScanWidth /
                EditorContext.Pattern.patternMetadata.bps /
                Pattern.pulsesPerBeat;
        }
    }
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
        numScans = 0;  // Will be updated in Refresh()
        zoom = 100;
        beatSnapDivisor = 2;

        // Scanline
        scanline.floatPulse = 0f;
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        workspaceScrollRect.horizontalNormalizedPosition = 0f;
        headerScrollRect.horizontalNormalizedPosition = 0f;
        scanlinePositionSlider.SetValueWithoutNotify(0f);

        // UI and options
        noteType = NoteType.Basic;
        UpdateNoteTypeButtons();
        UpdateBeatSnapDivisorDisplay();
        keysoundSheet.Initialize();

        // Playback
        audioLoaded = false;
        isPlaying = false;
        UpdatePlaybackUI();
        ResourceLoader.CacheAudioResources(
            EditorContext.trackFolder,
            cacheAudioCompleteCallback: OnResourceLoadComplete);

        Refresh();
        OnKeysoundVisibilityChanged(showKeysoundToggle.isOn);
        EditorContext.UndoneOrRedone += Refresh;
        NoteInEditor.LeftClicked += OnNoteObjectLeftClick;
        NoteInEditor.RightClicked += OnNoteObjectRightClick;
        NoteInEditor.BeginDrag += OnNoteObjectBeginDrag;
        NoteInEditor.Drag += OnNoteObjectDrag;
        NoteInEditor.EndDrag += OnNoteObjectEndDrag;
        NoteInEditor.DurationHandleBeginDrag += OnDurationHandleBeginDrag;
        NoteInEditor.DurationHandleDrag += OnDurationHandleDrag;
        NoteInEditor.DurationHandleEndDrag += OnDurationHandleEndDrag;
        NoteInEditor.AnchorReceiverClicked += OnAnchorReceiverClicked;
        NoteInEditor.AnchorRightClicked += OnAnchorRightClicked;
        NoteInEditor.AnchorBeginDrag += OnAnchorBeginDrag;
        NoteInEditor.AnchorDrag += OnAnchorDrag;
        NoteInEditor.AnchorEndDrag += OnAnchorEndDrag;
        NoteInEditor.ControlPointRightClicked += OnControlPointRightClicked;
        NoteInEditor.ControlPointBeginDrag += OnControlPointBeginDrag;
        NoteInEditor.ControlPointDrag += OnControlPointDrag;
        NoteInEditor.ControlPointEndDrag += OnControlPointEndDrag;
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
        NoteInEditor.DurationHandleBeginDrag -= OnDurationHandleBeginDrag;
        NoteInEditor.DurationHandleDrag -= OnDurationHandleDrag;
        NoteInEditor.DurationHandleEndDrag -= OnDurationHandleEndDrag;
        NoteInEditor.AnchorReceiverClicked -= OnAnchorReceiverClicked;
        NoteInEditor.AnchorRightClicked -= OnAnchorRightClicked;
        NoteInEditor.AnchorBeginDrag -= OnAnchorBeginDrag;
        NoteInEditor.AnchorDrag -= OnAnchorDrag;
        NoteInEditor.AnchorEndDrag -= OnAnchorEndDrag;
        NoteInEditor.ControlPointRightClicked -= OnControlPointRightClicked;
        NoteInEditor.ControlPointBeginDrag -= OnControlPointBeginDrag;
        NoteInEditor.ControlPointDrag -= OnControlPointDrag;
        NoteInEditor.ControlPointEndDrag -= OnControlPointEndDrag;
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

        bool mouseInWorkspace = RectTransformUtility
            .RectangleContainsScreenPoint(
                workspaceScrollRect.GetComponent<RectTransform>(),
                Input.mousePosition);
        bool mouseInHeader = RectTransformUtility
            .RectangleContainsScreenPoint(
                header, Input.mousePosition);
        if (Input.mouseScrollDelta.y != 0)
        {
            HandleMouseScroll(Input.mouseScrollDelta.y,
                mouseInWorkspace || mouseInHeader);
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
            mouseInHeader &&
            !isPlaying)
        {
            MoveScanlineToMouse();
        }

        HandleKeyboardShortcuts();
    }
    #endregion

    #region Mouse and Keyboard Update
    private void HandleMouseScroll(float y,
        bool mouseInWorkspaceOrHeader)
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);

        // Is the cursor inside the workspace?
        if (mouseInWorkspaceOrHeader && !alt)
        {
            if (ctrl)
            {
                // Adjust zoom
                zoom += Mathf.FloorToInt(y * 5f);
                zoom = Mathf.Clamp(zoom, 10, 500);
                float horizontal = workspaceScrollRect
                    .horizontalNormalizedPosition;
                ResizeWorkspace();
                RepositionNeeded?.Invoke();
                AdjustAllPathsAndTrails();
                workspaceScrollRect.horizontalNormalizedPosition = 
                    horizontal;
            }
            else
            {
                // Scroll workspace
                workspaceScrollRect.horizontalNormalizedPosition +=
                    y * 100f / WorkspaceContentWidth;
                workspaceScrollRect.horizontalNormalizedPosition =
                    Mathf.Clamp01(
                    workspaceScrollRect.horizontalNormalizedPosition);
            }
            SynchronizeScrollRects();
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
        noteCursor.GetComponent<SelfPositionerInEditor>().Reposition();
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
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
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
    public void OnWorkspaceScrollRectValueChanged(
        Vector2 value)
    {
        SynchronizeScrollRects();
    }

    public void OnNoteContainerClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        if ((eventData as PointerEventData).button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }
        if (!noteCursor.gameObject.activeSelf) return;
        if (EditorContext.Pattern.HasNoteAt(
            noteCursor.note.pulse, noteCursor.note.lane))
        {
            return;
        }
        if (isPlaying) return;

        string invalidReason;
        // No need to call CanAddHoldNote or CanAddDragNote here
        // because durations and curves are flexible.
        if (!CanAddNote(noteType, noteCursor.note.pulse,
            noteCursor.note.lane, out invalidReason))
        {
            snackbar.Show(invalidReason);
            return;
        }

        string sound = keysoundSheet.UpcomingKeysound();
        keysoundSheet.AdvanceUpcoming();
        EditorContext.PrepareForChange();
        switch (noteType)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.RepeatHead:
            case NoteType.Repeat:
                AddNote(noteType, noteCursor.note.pulse,
                    noteCursor.note.lane, sound);
                break;
            case NoteType.Hold:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                AddHoldNote(noteType, noteCursor.note.pulse,
                    noteCursor.note.lane, duration: null, sound);
                break;
            case NoteType.Drag:
                AddDragNote(noteCursor.note.pulse,
                    noteCursor.note.lane,
                    nodes: null,
                    sound);
                break;
        }
        
        EditorContext.DoneWithChange();
    }

    public void OnNoteContainerDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData p = eventData as PointerEventData;
        if (p.button != PointerEventData.InputButton.Middle) return;

        float viewPortWidth = workspaceScrollRect
            .GetComponent<RectTransform>().rect.width;
        if (WorkspaceContentWidth < viewPortWidth) return;
        float horizontal = 
            workspaceScrollRect.horizontalNormalizedPosition *
            (WorkspaceContentWidth - viewPortWidth);
        horizontal -= p.delta.x / rootCanvas.localScale.x;
        workspaceScrollRect.horizontalNormalizedPosition = 
            Mathf.Clamp01(horizontal /
                (WorkspaceContentWidth - viewPortWidth));
        SynchronizeScrollRects();
    }

    public void OnNoteObjectLeftClick(GameObject o)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        Note clickedNote = GetNoteFromGameObject(o);
        if (shift)
        {
            if (lastSelectedNoteWithoutShift == null)
            {
                lastSelectedNoteWithoutShift =
                    EditorContext.Pattern.notes.Min;
            }
            // At this point lastSelectedNoteObjectWithoutShift
            // might still be null.
            List<Note> range = EditorContext.Pattern
                .GetRangeBetween(
                lastSelectedNoteWithoutShift,
                clickedNote);
            if (ctrl)
            {
                // Add [prev, o] to current selection.
                foreach (Note oInRange in range)
                {
                    selectedNoteObjects.Add(
                        GetGameObjectFromNote(oInRange));
                }
            }
            else  // !ctrl
            {
                // Overwrite current selection with [prev, o].
                selectedNoteObjects.Clear();
                foreach (Note oInRange in range)
                {
                    selectedNoteObjects.Add(
                        GetGameObjectFromNote(oInRange));
                }
            }
        }
        else  // !shift
        {
            lastSelectedNoteWithoutShift = clickedNote;
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

        EditorContext.PrepareForChange();
        selectedNoteObjects.Remove(o);
        DeleteNote(o);
        EditorContext.DoneWithChange();
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
        AdjustAllPathsAndTrails();
    }

    public void OnNoteTypeButtonClick(NoteTypeButton clickedButton)
    {
        noteType = clickedButton.type;
        UpdateNoteTypeButtons();

        // Apply to selection if asked to.
        if (!applyNoteTypeToSelectionToggle.isOn) return;
        if (isPlaying) return;
        if (selectedNoteObjects.Count == 0) return;

        HashSet<GameObject> newSelection = new HashSet<GameObject>();
        EditorContext.PrepareForChange();
        foreach (GameObject o in selectedNoteObjects)
        {
            NoteObject n = o.GetComponent<NoteObject>();
            int pulse = n.note.pulse;
            int lane = n.note.lane;
            string sound = n.note.sound;

            GameObject newObject = null;
            string invalidReason = "";
            switch (noteType)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    if (!CanAddNote(noteType, pulse, lane,
                        ignoredExistingNotes: new HashSet<GameObject>() { o },
                        out invalidReason))
                    {
                        snackbar.Show(invalidReason);
                        break;
                    }
                    DeleteNote(o);
                    newObject = AddNote(noteType, pulse, lane, sound);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    // No need to call CanAddHoldNote because
                    // duration is flexible.
                    if (!CanAddNote(noteType, pulse, lane,
                        ignoredExistingNotes: new HashSet<GameObject>() { o },
                        out invalidReason))
                    {
                        snackbar.Show(invalidReason);
                        break;
                    }
                    DeleteNote(o);
                    newObject = AddHoldNote(noteType, pulse, lane,
                        duration: null, sound);
                    break;
                case NoteType.Drag:
                    // No need to call CanAddDragNote because
                    // curve is flexible.
                    if (!CanAddNote(noteType, pulse, lane,
                        ignoredExistingNotes: new HashSet<GameObject>() { o },
                        out invalidReason))
                    {
                        snackbar.Show(invalidReason);
                        break;
                    }
                    DeleteNote(o);
                    newObject = AddDragNote(pulse, lane,
                        nodes: null, sound);
                    break;
            }

            if (newObject != null)
            {
                newSelection.Add(newObject);
            }
            else
            {
                newSelection.Add(o);
            }
        }
        EditorContext.DoneWithChange();

        selectedNoteObjects = newSelection;
        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    private void UpdateNoteTypeButtons()
    {
        foreach (NoteTypeButton b in noteTypeButtons)
        {
            b.GetComponent<MaterialToggleButton>().SetIsOn(
                b.type == noteType);
        }
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
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
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
        if (isPlaying) return;
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
            n.note.sound = keysounds[keysoundIndex];
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
        lastSelectedNoteWithoutShift = GetNoteFromGameObject(o);
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
        delta /= rootCanvas.localScale.x;

        foreach (GameObject o in selectedNoteObjects)
        {
            // This is only visual. Notes are only really moved
            // in OnNoteObjectEndDrag.
            o.GetComponent<RectTransform>().anchoredPosition += delta;
            o.GetComponent<NoteInEditor>().KeepPathInPlaceWhileNoteBeingDragged(delta);
        }
    }

    private void OnNoteObjectEndDrag()
    {
        if (isPlaying) return;

        // Calculate delta pulse and delta lane.
        Note draggedNote = GetNoteFromGameObject(draggedNoteObject);
        int oldPulse = draggedNote.pulse;
        int oldLane = draggedNote.lane;
        int deltaPulse = noteCursor.note.pulse - oldPulse;
        int deltaLane = noteCursor.note.lane - oldLane;

        // Is the move valid?
        bool movable = true;
        string invalidReason = "";
        HashSet<GameObject> selectionAsSet =
            new HashSet<GameObject>(selectedNoteObjects);
        foreach (GameObject o in selectedNoteObjects)
        {
            Note n = o.GetComponent<NoteObject>().note;
            int newPulse = n.pulse + deltaPulse;
            int newLane = n.lane + deltaLane;

            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    movable = movable && CanAddNote(n.type,
                        newPulse, newLane,
                        ignoredExistingNotes: selectionAsSet,
                        out invalidReason);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    movable = movable && CanAddHoldNote(n.type,
                        newPulse, newLane, (n as HoldNote).duration,
                        ignoredExistingNotes: selectionAsSet,
                        out invalidReason);
                    break;
                case NoteType.Drag:
                    movable = movable && CanAddDragNote(
                        newPulse, newLane, (n as DragNote).nodes,
                        ignoredExistingNotes: selectionAsSet,
                        out invalidReason);
                    break;
            }

            if (!movable)
            {
                snackbar.Show(invalidReason);
                break;
            }
        }

        if (movable)
        {
            // Apply move. We need to delete and respawn note
            // objects, because they may have been moved between
            // playable and hidden lanes.
            EditorContext.PrepareForChange();
            HashSet<GameObject> replacedSelection =
                new HashSet<GameObject>();
            // These notes are not the ones added to the pattern.
            // They are created only to pass information to AddNote
            // methods.
            List<Note> movedNotes = new List<Note>();
            foreach (GameObject o in selectedNoteObjects)
            {
                NoteObject n = o.GetComponent<NoteObject>();
                Note movedNote = n.note.Clone();
                movedNote.pulse += deltaPulse;
                movedNote.lane += deltaLane;
                movedNotes.Add(movedNote);

                DeleteNote(o);
            }
            foreach (Note movedNote in movedNotes)
            {
                GameObject o = null;
                switch (movedNote.type)
                {
                    case NoteType.Basic:
                    case NoteType.ChainHead:
                    case NoteType.ChainNode:
                    case NoteType.RepeatHead:
                    case NoteType.Repeat:
                        o = AddNote(movedNote.type,
                            movedNote.pulse,
                            movedNote.lane,
                            movedNote.sound);
                        break;
                    case NoteType.Hold:
                    case NoteType.RepeatHeadHold:
                    case NoteType.RepeatHold:
                        o = AddHoldNote(movedNote.type,
                            movedNote.pulse,
                            movedNote.lane,
                            (movedNote as HoldNote).duration,
                            movedNote.sound);
                        break;
                    case NoteType.Drag:
                        o = AddDragNote(
                            movedNote.pulse,
                            movedNote.lane,
                            (movedNote as DragNote).nodes,
                            movedNote.sound);
                        break;
                }
                replacedSelection.Add(o);
                if (movedNote.pulse == oldPulse + deltaPulse &&
                    movedNote.lane == oldLane + deltaLane)
                {
                    lastSelectedNoteWithoutShift =
                        GetNoteFromGameObject(o);
                }
            }
            EditorContext.DoneWithChange();
            selectedNoteObjects = replacedSelection;
            SelectionChanged?.Invoke(selectedNoteObjects);
        }

        foreach (GameObject o in selectedNoteObjects)
        {
            o.GetComponent<SelfPositionerInEditor>().Reposition();
            o.GetComponent<NoteInEditor>().ResetPathPosition();
        }
    }
    #endregion

    #region Hold Note Duration Adjustment
    private List<GameObject> holdNotesBeingAdjusted;
    private GameObject initialHoldNoteBeingAdjusted;
    private void OnDurationHandleBeginDrag(GameObject note)
    {
        if (isPlaying) return;

        holdNotesBeingAdjusted = new List<GameObject>();
        if (selectedNoteObjects.Contains(note))
        {
            // Adjust all hold notes in the selection.
            foreach (GameObject o in selectedNoteObjects)
            {
                NoteType noteType = o.GetComponent<NoteObject>().note.type;
                if (noteType == NoteType.Hold ||
                    noteType == NoteType.RepeatHeadHold ||
                    noteType == NoteType.RepeatHold)
                {
                    holdNotesBeingAdjusted.Add(o);
                }
            }
        }
        else
        {
            // Adjust only the dragged note and ignore selection.
            holdNotesBeingAdjusted.Add(note);
        }
        initialHoldNoteBeingAdjusted = note;

        foreach (GameObject o in holdNotesBeingAdjusted)
        {
            o.GetComponent<NoteInEditor>().RecordTrailActualLength();
        }
    }

    private void OnDurationHandleDrag(float delta)
    {
        if (isPlaying) return;
        delta /= rootCanvas.localScale.x;

        foreach (GameObject o in holdNotesBeingAdjusted)
        {
            // This is only visual; duration is only really changed
            // in OnDurationHandleEndDrag.
            o.GetComponent<NoteInEditor>().AdjustTrailLength(delta);
        }
    }

    private void OnDurationHandleEndDrag()
    {
        if (isPlaying) return;

        int oldDuration = (initialHoldNoteBeingAdjusted.
            GetComponent<NoteObject>().note as HoldNote).duration;
        int newDuration = noteCursor.note.pulse -
            initialHoldNoteBeingAdjusted.GetComponent<NoteObject>().
            note.pulse;
        int deltaDuration = newDuration - oldDuration;

        // Is the adjustment valid?
        bool adjustable = true;
        foreach (GameObject o in holdNotesBeingAdjusted)
        {
            HoldNote holdNote = o.GetComponent<NoteObject>().note as HoldNote;
            oldDuration = holdNote.duration;
            newDuration = oldDuration + deltaDuration;
            if (newDuration <= 0)
            {
                snackbar.Show("Hold Notes cannot have zero length.");
                adjustable = false;
                break;
            }
            if (HoldNoteCoversAnotherNote(holdNote.pulse, holdNote.lane,
                newDuration, ignoredExistingNotes: null))
            {
                snackbar.Show("Hold Notes cannot cover other notes.");
                adjustable = false;
                break;
            }
        }

        if (adjustable)
        {
            // Apply adjustment. No need to delete and respawn notes
            // this time.
            EditorContext.PrepareForChange();
            foreach (GameObject o in holdNotesBeingAdjusted)
            {
                HoldNote holdNote = o.GetComponent<NoteObject>().note as HoldNote;
                holdNote.duration += deltaDuration;
            }
            EditorContext.DoneWithChange();
            UpdateNumScansAndRelatedUI();
        }

        foreach (GameObject o in holdNotesBeingAdjusted)
        {
            o.GetComponent<NoteInEditor>().ResetTrail();
        }
    }
    #endregion

    #region Drag Notes
    private void OnAnchorReceiverClicked(GameObject note)
    {
        DragNote dragNote = note.GetComponent<NoteObject>()
            .note as DragNote;
        IntPoint newAnchor = new IntPoint(
            noteCursor.note.pulse - dragNote.pulse,
            noteCursor.note.lane - dragNote.lane);

        // Is there an existing anchor at the same pulse?
        if (dragNote.nodes.Find((DragNode node) =>
        {
            return node.anchor.pulse == newAnchor.pulse;
        }) != null)
        {
            snackbar.Show("The Anchor you are trying to add is too close to an existing Anchor.");
            return;
        }

        DragNode newNode = new DragNode()
        {
            anchor = newAnchor,
            controlLeft = new FloatPoint(0f, 0f),
            controlRight = new FloatPoint(0f, 0f)
        };
        EditorContext.PrepareForChange();
        dragNote.nodes.Add(newNode);
        dragNote.nodes.Sort((DragNode node1, DragNode node2) =>
        {
            return node1.anchor.pulse - node2.anchor.pulse;
        });
        EditorContext.DoneWithChange();
        UpdateNumScansAndRelatedUI();

        NoteInEditor noteInEditor = note
            .GetComponent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetAllAnchorsAndControlPoints();
    }

    private GameObject draggedAnchor;
    private DragNode draggedDragNode;
    private DragNode draggedDragNodeClone;
    private bool ctrlHeldOnAnchorBeginDrag;
    private Vector2 mousePositionRelativeToDraggedAnchor;
    private void OnAnchorRightClicked(GameObject anchor)
    {
        int anchorIndex = anchor
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;

        if (anchorIndex == 0)
        {
            snackbar.Show("Cannot delete the first Anchor in each Drag Note.");
            return;
        }

        DragNote dragNote = anchor
            .GetComponentInParent<NoteObject>().note as DragNote;
        if (dragNote.nodes.Count == 2)
        {
            snackbar.Show("Drag Notes must contain at least 2 Anchors.");
            return;
        }

        EditorContext.PrepareForChange();
        dragNote.nodes.RemoveAt(anchorIndex);
        EditorContext.DoneWithChange();
        UpdateNumScansAndRelatedUI();

        NoteInEditor noteInEditor = anchor
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetAllAnchorsAndControlPoints();
    }

    private void OnAnchorBeginDrag(GameObject anchor)
    {
        if (isPlaying) return;

        draggedAnchor = anchor
            .GetComponentInParent<DragNoteAnchor>().gameObject;
        
        int anchorIndex = anchor
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        draggedDragNode = (anchor
            .GetComponentInParent<NoteObject>().note as DragNote)
            .nodes[anchorIndex];
        draggedDragNodeClone = draggedDragNode.Clone();

        ctrlHeldOnAnchorBeginDrag = Input.GetKey(KeyCode.LeftControl)
            || Input.GetKey(KeyCode.RightControl);
        if (ctrlHeldOnAnchorBeginDrag)
        {
            // Reset control points.
            mousePositionRelativeToDraggedAnchor = new Vector2();
            draggedDragNode.controlLeft = new FloatPoint(0f, 0f);
            draggedDragNode.controlRight = new FloatPoint(0f, 0f);

            NoteInEditor noteInEditor = draggedAnchor
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetControlPointPosition(draggedDragNode,
                draggedAnchor, 0);
            noteInEditor.ResetControlPointPosition(draggedDragNode,
                draggedAnchor, 1);
            noteInEditor.ResetPathsToControlPoints(
                draggedAnchor.GetComponent<DragNoteAnchor>());
        }
    }

    private void MoveDraggedAnchor()
    {
        Note noteHead = draggedAnchor.GetComponentInParent<NoteObject>().note;
        draggedDragNode.anchor.pulse = noteCursor.note.pulse
            - noteHead.pulse;
        draggedDragNode.anchor.lane = noteCursor.note.lane
            - noteHead.lane;
        draggedAnchor.GetComponent<RectTransform>().anchoredPosition
            = new Vector2(
                draggedDragNode.anchor.pulse * PulseWidth,
                -draggedDragNode.anchor.lane * LaneHeight);

        NoteInEditor noteInEditor = draggedAnchor
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
    }

    private void MoveControlPointsBeingReset(Vector2 delta)
    {
        mousePositionRelativeToDraggedAnchor += delta;

        Vector2 pointLeft, pointRight;
        if (mousePositionRelativeToDraggedAnchor.x < 0f)
        {
            pointLeft = mousePositionRelativeToDraggedAnchor;
        }
        else
        {
            pointLeft = -mousePositionRelativeToDraggedAnchor;
        }
        pointRight = -pointLeft;

        draggedDragNode.controlLeft = new FloatPoint(
            pulse: pointLeft.x / PulseWidth,
            lane: -pointLeft.y / LaneHeight);
        draggedDragNode.controlRight = new FloatPoint(
           pulse: pointRight.x / PulseWidth,
           lane: -pointRight.y / LaneHeight);

        NoteInEditor noteInEditor = draggedAnchor
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetControlPointPosition(draggedDragNode,
            draggedAnchor, 0);
        noteInEditor.ResetControlPointPosition(draggedDragNode,
            draggedAnchor, 1);
        noteInEditor.ResetPathsToControlPoints(
            draggedAnchor.GetComponent<DragNoteAnchor>());
    }

    private void OnAnchorDrag(Vector2 delta)
    {
        if (isPlaying) return;
        delta /= rootCanvas.localScale.x;

        if (ctrlHeldOnAnchorBeginDrag)
        {
            MoveControlPointsBeingReset(delta);
        }
        else
        {
            if (draggedAnchor
                .GetComponentInParent<DragNoteAnchor>()
                .anchorIndex == 0)
            {
                return;
            }
            MoveDraggedAnchor();
        }
    }

    private bool ValidateMovedAnchor(DragNode movedNode,
        out string invalidReason)
    {
        DragNote dragNote = draggedAnchor
            .GetComponentInParent<NoteObject>().note as DragNote;
        int anchorIndex = draggedAnchor
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;

        // Check 1: anchors are still in order.
        bool anchorsInOrder = movedNode.anchor.pulse >
            dragNote.nodes[anchorIndex - 1].anchor.pulse;
        if (anchorIndex < dragNote.nodes.Count - 1)
        {
            anchorsInOrder = anchorsInOrder &&
                movedNode.anchor.pulse <
                dragNote.nodes[anchorIndex + 1].anchor.pulse;
        }
        if (!anchorsInOrder)
        {
            invalidReason = "Anchors must flow from left to right.";
            return false;
        }

        // Check 2: anchor is at a valid place for notes.
        string invalidPositionReason;
        bool validPosition = CanAddNote(NoteType.Basic,
            movedNode.anchor.pulse + dragNote.pulse,
            movedNode.anchor.lane + dragNote.lane,
            out invalidPositionReason);
        if (!validPosition)
        {
            invalidReason = "Anchors cannot be placed on top of or inside other notes.";
            return false;
        }

        invalidReason = null;
        return true;
    }

    private void OnAnchorEndDrag()
    {
        if (isPlaying) return;
        if (!ctrlHeldOnAnchorBeginDrag &&
            draggedAnchor.GetComponentInParent<DragNoteAnchor>()
                .anchorIndex == 0)
        {
            return;
        }

        DragNode cloneAtEndDrag = draggedDragNode.Clone();
        // Temporarily restore pre-drag data for snapshotting.
        draggedDragNode.CopyFrom(draggedDragNodeClone);
        
        bool valid;
        string invalidReason = "";
        if (ctrlHeldOnAnchorBeginDrag)
        {
            valid = true;
        }
        else
        {
            valid = ValidateMovedAnchor(cloneAtEndDrag,
                out invalidReason);
        }
        if (!valid)
        {
            snackbar.Show(invalidReason);
            NoteInEditor noteInEditor = draggedAnchor
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetAllAnchorsAndControlPoints();
            return;
        }

        EditorContext.PrepareForChange();
        draggedDragNode.CopyFrom(cloneAtEndDrag);
        EditorContext.DoneWithChange();
        UpdateNumScansAndRelatedUI();
    }

    private void OnControlPointRightClicked(GameObject controlPoint,
        int controlPointIndex)
    {
        if (isPlaying) return;

        int anchorIndex = controlPoint
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        DragNode node = (controlPoint
            .GetComponentInParent<NoteObject>().note as DragNote)
            .nodes[anchorIndex];

        EditorContext.PrepareForChange();
        node.SetControlPoint(controlPointIndex,
            new FloatPoint(0f, 0f));
        EditorContext.DoneWithChange();

        NoteInEditor noteInEditor = controlPoint
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetAllAnchorsAndControlPoints();
    }

    private GameObject draggedControlPoint;
    private int draggedControlPointIndex;
    private void OnControlPointBeginDrag(GameObject controlPoint,
        int controlPointIndex)
    {
        if (isPlaying) return;
        draggedControlPoint = controlPoint;
        draggedControlPointIndex = controlPointIndex;

        int anchorIndex = draggedControlPoint
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        draggedDragNode = (draggedControlPoint
            .GetComponentInParent<NoteObject>().note as DragNote)
            .nodes[anchorIndex];
        draggedDragNodeClone = draggedDragNode.Clone();
    }

    private void OnControlPointDrag(Vector2 delta)
    {
        if (isPlaying) return;
        delta /= rootCanvas.localScale.x;
        draggedControlPoint.GetComponent<RectTransform>()
            .anchoredPosition += delta;

        Vector2 newPosition = draggedControlPoint
            .GetComponent<RectTransform>()
            .anchoredPosition;
        FloatPoint newPoint = new FloatPoint(
            pulse: newPosition.x / PulseWidth,
            lane: -newPosition.y / LaneHeight);
        draggedDragNode.SetControlPoint(draggedControlPointIndex,
            newPoint);

        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);
        if (!alt && newPosition.sqrMagnitude > 0f)
        {
            // Rotate opposing control point.
            int otherIndex = 1 - draggedControlPointIndex;
            RectTransform otherTransform = draggedControlPoint
                .GetComponentInParent<DragNoteAnchor>()
                .GetControlPoint(otherIndex)
                .GetComponent<RectTransform>();
            Vector2 otherPosition = otherTransform
                .anchoredPosition;

            float angle = Mathf.Atan2(newPosition.y, newPosition.x);
            angle += Mathf.PI;
            otherPosition = new Vector2(
                otherPosition.magnitude * Mathf.Cos(angle),
                otherPosition.magnitude * Mathf.Sin(angle));
            FloatPoint otherPoint = new FloatPoint(
                pulse: otherPosition.x / PulseWidth,
                lane: -otherPosition.y / LaneHeight);

            otherTransform.anchoredPosition = otherPosition;
            draggedDragNode.SetControlPoint(otherIndex,
                otherPoint);
        }

        NoteInEditor noteInEditor = draggedControlPoint
                .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetPathsToControlPoints(
            draggedControlPoint
            .GetComponentInParent<DragNoteAnchor>());
        noteInEditor.ResetCurve();
    }

    private void OnControlPointEndDrag()
    {
        if (isPlaying) return;

        DragNode cloneAtEndDrag = draggedDragNode.Clone();
        // Temporarily restore pre-drag data for snapshotting.
        draggedDragNode.CopyFrom(draggedDragNodeClone);

        // Are the control points' current position valid?
        bool valid = cloneAtEndDrag.GetControlPoint(0).pulse <= 0f
            && cloneAtEndDrag.GetControlPoint(1).pulse >= 0f;
        if (!valid)
        {
            snackbar.Show("Left and Right Control Points must stay on the respective sides of the Anchor Point.");
            NoteInEditor noteInEditor = draggedControlPoint
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetAllAnchorsAndControlPoints();
            return;
        }

        EditorContext.PrepareForChange();
        draggedDragNode.CopyFrom(cloneAtEndDrag);
        EditorContext.DoneWithChange();
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
    // During an editing session the number of scans will never
    // decrease. This prevents unintended scrolling when deleting
    // the last notes.
    private bool UpdateNumScans()
    {
        int numScansBackup = numScans;

        int lastPulse = 0;
        if (EditorContext.Pattern.notes.Count > 0)
        {
            lastPulse = EditorContext.Pattern.notes.Max.pulse;
        }
        int pulsesPerScan = Pattern.pulsesPerBeat *
            EditorContext.Pattern.patternMetadata.bps;

        // Look at all hold and drag notes in the last few scans
        // in case their duration outlasts the currently considered
        // last scan.
        foreach (Note n in EditorContext.Pattern.GetViewBetween(
            minPulseInclusive: lastPulse - pulsesPerScan * 2,
            maxPulseInclusive: lastPulse))
        {
            int endingPulse;
            if (n is HoldNote)
            {
                endingPulse = n.pulse + (n as HoldNote).duration;
            }
            else if (n is DragNote)
            {
                endingPulse = n.pulse + (n as DragNote).Duration();
            }
            else
            {
                continue;
            }

            if (endingPulse > lastPulse)
            {
                lastPulse = endingPulse;
            }
        }

        int lastScan = lastPulse / pulsesPerScan;
        // 1 empty scan at the end
        numScans = Mathf.Max(numScansBackup, lastScan + 2);

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
        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(
                workspaceScrollRect.horizontalNormalizedPosition);
        SynchronizeScrollRects();
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
        for (int i = 0; i < markerInHeaderContainer.childCount; i++)
        {
            GameObject child = markerInHeaderContainer.GetChild(i)
                .gameObject;
            if (child == scanMarkerInHeaderTemplate) continue;
            if (child == beatMarkerInHeaderTemplate) continue;
            if (child == bpmMarkerTemplate) continue;
            Destroy(child.gameObject);
        }
        for (int i = 0; i < markerContainer.childCount; i++)
        {
            GameObject child = markerContainer.GetChild(i)
                .gameObject;
            if (child == scanMarkerTemplate) continue;
            if (child == beatMarkerTemplate) continue;
            Destroy(child.gameObject);
        }

        EditorContext.Pattern.PrepareForTimeCalculation();
        int bps = EditorContext.Pattern.patternMetadata.bps;
        // BPM markers in the header need to be drawn on top of
        // other markers, so we sort them. No need to do this for
        // markers in the workspace.
        List<KeyValuePair<Transform, MarkerPriority>> 
            allMarkersInHeader =
            new List<KeyValuePair<Transform, MarkerPriority>>();

        for (int scan = 0; scan < numScans; scan++)
        {
            GameObject markerInHeader = Instantiate(
                scanMarkerInHeaderTemplate, markerInHeaderContainer);
            GameObject marker = Instantiate(
                scanMarkerTemplate, markerContainer);
            markerInHeader.SetActive(true);  // This calls OnEnabled
            marker.SetActive(true);

            int pulse = scan * bps * Pattern.pulsesPerBeat;
            Marker m = markerInHeader.GetComponent<Marker>();
            m.pulse = pulse;
            m.SetTimeDisplay();
            m.GetComponent<SelfPositionerInEditor>().Reposition();
            m = marker.GetComponent<Marker>();
            m.pulse = pulse;
            m.GetComponent<SelfPositionerInEditor>().Reposition();

            allMarkersInHeader.Add(new KeyValuePair<
                Transform, MarkerPriority>(
                markerInHeader.transform, MarkerPriority.Other));

            for (int beat = 1; beat < bps; beat++)
            {
                markerInHeader = Instantiate(
                    beatMarkerInHeaderTemplate, 
                    markerInHeaderContainer);
                marker = Instantiate(
                    beatMarkerTemplate,
                    markerContainer);
                markerInHeader.SetActive(true);
                marker.SetActive(true);

                pulse = (scan * bps + beat) *
                    Pattern.pulsesPerBeat;
                m = markerInHeader.GetComponent<Marker>();
                m.pulse = pulse;
                m.SetTimeDisplay();
                m.GetComponent<SelfPositionerInEditor>()
                    .Reposition();
                m = marker.GetComponent<Marker>();
                m.pulse = pulse;
                m.GetComponent<SelfPositionerInEditor>()
                    .Reposition();

                allMarkersInHeader.Add(new KeyValuePair<
                    Transform, MarkerPriority>(
                    markerInHeader.transform, MarkerPriority.Other));
            }
        }

        foreach (BpmEvent e in EditorContext.Pattern.bpmEvents)
        {
            GameObject marker = Instantiate(
                bpmMarkerTemplate, markerInHeaderContainer);
            marker.SetActive(true);
            Marker m = marker.GetComponent<Marker>();
            m.pulse = e.pulse;
            m.SetBpmText(e.bpm);
            m.GetComponent<SelfPositionerInEditor>().Reposition();
            allMarkersInHeader.Add(new KeyValuePair<
                Transform, MarkerPriority>(
                marker.transform, MarkerPriority.Bpm));
        }

        // Sort all markers in the header.
        allMarkersInHeader.Sort((
            KeyValuePair<Transform, MarkerPriority> p1,
            KeyValuePair<Transform, MarkerPriority> p2) =>
        {
            float deltaX = p1.Key.position.x - p2.Key.position.x;
            if (deltaX < 0) return -1;
            if (deltaX > 0) return 1;
            // At the same position, BPM markers should be
            // drawn later.
            if (p1.Value == MarkerPriority.Bpm) return 1;
            if (p2.Value == MarkerPriority.Bpm) return -1;
            return 0;
        });
        for (int i = 0; i < allMarkersInHeader.Count; i++)
        {
            allMarkersInHeader[i].Key.SetSiblingIndex(i);
        }
    }

    // This will call Reposition on the new object.
    private GameObject SpawnNoteObject(Note n)
    {
        GameObject prefab = null;
        switch (n.type)
        {
            case NoteType.Basic:
                prefab = basicNotePrefab;
                break;
            case NoteType.ChainHead:
                prefab = chainHeadPrefab;
                break;
            case NoteType.ChainNode:
                prefab = chainNodePrefab;
                break;
            case NoteType.RepeatHead:
                prefab = repeatHeadPrefab;
                break;
            case NoteType.RepeatHeadHold:
                prefab = repeatHeadHoldPrefab;
                break;
            case NoteType.Repeat:
                prefab = repeatNotePrefab;
                break;
            case NoteType.RepeatHold:
                prefab = repeatHoldPrefab;
                break;
            case NoteType.Hold:
                prefab = holdNotePrefab;
                break;
            case NoteType.Drag:
                prefab = dragNotePrefab;
                break;
            default:
                Debug.LogError("Unknown note type: " + n.type);
                prefab = basicNotePrefab;
                break;
        }

        NoteObject noteObject = Instantiate(prefab,
            noteContainer).GetComponent<NoteObject>();
        noteObject.note = n;
        NoteInEditor noteInEditor = noteObject
            .GetComponent<NoteInEditor>();
        noteInEditor.SetKeysoundText();
        noteInEditor.SetKeysoundVisibility(showKeysoundToggle.isOn);
        if (n.lane >= PlayableLanes) noteInEditor.UseHiddenSprite();
        noteObject.GetComponent<SelfPositionerInEditor>()
            .Reposition();

        noteToNoteObject.Add(n, noteObject);

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
            float middleX = noteContainer.GetChild(middle)
                .position.x;
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
        noteToNoteObject = new Dictionary<Note, NoteObject>();
        lastSelectedNoteWithoutShift = null;
        selectedNoteObjects = new HashSet<GameObject>();

        foreach (Note n in EditorContext.Pattern.notes)
        {
            SpawnNoteObject(n);
        }

        AdjustAllPathsAndTrails();
    }

    private void GetPreviousAndNextNoteObjects(
        Note n, HashSet<NoteType> types,
        int minLaneInclusive, int maxLaneInclusive,
        out GameObject prev, out GameObject next)
    {
        Note prevNote = EditorContext.Pattern
            .GetClosestNoteBefore(n.pulse, types,
            minLaneInclusive,
            maxLaneInclusive);
        Note nextNote = EditorContext.Pattern
            .GetClosestNoteAfter(n.pulse, types,
            minLaneInclusive,
            maxLaneInclusive);
        prev = null;
        if (prevNote != null)
        {
            prev = GetGameObjectFromNote(prevNote);
        }
        next = null;
        if (nextNote != null)
        {
            next = GetGameObjectFromNote(nextNote);
        }
    }

    private void GetPreviousAndNextChainNotes(Note n,
        out GameObject prev, out GameObject next)
    {
        GetPreviousAndNextNoteObjects(n,
            new HashSet<NoteType>()
                { NoteType.ChainHead, NoteType.ChainNode },
            minLaneInclusive: 0,
            maxLaneInclusive: PlayableLanes - 1,
            out prev, out next);
    }

    private void GetPreviousAndNextRepeatNotes(Note n,
        out GameObject prev, out GameObject next)
    {
        GetPreviousAndNextNoteObjects(n,
            new HashSet<NoteType>()
                { NoteType.RepeatHead,
                NoteType.RepeatHeadHold,
                NoteType.Repeat,
                NoteType.RepeatHold},
            minLaneInclusive: n.lane,
            maxLaneInclusive: n.lane,
            out prev, out next);
    }

    // This may modify o, the same-type note before o, and/or
    // the same-type note after o.
    private void AdjustPathOrTrailAround(GameObject o)
    {
        NoteObject n = o.GetComponent<NoteObject>();

        if (n.note.type == NoteType.ChainHead ||
            n.note.type == NoteType.ChainNode)
        {
            if (n.note.lane >= 0 && n.note.lane < PlayableLanes)
            {
                GameObject prev, next;
                GetPreviousAndNextChainNotes(n.note,
                    out prev, out next);

                if (n.note.type == NoteType.ChainNode)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(prev);
                }
                if (next != null &&
                    next.GetComponent<NoteObject>().note.type
                    == NoteType.ChainNode)
                {
                    next.GetComponent<NoteInEditor>()
                        .PointPathToward(o);
                }
            }
        }

        if (n.note.type == NoteType.RepeatHead ||
            n.note.type == NoteType.RepeatHeadHold ||
            n.note.type == NoteType.Repeat ||
            n.note.type == NoteType.RepeatHold)
        {
            if (n.note.lane >= 0 && n.note.lane < PlayableLanes)
            {
                GameObject prev, next;
                GetPreviousAndNextRepeatNotes(n.note,
                    out prev, out next);

                if (n.note.type == NoteType.Repeat ||
                    n.note.type == NoteType.RepeatHold)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(prev);
                }
                
                if (next != null)
                {
                    NoteType nextType = next
                        .GetComponent<NoteObject>().note.type;
                    if (nextType == NoteType.Repeat ||
                        nextType == NoteType.RepeatHold)
                    {
                        next.GetComponent<NoteInEditor>()
                            .PointPathToward(o);
                    }
                }
            }
        }

        if (n.note.type == NoteType.Hold ||
            n.note.type == NoteType.RepeatHeadHold ||
            n.note.type == NoteType.RepeatHold)
        {
            o.GetComponent<NoteInEditor>().ResetTrail();
        }

        if (n.note.type == NoteType.Drag)
        {
            o.GetComponent<NoteInEditor>().ResetCurve();
            o.GetComponent<NoteInEditor>()
                .ResetAllAnchorsAndControlPoints();
        }
    }

    // This may modify o, the same-type note before o, and/or
    // the same-type note after o.
    private void AdjustPathBeforeDeleting(GameObject o)
    {
        NoteObject n = o.GetComponent<NoteObject>();
        if (n.note.lane < 0 || n.note.lane >= PlayableLanes) return;
        switch (n.note.type)
        {
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                {
                    GameObject prev, next;
                    GetPreviousAndNextChainNotes(n.note,
                        out prev, out next);

                    if (next != null &&
                        next.GetComponent<NoteObject>().note.type
                        == NoteType.ChainNode)
                    {
                        next.GetComponent<NoteInEditor>()
                            .PointPathToward(prev);
                    }
                    else if (prev != null &&
                        prev.GetComponent<NoteObject>().note.type
                        == NoteType.ChainHead)
                    {
                        prev.GetComponent<NoteInEditor>()
                            .ResetNoteImageRotation();
                    }
                }
                break;
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                {
                    GameObject prev, next;
                    GetPreviousAndNextRepeatNotes(n.note,
                        out prev, out next);

                    if (next != null)
                    {
                        NoteType nextType = next
                            .GetComponent<NoteObject>().note.type;
                        if (nextType == NoteType.Repeat ||
                            nextType == NoteType.RepeatHold)
                        {
                            next.GetComponent<NoteInEditor>()
                                .PointPathToward(prev);
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    private void AdjustAllPathsAndTrails()
    {
        GameObject previousChain = null;
        List<GameObject> previousRepeat = new List<GameObject>();
        for (int i = 0; i < PlayableLanes; i++)
        {
            previousRepeat.Add(null);
        }

        foreach (Note n in EditorContext.Pattern.notes)
        {
            GameObject o = GetGameObjectFromNote(n);
            if (n.type == NoteType.ChainHead ||
                n.type == NoteType.ChainNode)
            {
                // Adjust the paths of chain nodes.
                if (n.lane >= PlayableLanes) continue;
                if (n.type == NoteType.ChainNode)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(previousChain);
                }
                previousChain = o;
            }
            if (n.type == NoteType.RepeatHead ||
                n.type == NoteType.Repeat ||
                n.type == NoteType.RepeatHeadHold ||
                n.type == NoteType.RepeatHold)
            {
                // Adjust the paths of repeat notes.
                if (n.lane >= PlayableLanes) continue;
                if (n.type == NoteType.Repeat ||
                    n.type == NoteType.RepeatHold)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(previousRepeat[n.lane]);
                }
                previousRepeat[n.lane] = o;
            }
            if (n.type == NoteType.Hold ||
                n.type == NoteType.RepeatHeadHold ||
                n.type == NoteType.RepeatHold)
            {
                // Adjust the trails of hold notes.
                o.GetComponent<NoteInEditor>().ResetTrail();
            }
            if (n.type == NoteType.Drag)
            {
                // Draw curves of drag notes.
                o.GetComponent<NoteInEditor>().ResetCurve();
                o.GetComponent<NoteInEditor>()
                    .ResetAllAnchorsAndControlPoints();
            }
        }
    }
    #endregion

    #region Pattern Modification
    private bool CanAddNote(NoteType type, int pulse, int lane,
        out string reason)
    {
        return CanAddNote(type, pulse, lane, null, out reason);
    }

    private bool CanAddNote(NoteType type, int pulse, int lane,
        HashSet<GameObject> ignoredExistingNotes,
        out string reason)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }

        // Boundary check.
        if (pulse < 0)
        {
            reason = "Cannot place notes before scan 0.";
            return false;
        }
        if (lane < 0)
        {
            reason = "Cannot place notes above the topmost lane.";
            return false;
        }
        if (lane >= TotalLanes)
        {
            reason = "Cannot place notes below the bottommost lane.";
            return false;
        }

        // Overlap check.
        Note noteAtSamePulseAndLane = EditorContext.Pattern
            .GetNoteAt(pulse, lane);
        if (noteAtSamePulseAndLane != null &&
            !ignoredExistingNotes.Contains(
                GetGameObjectFromNote(noteAtSamePulseAndLane)))
        {
            reason = "Cannot place notes on top of an existing note.";
            return false;
        }

        // Chain check.
        if (type == NoteType.ChainHead || type == NoteType.ChainNode)
        {
            foreach (Note noteAtPulse in EditorContext.Pattern
                .GetViewBetween(pulse, pulse))
            {
                if (ignoredExistingNotes.Contains(
                    GetGameObjectFromNote(noteAtPulse)))
                {
                    continue;
                }
                
                if (noteAtPulse.type == NoteType.ChainHead ||
                    noteAtPulse.type == NoteType.ChainNode)
                {
                    reason = "No two Chain Notes may occupy the same timepoint.";
                    return false;
                }
            }
        }

        // Hold check.
        Note holdNoteBeforePivot =
            EditorContext.Pattern.GetClosestNoteBefore(pulse,
            new HashSet<NoteType>()
            {
                NoteType.Hold,
                NoteType.RepeatHeadHold,
                NoteType.RepeatHold
            }, minLaneInclusive: lane, maxLaneInclusive: lane);
        if (holdNoteBeforePivot != null &&
            !ignoredExistingNotes.Contains(
                GetGameObjectFromNote(holdNoteBeforePivot)))
        {
            HoldNote holdNote = holdNoteBeforePivot as HoldNote;
            if (holdNote.pulse + holdNote.duration >= pulse)
            {
                reason = "Notes cannot be covered by Hold Notes.";
                return false;
            }
        }
            
        reason = null;
        return true;
    }

    private bool CanAddHoldNote(NoteType type, int pulse, int lane,
        int duration, HashSet<GameObject> ignoredExistingNotes,
        out string reason)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }
        if (!CanAddNote(type, pulse, lane, ignoredExistingNotes,
            out reason))
        {
            return false;
        }

        // Additional check for hold notes.
        if (HoldNoteCoversAnotherNote(pulse, lane, duration,
            ignoredExistingNotes))
        {
            reason = "Hold Notes cannot cover other notes.";
            return false;
        }

        return true;
    }

    private bool CanAddDragNote(int pulse, int lane,
        List<DragNode> nodes,
        HashSet<GameObject> ignoredExistingNotes,
        out string reason)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }
        if (!CanAddNote(NoteType.Drag, pulse, lane,
            ignoredExistingNotes,
            out reason))
        {
            return false;
        }

        // Additional check for hold notes.
        if (DragNoteCoversAnotherNote(pulse, lane, nodes,
            ignoredExistingNotes))
        {
            reason = "Drag Notes cannot cover other notes.";
            return false;
        }

        return true;
    }

    // Ignores notes at (pulse, lane), if any.
    private bool HoldNoteCoversAnotherNote(int pulse, int lane,
        int duration,
        HashSet<GameObject> ignoredExistingNotes)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }

        Note noteAfterPivot = EditorContext.Pattern
            .GetClosestNoteAfter(
            pulse, types: null,
            minLaneInclusive: lane,
            maxLaneInclusive: lane);
        if (noteAfterPivot != null &&
            !ignoredExistingNotes.Contains(
                GetGameObjectFromNote(noteAfterPivot)))
        {
            if (pulse + duration >= noteAfterPivot.pulse)
            {
                return true;
            }
        }

        return false;
    }

    // Ignores notes at (pulse, lane), if any.
    private bool DragNoteCoversAnotherNote(int pulse, int lane,
        List<DragNode> nodes,
        HashSet<GameObject> ignoredExistingNotes)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }

        // For now we only check anchors.
        for (int i = 1; i < nodes.Count; i++)
        {
            int anchorPulse = pulse + nodes[i].anchor.pulse;
            int anchorLane = lane + nodes[i].anchor.lane;
            Note existingNote = EditorContext.Pattern.GetNoteAt(
                anchorPulse, anchorLane);

            if (existingNote != null &&
                !ignoredExistingNotes.Contains(
                    GetGameObjectFromNote(existingNote)))
            {
                return true;
            }
        }

        return false;
    }

    private int HoldNoteDefaultDuration(int pulse, int lane)
    {
        Note noteAfterPivot = EditorContext.Pattern
            .GetClosestNoteAfter(
                pulse, types: null,
                minLaneInclusive: lane,
                maxLaneInclusive: lane);
        if (noteAfterPivot != null)
        {
            int nextPulse = noteAfterPivot.pulse;
            if (nextPulse - pulse <= Pattern.pulsesPerBeat)
            {
                return nextPulse - pulse - 1;
            }
        }
        return Pattern.pulsesPerBeat;
    }

    private GameObject FinishAddNote(Note n)
    {
        // Add to pattern.
        EditorContext.Pattern.notes.Add(n);

        // Add to UI. SpawnNoteObject will add n to
        // noteToNoteObject.
        GameObject newNote = SpawnNoteObject(n);
        AdjustPathOrTrailAround(newNote);
        UpdateNumScansAndRelatedUI();
        return newNote;
    }

    private GameObject AddNote(NoteType type, int pulse, int lane,
        string sound)
    {
        Note n = new Note()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound
        };
        return FinishAddNote(n);
    }

    private GameObject AddHoldNote(NoteType type,
        int pulse, int lane,
        int? duration, string sound)
    {
        if (!duration.HasValue)
        {
            duration = HoldNoteDefaultDuration(pulse, lane);
        }
        HoldNote n = new HoldNote()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,
            duration = duration.Value
        };
        return FinishAddNote(n);
    }

    private GameObject AddDragNote(int pulse, int lane,
        List<DragNode> nodes, string sound)
    {
        if (nodes == null)
        {
            nodes = new List<DragNode>();
            int relativePulseOfLastNode = 
                HoldNoteDefaultDuration(pulse, lane);
            nodes.Add(new DragNode()
            {
                anchor = new IntPoint(0, 0),
                controlLeft = new FloatPoint(0f, 0f),
                controlRight = new FloatPoint(0f, 0f)
            });
            nodes.Add(new DragNode()
            {
                anchor = new IntPoint(relativePulseOfLastNode, 0),
                controlLeft = new FloatPoint(0f, 0f),
                controlRight = new FloatPoint(0f, 0f)
            });
        }
        DragNote n = new DragNote()
        {
            type = NoteType.Drag,
            pulse = pulse,
            lane = lane,
            sound = sound,
            nodes = nodes
        };
        return FinishAddNote(n);
    }

    // Cannot remove o from selectedNoteObjects because the
    // caller may be enumerating that list.
    private void DeleteNote(GameObject o)
    {
        // Delete from pattern.
        Note n = GetNoteFromGameObject(o);
        EditorContext.Pattern.notes.Remove(n);

        // Delete from UI.
        AdjustPathBeforeDeleting(o);
        noteToNoteObject.Remove(n);
        if (lastSelectedNoteWithoutShift == n)
        {
            lastSelectedNoteWithoutShift = null;
        }
        // Destroy doesn't immedialy destroy, so we move the note
        // to the cemetary so as to not interfere with the
        // binary searches when spawning new notes on the same frame.
        o.transform.SetParent(noteCemetary);
        Destroy(o);
        UpdateNumScansAndRelatedUI();
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
            clipboard = new List<Note>();
        }
        clipboard.Clear();
        minPulseInClipboard = int.MaxValue;
        foreach (GameObject o in selectedNoteObjects)
        {
            Note n = GetNoteFromGameObject(o);
            if (n.pulse < minPulseInClipboard)
            {
                minPulseInClipboard = n.pulse;
            }
            clipboard.Add(n.Clone());
        }
    }

    public void PasteAtScanline()
    {
        if (clipboard == null) return;
        if (clipboard.Count == 0) return;
        if (isPlaying) return;

        int scanlinePulse = (int)scanline.floatPulse;
        int deltaPulse = scanlinePulse - minPulseInClipboard;

        // Can we paste here?
        bool pastable = true;
        string invalidReason = "";
        foreach (Note n in clipboard)
        {
            int newPulse = n.pulse + deltaPulse;
            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    pastable = CanAddNote(n.type, newPulse,
                        n.lane, out invalidReason);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    pastable = CanAddHoldNote(n.type,
                        newPulse, n.lane,
                        (n as HoldNote).duration,
                        ignoredExistingNotes: null,
                        out invalidReason);
                    break;
                case NoteType.Drag:
                    pastable = CanAddDragNote(newPulse, n.lane,
                        (n as DragNote).nodes,
                        ignoredExistingNotes: null,
                        out invalidReason);
                    break;
            }
            if (!pastable)
            {
                snackbar.Show(invalidReason);
                return;
            }
        }

        // Paste.
        EditorContext.PrepareForChange();
        foreach (Note n in clipboard)
        {
            int newPulse = n.pulse + deltaPulse;
            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    AddNote(n.type, newPulse,
                        n.lane, n.sound);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    AddHoldNote(n.type, newPulse,
                        n.lane, (n as HoldNote).duration,
                        n.sound);
                    break;
                case NoteType.Drag:
                    AddDragNote(newPulse, n.lane,
                        (n as DragNote).nodes,
                        n.sound);
                    break;
            }
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
            DeleteNote(o);
        }
        selectedNoteObjects.Clear();
        EditorContext.DoneWithChange();
    }
    #endregion

    #region Playback
    // During playback, the following features are disabled:
    // - Adding or deleting notes, including by clicking, dragging
    //   and cut/copy/paste
    // - Applying note types and/or keysounds to selection, if
    //   specified in options
    // - Moving the scanline, including by clicking the header
    //   and dragging the scanline position slider.
    private bool audioLoaded;
    private bool isPlaying;
    private float playbackStartingPulse;
    private float playbackStartingTime;
    private bool backingTrackPlaying;
    private DateTime systemTimeOnPlaybackStart;

    // Each queue represents one lane; each lane is sorted by pulse.
    // Played notes are popped from the corresponding queue. This
    // makes it easy to tell if it's time to play the next note
    // in each lane.
    //
    // This data structure is only used for playback, so it's not
    // defined in the Internal Data Structures region.
    private List<Queue<Note>> notesInLanes;

    private void OnResourceLoadComplete(string error)
    {
        if (error != null)
        {
            messageDialog.Show(error + "\n\n" +
                "You can continue to edit this pattern, but playback and preview will be disabled.");
        }
        audioLoaded = true;
        playButton.GetComponent<Button>().interactable =
            error == null;
        UpdatePlaybackUI();
    }

    private void UpdatePlaybackUI()
    {
        if (audioLoaded)
        {
            playButton.SetActive(!isPlaying);
            stopButton.SetActive(isPlaying);
        }
        else
        {
            playButton.SetActive(false);
            stopButton.SetActive(false);
        }
        audioLoadingIndicator.SetActive(!audioLoaded);
        scanlinePositionSlider.interactable = !isPlaying;
    }

    public void StartPlayback()
    {
        if (!audioLoaded) return;
        if (isPlaying) return;
        if (!playButton.GetComponent<Button>().interactable)
            return;
        isPlaying = true;
        UpdatePlaybackUI();

        Pattern pattern = EditorContext.Pattern;
        pattern.PrepareForTimeCalculation();
        pattern.CalculateTimeOfAllNotes();
        playbackStartingPulse = scanline.floatPulse;
        playbackStartingTime = pattern.PulseToTime(
            (int)playbackStartingPulse);

        // Put notes into queues, each corresponding to a lane.
        // Use MaxTotalLanes instead of TotalLanes, so that, for
        // example, when user sets hidden lanes to 4, lanes
        // 8~11 are still played.
        notesInLanes = new List<Queue<Note>>();
        for (int i = 0; i < MaxTotalLanes; i++)
        {
            notesInLanes.Add(new Queue<Note>());
        }
        foreach (Note n in EditorContext.Pattern.GetViewBetween(
            (int)playbackStartingPulse,
            int.MaxValue))
        {
            notesInLanes[n.lane].Enqueue(n);
        }

        systemTimeOnPlaybackStart = DateTime.Now;
        backingTrackPlaying = false;
    }

    public void StopPlayback()
    {
        if (!isPlaying) return;
        isPlaying = false;
        UpdatePlaybackUI();

        audioSourceManager.StopAll();
        scanline.floatPulse = playbackStartingPulse;
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        ScrollScanlineIntoView();
        RefreshScanlinePositionSlider();
    }

    public void UpdatePlayback()
    {
        // Calculate time.
        float elapsedTime = (float)(DateTime.Now - 
            systemTimeOnPlaybackStart).TotalSeconds;
        float playbackCurrentTime = playbackStartingTime + 
            elapsedTime;
        float playbackCurrentPulse = EditorContext.Pattern
            .TimeToPulse(playbackCurrentTime);

        // Start playing backing track if applicable.
        if (!backingTrackPlaying &&
            playbackCurrentTime >= 0f)
        {
            backingTrackPlaying = true;
            audioSourceManager.PlayBackingTrack(
                ResourceLoader.GetCachedClip(
                    EditorContext.Pattern.patternMetadata
                    .backingTrack),
                playbackCurrentTime);
        }

        // Stop playback after the last scan.
        int totalPulses = numScans * 
            EditorContext.Pattern.patternMetadata.bps * 
            Pattern.pulsesPerBeat;
        if (playbackCurrentPulse > totalPulses)
        {
            StopPlayback();
            return;
        }

        // Play keysounds if it's their time.
        for (int i = 0; i < notesInLanes.Count; i++)
        {
            if (notesInLanes[i].Count == 0) continue;
            Note nextNote = notesInLanes[i].Peek();
            if (playbackCurrentTime >= nextNote.time)
            {
                AudioClip clip = ResourceLoader.GetCachedClip(
                    nextNote.sound);
                float startTime = playbackCurrentTime - 
                    nextNote.time;
                audioSourceManager.PlayKeysound(clip,
                    nextNote.lane > PlayableLanes,
                    startTime);

                notesInLanes[i].Dequeue();
            }
        }

        // Move scanline.
        scanline.floatPulse = playbackCurrentPulse;
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        ScrollScanlineIntoView();
        RefreshScanlinePositionSlider();
    }

    private void PlaySound(AudioSource source, AudioClip clip,
        float startTime)
    {
        if (clip == null) return;

        int startSample = Mathf.FloorToInt(
            startTime * clip.frequency);
        source.clip = clip;
        source.timeSamples = Mathf.Min(clip.samples, startSample);
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
        float viewPortWidth = workspaceScrollRect
            .GetComponent<RectTransform>().rect.width;
        if (WorkspaceContentWidth <= viewPortWidth) return;

        float scanlinePosition = scanline
            .GetComponent<RectTransform>().anchoredPosition.x;

        float xAtViewPortLeft = (WorkspaceContentWidth - viewPortWidth)
            * workspaceScrollRect.horizontalNormalizedPosition;
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
        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(normalizedPosition);
        SynchronizeScrollRects();
    }

    private void SynchronizeScrollRects()
    {
        headerContent.sizeDelta = new Vector2(
            workspaceContent.sizeDelta.x,
            workspaceContent.sizeDelta.y);
        headerScrollRect.horizontalNormalizedPosition =
            workspaceScrollRect.horizontalNormalizedPosition;
    }
    #endregion
}
