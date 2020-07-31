using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Note to self: (0, 0) is bottom-left.
public class PatternPanel : MonoBehaviour
{
    [Header("Pattern Container")]
    public ScrollRect scrollRect;
    public Transform patternContainer;
    public Transform lineAndMarkerContainer;
    public Transform noteObjectContainer;
    public GameObject markerTemplate;
    public GameObject lineTemplate;
    public GameObject dottedLineTemplate;
    public GameObject laneDividers;
    public GameObject cursor;
    public GameObject scanline;

    public static float ScanWidth
    {
        get
        {
            return 1500f * zoom * 0.01f;
        }
    }
    
    private float containerWidth
    {
        get
        {
            return ScanWidth * numScans;
        }
    }

    private int numScans;
    private static int zoom;
    private int divisionsPerBeat;
    private SortedNoteObjects sortedNoteObjects;
    private GameObject lastSelectedNoteObjectWithoutShift;
    private HashSet<GameObject> selectedNoteObjects;

    [Header("UI and options")]
    public Button cutButton;
    public Button copyButton;
    public Button pasteButton;
    public Button deleteButton;
    public Text divisionsPerBeatDisplay;
    public Text selectedKeysoundsDisplay;
    public Button modifySelectedKeysoundsButton;
    public Text upcomingKeysoundDisplay;
    private List<string> upcomingKeysounds;
    private int upcomingKeysoundIndex;

    [Header("Note Prefabs")]
    public GameObject basicNote;

    public static event UnityAction RepositionNeeded;
    public static event UnityAction<HashSet<GameObject>> SelectionChanged;

    private int snappedCursorPulse;
    private int snappedCursorLane;

    #region Spawning Markers and Lines
    private void SpawnLine(int scan, GameObject template)
    {
        GameObject line = Instantiate(template, lineAndMarkerContainer);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.Line;
        element.scan = scan;
    }

    private void SpawnDottedLine(int beat, GameObject template)
    {
        GameObject line = Instantiate(template, lineAndMarkerContainer);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.DottedLine;
        element.beat = beat;
    }
    
    private void SpawnScanBasedMarker(EditorElement.Type type, int scan, string text)
    {
        GameObject marker = Instantiate(markerTemplate, lineAndMarkerContainer);
        marker.GetComponentInChildren<Text>().text = text;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.scan = scan;
    }

    private void SpawnBeatBasedMarker(EditorElement.Type type, int beat, string text)
    {
        GameObject marker = Instantiate(markerTemplate, lineAndMarkerContainer);
        marker.GetComponentInChildren<Text>().text = text;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.beat = beat;
    }

    private void ResizeContainer()
    {
        RectTransform containerRect = patternContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector3(
            containerWidth,
            containerRect.sizeDelta.y);
    }

    private void SpawnMarkersAndLines()
    {
        for (int i = 0; i < lineAndMarkerContainer.childCount; i++)
        {
            Destroy(lineAndMarkerContainer.GetChild(i).gameObject);
        }

        Pattern pattern = Navigation.GetCurrentPattern();

        // Scan based stuff
        for (int scan = 0; scan <= numScans; scan++)
        {
            SpawnLine(scan, lineTemplate);
            SpawnScanBasedMarker(EditorElement.Type.ScanMarker,
                scan, $"Scan {scan}");
        }

        // Beat based stuff
        int bps = pattern.patternMetadata.bps;
        double secondsPerBeat = 60.0 / pattern.patternMetadata.initBpm;
        for (int beat = 0; beat < numScans * bps ; beat++)
        {
            float time = (float)(secondsPerBeat * beat +
                pattern.patternMetadata.firstBeatOffset);
            int minute = Mathf.FloorToInt(time / 60f);
            time -= minute * 60f;
            int second = Mathf.FloorToInt(time);
            time -= second;
            int milliSecond = Mathf.FloorToInt(time * 1000f);

            SpawnDottedLine(beat, dottedLineTemplate);
            SpawnBeatBasedMarker(EditorElement.Type.BeatMarker,
                beat, $"Beat {beat}");
            SpawnBeatBasedMarker(EditorElement.Type.TimeMarker,
                beat, $"{minute}:{second:D2}.{milliSecond:D3}");
        }
    }
    #endregion

