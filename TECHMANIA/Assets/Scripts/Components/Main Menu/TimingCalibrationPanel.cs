using System.Collections;
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
    public RectTransform vfxContainer;
    public GameObject vfxPrefab;

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
    // Unique to this panel, clampedTime is the public timer but
    // clamped into the 0th scan.
    private float clampedTime;

    private readonly int[] pulses = { 0, 240, 480, 600, 720 };
    private readonly int[] lanes = { 1, 0, 1, 1, 0 };
    private const float beatPerSecond = 1.5f;
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
        float laneHeight = scanHeight / 4f;
        Scan.InjectLaneHeight(laneHeight);

        timingHistory = new List<List<string>>();
        historyDisplay = new List<TextMeshProUGUI>();
        float noteScale = GlobalResource.noteSkin.basic.scale;
        for (int i = 0; i < pulses.Length; i++)
        {
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
        return Mathf.LerpUnclamped(
            Ruleset.instance.scanMarginBeforeFirstBeat,
            1f - Ruleset.instance.scanMarginAfterLastBeat,
            scan);
    }

    // Update is called once per frame
    void Update()
    {
        // Update timers.

        int offsetMs = calibratingTouchscreen ?
            options.touchOffsetMs :
            options.keyboardMouseOffsetMs;
        Game.InjectBaseTimeAndOffset(
            (float)stopwatch.Elapsed.TotalSeconds,
            offsetMs * 0.001f);

        float timePerScan = 4f / beatPerSecond;
        clampedTime = Game.Time;
        while (clampedTime >= timePerScan * 0.875f)
        {
            clampedTime -= timePerScan;
        }

        // Move scanline.

        float beat = Game.Time * beatPerSecond;
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

        // Animate notes.

        Sprite noteSprite = GlobalResource.noteSkin.basic.
            GetSpriteAtFloatIndex(beat);
        foreach (RectTransform r in notes)
        {
            r.GetComponent<NoteAppearance>().noteImage.sprite = 
                noteSprite;
        }

        // Handle input.

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
                    clampedTime - correctTime);
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
            Mathf.Abs(clampedTime - correctTime) * 1000f);

        // The usual stuff: explosion and keysound.
        if (timeDifferenceInMs <= 200)
        {
            foreach (SpriteSheet layer in
                GlobalResource.vfxSkin.basicMax)
            {
                GameObject vfx = Instantiate(
                    vfxPrefab, vfxContainer);
                vfx.GetComponent<VFXDrawer>().Initialize(
                    notes[id].transform.position,
                    layer, loop: false);
            }

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
        string deviceAcronym = device switch
        {
            InputDevice.Touchscreen => "T",
            InputDevice.Keyboard => "K",
            InputDevice.Mouse => "M",
            _ => ""
        };
        string earlyLateColor;
        string earlyLateIndicator;
        if (clampedTime < correctTime)
        {
            earlyLateColor = ColorUtility.ToHtmlStringRGB(earlyColor);
            earlyLateIndicator = Locale.GetString(
                "timing_calibration_early_indicator");
        }
        else
        {
            earlyLateColor = ColorUtility.ToHtmlStringRGB(lateColor);
            earlyLateIndicator = Locale.GetString(
                "timing_calibration_late_indicator");
        }
        historyLine = $"{deviceAcronym} {timeDifferenceInMs}ms <color=#{earlyLateColor}>{earlyLateIndicator}</color>";

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

    #region Refreshing controls
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
