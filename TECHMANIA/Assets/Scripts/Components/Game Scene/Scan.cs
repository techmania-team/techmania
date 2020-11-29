using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.UI;

public class Scan : MonoBehaviour
{
    [HideInInspector]
    public int scanNumber;

    private const float kSpaceBeforeScan = 0.15f;
    private const float kSpaceAfterScan = 0.1f;
    private float screenWidth;
    private float scanHeight;
    public static float laneHeight { get; private set; }
    private List<NoteAppearance> noteAppearances;
    private List<HoldExtension> holdExtensions;
    private Scanline scanline;

    private void OnDestroy()
    {
        Game.ScanChanged -= OnScanChanged;
        Game.ScanAboutToChange -= OnScanAboutToChange;
    }

    public void Initialize()
    {
        Game.ScanChanged += OnScanChanged;
        Game.ScanAboutToChange += OnScanAboutToChange;

        Rect rect = GetComponent<RectTransform>().rect;
        screenWidth = rect.width;
        scanHeight = rect.height;
        laneHeight = scanHeight * 0.25f;
        noteAppearances = new List<NoteAppearance>();
        holdExtensions = new List<HoldExtension>();

        scanline = GetComponentInChildren<Scanline>();
        scanline.scanNumber = scanNumber;
        scanline.Initialize(this, scanHeight);
    }

    public NoteObject SpawnNoteObject(GameObject prefab, Note n, 
        string sound, bool hidden)
    {
        GameObject o = Instantiate(prefab, transform);

        NoteObject noteObject = o.GetComponent<NoteObject>();
        noteObject.note = n;
        noteObject.sound = sound;

        float x = FloatPulseToXPosition(n.pulse);
        float y = FloatLaneToYPosition(n.lane);
        RectTransform rect = o.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(laneHeight, laneHeight);

        NoteAppearance appearance = o.GetComponent<NoteAppearance>();
        appearance.SetHidden(hidden);
        noteAppearances.Add(appearance);

        switch (n.type)
        {
            case NoteType.Hold:
                appearance.InitializeTrail(this, scanline);
                break;
            case NoteType.Drag:
                appearance.InitializeCurve(this, scanline);
                break;
        }

        return noteObject;
    }

    public HoldExtension SpawnHoldExtension(GameObject prefab,
        HoldNote n)
    {
        GameObject o = Instantiate(prefab, transform);

        float x = FloatPulseToXPosition((float)n.pulse,
            extendOutOfBoundPosition: true);
        float y = scanHeight - (n.lane + 0.5f) * laneHeight;
        RectTransform rect = o.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(laneHeight, laneHeight);

        HoldExtension extension = o.GetComponent<HoldExtension>();
        holdExtensions.Add(extension);
        extension.Initialize(this, scanline, n);
        
        return extension;
    }

    private void OnScanAboutToChange(int scan)
    {
        if (scan == scanNumber)
        {
            foreach (NoteAppearance o in noteAppearances)
            {
                o.Activate();
            }
            foreach (HoldExtension e in holdExtensions)
            {
                e.Activate();
            }
        }
    }

    private void OnScanChanged(int scan)
    {
        if (scan == scanNumber - 1)
        {
            foreach (NoteAppearance o in noteAppearances)
            {
                o.Prepare();
            }
            foreach (HoldExtension e in holdExtensions)
            {
                e.Prepare();
            }
        }
    }

    // If extendOutOfBoundPosition, pulses not inside the
    // scan will be mapped to a position outside the screen
    // width. Used for extensions.
    public float FloatPulseToXPosition(float pulse,
        bool extendOutOfBoundPosition = false)
    {
        float relativeNormalizedScan = pulse / Game.PulsesPerScan
            - scanNumber;
        float normalizedX = Mathf.LerpUnclamped(
            kSpaceBeforeScan, 1f - kSpaceAfterScan,
            relativeNormalizedScan);

        if (extendOutOfBoundPosition)
        {
            if (relativeNormalizedScan <= 0f)
            {
                // If relativeNormalizedScan == 0f, it could be
                // due to the note head being set as an end-of-scan
                // note.
                normalizedX = -0.1f;
            }
            else if (relativeNormalizedScan > 1f)
            {
                normalizedX = 1.1f;
            }
        }

        if (scanNumber % 2 != 0)
        {
            normalizedX = 1f - normalizedX;
        }

        return normalizedX * screenWidth;
    }

    public float FloatLaneToYPosition(float lane)
    {
        return scanHeight - (lane + 0.5f) * laneHeight;
    }
}
