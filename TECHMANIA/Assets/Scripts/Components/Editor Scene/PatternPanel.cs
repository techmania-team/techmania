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
    public EditorElement scanline;

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
    public EditorElement noteCursor;
    public GameObject basicNotePrefab;
    public GameObject hiddenNotePrefab;

    [Header("UI And Options")]
    public TextMeshProUGUI beatSnapDividerDisplay;
    public KeysoundSideSheet keysoundSheet;
    public MessageDialog messageDialog;

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
        public static NoteWithSound FromEditorElement(GameObject o)
        {
            EditorElement e = o.GetComponent<EditorElement>();
            return new NoteWithSound()
            {
                note = e.note.Clone(),
                sound = e.sound
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

    #region Outward Events
    public static event UnityAction RepositionNeeded;
    public static event UnityAction<HashSet<GameObject>> SelectionChanged;
    #endregion

    #region MonoBehavior APIs
    private void OnEnable()
    {
        // TODO: save this to game options and load from disk

        // Vertical spacing
        HiddenLanes = 4;
        Canvas.ForceUpdateCanvases();
        AllLaneTotalHeight = laneDividerParent.rect.height;

        // Horizontal spacing
        zoom = 100;
        beatSnapDivisor = 2;

        // Scanline
        scanline.floatPulse = 0f;
        scanline.Reposition();

        // UI
        UpdateBeatSnapDivisorDisplay();

        Refresh();
        EditorContext.UndoneOrRedone += Refresh;
        EditorElement.LeftClicked += OnNoteObjectLeftClick;
        EditorElement.RightClicked += OnNoteObjectRightClick;
        EditorElement.BeginDrag += OnNoteObjectBeginDrag;
        EditorElement.Drag += OnNoteObjectDrag;
        EditorElement.EndDrag += OnNoteObjectEndDrag;
    }

    private void OnDisable()
    {
        EditorContext.UndoneOrRedone -= Refresh;
        EditorElement.LeftClicked -= OnNoteObjectLeftClick;
        EditorElement.RightClicked -= OnNoteObjectRightClick;
        EditorElement.BeginDrag -= OnNoteObjectBeginDrag;
        EditorElement.Drag -= OnNoteObjectDrag;
        EditorElement.EndDrag -= OnNoteObjectEndDrag;
    }

    // Update is called once per frame
    void Update()
    {
        if (messageDialog.gameObject.activeSelf)
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

        if (Input.GetMouseButton(0) && mouseInWorkspace && mouseInHeader)
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
        int pulsesPerDivision = Pattern.pulsesPerBeat / beatSnapDivisor;
        int snappedCursorPulse = Mathf.RoundToInt(cursorPulse / pulsesPerDivision)
            * pulsesPerDivision;

        int snappedLane = Mathf.FloorToInt(-pointInContainer.y / LaneHeight);

        noteCursor.note = new Note();
        noteCursor.note.pulse = snappedCursorPulse;
        noteCursor.note.lane = snappedLane;
        noteCursor.Reposition();
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
        int pulsesPerDivision = Pattern.pulsesPerBeat / beatSnapDivisor;
        int snappedCursorPulse = Mathf.RoundToInt(cursorPulse / pulsesPerDivision)
            * pulsesPerDivision;

        scanline.floatPulse = snappedCursorPulse;
        scanline.Reposition();
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
        // Delete note from pattern
        EditorElement e = o.GetComponent<EditorElement>();
        EditorContext.PrepareForChange();
        EditorContext.Pattern.DeleteNote(e.note, e.sound);
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

    #region Events From UI
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
    #endregion

    #region Dragging And Dropping Notes
    private GameObject draggedNoteObject;
    private void OnNoteObjectBeginDrag(GameObject o)
    {
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
        foreach (GameObject o in selectedNoteObjects)
        {
            // This is only visual. Notes are only really moved
            // in OnNoteObjectEndDrag.
            o.GetComponent<RectTransform>().anchoredPosition += delta;
        }
    }

    private void OnNoteObjectEndDrag()
    {
        // Calculate delta pulse and delta lane
        EditorElement element = draggedNoteObject.GetComponent<EditorElement>();
        int oldPulse = element.note.pulse;
        int oldLane = element.note.lane;
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
            Note n = o.GetComponent<EditorElement>().note;
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
                Note n = o.GetComponent<EditorElement>().note;
                n.pulse += deltaPulse;
                n.lane += deltaLane;
                GameObject newO = SpawnNoteObject(
                    n, o.GetComponent<EditorElement>().sound);
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
            o.GetComponent<EditorElement>().Reposition();
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

        int lastPulse = o.GetComponent<EditorElement>().note.pulse;
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
    #endregion

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

        EditorContext.Pattern.PrepareForTimeCalculation();
        int bps = EditorContext.Pattern.patternMetadata.bps;
        List<KeyValuePair<Transform, EditorElement.Type>> allMarkers =
            new List<KeyValuePair<Transform, EditorElement.Type>>();
        for (int scan = 0; scan < numScans; scan++)
        {
            GameObject marker = Instantiate(scanMarkerTemplate, markerContainer);
            marker.SetActive(true);  // This calls OnEnabled
            EditorElement element = marker.GetComponent<EditorElement>();
            element.pulse = scan * bps * Pattern.pulsesPerBeat;
            element.SetTimeDisplay();
            element.Reposition();
            allMarkers.Add(new KeyValuePair<Transform, EditorElement.Type>(
                marker.transform, element.type));

            for (int beat = 1; beat < bps; beat++)
            {
                marker = Instantiate(beatMarkerTemplate, markerContainer);
                marker.SetActive(true);
                element = marker.GetComponent<EditorElement>();
                element.pulse = (scan * bps + beat) * Pattern.pulsesPerBeat;
                element.SetTimeDisplay();
                element.Reposition();
                allMarkers.Add(new KeyValuePair<Transform, EditorElement.Type>(
                    marker.transform, element.type));
            }
        }

        foreach (BpmEvent e in EditorContext.Pattern.bpmEvents)
        {
            GameObject marker = Instantiate(bpmMarkerTemplate, markerContainer);
            marker.SetActive(true);
            EditorElement element = marker.GetComponent<EditorElement>();
            element.pulse = e.pulse;
            element.SetBpmText(e.bpm);
            element.Reposition();
            allMarkers.Add(new KeyValuePair<Transform, EditorElement.Type>(
                marker.transform, element.type));
        }

        // Sort all markers so they are drawn from left to right.
        allMarkers.Sort((
            KeyValuePair<Transform, EditorElement.Type> p1,
            KeyValuePair<Transform, EditorElement.Type> p2) =>
        {
            float deltaX = p1.Key.position.x - p2.Key.position.x;
            if (deltaX < 0) return -1;
            if (deltaX > 0) return 1;
            // At the same position, BPM markers should be drawn later.
            if (p1.Value == EditorElement.Type.BpmMarker) return 1;
            if (p2.Value == EditorElement.Type.BpmMarker) return -1;
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
        EditorElement noteObject = Instantiate(prefab,
            noteContainer).GetComponent<EditorElement>();
        noteObject.note = n;
        noteObject.sound = sound;
        noteObject.SetKeysoundText();
        noteObject.Reposition();

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
            NoteWithSound n = NoteWithSound.FromEditorElement(o);
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

        // Delete notes from pattern.
        EditorContext.PrepareForChange();
        foreach (GameObject o in selectedNoteObjects)
        {
            EditorElement e = o.GetComponent<EditorElement>();
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
}
