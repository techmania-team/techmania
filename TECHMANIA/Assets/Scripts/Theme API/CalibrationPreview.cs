using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThemeApi
{
    // This is a MonoBehaviour so we can receive Update() events
    // without relying on GameController.
    [MoonSharpUserData]
    public class CalibrationPreview : MonoBehaviour
    {
        [MoonSharpHidden]
        public static CalibrationPreview instance;

        [MoonSharpHidden]
        public VisualTreeAsset calibrationPreviewTemplate;
        [MoonSharpHidden]
        public VFXManager vfxManager;
        [Header("Audio")]
        [MoonSharpHidden]
        public AudioSourceManager audioSourceManager;
        [MoonSharpHidden]
        public AudioClip backingTrack;
        [MoonSharpHidden]
        public AudioClip kick;
        [MoonSharpHidden]
        public AudioClip snare;

        [HideInInspector]
        public VisualElementWrap previewContainer;
        [HideInInspector]
        public List<string> timingDisplayClasses;
        [HideInInspector]
        public int timingDisplayMaxLines;
        [HideInInspector]
        public string earlyString;
        [HideInInspector]
        public string lateString;
        [HideInInspector]
        public bool setEarlyLateColors;
        [HideInInspector]
        public Color earlyColor;
        [HideInInspector]
        public Color lateColor;

        private bool running;
        private float scanHeight => 
            previewContainer.resolvedStyle.height;
        private const int lanes = 4;
        private float laneHeight => scanHeight / lanes;
        private const int bps = 4;
        private readonly float[] beatOfNote = { 0f, 1f, 2f, 2.5f, 3f };
        private readonly int[] laneOfNote = { 1, 0, 1, 1, 0 };
        private const float beatPerSecond = 1.5f;
        private const float secondPerBeat = 1f / beatPerSecond;
        private const float secondPerScan = secondPerBeat * bps;
        private const int differenceThresholdMs = 200;
        private TemplateContainer previewBg;
        private List<VisualElement> scanlineAnchors;
        private List<VisualElement> scanlines;
        private List<VisualElement> noteAnchors;
        private List<VisualElement> noteImages;
        private List<TextElement> timingDisplays;
        private List<int> displayLinesOfNote;
        private Stopwatch stopwatch;
        private float baseTime;
        private float gameTime;
        private AudioSource backingTrackSource;

        private enum InputMethod
        {
            Touch,
            KM
        }
        private InputMethod inputMethod;

        // Start is called before the first frame update
        private void Start()
        {
            running = false;
            instance = this;
        }

        #region Helpers
        private StyleLength StyleFromPortion(float portion) =>
            new StyleLength(
                new Length(portion * 100f, LengthUnit.Percent));

        private StyleLength LeftFromBeat(float beat)
        {
            float leftMargin =
                Ruleset.instance.scanMarginBeforeFirstBeat;
            float leftPerBeat = (1f - leftMargin -
                Ruleset.instance.scanMarginAfterLastBeat) / bps;
            float left = leftMargin + leftPerBeat * beat;
            return StyleFromPortion(left);
        }

        private StyleLength LeftFromScan(float scan)
        {
            float left = Mathf.LerpUnclamped(
                Ruleset.instance.scanMarginBeforeFirstBeat,
                1f - Ruleset.instance.scanMarginAfterLastBeat,
                scan);
            return StyleFromPortion(left);
        }
        #endregion

        // Render a calibration preview in the specified
        // previewContainer. The scanline will move from left to right,
        // scanning over 5 predefined notes in the process.
        // When the player hits notes with the currently selected
        // input method (touch / K+M), the notes will resolve,
        // and a line of text will be added to a text element under
        // the note, showing whether the hit was early or late, and
        // by how much.
        //
        // The timing of the scanline, notes, and judgement will
        // reflect the current offset and latency settings of the
        // currently selected input method. Call SwitchToTouch()
        // and SwitchToKeyboardMouse() to switch. The preview will
        // fetch options every frame, so there is no need to restart
        // the preview after changing offset and latency.
        public void Begin()
        {
            // Initialize scanlines and notes
            previewBg = calibrationPreviewTemplate.Instantiate();
            previewBg.style.flexGrow = new StyleFloat(1f);
            previewContainer.inner.Add(previewBg);

            scanlineAnchors = new List<VisualElement>();
            scanlines = new List<VisualElement>();
            VisualElement scanlineContainer = previewBg.Q("scanlines");
            foreach (VisualElement anchor in 
                scanlineContainer.Children())
            {
                scanlineAnchors.Add(anchor);
                scanlines.Add(anchor.Q("scanline"));
            }

            if (scanlineAnchors.Count != 2)
            {
                throw new Exception($"Unexpected number of scanlines. Expected 2, got {scanlineAnchors.Count}.");
            }

            noteAnchors = new List<VisualElement>();
            noteImages = new List<VisualElement>();
            timingDisplays = new List<TextElement>();
            displayLinesOfNote = new List<int>();
            VisualElement noteContainer = previewBg.Q("notes");
            foreach (VisualElement anchor in noteContainer.Children())
            {
                noteAnchors.Add(anchor);
                VisualElement image = anchor.Q("note-image");
                noteImages.Add(image);
                TextElement display = image.Q("timing-display")
                    as TextElement;
                foreach (string ussClass in timingDisplayClasses)
                {
                    display.AddToClassList(ussClass);
                }
                display.text = "";
                timingDisplays.Add(display);
                displayLinesOfNote.Add(0);
            }

            if (noteAnchors.Count != beatOfNote.Length)
            {
                throw new Exception($"Unexpected number of notes in calibration preview. Expected {beatOfNote.Length}, got {noteAnchors.Count}.");
            }

            // Place notes
            for (int i = 0; i < noteAnchors.Count; i++)
            {
                noteAnchors[i].style.left = LeftFromBeat(beatOfNote[i]);
                noteAnchors[i].style.top = StyleFromPortion(
                    (laneOfNote[i] + 0.5f) / lanes);
            }

            ResetSize();

            inputMethod = InputMethod.Touch;

            backingTrackSource = audioSourceManager.PlayBackingTrack(
                backingTrack);
            backingTrackSource.loop = true;

            stopwatch = new Stopwatch();
            stopwatch.Start();

            running = true;
        }   
        
        public void ResetSize()
        {
            Rect spriteSize = GlobalResource.gameUiSkin
                .scanline.sprites[0].rect;
            foreach (VisualElement scanline in scanlines)
            {
                scanline.style.width = scanHeight
                    * spriteSize.width / spriteSize.height;
            }

            float noteScale = GlobalResource.noteSkin.basic.scale;
            foreach (VisualElement image in noteImages)
            {
                image.style.width = laneHeight * noteScale;
                image.style.height = laneHeight * noteScale;
            }

            vfxManager.ResetSize(laneHeight);
        }

        public void SwitchToTouch()
        {
            inputMethod = InputMethod.Touch;
        }

        public void SwitchToKeyboardMouse()
        {
            inputMethod = InputMethod.KM;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!running) return;

            // Calculate time
            int offsetMs = inputMethod switch
            {
                InputMethod.Touch => Options.instance.touchOffsetMs,
                InputMethod.KM => 
                    Options.instance.keyboardMouseOffsetMs,
                _ => 0
            };
            baseTime = (float)stopwatch.Elapsed.TotalSeconds;
            gameTime = baseTime - offsetMs * 0.001f;
            float beat = gameTime * beatPerSecond;
            float scan = beat / bps;

            // Move scanline
            float scanOfScanline0 = scan;
            float scanOfScanline1 = scan + 1f;
            while (scanOfScanline0 > 1.5f) scanOfScanline0 -= 2f;
            while (scanOfScanline1 > 1.5f) scanOfScanline1 -= 2f;
            scanlineAnchors[0].style.left = LeftFromScan(
                scanOfScanline0);
            scanlineAnchors[1].style.left = LeftFromScan(
                scanOfScanline1);

            // Animate scanline and note
            foreach (VisualElement scanline in scanlines)
            {
                scanline.style.backgroundImage = new StyleBackground(
                    GlobalResource.gameUiSkin.scanline
                    .GetSpriteAtFloatIndex(scan % 1f));
            }
            foreach (VisualElement noteImage in noteImages)
            {
                noteImage.style.backgroundImage = new StyleBackground(
                    GlobalResource.noteSkin.basic
                    .GetSpriteAtFloatIndex(beat % 1f));
            }

            // Handle input
            if (Input.GetMouseButtonDown(0))
            {
                OnMouseOrTouchDown(Input.mousePosition, 
                    InputDevice.Mouse);
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
                OnAnyKeyDown();
            }
        }

        public void Conclude()
        {
            previewBg.RemoveFromHierarchy();
            stopwatch.Stop();
            vfxManager.Dispose();
            backingTrackSource.loop = false;
            backingTrackSource.Stop();

            running = false;
        }

        #region Input handling
        private void OnMouseOrTouchDown(Vector2 screenPosition,
            InputDevice device)
        {
            // Which note does this click / touch go to?
            int noteIndex = -1;
            for (int i = 0; i < noteImages.Count; i++)
            {
                if (VisualElementTransform
                    .ElementContainsPointInScreenSpace(
                    noteImages[i], screenPosition))
                {
                    noteIndex = i;
                    break;
                }
            }

            // Is the time difference within threshold?
            if (noteIndex == -1) return;
            int differenceMs = TimeDifferenceFromNote(
                noteIndex, device);
            if (Mathf.Abs(differenceMs) > differenceThresholdMs)
            {
                return;
            }

            OnNoteHit(noteIndex, differenceMs, device);
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

        private void OnAnyKeyDown()
        {
            // Which note does this keystroke go to?
            int minDifferenceMs = differenceThresholdMs + 1;
            int noteIndex = -1;
            for (int i = 0; i < beatOfNote.Length; i++)
            {
                int differenceMs = TimeDifferenceFromNote(i, 
                    InputDevice.Keyboard);
                if (Mathf.Abs(differenceMs) <
                    Mathf.Abs(minDifferenceMs))
                {
                    minDifferenceMs = differenceMs;
                    noteIndex = i;
                }
            }

            // Is the time difference within threshold?
            if (noteIndex == -1) return;
            if (Mathf.Abs(minDifferenceMs) > differenceThresholdMs)
            {
                return;
            }

            OnNoteHit(noteIndex, minDifferenceMs, InputDevice.Keyboard);
        }

        // In milliseconds.
        // Negative = early, positive = late
        private int TimeDifferenceFromNote(int i, InputDevice device)
        {
            float correctTime = beatOfNote[i] / beatPerSecond +
                Options.instance.GetLatencyForDevice(device) * 0.001f;
            float clampedCurrentTime = gameTime % secondPerScan;

            float minDifference = secondPerScan;
            for (int scan = -1; scan <= 1; scan++)
            {
                float shiftedCorrectTime = correctTime +
                    scan * secondPerScan;
                float difference = clampedCurrentTime
                    - shiftedCorrectTime;
                if (Mathf.Abs(difference) < Mathf.Abs(minDifference))
                {
                    minDifference = difference;
                }
            }
            return Mathf.FloorToInt(minDifference * 1000f);
        }

        private void OnNoteHit(int noteIndex, int timeDifferenceMs,
            InputDevice device)
        {
            // VFX and keysound
            vfxManager.SpawnOneShotVFX(noteImages[noteIndex], 
                Judgement.RainbowMax);
            switch (laneOfNote[noteIndex])
            {
                case 0:
                    audioSourceManager.PlayKeysound(snare,
                        hiddenLane: false);
                    break;
                case 1:
                    audioSourceManager.PlayKeysound(kick,
                        hiddenLane: false);
                    break;
                default:
                    break;
            }

            // Timing history
            string deviceAcronym = device switch
            {
                InputDevice.Touchscreen => "T",
                InputDevice.Keyboard => "K",
                InputDevice.Mouse => "M",
                _ => ""
            };
            string earlyLateIndicator = timeDifferenceMs < 0 ?
                earlyString : lateString;
            if (setEarlyLateColors)
            {
                Color color = timeDifferenceMs < 0 ?
                    earlyColor : lateColor;
                earlyLateIndicator = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{earlyLateIndicator}</color>";
            }
            string historyLine = $"{deviceAcronym} {Mathf.Abs(timeDifferenceMs)}ms {earlyLateIndicator}\n";

            string displayString = historyLine +
                timingDisplays[noteIndex].text;
            displayLinesOfNote[noteIndex]++;
            if (displayLinesOfNote[noteIndex] > timingDisplayMaxLines)
            {
                // Remove last line. Note that the last character
                // is always '\n'.
                displayString = displayString.Substring(0, 
                    displayString.LastIndexOf('\n',
                    displayString.Length - 2));
                displayLinesOfNote[noteIndex]--;
            }
            timingDisplays[noteIndex].text = displayString;
        }
        #endregion
    }
}