    private void SpawnNoteObject(Note n, string sound)
    {
        GameObject noteObject = Instantiate(basicNote, noteObjectContainer);
        noteObject.GetComponentInChildren<Text>().text =
            UIUtils.StripExtension(sound);

        EditorElement element = noteObject.GetComponent<EditorElement>();
        element.type = EditorElement.Type.Note;
        element.note = n;
        element.sound = sound;
        element.Reposition();

        sortedNoteObjects.Add(noteObject);
    }

    private void SpawnExistingNotes()
    {
        for (int i = 0; i < noteObjectContainer.childCount; i++)
        {
            Destroy(noteObjectContainer.GetChild(i).gameObject);
        }

        sortedNoteObjects = new SortedNoteObjects();
        lastSelectedNoteObjectWithoutShift = null;
        selectedNoteObjects = new HashSet<GameObject>();
        foreach (SoundChannel channel in Navigation.GetCurrentPattern().soundChannels)
        {
            foreach (Note n in channel.notes)
            {
                SpawnNoteObject(n, channel.name);
            }
        }
    }

    private void OnEnable()
    {
        numScans = 4;
        zoom = 100;
        divisionsPerBeat = 2;
        upcomingKeysounds = new List<string>();
        upcomingKeysounds.Add("");
        upcomingKeysoundIndex = 0;
        clipboard = new List<NoteWithSound>();

        // MemoryToUI();

        EditorElement.LeftClicked += OnNoteObjectLeftClick;
        EditorElement.RightClicked += OnNoteObjectRightClick;
        EditorElement.BeginDrag += OnNoteObjectBeginDrag;
        EditorElement.Drag += OnNoteObjectDrag;
        EditorElement.EndDrag += OnNoteObjectEndDrag;
    }

    private void OnDisable()
    {
        EditorElement.LeftClicked -= OnNoteObjectLeftClick;
        EditorElement.RightClicked -= OnNoteObjectRightClick;
        EditorElement.BeginDrag -= OnNoteObjectBeginDrag;
        EditorElement.Drag -= OnNoteObjectDrag;
        EditorElement.EndDrag -= OnNoteObjectEndDrag;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
            {
                // Adjust zoom
                zoom += Mathf.FloorToInt(Input.mouseScrollDelta.y * 10f);
                zoom = Mathf.Clamp(zoom, 10, 500);
                // Debug.Log($"zoom={zoom} ScanLength={ScanLength}");
                float horizontal = scrollRect.horizontalNormalizedPosition;

                ResizeContainer();
                RepositionNeeded?.Invoke();

                scrollRect.horizontalNormalizedPosition = horizontal;
            }
            else
            {
                // Scroll
                scrollRect.horizontalNormalizedPosition +=
                    Input.mouseScrollDelta.y * 0.05f;
            }
        }

        SnapCursorAndScanline();

        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
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

    public void MemoryToUI()
    {
        ResizeContainer();
        SpawnMarkersAndLines();
        SpawnExistingNotes();
        RepositionNeeded?.Invoke();
        RefreshControls();
    }

    private void RefreshControls()
    {
        // Edit panel
        cutButton.interactable = selectedNoteObjects.Count > 0;
        copyButton.interactable = selectedNoteObjects.Count > 0;
        pasteButton.interactable = clipboard.Count > 0;
        deleteButton.interactable = selectedNoteObjects.Count > 0;

        // Timing panel
        divisionsPerBeatDisplay.text = divisionsPerBeat.ToString();

        // Keysounds panel
        UpdateSelectedKeysoundDisplay();
        UpdateUpcomingKeysoundDisplay();
    }

    private void SnapCursorAndScanline()
    {
        if (Input.mousePosition.x < 0 ||
            Input.mousePosition.x > Screen.width ||
            Input.mousePosition.y < 0 ||
            Input.mousePosition.y > Screen.height)
        {
            cursor.SetActive(false);
            return;
        }

        Vector2 pointInContainer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            patternContainer.GetComponent<RectTransform>(),
            Input.mousePosition,
            cam: null,
            out pointInContainer);

