using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Controls scanline and scan countdowns.
public class GameLayout
{
    private Pattern pattern;
    private VisualElement gameContainer;
    private float scanHeight => gameContainer.resolvedStyle
        .height * 0.5f;

    private enum Direction
    {
        Left,
        Right
    }
    private enum Position
    {
        Top,
        Bottom
    }

    private TemplateContainer layout;
    private class ScanElements
    {
        public Direction direction;
        public VisualElement scanlineAnchor;
        public VisualElement scanline;
        public VisualElement countdownBg;
        public VisualElement countdown;
    }
    private ScanElements topScan;
    private ScanElements bottomScan;
    private Position evenScanPosition;

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
                countdown = element.Q("countdown")
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
                setAspectRatio(scanElements.countdown,
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
            elements.countdown.EnableInClassList("left-side",
                elements.direction == Direction.Right);
            elements.countdown.EnableInClassList("right-side",
                elements.direction == Direction.Left);
        };
        setUpFlip(topScan);
        setUpFlip(bottomScan);

        // Respond to swap.
        switch (Modifiers.instance.scanPosition)
        {
            case Modifiers.ScanPosition.Normal:
                evenScanPosition = Position.Bottom;
                break;
            case Modifiers.ScanPosition.Swap:
                evenScanPosition = Position.Top;
                break;
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
        while (scan < 0f) scan += 2f;

        // Update scanline position.
        float marginBeforeScan = Ruleset.instance
            .scanMarginBeforeFirstBeat;
        float marginAfterScan = Ruleset.instance
            .scanMarginAfterLastBeat;

        // Limit scan to [-0.5, 1.5].
        float relativeScanEven = Mathf.Repeat(scan + 0.5f, 2f) - 0.5f;
        float relativeScanOdd = Mathf.Repeat(scan - 0.5f, 2f) - 0.5f;
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
        switch (evenScanPosition)
        {
            case Position.Top:
                setRelativeScan(topScan, relativeScanEven);
                setRelativeScan(bottomScan, relativeScanOdd);
                break;
            case Position.Bottom:
                setRelativeScan(bottomScan, relativeScanEven);
                setRelativeScan(topScan, relativeScanOdd);
                break;
        }
    }
}
