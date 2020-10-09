using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Note to self: (0, 0) is bottom-left.
// TODO: Deprecate.
public class PatternPanelBackup : MonoBehaviour
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
    private void ResizeContainer()
    {
        // Deleted
    }

    private void SpawnMarkersAndLines()
    {
        // Deleted
    }
    #endregion

    private void SpawnNoteObject(Note n, string sound)
    {
        // Deleted
    }

    private void SpawnExistingNotes()
    {
        // Deleted
    }

    private void OnEnable()
    {
        // Deleted
        clipboard = new List<NoteWithSound>();
        isPlaying = false;

        // Deleted

        // MemoryToUI();
        resourceLoader.LoadResources(EditorNavigation.GetCurrentTrackPath());

        // Deleted
    }

    private void OnDisable()
    {
        // Deleted
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
            // Deleted
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
        // Deleted
    }

    private int ScanlinePulse()
    {
        return (int)scanline.GetComponent<EditorElement>().floatPulse;
    }

    public void ModifyDevisionsPerBeat(int direction)
    {
        // Deleted
    }

    public void OnClickPatternContainer(BaseEventData eventData)
    {
        // Deleted
    }

    #region Left and Right click on note objects
    public void OnNoteObjectLeftClick(GameObject o)
    {
        // Deleted
        RefreshControls();
    }

    private void ToggleSelection(GameObject o)
    {
        // Deleted
    }

    public void OnNoteObjectRightClick(GameObject o)
    {
        if (isPlaying) return;
        // Deleted
        RefreshControls();
        // Deleted
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
