using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scan : MonoBehaviour
{
    [HideInInspector]
    public int scanNumber;

    public const float kSpaceBeforeScan = 0.15f;
    public const float kSpaceAfterScan = 0.1f;
    private int pulsesPerScan;
    private float screenWidth;
    private float height;
    private float laneHeight;
    private List<GameObject> noteObjects;

    private void OnDestroy()
    {
        Game.ScanChanged -= OnScanChanged;
    }

    public void Initialize()
    {
        Game.ScanChanged += OnScanChanged;

        Rect rect = GetComponent<RectTransform>().rect;
        screenWidth = rect.width;
        height = rect.height;
        laneHeight = height * 0.25f;
        noteObjects = new List<GameObject>();
        pulsesPerScan = Pattern.pulsesPerBeat *
            GameSetup.pattern.patternMetadata.bps;

        Scanline scanline = GetComponentInChildren<Scanline>();
        scanline.scanNumber = scanNumber;
        scanline.Initialize(this, height);
    }

    public void SpawnNoteObject(GameObject prefab, Note n, string sound)
    {
        GameObject o = Instantiate(prefab, transform);
        NoteObject noteObject = o.GetComponent<NoteObject>();
        noteObject.note = n;
        noteObject.sound = sound;

        float x = FloatPulseToXPosition((float)n.pulse);
        float y = height - n.lane * laneHeight;
        RectTransform rect = o.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(laneHeight, laneHeight);

        noteObjects.Add(o);
    }

    private void OnScanChanged(int scan)
    {

    }

    public float FloatPulseToXPosition(float pulse)
    {
        float relativeNormalizedScan = pulse / pulsesPerScan - scanNumber;
        float normalizedX = Mathf.LerpUnclamped(
            kSpaceBeforeScan, 1f - kSpaceAfterScan,
            relativeNormalizedScan);
        if (scanNumber % 2 != 0)
        {
            normalizedX = 1f - normalizedX;
        }

        return normalizedX * screenWidth;
    }
}