        float cursorScan = pointInContainer.x / ScanWidth;
        float cursorBeat = cursorScan *
            Navigation.GetCurrentPattern().patternMetadata.bps;
        float cursorPulse = cursorBeat * Pattern.pulsesPerBeat;
        int pulsesPerDivision = Pattern.pulsesPerBeat / divisionsPerBeat;
        snappedCursorPulse = Mathf.RoundToInt(cursorPulse / pulsesPerDivision)
            * pulsesPerDivision;
        float snappedScan = (float)snappedCursorPulse / Pattern.pulsesPerBeat
            / Navigation.GetCurrentPattern().patternMetadata.bps;
        float snappedX = snappedScan * ScanWidth;

        const float kHeightAboveFirstLane = 80f;

        snappedCursorLane = Mathf.FloorToInt((pointInContainer.y + kHeightAboveFirstLane)
            / -100f);
        float snappedY = -130f - 100f * snappedCursorLane;

        if (snappedX >= 0f &&
            snappedX <= containerWidth &&
            snappedCursorLane >= 0 &&
            snappedCursorLane <= 3)
        {
            cursor.SetActive(true);
            cursor.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(snappedX, snappedY);
        }
        else
        {
            cursor.SetActive(false);
        }

        if (snappedX >= 0f &&
            snappedX <= containerWidth &&
            pointInContainer.y <= 0f &&
            pointInContainer.y >= -kHeightAboveFirstLane &&
            Input.GetMouseButton(0))
        {
            // Move scanline to cursor
            EditorElement scanlineElement = scanline.GetComponent<EditorElement>();
            scanlineElement.pulse = snappedCursorPulse;
            scanlineElement.Reposition();
        }
    }

    public void ModifyDevisionsPerBeat(int direction)
    {
        do
        {
            divisionsPerBeat += direction;
            if (divisionsPerBeat <= 0 && direction < 0)
            {
                divisionsPerBeat = Pattern.pulsesPerBeat;
            }
            if (divisionsPerBeat > Pattern.pulsesPerBeat && direction > 0)
            {
                divisionsPerBeat = 1;
            }
        }
        while (Pattern.pulsesPerBeat % divisionsPerBeat != 0);
        RefreshControls();
    }

    public void OnClickPatternContainer(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        if ((eventData as PointerEventData).button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }
        if (!cursor.activeSelf) return;
        if (sortedNoteObjects.HasAt(
            snappedCursorPulse, snappedCursorLane))
        {
            return;
        }

        // Add note to pattern
        string sound = upcomingKeysounds[upcomingKeysoundIndex];
        upcomingKeysoundIndex = (upcomingKeysoundIndex + 1) % upcomingKeysounds.Count;
        Note n = new Note();
        n.pulse = snappedCursorPulse;
        n.lane = snappedCursorLane;
        n.type = NoteType.Basic;
        Navigation.PrepareForChange();
        Navigation.GetCurrentPattern().AddNote(n, sound);
        Navigation.DoneWithChange();

        // Add note to UI
        SpawnNoteObject(n, sound);
        UpdateUpcomingKeysoundDisplay();
    }

    #region Left and Right click on note objects
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
        RefreshControls();
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
        Navigation.PrepareForChange();
        Navigation.GetCurrentPattern().DeleteNote(e.note, e.sound);
        Navigation.DoneWithChange();

        // Delete note from UI
        sortedNoteObjects.Delete(o);
        if (lastSelectedNoteObjectWithoutShift == o)
        {
            lastSelectedNoteObjectWithoutShift = null;
        }
        selectedNoteObjects.Remove(o);
        RefreshControls();
        Destroy(o);
    }
    #endregion

    #region Drag and Drop
    private GameObject draggedNoteObject;
    private void OnNoteObjectBeginDrag(GameObject o)
    {
        draggedNoteObject = o;
        if (!selectedNoteObjects.Contains(o))
        {
            selectedNoteObjects.Clear();
            selectedNoteObjects.Add(o);

            SelectionChanged?.Invoke(selectedNoteObjects);
            RefreshControls();
        }
    }

    private void OnNoteObjectDrag(Vector2 delta)
    {
        foreach (GameObject o in selectedNoteObjects)
        {
            // This is only visual. Notes are only really moved
            // in EndDrag.
            o.GetComponent<RectTransform>().anchoredPosition += delta;
        }
    }

    private void OnNoteObjectEndDrag()
    {
        // Calculate delta pulse and delta lane
        EditorElement element = draggedNoteObject.GetComponent<EditorElement>();
        int oldPulse = element.note.pulse;
        int oldLane = element.note.lane;
        int deltaPulse = snappedCursorPulse - oldPulse;
        int deltaLane = snappedCursorLane - oldLane;

        // Is the movement applicable to all notes?
        int minPulse = 0;
        int pulsesPerScan = Navigation.GetCurrentPattern().patternMetadata.bps
            * Pattern.pulsesPerBeat;
        int maxPulse = numScans * pulsesPerScan;
        int minLane = 0;
        int maxLane = 3;
        bool movable = true;
        int addedScans = 0;
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
            if (newPulse < minPulse ||
                newLane < minLane ||
                newLane > maxLane)
            {
                movable = false;
                break;
            }
            while (newPulse >= maxPulse)
            {
                addedScans++;
                maxPulse += pulsesPerScan;
            }
        }

        if (movable)
        {
            // Add scans if needed.
            if (addedScans > 0)
            {
                numScans += addedScans;
                ResizeContainer();
                SpawnMarkersAndLines();
                RepositionNeeded?.Invoke();
            }

            // Apply move.
            Navigation.PrepareForChange();
            foreach (GameObject o in selectedNoteObjects)
            {
                sortedNoteObjects.Delete(o);
            }
            foreach (GameObject o in selectedNoteObjects)
            {
                Note n = o.GetComponent<EditorElement>().note;
                n.pulse += deltaPulse;
                n.lane += deltaLane;
                sortedNoteObjects.Add(o);
            }
            Navigation.DoneWithChange();
        }

        foreach (GameObject o in selectedNoteObjects)
        {
            o.GetComponent<EditorElement>().Reposition();
        }
    }
    #endregion

    #region Keysounds
    private string KeysoundName(string filename)
    {
        if (filename == "") return UIUtils.kEmptyKeysoundDisplayText;
        return UIUtils.StripExtension(filename);
    }

    private void UpdateUpcomingKeysoundDisplay()
    {
        string display = KeysoundName(upcomingKeysounds[upcomingKeysoundIndex]);
        if (upcomingKeysounds.Count > 1)
        {
            display += $" ({upcomingKeysoundIndex + 1}/{upcomingKeysounds.Count})";
        }

        upcomingKeysoundDisplay.text = display;
    }

    public void ModifyUpcomingKeysounds()
    {
        StartCoroutine(InternalModifyUpcomingKeysounds());
    }

    private IEnumerator InternalModifyUpcomingKeysounds()
    {
        SelectKeysoundDialog.Show("Select keysounds to apply to new notes. " +
            "You can select multiple, and they will apply successively.",
            upcomingKeysounds);
        yield return new WaitUntil(() =>
        {
            return SelectKeysoundDialog.IsResolved();
        });
        if (SelectKeysoundDialog.GetResult() ==
            SelectKeysoundDialog.Result.Cancelled)
        {
            yield return null;
        }

        upcomingKeysounds = SelectKeysoundDialog.GetSelectedKeysounds();
        upcomingKeysoundIndex = 0;
        RefreshControls();
    }

    private void UpdateSelectedKeysoundDisplay()
    {
        HashSet<string> keysounds = new HashSet<string>();
        foreach (GameObject noteObject in selectedNoteObjects)
        {
            keysounds.Add(noteObject.GetComponent<EditorElement>().sound);
        }

        if (keysounds.Count == 0)
        {
            // Assume empty selection.
            selectedKeysoundsDisplay.text =
                UIUtils.kEmptyKeysoundDisplayText;
            modifySelectedKeysoundsButton.interactable = false;
        }
        else if (keysounds.Count == 1)
        {
            HashSet<string>.Enumerator enumerator = keysounds.GetEnumerator();
            enumerator.MoveNext();
            selectedKeysoundsDisplay.text = 
                KeysoundName(enumerator.Current);
            modifySelectedKeysoundsButton.interactable = true;
        }
        else
        {
            selectedKeysoundsDisplay.text = "(Multiple)";
            modifySelectedKeysoundsButton.interactable = true;
        }
    }

    public void ModifySelectedKeysounds()
    {
        StartCoroutine(InternalModifySelectedKeysounds());
    }

    private IEnumerator InternalModifySelectedKeysounds()
    {
        SelectKeysoundDialog.Show("Select keysounds to apply to selected notes. " +
            "You can select multiple, and they will apply successively.",
            upcomingKeysounds);
        yield return new WaitUntil(() =>
        {
            return SelectKeysoundDialog.IsResolved();
        });
        if (SelectKeysoundDialog.GetResult() ==
            SelectKeysoundDialog.Result.Cancelled)
        {
            yield return null;
        }

        // Sort selection by pulse, then by lane.
        List<EditorElement> selectionAsList = new List<EditorElement>();
        foreach (GameObject o in selectedNoteObjects)
        {
            selectionAsList.Add(o.GetComponent<EditorElement>());
        }
        selectionAsList.Sort((EditorElement e1, EditorElement e2) =>
        {
            if (e1.pulse != e2.pulse)
            {
                return e1.pulse - e2.pulse;
            }
            else
            {
                return e1.lane - e2.lane;
            }
        });

        // Apply new keysounds.
        List<string> newKeysounds = SelectKeysoundDialog.GetSelectedKeysounds();
        Navigation.PrepareForChange();
        for (int i = 0; i < selectionAsList.Count; i++)
        {
            int soundIndex = i % newKeysounds.Count;
            string newSound = newKeysounds[soundIndex];

            // Update pattern
            Navigation.GetCurrentPattern().ModifyNoteKeysound(
                selectionAsList[i].note, selectionAsList[i].sound,
                newSound);

            // Update in display
            selectionAsList[i].sound = newKeysounds[soundIndex];
            selectionAsList[i].GetComponentInChildren<Text>().text =
                UIUtils.StripExtension(newSound);
        }
        Navigation.DoneWithChange();

        RefreshControls();
    }
    #endregion

    #region Scans
    public void AddScan()
    {
        numScans++;

        ResizeContainer();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
    }

    public void TrimTrailingScans()
    {
        // Which scan is the last note in?
        GameObject lastNoteObject = sortedNoteObjects.GetLast();
        if (lastNoteObject == null) return;
        Note lastNote = lastNoteObject.GetComponent<EditorElement>().note;
        int lastPulse = lastNote.pulse;
        int pulsesPerScan = Pattern.pulsesPerBeat *
            Navigation.GetCurrentPattern().patternMetadata.bps;
        int lastScan = lastPulse / pulsesPerScan;
        numScans = lastScan + 1;

        // Move scanline if needed.
        EditorElement scanlineElement = scanline.GetComponent<EditorElement>();
        if (scanlineElement.pulse > lastPulse)
        {
            scanlineElement.pulse = lastPulse;
        }
        
        ResizeContainer();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
    }
    #endregion

    #region Cut Copy Paste Delete
    private class NoteWithSound
    {
        public Note note;
        public string sound;
        public static NoteWithSound MakeFrom(GameObject o)
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
    private List<NoteWithSound> clipboard;
    public void CutSelection()
    {
        if (selectedNoteObjects.Count == 0) return;
        CopySelection();
        DeleteSelection();
    }

    public void CopySelection()
    {
        if (selectedNoteObjects.Count == 0) return;

        clipboard.Clear();
        foreach (GameObject o in selectedNoteObjects)
        {
            clipboard.Add(NoteWithSound.MakeFrom(o));
        }
    }

    public void PasteAtScanline()
    {

    }

    public void DeleteSelection()
    {
        if (selectedNoteObjects.Count == 0) return;

        // Delete notes from pattern.
        Navigation.PrepareForChange();
        foreach (GameObject o in selectedNoteObjects)
        {
            EditorElement e = o.GetComponent<EditorElement>();
            Navigation.GetCurrentPattern().DeleteNote(e.note, e.sound);
        }
        Navigation.DoneWithChange();

        // Delete notes from UI.
        foreach (GameObject o in selectedNoteObjects)
        {
            sortedNoteObjects.Delete(o);
            Destroy(o);
        }
        lastSelectedNoteObjectWithoutShift = null;
        selectedNoteObjects.Clear();
        RefreshControls();
    }
    #endregion
}
