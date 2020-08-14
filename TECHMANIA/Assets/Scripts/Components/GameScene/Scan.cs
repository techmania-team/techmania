using System.Collections;
using System.Collections.Generic;
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
    private List<GameObject> noteObjects;

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
        noteObjects = new List<GameObject>();

        Scanline scanline = GetComponentInChildren<Scanline>();
        scanline.scanNumber = scanNumber;
        scanline.Initialize(this, scanHeight);
    }

    public NoteObject SpawnNoteObject(GameObject prefab, Note n, string sound)
    {
        GameObject o = Instantiate(prefab, transform);
        NoteObject noteObject = o.GetComponent<NoteObject>();
        noteObject.note = n;
        noteObject.sound = sound;

        float x = FloatPulseToXPosition((float)n.pulse);
        float y = scanHeight - (n.lane + 0.5f) * laneHeight;
        RectTransform rect = o.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(laneHeight, laneHeight);

        o.SetActive(false);
        noteObjects.Add(o);

        return noteObject;
    }

    private void OnScanAboutToChange(int scan)
    {
        if (scan == scanNumber)
        {
            // Activate.
            foreach (GameObject o in noteObjects)
            {
                o.SetActive(true);
                o.GetComponent<Image>().color = Color.white;
            }
        }
    }

    private void OnScanChanged(int scan)
    {
        if (scan == scanNumber - 1)
        {
            // Prepare.
            foreach (GameObject o in noteObjects)
            {
                o.SetActive(true);
                o.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
    }

    public float FloatPulseToXPosition(float pulse)
    {
        float relativeNormalizedScan = pulse / Game.PulsesPerScan
            - scanNumber;
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
