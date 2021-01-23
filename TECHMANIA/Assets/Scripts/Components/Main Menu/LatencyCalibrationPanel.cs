using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LatencyCalibrationPanel : MonoBehaviour
{
    public GraphicRaycaster raycaster;

    [Header("Controls")]
    public Slider touchSlider;
    public Slider keyboardSlider;
    public Slider mouseSlider;
    public TMP_InputField touchInputField;
    public TMP_InputField keyboardInputField;
    public TMP_InputField mouseInputField;

    [Header("Scan and notes")]
    public RectTransform scan;
    public RectTransform scanline0;
    public RectTransform scanline1;
    public List<RectTransform> notes;

    [Header("Audio")]
    public AudioSourceManager audioSourceManager;
    public AudioSource bgSource;
    public AudioClip kick;
    public AudioClip snare;

    [Header("Colors")]
    public Color earlyColor;
    public Color lateColor;

    private Options options;

    private System.Diagnostics.Stopwatch stopwatch;
    private readonly int[] pulses = { 0, 240, 480, 600, 720 };
    private readonly int[] lanes = { 1, 0, 1, 1, 0 };
    private const float beatPerSecond = 1.5f;
    private List<List<string>> timingHistory;
    private List<TextMeshProUGUI> historyDisplay;

    private enum InputDevice
    {
        Touchscreen,
        Keyboard,
        Mouse
    }

    private void OnEnable()
    {
        options = OptionsBase.LoadFromFile(
            Paths.GetOptionsFilePath()) as Options;

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
        float time = (float)stopwatch.Elapsed.TotalSeconds;

        // Move scanline.

        float beat = time * beatPerSecond;
        float pulse = beat * Pattern.pulsesPerBeat;
        // Debug.Log($"time:{time} beat:{beat} pulse:{pulse}");
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
        // TODO: handle keyboard
    }

    private float CurrentTime()
    {
        float time = (float)stopwatch.Elapsed.TotalSeconds;
        float timePerScan = 4f / beatPerSecond;
        while (time >= timePerScan * 0.875f) time -= timePerScan;
        return time;
    }
    
    private float CorrectTime(int noteId, InputDevice device)
    {
        // TODO: apply latency.
        float beat = (float)pulses[noteId] / Pattern.pulsesPerBeat;
        return beat / beatPerSecond;
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
            NoteImageTouchReceiver touchReceiver = r.gameObject
                .GetComponent<NoteImageTouchReceiver>();
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
        float currentTime = CurrentTime();
        float correctTime = CorrectTime(id, device);
        string historyLine;
        char deviceLetter = device.ToString()[0];
        int timeDifferenceInMs = Mathf.FloorToInt(
            Mathf.Abs(currentTime - correctTime) * 1000f);
        if (currentTime < correctTime)
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

    public void OnSliderValueChanged()
    {
        options.touchLatencyMs = (int)touchSlider.value;
        options.keyboardLatencyMs = (int)keyboardSlider.value;
        options.mouseLatencyMs = (int)mouseSlider.value;

        RefreshInputFields();
    }

    public void OnInputFieldEndEdit()
    {
        UIUtils.ClampInputField(touchInputField,
            (int)touchSlider.minValue,
            (int)touchSlider.maxValue);
        UIUtils.ClampInputField(keyboardInputField,
            (int)keyboardSlider.minValue,
            (int)keyboardSlider.maxValue);
        UIUtils.ClampInputField(mouseInputField,
            (int)mouseSlider.minValue,
            (int)mouseSlider.maxValue);
        options.touchLatencyMs = int.Parse(
            touchInputField.text);
        options.keyboardLatencyMs = int.Parse(
            keyboardInputField.text);
        options.mouseLatencyMs = int.Parse(
            mouseInputField.text);

        RefreshSliders();
    }

    private void RefreshSliders()
    {
        touchSlider.SetValueWithoutNotify(
            options.touchLatencyMs);
        keyboardSlider.SetValueWithoutNotify(
            options.keyboardLatencyMs);
        mouseSlider.SetValueWithoutNotify(
            options.mouseLatencyMs);
    }

    private void RefreshInputFields()
    {
        touchInputField.SetTextWithoutNotify(
            options.touchLatencyMs.ToString());
        keyboardInputField.SetTextWithoutNotify(
            options.keyboardLatencyMs.ToString());
        mouseInputField.SetTextWithoutNotify(
            options.mouseLatencyMs.ToString());
    }
}
