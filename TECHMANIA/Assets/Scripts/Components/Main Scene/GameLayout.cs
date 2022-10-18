using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Controls scanline and scan countdowns.
public class GameLayout
{
    private Pattern pattern;
    private float countdownLengthInScans;

    private VisualElement gameContainer;
    private float scanHeight => gameContainer.resolvedStyle
        .height * 0.5f;

    private enum Direction
    {
        Left,
        Right
    }

    private TemplateContainer layout;
    private class ScanElements
    {
        public Direction direction;
        public VisualElement scanlineAnchor;
        public VisualElement scanline;
        public VisualElement countdownBg;
        public VisualElement countdownNum;
    }
    private ScanElements topScan;
    private ScanElements bottomScan;
    // Points to one of topScan/bottomScan.
    private ScanElements evenScan;
    // Points to one of topScan/bottomScan.
    private ScanElements oddScan;

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

        System.Func<VisualElement, ScanElements> makeScanElements =
            (VisualElement element) => new ScanElements()
            {
                scanlineAnchor = element.Q("scanline-anchor"),
                scanline = element.Q("scanline"),
                countdownBg = element.Q("countdown-bg"),
                countdownNum = element.Q("countdown-num")
            };
        topScan = makeScanElements(layout.Q("top-half"));
        bottomScan = makeScanElements(layout.Q("bottom-half"));
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
        System.Action<ScanElements> setAspectRatioForScan =
            (ScanElements scanElements) =>
            {
                setAspectRatio(scanElements.scanline,
                    skin.scanline);
                setAspectRatio(scanElements.countdownBg,
                    skin.scanCountdownBackground);
                setAspectRatio(scanElements.countdownNum,
                    skin.scanCountdownNumbers);
            };
        setAspectRatioForScan(topScan);
        setAspectRatioForScan(bottomScan);
    }

    public void Prepare()
    {
        ResetAspectRatio();

        // Respond to scan direction.
        switch (Modifiers.instance.scanDirection)
        {
            case Modifiers.ScanDirection.Normal:
                topScan.direction = Direction.Right;
                bottomScan.direction = Direction.Left;
                break;
            case Modifiers.ScanDirection.RR:
                topScan.direction = Direction.Right;
                bottomScan.direction = Direction.Right;
                break;
            case Modifiers.ScanDirection.LR:
                topScan.direction = Direction.Left;
                bottomScan.direction = Direction.Right;
                break;
            case Modifiers.ScanDirection.LL:
                topScan.direction = Direction.Left;
                bottomScan.direction = Direction.Left;
                break;
        }
        System.Action<ScanElements> setUpFlip =
            (ScanElements elements) =>
        {
            elements.scanline.EnableInClassList("h-flipped",
                elements.direction == Direction.Left);
            elements.countdownBg.EnableInClassList("left-side",
                elements.direction == Direction.Right);
            elements.countdownBg.EnableInClassList("right-side",
                elements.direction == Direction.Left);
            elements.countdownBg.EnableInClassList("h-flipped",
                elements.direction == Direction.Left);
            elements.countdownNum.EnableInClassList("left-side",
                elements.direction == Direction.Right);
            elements.countdownNum.EnableInClassList("right-side",
                elements.direction == Direction.Left);
        };
        setUpFlip(topScan);
        setUpFlip(bottomScan);

        // Respond to swap.
        switch (Modifiers.instance.scanPosition)
        {
            case Modifiers.ScanPosition.Normal:
                evenScan = bottomScan;
                oddScan = topScan;
                break;
            case Modifiers.ScanPosition.Swap:
                evenScan = topScan;
                oddScan = bottomScan;
                break;
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
    }

    public void Update(float scan)
    {
        // Because modulo doesn't work on negative numbers.
        while (scan < 0f) scan += 2f;

        // Update scanline position.

        float marginBeforeScan = Ruleset.instance
            .scanMarginBeforeFirstBeat;
        float marginAfterScan = Ruleset.instance
            .scanMarginAfterLastBeat;

        // Clamp scan to [-0.5, 1.5].
        float relativeScanEven = Mathf.Repeat(scan + 0.5f, 2f) - 0.5f;
        float relativeScanOdd = Mathf.Repeat(scan + 1.5f, 2f) - 0.5f;
        // And then take margins into account.
        relativeScanEven = Mathf.LerpUnclamped(marginBeforeScan,
            1f - marginAfterScan, relativeScanEven);
        relativeScanOdd = Mathf.LerpUnclamped(marginBeforeScan,
            1f - marginAfterScan, relativeScanOdd);

        System.Action<ScanElements, float> setRelativeScan =
            (ScanElements elements, float relativeScan) =>
            {
                if (elements.direction == Direction.Left)
                {
                    relativeScan = 1f - relativeScan;
                }
                elements.scanlineAnchor.style.left =
                    new StyleLength(new Length(
                        relativeScan * 100f, LengthUnit.Percent));
            };
        setRelativeScan(evenScan, relativeScanEven);
        setRelativeScan(oddScan, relativeScanOdd);

        // Update countdown.

        GameUISkin skin = GlobalResource.gameUiSkin;
        // Clamp scan to [0, 2].
        relativeScanEven = Mathf.Repeat(scan, 2f);
        relativeScanOdd = Mathf.Repeat(scan + 1f, 2f);
        // Calculate countdown progress.
        float countdownProgresEven = Mathf.InverseLerp(
            2f - countdownLengthInScans, 2f, relativeScanEven);
        float countdownProgresOdd = Mathf.InverseLerp(
            2f - countdownLengthInScans, 2f, relativeScanOdd);
        System.Action<ScanElements, float> setCountdownProgress =
            (ScanElements elements, float progress) =>
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
        setCountdownProgress(evenScan, countdownProgresEven);
        setCountdownProgress(oddScan, countdownProgresOdd);
    }
}
