using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Controls scanline and scan countdowns.
// TODO: spawn one scanline per scan, after all.
public class GameLayout
{
    private Pattern pattern;
    private float countdownLengthInScans;

    private VisualElement gameContainer;
    private TemplateContainer layout;

    public enum ScanDirection
    {
        Left,
        Right
    }
    private class HalfElements
    {
        public ScanDirection direction;
        public VisualElement scanlineContainer;
        public VisualElement countdownBg;
        public VisualElement countdownNum;
        public VisualElement noteContainer;
    }
    private HalfElements topHalf;
    private HalfElements bottomHalf;
    // Points to one of topHalf/bottomHalf.
    private HalfElements evenHalf;
    // Points to one of topHalf/bottomHalf.
    private HalfElements oddHalf;

    private class ScanElements
    {
        public int scanNumber;
        public ScanDirection direction;
        public TemplateContainer anchor;
        public VisualElement scanline;
    }
    private List<ScanElements> scanElements;

    #region Public properties
    public float scanHeight => gameContainer.resolvedStyle
        .height * 0.5f;
    public float screenWidth => gameContainer.resolvedStyle.width;
    public ScanDirection evenScanDirection => evenHalf.direction;
    public ScanDirection oddScanDirection => oddHalf.direction;
    public VisualElement evenScanNoteContainer =>
        evenHalf.noteContainer;
    public VisualElement oddScanNoteContainer =>
        oddHalf.noteContainer;
    #endregion

    public GameLayout(Pattern pattern,
        VisualElement gameContainer,
        VisualTreeAsset layoutTemplate)
    {
        this.pattern = pattern;
        this.gameContainer = gameContainer;

        layout = layoutTemplate.Instantiate();
        // Make sure the game - where everything uses absolute
        // positioning - takes up the entirety of setup.gameContainer.
        layout.style.flexGrow = new StyleFloat(1f);
        // Layout becomes visible on Begin().
        layout.style.display =
            new StyleEnum<DisplayStyle>(DisplayStyle.None);
        gameContainer.Add(layout);

        System.Func<VisualElement, HalfElements> makeHalfElements =
            (VisualElement element) => new HalfElements()
            {
                scanlineContainer = element.Q("scanline-container"),
                countdownBg = element.Q("countdown-bg"),
                countdownNum = element.Q("countdown-num"),
                noteContainer = element.Q("note-container")
            };
        topHalf = makeHalfElements(layout.Q("top-half"));
        bottomHalf = makeHalfElements(layout.Q("bottom-half"));
    }

    public void ResetAspectRatio()
    {
        GameUISkin skin = GlobalResource.gameUiSkin;

        System.Action<VisualElement, SpriteSheet> setAspectRatio =
            (VisualElement element, SpriteSheet spriteSheet) =>
            {
                Rect rect = spriteSheet.sprites[0].rect;
                element.style.width = scanHeight *
                    rect.width / rect.height;
            };

        System.Action<HalfElements> setAspectRatioForHalf =
            (HalfElements half) =>
            {
                setAspectRatio(half.countdownBg,
                    skin.scanCountdownBackground);
                setAspectRatio(half.countdownNum,
                    skin.scanCountdownNumbers);
            };
        setAspectRatioForHalf(topHalf);
        setAspectRatioForHalf(bottomHalf);

        foreach (ScanElements scan in scanElements)
        {
            setAspectRatio(scan.scanline, skin.scanline);
        }
    }

