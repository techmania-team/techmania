using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatencyCalibrationPanel : MonoBehaviour
{
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
    private int[] pulses = { 0, 240, 480, 600, 720 };
    private int[] lanes = { 1, 0, 1, 1, 0 };

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
            notes[i].GetComponent<NoteAppearance>().Activate();
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

        // Background is 90 BPM, or 1.5 beats per second
        float beat = time * 1.5f;
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
    }
}
