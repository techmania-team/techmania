using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// EditorElement refers to anything that can position themselves
// inside the workspace.
//
// This does the following (TODO: split the reposition thing into separate class)
// - Reposition GameObject
// - Store reference to Note and sound
// - Relay click and drag events
// - Control appearance
public class EditorElement : MonoBehaviour
{
    public enum Type
    {
        // Position expressed in pulses
        BeatMarker,
        BpmMarker,

        // Position expressed in floatPulses
        Scanline,

        // Position expressed in pulses and lane, which are
        // stored in the note field
        Note
    }
    public Type type;
    public Image selectionOverlay;
    [HideInInspector]
    public int pulse;
    [HideInInspector]
    public float floatPulse;

    // Specific to notes
    [HideInInspector]
    public Note note;  // Typically a reference to a note in EditorContext.Pattern.
    [HideInInspector]
    public string sound;

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;
    public static event UnityAction<GameObject> BeginDrag;
    public static event UnityAction<Vector2> Drag;
    public static event UnityAction EndDrag;

    private void OnEnable()
    {
        PatternPanel.RepositionNeeded += Reposition;
        PatternPanel.SelectionChanged += UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged += SetKeysoundVisibility;
    }

    private void OnDisable()
    {
        PatternPanel.RepositionNeeded -= Reposition;
        PatternPanel.SelectionChanged -= UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged -= SetKeysoundVisibility;
    }

    public void Reposition()
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;

        float scan = 0f;
        switch (type)
        {
            case Type.BeatMarker:
            case Type.BpmMarker:
                {
                    float beat = (float)pulse / Pattern.pulsesPerBeat;
                    scan = beat / bps;
                }
                break;
            case Type.Scanline:
                {
                    float beat = floatPulse / Pattern.pulsesPerBeat;
                    scan = beat / bps;
                }
                break;
            case Type.Note:
                {
                    float beat = (float)note.pulse / Pattern.pulsesPerBeat;
                    scan = beat / bps;
                }
                break;
        }
        float x = PatternPanel.ScanWidth * scan;

        float y = 0;
        if (type == Type.Note)
        {
            y = -PatternPanel.LaneHeight * (note.lane + 0.5f);
        }

        RectTransform rect = GetComponent<RectTransform>();
        switch (type)
        {
            case Type.BeatMarker:
            case Type.BpmMarker:
            case Type.Scanline:
                rect.anchoredPosition = new Vector2(x, 0f);
                break;
            case Type.Note:
                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = new Vector2(
                    PatternPanel.LaneHeight, PatternPanel.LaneHeight);
                break;
            default:
                break;
        }
    }

    private void UpdateSelection(HashSet<GameObject> selection)
    {
        if (type != Type.Note) return;
        if (selectionOverlay == null) return;
        selectionOverlay.enabled = selection.Contains(gameObject);
    }

    #region Event Relay
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
    #endregion

    #region Text
    public void SetKeysoundVisibility(bool visible)
    {
        if (type != Type.Note) return;
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .gameObject.SetActive(visible);
    }

    public void SetTimeDisplay()
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;
        int beat = pulse / Pattern.pulsesPerBeat;
        int scan = beat / bps;
        int beatInScan = beat % bps;

        float time = EditorContext.Pattern.PulseToTime(pulse);
        int minute = Mathf.FloorToInt(time / 60f);
        time -= minute * 60f;
        int second = Mathf.FloorToInt(time);
        time -= second;
        int milliSecond = Mathf.FloorToInt(time * 1000f);

        SetText($"{scan}-{beatInScan}\n{minute}:{second:D2}.{milliSecond:D3}");
    }

    public void SetBpmText(double bpm)
    {
        SetText($"BPM={bpm}");
    }

    public void SetKeysoundText()
    {
        SetText(UIUtils.StripExtension(sound));
    }

    private void SetText(string s)
    {
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .text = s;
    }
    #endregion
}
