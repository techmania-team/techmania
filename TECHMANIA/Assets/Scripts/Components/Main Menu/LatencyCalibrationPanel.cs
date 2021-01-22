using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LatencyCalibrationPanel : MonoBehaviour
{
    public GraphicRaycaster raycaster;

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

    private System.Diagnostics.Stopwatch stopwatch;
    private readonly int[] pulses = { 0, 240, 480, 600, 720 };
    private readonly int[] lanes = { 1, 0, 1, 1, 0 };
    private const float beatPerSecond = 1.5f;

    private enum InputDevice
    {
        Touchscreen,
        Keyboard,
        Mouse
    }

    private void OnEnable()
    {
        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        bgSource.Play();
    }

    private void OnDisable()
    {
        bgSource.Stop();
        audioSourceManager.StopAll();
    }

    // Start is called before the first frame update
    void Start()
    {
        float scanHeight = scan.rect.height;
        float laneHeight = scanHeight / 4f;
        for (int i = 0; i < pulses.Length; i++)
        {
            notes[i].sizeDelta = new Vector2(laneHeight, laneHeight);
            float scan = PulseToFloatScan(pulses[i]);
            notes[i].anchorMin = new Vector2(
                FloatScanToAnchorX(scan),
                1f - 0.25f * lanes[i] - 0.125f);
            notes[i].anchorMax = notes[i].anchorMin;
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
    
    private float CorrectTime(int noteId)
    {
        float beat = pulses[noteId] / Pattern.pulsesPerBeat;
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
        Debug.Log($"Note #{id} is hit by {device}");
        float currentTime = CurrentTime();
        float correctTime = CorrectTime(id);
        if (currentTime < correctTime)
        {
            Debug.Log($"{correctTime - currentTime}s early");
        }
        else
        {
            Debug.Log($"{currentTime - correctTime}s late");
        }
    }
}