    public void Prepare(int firstScan, int lastScan,
        VisualTreeAsset scanlineTemplate)
    {
        // Respond to scan direction.
        switch (Modifiers.instance.scanDirection)
        {
            case Modifiers.ScanDirection.Normal:
                topHalf.direction = ScanDirection.Right;
                bottomHalf.direction = ScanDirection.Left;
                break;
            case Modifiers.ScanDirection.RR:
                topHalf.direction = ScanDirection.Right;
                bottomHalf.direction = ScanDirection.Right;
                break;
            case Modifiers.ScanDirection.LR:
                topHalf.direction = ScanDirection.Left;
                bottomHalf.direction = ScanDirection.Right;
                break;
            case Modifiers.ScanDirection.LL:
                topHalf.direction = ScanDirection.Left;
                bottomHalf.direction = ScanDirection.Left;
                break;
        }
        System.Action<HalfElements> setUpFlip =
            (HalfElements half) =>
        {
            half.countdownBg.EnableInClassList("left-side",
                half.direction == ScanDirection.Right);
            half.countdownBg.EnableInClassList("right-side",
                half.direction == ScanDirection.Left);
            half.countdownBg.EnableInClassList("h-flipped",
                half.direction == ScanDirection.Left);
            half.countdownNum.EnableInClassList("left-side",
                half.direction == ScanDirection.Right);
            half.countdownNum.EnableInClassList("right-side",
                half.direction == ScanDirection.Left);
        };
        setUpFlip(topHalf);
        setUpFlip(bottomHalf);

        // Respond to swap.
        switch (Modifiers.instance.scanPosition)
        {
            case Modifiers.ScanPosition.Normal:
                evenHalf = bottomHalf;
                oddHalf = topHalf;
                break;
            case Modifiers.ScanPosition.Swap:
                evenHalf = topHalf;
                oddHalf = bottomHalf;
                break;
        }

        // Spawn scanlines.
        scanElements = new List<ScanElements>();
        for (int i = firstScan; i <= lastScan; i++)
        {
            HalfElements half = (i % 2 == 0) ? evenHalf : oddHalf;
            ScanElements scan = new ScanElements()
            {
                scanNumber = i,
                direction = half.direction,
                anchor = scanlineTemplate.Instantiate()
            };
            scan.anchor.pickingMode = PickingMode.Ignore;
            scan.anchor.AddToClassList("scanline-anchor");
            scan.scanline = scan.anchor.Q("scanline");
            scan.scanline.EnableInClassList("h-flipped",
                scan.direction == ScanDirection.Left);

            half.scanlineContainer.Add(scan.anchor);
            scanElements.Add(scan);
        }

        // Calculate countdown length.
        if (GlobalResource.gameUiSkin
            .scanCountdownCoversFiveEighthScans)
        {
            countdownLengthInScans = 5f / 8f;
        }
        else
        {
            int bps = pattern.patternMetadata.bps;
            if (bps <= 2)
            {
                countdownLengthInScans = 3f / 4f;
            }
            else
            {
                countdownLengthInScans = 3f / bps;
            }
        }
    }

    public void Begin()
    {
        layout.style.display = 
            new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        Update(scan: 0f);
    }

    public void Dispose()
    {
        layout.RemoveFromHierarchy();
        scanElements.Clear();
    }

    public void Update(float scan)
    {
        // Because modulo doesn't work on negative numbers.
        while (scan < 0f) scan += 2f;

        // Update scanline.

        GameUISkin skin = GlobalResource.gameUiSkin;
        float scanAfterRepeat = Mathf.Repeat(scan, 1f);
        Sprite scanlineSprite = skin.scanline.GetSpriteAtFloatIndex(
            scanAfterRepeat);

        float marginBeforeScan = Ruleset.instance
            .scanMarginBeforeFirstBeat;
        float marginAfterScan = Ruleset.instance
            .scanMarginAfterLastBeat;
        foreach (ScanElements s in scanElements)
        {
            float relativeScan = scan - s.scanNumber;
            float relativeX = Mathf.LerpUnclamped(marginBeforeScan,
                1f - marginAfterScan, relativeScan);
            if (s.direction == ScanDirection.Left)
            {
                relativeX = 1f - relativeX;
            }

            s.anchor.style.left = new StyleLength(new Length(
                relativeX * 100f, LengthUnit.Percent));
            s.scanline.style.backgroundImage = new StyleBackground(
                scanlineSprite);
        }

        // Update countdown.

        // Clamp scan to [0, 2].
        float clampedScanEven = Mathf.Repeat(scan, 2f);
        float clampedScanOdd = Mathf.Repeat(scan + 1f, 2f);
        // Calculate countdown progress.
        float countdownProgresEven = Mathf.InverseLerp(
            2f - countdownLengthInScans, 2f, clampedScanEven);
        float countdownProgresOdd = Mathf.InverseLerp(
            2f - countdownLengthInScans, 2f, clampedScanOdd);
        System.Action<HalfElements, float> setCountdownProgress =
            (HalfElements elements, float progress) =>
            {
                if (progress < 0f)
                {
                    elements.countdownBg.visible = false;
                    elements.countdownNum.visible = false;
                }
                else
                {
                    elements.countdownBg.visible = true;
                    elements.countdownBg.style.backgroundImage =
                        new StyleBackground(
                            skin.scanCountdownBackground
                            .GetSpriteAtFloatIndex(progress));
                    elements.countdownNum.visible = true;
                    elements.countdownNum.style.backgroundImage =
                        new StyleBackground(
                            skin.scanCountdownNumbers
                            .GetSpriteAtFloatIndex(progress));
                }
            };
        setCountdownProgress(evenHalf, countdownProgresEven);
        setCountdownProgress(oddHalf, countdownProgresOdd);
    }
}
