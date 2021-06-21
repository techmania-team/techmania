using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scan : MonoBehaviour
{
    public enum Direction
    {
        Left,
        Right
    }
    [HideInInspector]
    public Direction direction;
    [HideInInspector]
    public int scanNumber;

    public Image countdownBackground;
    public Image countdownNumber;
    public Material additiveMaterial;

    public const float kSpaceBeforeScan = 0.15f;
    public const float kSpaceAfterScan = 0.1f;
    private float screenWidth;
    private float scanHeight;
    public static float laneHeight { get; private set; }
    private List<NoteAppearance> noteAppearances;
    private List<HoldExtension> holdExtensions;
    private List<RepeatPathExtension> repeatPathExtensions;
    private Scanline scanline;

    public static void InjectLaneHeight(float height)
    {
        laneHeight = height;
    }

    private void OnDestroy()
    {
        Game.ScanChanged -= OnScanChanged;
        Game.ScanAboutToChange -= OnScanAboutToChange;
        Game.JumpedToScan -= OnJumpedToScan;
    }

    public void Initialize(int scanNumber, Direction direction)
    {
        Game.ScanChanged += OnScanChanged;
        Game.ScanAboutToChange += OnScanAboutToChange;
        Game.JumpedToScan += OnJumpedToScan;

        Rect rect = GetComponent<RectTransform>().rect;
        screenWidth = rect.width;
        scanHeight = rect.height;
        laneHeight = scanHeight 
            * (1f - Ruleset.instance.scanMargin * 2f) * 0.25f;
        noteAppearances = new List<NoteAppearance>();
        holdExtensions = new List<HoldExtension>();
        repeatPathExtensions = new List<RepeatPathExtension>();

        this.scanNumber = scanNumber;
        this.direction = direction;
        scanline = GetComponentInChildren<Scanline>();
        scanline.scanNumber = scanNumber;
        scanline.Initialize(this, scanHeight);

        InitializeCountdown();
    }

    public NoteObject SpawnNoteObject(GameObject prefab, Note n, 
        bool hidden)
    {
        GameObject o = Instantiate(prefab, transform);

        NoteObject noteObject = o.GetComponent<NoteObject>();
        noteObject.note = n;

        if (hidden) return noteObject;

        float x = FloatPulseToXPosition(n.pulse);
        float y = FloatLaneToYPosition(n.lane);
        RectTransform rect = o.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(laneHeight, laneHeight);

        NoteAppearance appearance = o.GetComponent<NoteAppearance>();
        appearance.SetScanAndScanlineRef(this, scanline);
        appearance.Initialize();
        noteAppearances.Add(appearance);

        switch (n.type)
        {
            case NoteType.Hold:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                appearance.InitializeTrail();
                break;
        }

        return noteObject;
    }

    public HoldExtension SpawnHoldExtension(GameObject prefab,
        HoldNote n)
    {
        GameObject o = Instantiate(prefab, transform);

        float x = OutOfBoundXPositionBeforeScan();
        float y = FloatLaneToYPosition(n.lane);
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

    public RepeatPathExtension SpawnRepeatPathExtension(
        GameObject prefab, NoteObject head, int lastRepeatNotePulse)
    {
        GameObject o = Instantiate(prefab, transform);

        float x = OutOfBoundXPositionBeforeScan();
        float y = FloatLaneToYPosition(head.note.lane);
        RectTransform rect = o.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(laneHeight, laneHeight);

        RepeatPathExtension extension = 
            o.GetComponent<RepeatPathExtension>();
        repeatPathExtensions.Add(extension);
        extension.Initialize(this, head, lastRepeatNotePulse);

        return extension;
    }

    public void SetAllNotesInactive()
    {
        foreach (NoteAppearance n in noteAppearances)
        {
            // This will take care of hold extensions and
            // repeat path extensions.
            n.SetInactive();
        }
    }

    private void Activate()
    {
        foreach (NoteAppearance o in noteAppearances)
        {
            o.Activate();
        }
        foreach (HoldExtension e in holdExtensions)
        {
            e.Activate();
        }
        foreach (RepeatPathExtension e in repeatPathExtensions)
        {
            e.Activate();
        }
    }

    private void OnScanAboutToChange(int scan)
    {
        if (scan == scanNumber)
        {
            Activate();
        }
    }

    private void Prepare()
    {
        foreach (NoteAppearance o in noteAppearances)
        {
            o.Prepare();
        }
        foreach (HoldExtension e in holdExtensions)
        {
            e.Prepare();
        }
        foreach (RepeatPathExtension e in repeatPathExtensions)
        {
            e.Prepare();
        }
    }

    private void OnScanChanged(int scan)
    {
        if (scan == scanNumber - 1)
        {
            Prepare();
        }
    }

    private void OnJumpedToScan(int scan)
    {
        foreach (NoteAppearance o in noteAppearances)
        {
            // This will take care of hold extensions and repeat
            // extensions.
            o.SetInactive();
        }

        if (scan > scanNumber)
        {
            foreach (NoteAppearance o in noteAppearances)
            {
                // This will take care of hold extensions and repeat
                // extensions.
                o.Resolve();
            }
        }
        else if (scan == scanNumber)
        {
            Activate();
        }
        else if (scan == scanNumber - 1)
        {
            Prepare();
        }
    }

    #region Positioning
    private float NormalizedXToXPosition(float normalizedX)
    {
        if (direction == Direction.Left)
        {
            normalizedX = 1f - normalizedX;
        }

        return normalizedX * screenWidth;
    }

    public float OutOfBoundXPositionBeforeScan()
    {
        return NormalizedXToXPosition(-0.1f);
    }
    
    // If positionEndOfScanOutOfBounds, then the pulse at
    // precisely the end of this scan will be positioned
    // out of the screen.
    // If positionAfterScanOutOfBounds, then pulses larger than
    // the end of this scan will be positioned out of the screen.
    // These are meant for notes that may cross scans.
    public float FloatPulseToXPosition(float pulse,
        bool positionEndOfScanOutOfBounds = false,
        bool positionAfterScanOutOfBounds = false)
    {
        float relativeNormalizedScan = pulse / Game.PulsesPerScan
            - scanNumber;
        float normalizedX = Mathf.LerpUnclamped(
            kSpaceBeforeScan, 1f - kSpaceAfterScan,
            relativeNormalizedScan);

        if (relativeNormalizedScan == 1f &&
            positionEndOfScanOutOfBounds)
        {
            normalizedX = 1.1f;
        }
        if (relativeNormalizedScan > 1f &&
            positionAfterScanOutOfBounds)
        {
            normalizedX = 1.1f;
        }

        return NormalizedXToXPosition(normalizedX);
    }

    public float FloatLaneToYPosition(float lane)
    {
        return scanHeight * (1f - Ruleset.instance.scanMargin)
            - (lane + 0.5f) * laneHeight;
    }
    #endregion

    private void Start()
    {
        countdownBackground.color = Color.clear;
        countdownNumber.color = Color.clear;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCountdown();
    }

    #region Countdown
    private float floatScanToStartCountdown;
    private void InitializeCountdown()
    {
        RectTransform backgroundRect = countdownBackground
            .GetComponent<RectTransform>();
        RectTransform numberRect = countdownNumber
            .GetComponent<RectTransform>();

        Rect backgroundSize = GlobalResource.gameUiSkin
            .scanCountdownBackground.sprites[0].rect;
        Rect numberSize = GlobalResource.gameUiSkin
            .scanCountdownNumbers.sprites[0].rect;
        float scanHeight = GetComponent<RectTransform>().rect.height;
        float backgroundWidth = scanHeight *
            backgroundSize.width / backgroundSize.height;
        float numberWidth = scanHeight *
            numberSize.width / numberSize.height;

        switch (direction)
        {
            case Direction.Left:
                backgroundRect.anchorMin = new Vector2(1f, 0f);
                backgroundRect.anchorMax = new Vector2(1f, 1f);
                backgroundRect.anchoredPosition = new Vector2(
                    -backgroundWidth * 0.5f, 0f);
                backgroundRect.localScale = new Vector3(-1f, 1f, 1f);
                numberRect.anchorMin = new Vector2(1f, 0f);
                numberRect.anchorMax = new Vector2(1f, 1f);
                numberRect.anchoredPosition = new Vector2(
                    -numberWidth * 0.5f, 0f);
                break;
            case Direction.Right:
                backgroundRect.anchorMin = new Vector2(0f, 0f);
                backgroundRect.anchorMax = new Vector2(0f, 1f);
                backgroundRect.anchoredPosition = new Vector2(
                    backgroundWidth * 0.5f, 0f);
                backgroundRect.localScale = new Vector3(1f, 1f, 1f);
                numberRect.anchorMin = new Vector2(0f, 0f);
                numberRect.anchorMax = new Vector2(0f, 1f);
                numberRect.anchoredPosition = new Vector2(
                    numberWidth * 0.5f, 0f);
                break;
        }

        // Additive shader.
        if (GlobalResource.gameUiSkin.scanCountdownBackground
            .additiveShader)
        {
            countdownBackground.material = additiveMaterial;
        }
        if (GlobalResource.gameUiSkin.scanCountdownNumbers
            .additiveShader)
        {
            countdownNumber.material = additiveMaterial;
        }

        // When do we start the countdown?
        int bps = GameSetup.pattern.patternMetadata.bps;
        float durationBeats;
        if (bps >= 3)
        {
            // Count down the last 3 beats.
            durationBeats = 3f;
        }
        else if (bps >= 2)
        {
            // Count down the last 3 half-beats.
            durationBeats = 1.5f;
        }
        else
        {
            // Count down the last 3 quarter-beats.
            durationBeats = 0.75f;
        }
        float durationScans = durationBeats / bps;
        floatScanToStartCountdown = scanNumber - durationScans;
    }

    private void UpdateCountdown()
    {
        if (Game.FloatScan < floatScanToStartCountdown ||
            Game.FloatScan > scanNumber)
        {
            countdownBackground.color = Color.clear;
            countdownNumber.color = Color.clear;
            return;
        }

        countdownBackground.color = Color.white;
        countdownNumber.color = Color.white;
        float progress = Mathf.InverseLerp(
            floatScanToStartCountdown,
            scanNumber,
            Game.FloatScan);
        UIUtils.SetSpriteAndAspectRatio(
            countdownBackground,
            GlobalResource.gameUiSkin.scanCountdownBackground
            .GetSpriteAtFloatIndex(progress));
        UIUtils.SetSpriteAndAspectRatio(
            countdownNumber,
            GlobalResource.gameUiSkin.scanCountdownNumbers
            .GetSpriteAtFloatIndex(progress));
    }
    #endregion
}
