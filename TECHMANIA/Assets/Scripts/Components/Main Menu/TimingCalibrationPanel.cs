﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TimingCalibrationPanel : MonoBehaviour
{
    public GraphicRaycaster raycaster;

    [Header("Controls")]
    public MaterialRadioButton touchscreenRadio;
    public MaterialRadioButton keyboardMouseRadio;
    public Slider offsetSlider;
    public Slider latencySlider;
    public TMP_InputField offsetInputField;
    public TMP_InputField latencyInputField;

    [Header("Scan and notes")]
    public RectTransform scan;
    public RectTransform scanline0;
    public RectTransform scanline1;
    public List<RectTransform> notes;
    public RectTransform explosionContainer;
    public GameObject explosionPrefab;

    [Header("Audio")]
    public AudioSourceManager audioSourceManager;
    public AudioSource bgSource;
    public AudioClip kick;
    public AudioClip snare;

    [Header("Colors")]
    public Color earlyColor;
    public Color lateColor;

    private Options options;

    // Timers. The stopwatch provides base time.
    private System.Diagnostics.Stopwatch stopwatch;
    // noteTime = stopwatch + offset.
    private float noteTime;
    // Unique to this panel, clampedNoteTime is noteTime but
    // clamped into the 0th scan.
    private float clampedNoteTime;

    private readonly int[] pulses = { 0, 240, 480, 600, 720 };
    private readonly int[] lanes = { 1, 0, 1, 1, 0 };
    private const float beatPerSecond = 1.5f;
    private float laneHeight = 0f;
    private bool calibratingTouchscreen;
    private List<List<string>> timingHistory;
    private List<TextMeshProUGUI> historyDisplay;

    private void OnEnable()
    {
        options = OptionsBase.LoadFromFile(
            Paths.GetOptionsFilePath()) as Options;
        calibratingTouchscreen = true;

        RefreshRadioButtons();
        RefreshSliders();
        RefreshInputFields();

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        bgSource.Play();
    }

    private void OnDisable()
    {
        bgSource.Stop();
        audioSourceManager.StopAll();

        options.SaveToFile(Paths.GetOptionsFilePath());
    }

    // Start is called before the first frame update
    void Start()
    {
        float scanHeight = scan.rect.height;
        laneHeight = scanHeight / 4f;
        timingHistory = new List<List<string>>();
        historyDisplay = new List<TextMeshProUGUI>();
        for (int i = 0; i < pulses.Length; i++)
        {
            notes[i].sizeDelta = new Vector2(laneHeight, laneHeight);
            float scan = PulseToFloatScan(pulses[i]);
            notes[i].anchorMin = new Vector2(
                FloatScanToAnchorX(scan),
                1f - 0.25f * lanes[i] - 0.125f);
            notes[i].anchorMax = notes[i].anchorMin;
            notes[i].anchoredPosition = Vector2.zero;
            timingHistory.Add(new List<string>());
            TextMeshProUGUI display = notes[i]
                .GetComponentInChildren<TextMeshProUGUI>();
            display.text = "";
            historyDisplay.Add(display);
        }
    }

    private float PulseToFloatScan(float pulse)
    {
        return pulse / 4f / Pattern.pulsesPerBeat;
    }

    private float FloatScanToAnchorX(float scan)
    {
        return Mathf.LerpUnclamped(Scan.kSpaceBeforeScan,
                    1f - Scan.kSpaceAfterScan,
                    scan);
    }

    // Update is called once per frame
    void Update()
    {
        // Update timers.

        int offsetMs = calibratingTouchscreen ?
            options.touchOffsetMs :
            options.keyboardMouseOffsetMs;
        noteTime = (float)stopwatch.Elapsed.TotalSeconds
            - offsetMs * 0.001f;

        float timePerScan = 4f / beatPerSecond;
        clampedNoteTime = noteTime;
        while (clampedNoteTime >= timePerScan * 0.875f)
        {
            clampedNoteTime -= timePerScan;
        }

        // Move scanline.

        float beat = noteTime * beatPerSecond;
        float pulse = beat * Pattern.pulsesPerBeat;
        float scan0 = PulseToFloatScan(pulse);
        float scan1 = scan0 + 1f;
        while (scan0 > 1.5f) scan0 -= 2f;
        while (scan1 > 1.5f) scan1 -= 2f;
        scanline0.anchorMin = new Vector2(
            FloatScanToAnchorX(scan0), 0f);
        scanline0.anchorMax = new Vector2(
            scanline0.anchorMin.x, 1f);
        scanline1.anchorMin = new Vector2(
            FloatScanToAnchorX(scan1), 0f);
        scanline1.anchorMax = new Vector2(
            scanline1.anchorMin.x, 1f);

        if (Input.GetMouseButtonDown(0))
        {
            OnMouseOrTouchDown(Input.mousePosition, InputDevice.Mouse);
        }
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                OnMouseOrTouchDown(Input.GetTouch(i).position,
                    InputDevice.Touchscreen);
            }
        }
        if (AnyKeyDown())
        {
            // Which note does this keystroke go to?
            int id = -1;
            float minDifference = float.MaxValue;
            for (int i = 0; i < pulses.Length; i++)
            {
                float correctTime = CorrectTime(i, 
                    InputDevice.Keyboard);
                float difference = Mathf.Abs(
                    clampedNoteTime - correctTime);
                if (difference < minDifference)
                {
                    id = i;
                    minDifference = difference;
                }
            }
            OnNoteHit(id, InputDevice.Keyboard);
        }
    }

    private bool AnyKeyDown()
    {
        for (int i = (int)KeyCode.A; i <= (int)KeyCode.Z; i++)
        {
            if (Input.GetKeyDown((KeyCode)i)) return true;
        }
        for (int i = (int)KeyCode.Alpha0;
            i <= (int)KeyCode.Alpha9;
            i++)
        {
            if (Input.GetKeyDown((KeyCode)i)) return true;
        }
        return false;
    }
    
    private float CorrectTime(int noteId, InputDevice device)
    {
        int latency = options.GetLatencyForDevice(device);
        float beat = (float)pulses[noteId] / Pattern.pulsesPerBeat;
        return beat / beatPerSecond + latency * 0.001f;
    }

    private void OnMouseOrTouchDown(Vector2 screenPosition,
        InputDevice device)
    {
        PointerEventData eventData = new PointerEventData(
            EventSystem.current);
        eventData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        foreach (RaycastResult r in results)
        {
            NoteHitbox touchReceiver = r.gameObject
                .GetComponent<NoteHitbox>();
            if (touchReceiver == null) continue;

            RectTransform noteRect = touchReceiver.transform.parent
                .GetComponent<RectTransform>();
            int noteId = -1;
            for (int i = 0; i < pulses.Length; i++)
            {
                if (notes[i] == noteRect)
                {
                    noteId = i;
                    break;
                }
            }
            if (noteId == -1) continue;
            OnNoteHit(noteId, device);
            return;
        }
    }

    private void OnNoteHit(int id, InputDevice device)
    {
        // Calculate time difference.
        float correctTime = CorrectTime(id, device);
        int timeDifferenceInMs = Mathf.FloorToInt(
            Mathf.Abs(clampedNoteTime - correctTime) * 1000f);

        // The usual stuff: explosion and keysound.
        if (timeDifferenceInMs <= 200)
        {
            GameObject vfx = Instantiate(
                explosionPrefab, explosionContainer);
            RectTransform rect = vfx.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(
                laneHeight * 3f, laneHeight * 3f);
            rect.position = notes[id].transform.position;

            if (lanes[id] == 0)
            {
                audioSourceManager.PlayKeysound(snare,
                    hiddenLane: false);
            }
            else
            {
                audioSourceManager.PlayKeysound(kick,
                    hiddenLane: false);
            }
        }

        // Write timing history.
        string historyLine;
        char deviceLetter = device.ToString()[0];
        if (clampedNoteTime < correctTime)
        {
            historyLine = $"{deviceLetter} {timeDifferenceInMs}ms <color=#{ColorUtility.ToHtmlStringRGB(earlyColor)}>early</color>";
        }
        else
        {
            historyLine = $"{deviceLetter} {timeDifferenceInMs}ms <color=#{ColorUtility.ToHtmlStringRGB(lateColor)}>late</color>";
        }

        timingHistory[id].Add(historyLine);
        StringBuilder history = new StringBuilder();
        int lowerBound = Mathf.Max(0, timingHistory[id].Count - 5);
        for (int i = timingHistory[id].Count - 1; i >= lowerBound; i--)
        {
            history.AppendLine(timingHistory[id][i]);
        }
        historyDisplay[id].text = history.ToString();
    }

    #region Events from controls
    public void OnTouchscreenRadioButtonClick()
    {
        calibratingTouchscreen = true;
        RefreshRadioButtons();
        RefreshSliders();
        RefreshInputFields();
    }

    public void OnKeyboardMouseButtonClick()
    {
        calibratingTouchscreen = false;
        RefreshRadioButtons();
        RefreshSliders();
        RefreshInputFields();
    }

    public void OnSliderValueChanged()
    {
        if (calibratingTouchscreen)
        {
            options.touchOffsetMs = (int)offsetSlider.value;
            options.touchLatencyMs = (int)latencySlider.value;
        }
        else
        {
            options.keyboardMouseOffsetMs = (int)offsetSlider.value;
            options.keyboardMouseLatencyMs = (int)latencySlider.value;
        }

        RefreshInputFields();
    }

    public void OnInputFieldEndEdit()
    {
        UIUtils.ClampInputField(offsetInputField,
            (int)offsetSlider.minValue,
            (int)offsetSlider.maxValue);
        UIUtils.ClampInputField(latencyInputField,
            (int)latencySlider.minValue,
            (int)latencySlider.maxValue);

        if (calibratingTouchscreen)
        {
            options.touchOffsetMs = int.Parse(offsetInputField.text);
            options.touchLatencyMs = int.Parse(latencyInputField.text);
        }
        else
        {
            options.keyboardMouseOffsetMs =
                int.Parse(offsetInputField.text);
            options.keyboardMouseLatencyMs =
                int.Parse(latencyInputField.text);
        }

        RefreshSliders();
    }
    #endregion

    #region Refresthing controls
    private void RefreshRadioButtons()
    {
        touchscreenRadio.SetIsOn(calibratingTouchscreen);
        keyboardMouseRadio.SetIsOn(!calibratingTouchscreen);
    }

    private void RefreshSliders()
    {
        if (calibratingTouchscreen)
        {
            offsetSlider.SetValueWithoutNotify(
                options.touchOffsetMs);
            latencySlider.SetValueWithoutNotify(
                options.touchLatencyMs);
        }
        else
        {
            offsetSlider.SetValueWithoutNotify(
                options.keyboardMouseOffsetMs);
            latencySlider.SetValueWithoutNotify(
                options.keyboardMouseLatencyMs);
        }
    }

    private void RefreshInputFields()
    {
        if (calibratingTouchscreen)
        {
            offsetInputField.SetTextWithoutNotify(
                options.touchOffsetMs.ToString());
            latencyInputField.SetTextWithoutNotify(
                options.touchLatencyMs.ToString());
        }
        else
        {
            offsetInputField.SetTextWithoutNotify(
                options.keyboardMouseOffsetMs.ToString());
            latencyInputField.SetTextWithoutNotify(
                options.keyboardMouseLatencyMs.ToString());
        }
    }
    #endregion
}
