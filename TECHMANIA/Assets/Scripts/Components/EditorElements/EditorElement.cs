using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Elements include lines, markers and notes.
public class EditorElement : MonoBehaviour
{
    public enum Type
    {
        // Position expressed in scans
        Line,
        ScanMarker,

        // Position expressed in beats
        DottedLine,
        BeatMarker,
        TimeMarker,

        // Position expressed in pulses
        BpmMarker,

        // Position expressed in pulses and lane, which are
        // stored in the note field
        Note
    }
    public Type type;
    [HideInInspector]
    public int scan;
    [HideInInspector]
    public int beat;
    [HideInInspector]
    public int pulse;
    [HideInInspector]
    public int lane;

    // Specific to notes
    [HideInInspector]
    public Note note;
    [HideInInspector]
    public string sound;

    private const float scanMarkerY = 0f;
    private const float beatMarkerY = -20f;
    private const float timeMarkerY = -40f;
    private const float bpmMarkerY = -60f;
    private const float containerHeight = 480f;
    private const float laneHeight = 100f;
    private const float firstLaneY = -80f - laneHeight * 0.5f;

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;
    public static event UnityAction<GameObject> BeginDrag;
    public static event UnityAction<Vector2> Drag;
    public static event UnityAction EndDrag;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        PatternPanel.RepositionNeeded += Reposition;
        PatternPanel.SelectionChanged += UpdateSelection;
    }

    private void OnDisable()
    {
        PatternPanel.RepositionNeeded -= Reposition;
        PatternPanel.SelectionChanged -= UpdateSelection;
    }

    public void Reposition()
    {
        int bps = Navigation.GetCurrentPattern().patternMetadata.bps;

        float x = 0;
        switch (type)
        {
            case Type.Line:
            case Type.ScanMarker:
                x = PatternPanel.ScanWidth * scan;
                break;
            case Type.DottedLine:
            case Type.BeatMarker:
            case Type.TimeMarker:
                {
                    float scan = (float)beat / bps;
                    x = PatternPanel.ScanWidth * scan;
                }
                break;
            case Type.BpmMarker:
            case Type.Note:
                {
                    float beat = (float)note.pulse / Pattern.pulsesPerBeat;
                    float scan = beat / bps;
                    x = PatternPanel.ScanWidth * scan;
                }
                break;
        }

        float y = 0;
        switch (type)
        {
            case Type.ScanMarker:
                y = scanMarkerY;
                break;
            case Type.BeatMarker:
                y = beatMarkerY;
                break;
            case Type.TimeMarker:
                y = timeMarkerY;
                break;
            case Type.BpmMarker:
                y = bpmMarkerY;
                break;
            case Type.Note:
                y = firstLaneY - laneHeight * note.lane;
                break;
            default:
                // Not supported yet
                break;
        }

        RectTransform rect = GetComponent<RectTransform>();
        switch (type)
        {
            case Type.Line:
            case Type.DottedLine:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, 0.5f);
                break;
            case Type.ScanMarker:
            case Type.BeatMarker:
            case Type.TimeMarker:
            case Type.BpmMarker:
            case Type.Note:
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, y);
                break;
            default:
                // Not supported yet
                break;
        }
    }

    private void UpdateSelection(HashSet<GameObject> selection)
    {
        if (type != Type.Note) return;
        GetComponent<Image>().enabled = selection.Contains(gameObject);
    }

    public void OnPointerClick(BaseEventData eventData)
    {
        if (type != Type.Note) return;
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.dragging) return;

        switch (pointerData.button)
        {
            case PointerEventData.InputButton.Left:
                LeftClicked?.Invoke(gameObject);
                break;
            case PointerEventData.InputButton.Right:
                RightClicked?.Invoke(gameObject);
                break;
        }
    }

    public void OnBeginDrag(BaseEventData eventData)
    {
        if (type != Type.Note) return;
        if (!(eventData is PointerEventData)) return;
        BeginDrag?.Invoke(gameObject);
    }

    public void OnDrag(BaseEventData eventData)
    {
        if (type != Type.Note) return;
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        Drag?.Invoke(pointerData.delta);
    }

    public void OnEndDrag(BaseEventData eventData)
    {
        if (type != Type.Note) return;
        if (!(eventData is PointerEventData)) return;
        EndDrag?.Invoke();
    }
}
