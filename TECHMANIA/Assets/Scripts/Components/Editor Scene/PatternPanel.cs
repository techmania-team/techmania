using System;
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
    public Button selectAllButton;
    public Button cutButton;
    public Button copyButton;
    public Button pasteButton;
    public Button deleteButton;
    public Text divisionsPerBeatDisplay;
    public Text bpmEventDisplay;
    public Button addBpmEventButton;
    public Button modifyBpmEventButton;
    public Button deleteBpmEventButton;
    public Text selectedKeysoundsDisplay;
    public Button modifySelectedKeysoundsButton;
    public Text upcomingKeysoundDisplay;
    private List<string> upcomingKeysounds;
    private int upcomingKeysoundIndex;

    [Header("Note Prefabs")]
    public GameObject basicNote;

    [Header("Audio")]
    public ResourceLoader resourceLoader;
    public GameObject playButton;
    public GameObject stopButton;
    public AudioSource backingTrackSource;
    public List<AudioSource> keysoundSources;

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

    private void SpawnPulseBasedMarker(EditorElement.Type type, int pulse, string text)
    {
        GameObject marker = Instantiate(markerTemplate, lineAndMarkerContainer);
        marker.GetComponentInChildren<Text>().text = text;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.pulse = pulse;
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

        Pattern pattern = EditorNavigation.GetCurrentPattern();
        pattern.PrepareForTimeCalculation();

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
            int pulse = beat * Pattern.pulsesPerBeat;
            float time = pattern.PulseToTime(pulse);
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

        // Pulse based stuff
        foreach (BpmEvent e in pattern.bpmEvents)
        {
            SpawnPulseBasedMarker(EditorElement.Type.BpmMarker,
                e.pulse, $"BPM {e.bpm}");
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

        // For newly created patterns, there's no sound channel yet.
        if (EditorNavigation.GetCurrentPattern().soundChannels == null)
        {
            EditorNavigation.GetCurrentPattern().CreateListsIfNull();
        }
        foreach (SoundChannel channel in EditorNavigation.GetCurrentPattern().soundChannels)
        {
            foreach (Note n in channel.notes)
            {
                SpawnNoteObject(n, channel.name);
            }
        }
    }

    private void OnEnable()
    {
        zoom = 100;
        divisionsPerBeat = 2;
        upcomingKeysounds = new List<string>();
        upcomingKeysounds.Add("");
        upcomingKeysoundIndex = 0;
        clipboard = new List<NoteWithSound>();
        isPlaying = false;

        // Calculate initial number of scans.
        int lastPulse = 0;
        foreach (SoundChannel c in EditorNavigation.GetCurrentPattern().soundChannels)
        {
            foreach (Note n in c.notes)
            {
                if (n.pulse > lastPulse) lastPulse = n.pulse;
            }
        }
        int pulsesPerScan = Pattern.pulsesPerBeat *
            EditorNavigation.GetCurrentPattern().patternMetadata.bps;
        numScans = lastPulse / pulsesPerScan + 1;

        // MemoryToUI();
        resourceLoader.LoadResources(EditorNavigation.GetCurrentTrackPath());

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
        if (isPlaying)
        {
            UpdatePlayback();
        }
        if (ModalDialog.IsAnyModalDialogActive())
        {
            return;
        }

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
        selectAllButton.interactable = true;
        cutButton.interactable = selectedNoteObjects.Count > 0;
        copyButton.interactable = selectedNoteObjects.Count > 0;
        pasteButton.interactable = clipboard.Count > 0;
        deleteButton.interactable = selectedNoteObjects.Count > 0;
        if (isPlaying)
        {
            selectAllButton.interactable = false;
            cutButton.interactable = false;
            copyButton.interactable = false;
            pasteButton.interactable = false;
            deleteButton.interactable = false;
        }

        // Timing panel
        divisionsPerBeatDisplay.text = divisionsPerBeat.ToString();
        UpdateBpmEventDisplay();

        // Keysounds panel
        UpdateSelectedKeysoundDisplay();
        UpdateUpcomingKeysoundDisplay();

        // Playback panel
        RefreshPlaybackPanel();
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
            EditorNavigation.GetCurrentPattern().patternMetadata.bps;
        float cursorPulse = cursorBeat * Pattern.pulsesPerBeat;
        int pulsesPerDivision = Pattern.pulsesPerBeat / divisionsPerBeat;
        snappedCursorPulse = Mathf.RoundToInt(cursorPulse / pulsesPerDivision)
            * pulsesPerDivision;
        float snappedScan = (float)snappedCursorPulse / Pattern.pulsesPerBeat
            / EditorNavigation.GetCurrentPattern().patternMetadata.bps;
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
            scanlineElement.floatPulse = snappedCursorPulse;
            scanlineElement.Reposition();

            UpdateBpmEventDisplay();
        }
    }

    private int ScanlinePulse()
    {
        return (int)scanline.GetComponent<EditorElement>().floatPulse;
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
        EditorNavigation.PrepareForChange();
        EditorNavigation.GetCurrentPattern().AddNote(n, sound);
        EditorNavigation.DoneWithChange();

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
        if (isPlaying) return;

        // Delete note from pattern
        EditorElement e = o.GetComponent<EditorElement>();
        EditorNavigation.PrepareForChange();
        EditorNavigation.GetCurrentPattern().DeleteNote(e.note, e.sound);
        EditorNavigation.DoneWithChange();

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
        if (isPlaying) return;

        draggedNoteObject = o;
        lastSelectedNoteObjectWithoutShift = o;
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
        if (isPlaying) return;

        foreach (GameObject o in selectedNoteObjects)
        {
            // This is only visual. Notes are only really moved
            // in EndDrag.
            o.GetComponent<RectTransform>().anchoredPosition += delta;
        }
    }

    private void OnNoteObjectEndDrag()
    {
        if (isPlaying) return;

        // Calculate delta pulse and delta lane
        EditorElement element = draggedNoteObject.GetComponent<EditorElement>();
        int oldPulse = element.note.pulse;
        int oldLane = element.note.lane;
        int deltaPulse = snappedCursorPulse - oldPulse;
        int deltaLane = snappedCursorLane - oldLane;

        // Is the movement applicable to all notes?
        int minPulse = 0;
        int pulsesPerScan = EditorNavigation.GetCurrentPattern().patternMetadata.bps
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
            EditorNavigation.PrepareForChange();
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
            EditorNavigation.DoneWithChange();
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
            yield break;
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

        if (isPlaying)
        {
            modifySelectedKeysoundsButton.interactable = false;
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
            yield break;
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
        EditorNavigation.PrepareForChange();
        for (int i = 0; i < selectionAsList.Count; i++)
        {
            int soundIndex = i % newKeysounds.Count;
            string newSound = newKeysounds[soundIndex];

            // Update pattern
            EditorNavigation.GetCurrentPattern().ModifyNoteKeysound(
                selectionAsList[i].note, selectionAsList[i].sound,
                newSound);

            // Update in display
            selectionAsList[i].sound = newKeysounds[soundIndex];
            selectionAsList[i].GetComponentInChildren<Text>().text =
                UIUtils.StripExtension(newSound);
        }
        EditorNavigation.DoneWithChange();

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
            EditorNavigation.GetCurrentPattern().patternMetadata.bps;
        int lastScan = lastPulse / pulsesPerScan;
        numScans = lastScan + 1;

        // Move scanline if needed.
        EditorElement scanlineElement = scanline.GetComponent<EditorElement>();
        if (scanlineElement.floatPulse > lastPulse)
        {
            scanlineElement.floatPulse = lastPulse;
        }
        
        ResizeContainer();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
    }
    #endregion

    #region Edit
    public void SelectAll()
    {
        selectedNoteObjects.Clear();
        for (int i = 0; i < noteObjectContainer.childCount; i++)
        {
            selectedNoteObjects.Add(noteObjectContainer.GetChild(i).gameObject);
        }
        SelectionChanged?.Invoke(selectedNoteObjects);
        RefreshControls();
    }

    private class NoteWithSound
    {
        public Note note;
        public string sound;
        public static NoteWithSound MakeFromEditorElement(GameObject o)
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
    private int minPulseInClipboard;
    public void CutSelection()
    {
        if (selectedNoteObjects.Count == 0) return;
        if (isPlaying) return;

        CopySelection();
        DeleteSelection();
    }

    public void CopySelection()
    {
        if (selectedNoteObjects.Count == 0) return;

        clipboard.Clear();
        minPulseInClipboard = int.MaxValue;
        foreach (GameObject o in selectedNoteObjects)
        {
            NoteWithSound n = NoteWithSound.MakeFromEditorElement(o);
            if (n.note.pulse < minPulseInClipboard)
            {
                minPulseInClipboard = n.note.pulse;
            }
            clipboard.Add(n);
        }
    }

    public void PasteAtScanline()
    {
        if (clipboard.Count == 0) return;
        if (isPlaying) return;

        int scanlinePulse = ScanlinePulse();
        int deltaPulse = scanlinePulse - minPulseInClipboard;

        // Does the paste conflict with any existing note?
        int pulsesPerScan = EditorNavigation.GetCurrentPattern().patternMetadata.bps
            * Pattern.pulsesPerBeat;
        int maxPulse = numScans * pulsesPerScan;
        int addedScans = 0;
        foreach (NoteWithSound n in clipboard)
        {
            int newPulse = n.note.pulse + deltaPulse;
            if (sortedNoteObjects.HasAt(newPulse, n.note.lane))
            {
                // MessageDialog.Show("Cannot paste here because some pasted notes would overwrite existing notes.");
                return;
            }
            while (newPulse >= maxPulse)
            {
                addedScans++;
                maxPulse += pulsesPerScan;
            }
        }

        // OK to paste. Add scans if needed.
        if (addedScans > 0)
        {
            numScans += addedScans;
            ResizeContainer();
            SpawnMarkersAndLines();
            RepositionNeeded?.Invoke();
        }

        // Paste.
        EditorNavigation.PrepareForChange();
        foreach (NoteWithSound n in clipboard)
        {
            Note noteClone = n.note.Clone();
            noteClone.pulse += deltaPulse;

            // Add note to pattern.
            EditorNavigation.GetCurrentPattern().AddNote(noteClone, n.sound);

            // Add note to UI.
            SpawnNoteObject(noteClone, n.sound);
        }
        EditorNavigation.DoneWithChange();
    }

    public void DeleteSelection()
    {
        if (selectedNoteObjects.Count == 0) return;
        if (isPlaying) return;

        // Delete notes from pattern.
        EditorNavigation.PrepareForChange();
        foreach (GameObject o in selectedNoteObjects)
        {
            EditorElement e = o.GetComponent<EditorElement>();
            EditorNavigation.GetCurrentPattern().DeleteNote(e.note, e.sound);
        }
        EditorNavigation.DoneWithChange();

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

    #region Playback
    private bool isPlaying;
    private float playbackStartingPulse;
    private float playbackStartingTime;
    private DateTime systemTimeOnPlaybackStart;
    private List<Queue<NoteWithSound>> notesInLanes;

    private void RefreshPlaybackPanel()
    {
        playButton.SetActive(!isPlaying);
        stopButton.SetActive(isPlaying);
    }

    public void StartPlayback()
    {
        if (isPlaying) return;
        isPlaying = true;
        // if (!resourceLoader.LoadComplete()) return;
        RefreshControls();

        Pattern currentPattern = EditorNavigation.GetCurrentPattern();

        currentPattern.PrepareForTimeCalculation();
        currentPattern.CalculateTimeOfAllNotes();
        playbackStartingPulse = scanline.GetComponent<EditorElement>()
            .floatPulse;
        playbackStartingTime = currentPattern.PulseToTime((int)playbackStartingPulse);

        // Put notes into queues, each corresponding to a lane.
        notesInLanes = new List<Queue<NoteWithSound>>();
        for (int i = 0; i < 4; i++)
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
                EditorElement e = o.GetComponent<EditorElement>();
                notesInLanes[e.note.lane].Enqueue(new NoteWithSound()
                {
                    note = e.note,
                    sound = e.sound
                });
            }
        }

        systemTimeOnPlaybackStart = DateTime.Now;
        // There's a bit time between the start of this frame
        // and when this method runs, so we keep time using
        // system time to be slightly more accurate.

        PlaySound(backingTrackSource,
            resourceLoader.GetClip(
                currentPattern.patternMetadata.backingTrack),
            playbackStartingTime);
    }

    public void StopPlayback()
    {
        if (!isPlaying) return;
        isPlaying = false;
        RefreshControls();

        backingTrackSource.Stop();
        EditorElement scanlineElement = scanline.GetComponent<EditorElement>();
        scanlineElement.floatPulse = playbackStartingPulse;
        scanlineElement.Reposition();
    }

    public void UpdatePlayback()
    {
        if (!backingTrackSource.isPlaying)
        {
            isPlaying = false;
            RefreshControls();
            return;
        }

        // Calculate time.
        float elapsedTime = (float)(DateTime.Now - systemTimeOnPlaybackStart).TotalSeconds;
        float playbackCurrentTime = playbackStartingTime + elapsedTime;
        float playbackCurrentPulse = EditorNavigation.GetCurrentPattern().TimeToPulse(playbackCurrentTime);

        // Debug.Log($"frame: {Time.frameCount} time: {time} timeFromSamples: {timeFromSamples} systemTime: {systemTime} unityTime: {unityTime} pulse: {pulse}");

        // Play keysounds if it's their time.
        for (int i = 0; i < 4; i++)
        {
            if (notesInLanes[i].Count == 0) continue;
            NoteWithSound nextNote = notesInLanes[i].Peek();
            if (playbackCurrentTime >= nextNote.note.time)
            {
                AudioClip clip = resourceLoader.GetClip(nextNote.sound);
                AudioSource source = keysoundSources[i];
                float startTime = playbackCurrentTime - nextNote.note.time;
                PlaySound(source, clip, startTime);

                notesInLanes[i].Dequeue();
            }
        }

        // Move scanline.
        EditorElement scanlineElement = scanline.GetComponent<EditorElement>();
        scanlineElement.floatPulse = playbackCurrentPulse;
        scanlineElement.Reposition();

        // Scroll pattern to keep up.
        float patternWidth = patternContainer.GetComponent<RectTransform>().rect.width;
        float viewPortWidth = scrollRect.GetComponent<RectTransform>().rect.width;
        if (patternWidth <= viewPortWidth) return;

        float scanlinePosition = scanline.GetComponent<RectTransform>().anchoredPosition.x;

        float xAtViewPortLeft = (patternWidth - viewPortWidth)
            * scrollRect.horizontalNormalizedPosition;
        float xAtViewPortRight = xAtViewPortLeft + viewPortWidth;
        if (scanlinePosition < xAtViewPortLeft ||
            scanlinePosition > xAtViewPortRight)
        {
            float normalizedPosition =
                scanlinePosition / (patternWidth - viewPortWidth);
            scrollRect.horizontalNormalizedPosition =
                Mathf.Clamp01(normalizedPosition);    
        }
    }

    private void PlaySound(AudioSource source, AudioClip clip, float startTime)
    {
        int startSample = Mathf.FloorToInt(startTime * clip.frequency);
        source.clip = clip;
        source.timeSamples = startSample;
        source.Play();
    }
    #endregion

    #region BPM Events
    private BpmEvent GetBpmEventAtScanline()
    {
        int pulse = ScanlinePulse();
        return EditorNavigation.GetCurrentPattern().bpmEvents.Find((BpmEvent e) =>
        {
            return e.pulse == pulse;
        });
    }

    private void UpdateBpmEventDisplay()
    {
        BpmEvent e = GetBpmEventAtScanline();
        if (e == null)
        {
            bpmEventDisplay.text = "(None)";
            addBpmEventButton.interactable = true;
            modifyBpmEventButton.interactable = false;
            deleteBpmEventButton.interactable = false;
        }
        else
        {
            bpmEventDisplay.text = e.bpm.ToString();
            addBpmEventButton.interactable = false;
            modifyBpmEventButton.interactable = true;
            deleteBpmEventButton.interactable = true;
        }
    }

    public void AddBpmEventAtScanline()
    {
        StartCoroutine(InnerAddBpmEvent());
    }

    private IEnumerator InnerAddBpmEvent()
    {
        InputDialog.Show("Change BPM to:", InputField.ContentType.DecimalNumber);
        yield return new WaitUntil(() =>
        {
            return InputDialog.IsResolved();
        });
        if (InputDialog.GetResult() == InputDialog.Result.Cancelled)
        {
            yield break;
        }

        int pulse = ScanlinePulse();
        double bpm = double.Parse(InputDialog.GetValue());
        if (bpm < Pattern.minBpm) bpm = Pattern.minBpm;
        if (bpm > Pattern.maxBpm) bpm = Pattern.maxBpm;

        EditorNavigation.PrepareForChange();
        EditorNavigation.GetCurrentPattern().bpmEvents.Add(new BpmEvent()
        {
            pulse = pulse,
            bpm = bpm,
        });
        EditorNavigation.DoneWithChange();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
        UpdateBpmEventDisplay();
    }

    public void ModifyBpmEventAtScanline()
    {
        StartCoroutine(InnerModifyBpmEvent());
    }

    private IEnumerator InnerModifyBpmEvent()
    {
        InputDialog.Show("Change BPM to:", InputField.ContentType.DecimalNumber);
        yield return new WaitUntil(() =>
        {
            return InputDialog.IsResolved();
        });
        if (InputDialog.GetResult() == InputDialog.Result.Cancelled)
        {
            yield break;
        }

        double bpm = double.Parse(InputDialog.GetValue());
        if (bpm < Pattern.minBpm) bpm = Pattern.minBpm;
        if (bpm > Pattern.maxBpm) bpm = Pattern.maxBpm;

        EditorNavigation.PrepareForChange();
        GetBpmEventAtScanline().bpm = bpm;
        EditorNavigation.DoneWithChange();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
        UpdateBpmEventDisplay();
    }

    public void DeleteBpmEventAtScanline()
    {
        int pulse = ScanlinePulse();
        EditorNavigation.PrepareForChange();
        EditorNavigation.GetCurrentPattern().bpmEvents.RemoveAll((BpmEvent e) =>
        {
            return e.pulse == pulse;
        });
        EditorNavigation.DoneWithChange();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
        UpdateBpmEventDisplay();
    }
    #endregion
}
