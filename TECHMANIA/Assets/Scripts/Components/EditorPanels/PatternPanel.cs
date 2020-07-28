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
    public GameObject markerTemplate;
    public GameObject lineTemplate;
    public GameObject dottedLineTemplate;
    public GameObject laneDividers;
    public GameObject cursor;

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
    public Text currentKeysoundDisplay;
    private List<string> currentKeysounds;
    private int currentKeysoundIndex;
    public Text divisionsPerBeatDisplay;
    public Text selectedNotesKeysoundDisplay;
    public Button modifySelectedNotesKeysoundButton;
    public Button cutButton;
    public Button copyButton;
    public Button pasteButton;
    public Button deleteButton;

    [Header("Note Prefabs")]
    public GameObject basicNote;

    public static event UnityAction RepositionNeeded;
    public static event UnityAction<HashSet<GameObject>> SelectionChanged;

    private int snappedCursorPulse;
    private int snappedCursorLane;

    #region Markers and Lines
    private void SpawnLine(int scan, GameObject template)
    {
        GameObject line = Instantiate(template, patternContainer);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.Line;
        element.scan = scan;
    }

    private void SpawnDottedLine(int beat, GameObject template)
    {
        GameObject line = Instantiate(template, patternContainer);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.DottedLine;
        element.beat = beat;
    }
    
    private void SpawnScanBasedMarker(EditorElement.Type type, int scan, string text)
    {
        GameObject marker = Instantiate(markerTemplate, patternContainer);
        marker.GetComponentInChildren<Text>().text = text;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.scan = scan;
    }

    private void SpawnBeatBasedMarker(EditorElement.Type type, int beat, string text)
    {
        GameObject marker = Instantiate(markerTemplate, patternContainer);
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
        for (int i = 0; i < patternContainer.childCount; i++)
        {
            Transform t = patternContainer.GetChild(i);
            if (t == markerTemplate.transform) continue;
            if (t == lineTemplate.transform) continue;
            if (t == dottedLineTemplate.transform) continue;
            if (t == laneDividers.transform) continue;
            if (t == cursor.transform) continue;
            Destroy(t.gameObject);
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
        GameObject noteObject = Instantiate(basicNote, patternContainer);
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
        currentKeysounds = new List<string>();
        currentKeysounds.Add("");
        currentKeysoundIndex = 0;

        MemoryToUI();

        EditorElement.LeftClicked += OnNoteObjectLeftClick;
        EditorElement.RightClicked += OnNoteObjectRightClick;
    }

    private void OnDisable()
    {
        EditorElement.LeftClicked -= OnNoteObjectLeftClick;
        EditorElement.RightClicked -= OnNoteObjectRightClick;
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

        SnapCursor();
    }

    public void MemoryToUI()
    {
        ResizeContainer();
        SpawnMarkersAndLines();
        SpawnExistingNotes();
        RepositionNeeded?.Invoke();
        UpdateCurrentKeysoundDisplay();
        UpdateSelectionKeysoundsDisplay();
    }

    private void SnapCursor()
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

        snappedCursorLane = Mathf.FloorToInt((pointInContainer.y + 80f)
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
        divisionsPerBeatDisplay.text = divisionsPerBeat.ToString();
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
        string sound = currentKeysounds[currentKeysoundIndex];
        currentKeysoundIndex = (currentKeysoundIndex + 1) % currentKeysounds.Count;
        Note n = new Note();
        n.pulse = snappedCursorPulse;
        n.lane = snappedCursorLane;
        n.type = NoteType.Basic;
        Navigation.PrepareForChange();
        Navigation.GetCurrentPattern().AddNote(n, sound);
        Navigation.DoneWithChange();

        // Add note to UI
        SpawnNoteObject(n, sound);
        UpdateCurrentKeysoundDisplay();
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
        UpdateSelectionKeysoundsDisplay();
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
        Destroy(o);
    }

    #region Keysounds
    private string KeysoundName(string filename)
    {
        if (filename == "") return UIUtils.kEmptyKeysoundDisplayText;
        return UIUtils.StripExtension(filename);
    }

    private void UpdateCurrentKeysoundDisplay()
    {
        string display = "Current ";
        if (currentKeysounds.Count > 1)
        {
            display += $"({currentKeysoundIndex + 1}/{currentKeysounds.Count})";
        }
        display += ": ";
        display += KeysoundName(currentKeysounds[currentKeysoundIndex]);

        currentKeysoundDisplay.text = display;
    }

    public void UpdateCurrentKeysounds()
    {
        StartCoroutine(InternalUpdateCurrentKeysounds());
    }

    private IEnumerator InternalUpdateCurrentKeysounds()
    {
        SelectKeysoundDialog.Show("Select keysounds to apply to new notes. " +
            "You can select multiple, and they will apply successively.",
            currentKeysounds);
        yield return new WaitUntil(() =>
        {
            return SelectKeysoundDialog.IsResolved();
        });
        if (SelectKeysoundDialog.GetResult() ==
            SelectKeysoundDialog.Result.Cancelled)
        {
            yield return null;
        }

        currentKeysounds = SelectKeysoundDialog.GetSelectedKeysounds();
        currentKeysoundIndex = 0;
        UpdateCurrentKeysoundDisplay();
    }

    private void UpdateSelectionKeysoundsDisplay()
    {
        HashSet<string> keysounds = new HashSet<string>();
        foreach (GameObject noteObject in selectedNoteObjects)
        {
            keysounds.Add(noteObject.GetComponent<EditorElement>().sound);
        }

        if (keysounds.Count == 0)
        {
            // Assume empty selection.
            selectedNotesKeysoundDisplay.text =
                UIUtils.kEmptyKeysoundDisplayText;
            modifySelectedNotesKeysoundButton.interactable = false;
        }
        else if (keysounds.Count == 1)
        {
            HashSet<string>.Enumerator enumerator = keysounds.GetEnumerator();
            enumerator.MoveNext();
            selectedNotesKeysoundDisplay.text = 
                KeysoundName(enumerator.Current);
            modifySelectedNotesKeysoundButton.interactable = true;
        }
        else
        {
            selectedNotesKeysoundDisplay.text = "(Multiple)";
            modifySelectedNotesKeysoundButton.interactable = true;
        }
    }

    public void UpdateSelectionKeysound()
    {

    }
    #endregion
}
