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
    public AudioSource backingTrackSource;
    public List<AudioSource> keysoundSources;

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
    public Slider scanlinePositionSlider;
    public Snackbar snackbar;
    public MessageDialog messageDialog;
    public BpmEventDialog bpmEventDialog;
    public Dialog shortcutDialog;

    #region Internal Data Structures
    // All note objects organized in a way that makes editor
    // operations efficient.
    //
    // This data structure must be updated alongside
    // EditorContext.Pattern at all times.
    private SortedNoteObjects sortedNoteObjects;

    private GameObject lastSelectedNoteObjectWithoutShift;
    private HashSet<GameObject> selectedNoteObjects;

    private NoteWithSound ConvertToNoteWithSound(GameObject o)
    {
        NoteObject n = o.GetComponent<NoteObject>();
        return new NoteWithSound()
        {
            note = n.note,
            sound = n.sound
        };
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
        noteType = NoteType.Basic;
        UpdateNoteTypeButtons();
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
        NoteInEditor.DurationHandleBeginDrag += OnDurationHandleBeginDrag;
        NoteInEditor.DurationHandleDrag += OnDurationHandleDrag;
        NoteInEditor.DurationHandleEndDrag += OnDurationHandleEndDrag;
        NoteInEditor.AnchorBeginDrag += OnAnchorBeginDrag;
        NoteInEditor.AnchorDrag += OnAnchorDrag;
        NoteInEditor.AnchorEndDrag += OnAnchorEndDrag;
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
        NoteInEditor.AnchorBeginDrag -= OnAnchorBeginDrag;
        NoteInEditor.AnchorDrag -= OnAnchorDrag;
        NoteInEditor.AnchorEndDrag -= OnAnchorEndDrag;
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
                AdjustAllPathsAndTrails();
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

        float viewPortWidth = workspace.GetComponent<RectTransform>().rect.width;
        if (WorkspaceContentWidth < viewPortWidth) return;
        float horizontal = workspace.horizontalNormalizedPosition *
            (WorkspaceContentWidth - viewPortWidth);
        horizontal -= p.delta.x / rootCanvas.localScale.x;
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
            string sound = n.sound;

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
        NoteObject noteObject = draggedNoteObject.GetComponent<NoteObject>();
        int oldPulse = noteObject.note.pulse;
        int oldLane = noteObject.note.lane;
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
            HashSet<GameObject> replacedSelection = new HashSet<GameObject>();
            List<NoteWithSound> movedNotes = new List<NoteWithSound>();
            foreach (GameObject o in selectedNoteObjects)
            {
                NoteObject n = o.GetComponent<NoteObject>();
                NoteWithSound movedNote = new NoteWithSound()
                {
                    note = n.note.Clone(),
                    sound = n.sound
                };
                movedNote.note.pulse += deltaPulse;
                movedNote.note.lane += deltaLane;
                movedNotes.Add(movedNote);

                DeleteNote(o);
            }
            foreach (NoteWithSound movedNote in movedNotes)
            {
                GameObject o = null;
                switch (movedNote.note.type)
                {
                    case NoteType.Basic:
                    case NoteType.ChainHead:
                    case NoteType.ChainNode:
                    case NoteType.RepeatHead:
                    case NoteType.Repeat:
                        o = AddNote(movedNote.note.type,
                            movedNote.note.pulse,
                            movedNote.note.lane,
                            movedNote.sound);
                        break;
                    case NoteType.Hold:
                    case NoteType.RepeatHeadHold:
                    case NoteType.RepeatHold:
                        o = AddHoldNote(movedNote.note.type,
                            movedNote.note.pulse,
                            movedNote.note.lane,
                            (movedNote.note as HoldNote).duration,
                            movedNote.sound);
                        break;
                    case NoteType.Drag:
                        o = AddDragNote(
                            movedNote.note.pulse,
                            movedNote.note.lane,
                            (movedNote.note as DragNote).nodes,
                            movedNote.sound);
                        break;
                }
                replacedSelection.Add(o);
            }
            EditorContext.DoneWithChange();
            selectedNoteObjects = replacedSelection;
            SelectionChanged?.Invoke(selectedNoteObjects);
        }

        foreach (GameObject o in selectedNoteObjects)
        {
            o.GetComponent<SelfPositioner>().Reposition();
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
        }

        foreach (GameObject o in holdNotesBeingAdjusted)
        {
            o.GetComponent<NoteInEditor>().ResetTrail();
        }
    }
    #endregion

    #region Drag Notes
    private GameObject draggedAnchor;
    private void OnAnchorBeginDrag(GameObject anchor)
    {
        if (isPlaying) return;
        draggedAnchor = anchor;
    }

    private void OnAnchorDrag(Vector2 delta)
    {
        if (isPlaying) return;
        delta /= rootCanvas.localScale.x;
        draggedAnchor.GetComponent<RectTransform>().anchoredPosition 
            += delta;
    }

    private void OnAnchorEndDrag()
    {
        if (isPlaying) return;
    }

    private GameObject draggedControlPoint;
    private int draggedControlPointIndex;
    private void OnControlPointBeginDrag(GameObject controlPoint,
        int controlPointIndex)
    {
        if (isPlaying) return;
        draggedControlPoint = controlPoint;
        draggedControlPointIndex = controlPointIndex;
    }

    private void OnControlPointDrag(Vector2 delta)
    {
        if (isPlaying) return;
        delta /= rootCanvas.localScale.x;
        draggedControlPoint.GetComponent<RectTransform>()
            .anchoredPosition += delta;
    }

    private void OnControlPointEndDrag()
    {
        if (isPlaying) return;
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

        int lastPulse = sortedNoteObjects.GetMaxPulse();
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

        foreach (KeyValuePair<Transform, MarkerPriority> pair in allMarkers)
        {
            SelfPositioner positioner = pair.Key.GetComponent<SelfPositioner>();
            positioner.Reposition();
        }
    }

    // This will call Reposition on the new object.
    private GameObject SpawnNoteObject(Note n, string sound)
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
        noteObject.sound = sound;
        NoteInEditor noteInEditor = noteObject.GetComponent<NoteInEditor>();
        noteInEditor.SetKeysoundText();
        noteInEditor.SetKeysoundVisibility(showKeysoundToggle.isOn);
        if (n.lane >= PlayableLanes) noteInEditor.UseHiddenSprite();
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
            foreach (HoldNote n in channel.holdNotes)
            {
                SpawnNoteObject(n, channel.name);
            }
            foreach (DragNote n in channel.dragNotes)
            {
                SpawnNoteObject(n, channel.name);
            }
        }

        AdjustAllPathsAndTrails();
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
                HashSet<NoteType> types = new HashSet<NoteType>()
                        { NoteType.ChainHead, NoteType.ChainNode };
                GameObject prev = sortedNoteObjects.GetClosestNoteBefore(
                    o, types, minLaneInclusive: 0, maxLaneInclusive: PlayableLanes - 1);
                GameObject next = sortedNoteObjects.GetClosestNoteAfter(
                    o, types, minLaneInclusive: 0, maxLaneInclusive: PlayableLanes - 1);

                if (n.note.type == NoteType.ChainNode)
                {
                    o.GetComponent<NoteInEditor>().PointPathToward(prev);
                }
                if (next != null &&
                    next.GetComponent<NoteObject>().note.type == NoteType.ChainNode)
                {
                    next.GetComponent<NoteInEditor>().PointPathToward(o);
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
                HashSet<NoteType> types = new HashSet<NoteType>()
                    { NoteType.RepeatHead,
                    NoteType.RepeatHeadHold,
                    NoteType.Repeat,
                    NoteType.RepeatHold};
                GameObject prev = sortedNoteObjects.GetClosestNoteBefore(
                    o, types, minLaneInclusive: n.note.lane, maxLaneInclusive: n.note.lane);
                GameObject next = sortedNoteObjects.GetClosestNoteAfter(
                    o, types, minLaneInclusive: n.note.lane, maxLaneInclusive: n.note.lane);

                if (n.note.type == NoteType.Repeat ||
                    n.note.type == NoteType.RepeatHold)
                {
                    o.GetComponent<NoteInEditor>().PointPathToward(prev);
                }
                
                if (next != null)
                {
                    NoteType nextType = next.GetComponent<NoteObject>().note.type;
                    if (nextType == NoteType.Repeat ||
                        nextType == NoteType.RepeatHold)
                    {
                        next.GetComponent<NoteInEditor>().PointPathToward(o);
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
                .ResetAnchorsAndControlPoints();
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
                    HashSet<NoteType> types = new HashSet<NoteType>()
                        { NoteType.ChainHead, NoteType.ChainNode };
                    GameObject prev = sortedNoteObjects.GetClosestNoteBefore(
                        o, types, minLaneInclusive: 0, maxLaneInclusive: PlayableLanes - 1);
                    GameObject next = sortedNoteObjects.GetClosestNoteAfter(
                        o, types, minLaneInclusive: 0, maxLaneInclusive: PlayableLanes - 1);

                    if (next != null &&
                        next.GetComponent<NoteObject>().note.type == NoteType.ChainNode)
                    {
                        next.GetComponent<NoteInEditor>()
                            .PointPathToward(prev);
                    }
                    else if (prev != null &&
                        prev.GetComponent<NoteObject>().note.type == NoteType.ChainHead)
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
                    HashSet<NoteType> types = new HashSet<NoteType>()
                        { NoteType.RepeatHead,
                        NoteType.RepeatHeadHold,
                        NoteType.Repeat,
                        NoteType.RepeatHold};
                    GameObject prev = sortedNoteObjects.GetClosestNoteBefore(
                        o, types, minLaneInclusive: n.note.lane, maxLaneInclusive: n.note.lane);
                    GameObject next = sortedNoteObjects.GetClosestNoteAfter(
                        o, types, minLaneInclusive: n.note.lane, maxLaneInclusive: n.note.lane);

                    if (next != null)
                    {
                        NoteType nextType = next.GetComponent<NoteObject>().note.type;
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
        // Adjust the paths of chain nodes.
        List<GameObject> chainHeadsAndNodes = sortedNoteObjects.
            GetAllNotesOfType(new HashSet<NoteType>()
            { NoteType.ChainHead, NoteType.ChainNode},
            minLaneInclusive: 0, maxLaneInclusive: PlayableLanes - 1);
        GameObject previousChain = null;
        foreach (GameObject o in chainHeadsAndNodes)
        {
            NoteObject n = o.GetComponent<NoteObject>();
            if (n.note.type == NoteType.ChainNode)
            {
                n.GetComponent<NoteInEditor>().PointPathToward(previousChain);
            }
            previousChain = o;
        }

        // Adjust the paths of repeat notes.
        List<GameObject> repeatHeadsAndNotes = sortedNoteObjects.
            GetAllNotesOfType(new HashSet<NoteType>()
                { NoteType.RepeatHead,
                NoteType.Repeat,
                NoteType.RepeatHeadHold,
                NoteType.RepeatHold},
            minLaneInclusive: 0, maxLaneInclusive: PlayableLanes - 1);
        List<GameObject> previousRepeat = new List<GameObject>();
        for (int i = 0; i < PlayableLanes; i++) previousRepeat.Add(null);
        foreach (GameObject o in repeatHeadsAndNotes)
        {
            NoteObject n = o.GetComponent<NoteObject>();
            if (n.note.type == NoteType.Repeat ||
                n.note.type == NoteType.RepeatHold)
            {
                n.GetComponent<NoteInEditor>().PointPathToward(
                    previousRepeat[n.note.lane]);
            }
            previousRepeat[n.note.lane] = o;
        }

        // Adjust the trails of hold notes.
        List<GameObject> holdNotes = sortedNoteObjects.
            GetAllNotesOfType(new HashSet<NoteType>()
            { NoteType.Hold, NoteType.RepeatHeadHold, NoteType.RepeatHold},
            minLaneInclusive: 0,
            maxLaneInclusive: TotalLanes - 1);
        foreach (GameObject o in holdNotes)
        {
            o.GetComponent<NoteInEditor>().ResetTrail();
        }

        // Draw curves of drag notes.
        List<GameObject> dragNotes = sortedNoteObjects.
            GetAllNotesOfType(new HashSet<NoteType>()
            { NoteType.Drag },
            minLaneInclusive: 0,
            maxLaneInclusive: TotalLanes - 1);
        foreach (GameObject o in dragNotes)
        {
            o.GetComponent<NoteInEditor>().ResetCurve();
            o.GetComponent<NoteInEditor>()
                .ResetAnchorsAndControlPoints();
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
        GameObject noteAtSamePulseAndLane = sortedNoteObjects.GetAt(pulse, lane);
        if (noteAtSamePulseAndLane != null &&
            !ignoredExistingNotes.Contains(noteAtSamePulseAndLane))
        {
            reason = "Cannot place notes on top of an existing note.";
            return false;
        }

        // Chain check.
        if (type == NoteType.ChainHead || type == NoteType.ChainNode)
        {
            foreach (GameObject noteAtPulse in sortedNoteObjects.GetAt(pulse))
            {
                if (ignoredExistingNotes.Contains(noteAtPulse))
                {
                    continue;
                }
                NoteObject noteObject = noteAtPulse.GetComponent<NoteObject>();
                if (noteObject.note.type == NoteType.ChainHead ||
                    noteObject.note.type == NoteType.ChainNode)
                {
                    reason = "No two Chain Notes may occupy the same timepoint.";
                    return false;
                }
            }
        }

        // Hold check.
        GameObject holdNoteBeforePivot = sortedNoteObjects.GetClosestNoteBefore(pulse,
            new HashSet<NoteType>()
            {
                NoteType.Hold,
                NoteType.RepeatHeadHold,
                NoteType.RepeatHold
            }, minLaneInclusive: lane, maxLaneInclusive: lane);
        if (holdNoteBeforePivot != null && !ignoredExistingNotes.Contains(holdNoteBeforePivot))
        {
            HoldNote holdNote = holdNoteBeforePivot.GetComponent<NoteObject>().note as HoldNote;
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

        GameObject noteAfterPivot = sortedNoteObjects.GetClosestNoteAfter(
            pulse, types: null,
            minLaneInclusive: lane,
            maxLaneInclusive: lane);
        if (noteAfterPivot != null &&
            !ignoredExistingNotes.Contains(noteAfterPivot))
        {
            if (pulse + duration >= noteAfterPivot.GetComponent<NoteObject>().note.pulse)
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
            GameObject existingNote = sortedNoteObjects.GetAt(
                anchorPulse, anchorLane);

            if (existingNote != null &&
                !ignoredExistingNotes.Contains(existingNote))
            {
                return true;
            }
        }

        return false;
    }

    private int HoldNoteDefaultDuration(int pulse, int lane)
    {
        GameObject noteAfterPivot = sortedNoteObjects.GetClosestNoteAfter(
            pulse, types: null,
            minLaneInclusive: lane,
            maxLaneInclusive: lane);
        if (noteAfterPivot != null)
        {
            int nextPulse = noteAfterPivot.GetComponent<NoteObject>().note.pulse;
            if (nextPulse - pulse <= Pattern.pulsesPerBeat)
            {
                return nextPulse - pulse - 1;
            }
        }
        return Pattern.pulsesPerBeat;
    }

    private GameObject FinishAddNote(Note n, string sound)
    {
        // Add to pattern.
        EditorContext.Pattern.AddNote(n, sound);

        // Add to UI.
        GameObject newNote = SpawnNoteObject(n, sound);
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
            lane = lane
        };
        return FinishAddNote(n, sound);
    }

    private GameObject AddHoldNote(NoteType type, int pulse, int lane,
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
            duration = duration.Value
        };
        return FinishAddNote(n, sound);
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
                controlBefore = new FloatPoint(0f, 0f),
                controlAfter = new FloatPoint(0f, 0f)
            });
            nodes.Add(new DragNode()
            {
                anchor = new IntPoint(relativePulseOfLastNode, 0),
                controlBefore = new FloatPoint(0f, 0f),
                controlAfter = new FloatPoint(0f, 0f)
            });
        }
        DragNote n = new DragNote()
        {
            type = NoteType.Drag,
            pulse = pulse,
            lane = lane,
            nodes = nodes
        };
        return FinishAddNote(n, sound);
    }

    // Cannot remove o from selectedNoteObjects because the
    // caller may be enumerating that list.
    private void DeleteNote(GameObject o)
    {
        // Delete from pattern.
        NoteObject n = o.GetComponent<NoteObject>();
        EditorContext.Pattern.DeleteNote(n.note, n.sound);

        // Delete from UI.
        AdjustPathBeforeDeleting(o);
        sortedNoteObjects.Delete(o);
        if (lastSelectedNoteObjectWithoutShift == o)
        {
            lastSelectedNoteObjectWithoutShift = null;
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
            clipboard = new List<NoteWithSound>();
        }
        clipboard.Clear();
        minPulseInClipboard = int.MaxValue;
        foreach (GameObject o in selectedNoteObjects)
        {
            NoteWithSound n = ConvertToNoteWithSound(o);
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

        // Can we paste here?
        bool pastable = true;
        string invalidReason = "";
        foreach (NoteWithSound n in clipboard)
        {
            int newPulse = n.note.pulse + deltaPulse;
            switch (n.note.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    pastable = CanAddNote(n.note.type, newPulse,
                        n.note.lane, out invalidReason);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    pastable = CanAddHoldNote(n.note.type,
                        newPulse, n.note.lane,
                        (n.note as HoldNote).duration,
                        ignoredExistingNotes: null,
                        out invalidReason);
                    break;
                case NoteType.Drag:
                    pastable = CanAddDragNote(newPulse, n.note.lane,
                        (n.note as DragNote).nodes,
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
        foreach (NoteWithSound n in clipboard)
        {
            int newPulse = n.note.pulse + deltaPulse;
            switch (n.note.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    AddNote(n.note.type, newPulse,
                        n.note.lane, n.sound);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    AddHoldNote(n.note.type, newPulse,
                        n.note.lane, (n.note as HoldNote).duration,
                        n.sound);
                    break;
                case NoteType.Drag:
                    AddDragNote(newPulse, n.note.lane,
                        (n.note as DragNote).nodes,
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
    private bool isPlaying;
    private float playbackStartingPulse;
    private float playbackStartingTime;
    private DateTime systemTimeOnPlaybackStart;

    // Each queue represents one lane; each lane is sorted by pulse.
    // Played notes are popped from the corresponding queue. This
    // makes it easy to tell if it's time to play the next note
    // in each lane.
    //
    // This data structure is only used for playback, so it's not
    // defined in the Internal Data Structures region.
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
            foreach (GameObject o in sortedNoteObjects.GetAt(pulse))
            {
                NoteObject n = o.GetComponent<NoteObject>();
                notesInLanes[n.note.lane].Enqueue(ConvertToNoteWithSound(o));
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
