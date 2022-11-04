using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Controls scanline and scan countdowns.
public class GameLayout
{
    private Pattern pattern;
    private float countdownLengthInScans;

    public TemplateContainer layoutContainer { get; private set; }

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
    public float scanHeight => layoutContainer.resolvedStyle
        .height * 0.5f;
    public float laneHeight => scanHeight * (1f -
        Ruleset.instance.scanMarginTopBottom[
            pattern.patternMetadata.playableLanes - 2] -
        Ruleset.instance.scanMarginMiddle[
            pattern.patternMetadata.playableLanes - 2]) /
        pattern.patternMetadata.playableLanes;

    public float screenWidth => layoutContainer.resolvedStyle.width;
    public ScanDirection evenScanDirection => evenHalf.direction;
    public ScanDirection oddScanDirection => oddHalf.direction;
    public VisualElement evenScanNoteContainer =>
        evenHalf.noteContainer;
    public VisualElement oddScanNoteContainer =>
        oddHalf.noteContainer;
    public VisualElement topHalfBg => topHalf.noteContainer;
    public VisualElement bottomHalfBg => bottomHalf.noteContainer;
    #endregion

    public GameLayout(Pattern pattern,
        VisualElement gameContainer,
        VisualTreeAsset layoutTemplate)
    {
        this.pattern = pattern;

        layoutContainer = layoutTemplate.Instantiate();
        // Make sure the game - where everything uses absolute
        // positioning - takes up the entirety of setup.gameContainer.
        layoutContainer.style.flexGrow = new StyleFloat(1f);
        // Layout becomes visible on Begin().
        layoutContainer.style.visibility = Visibility.Hidden;
        gameContainer.Add(layoutContainer);

        System.Func<VisualElement, HalfElements> makeHalfElements =
            (VisualElement element) => new HalfElements()
            {
                scanlineContainer = element.Q("scanline-container"),
                countdownBg = element.Q("countdown-bg"),
                countdownNum = element.Q("countdown-num"),
                noteContainer = element.Q("note-container")
            };
        topHalf = makeHalfElements(layoutContainer.Q("top-half"));
        bottomHalf = makeHalfElements(layoutContainer.Q(
            "bottom-half"));
    }

    public void ResetSize()
    {
        GameUISkin skin = GlobalResource.gameUiSkin;

        System.Action<VisualElement, SpriteSheet> setSize =
            (VisualElement element, SpriteSheet spriteSheet) =>
            {
                Rect rect = spriteSheet.sprites[0].rect;
                element.style.width = scanHeight *
                    rect.width / rect.height;
            };

        System.Action<HalfElements> setSizeForHalf =
            (HalfElements half) =>
            {
                setSize(half.countdownBg,
                    skin.scanCountdownBackground);
                setSize(half.countdownNum,
                    skin.scanCountdownNumbers);
            };
        setSizeForHalf(topHalf);
        setSizeForHalf(bottomHalf);

        foreach (ScanElements scan in scanElements)
        {
            setSize(scan.scanline, skin.scanline);
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

    public void PlaceNoteElements(float floatScan, int intScan,
        NoteElements elements)
    {
        // Place in hierarchy.
        VisualElement noteContainer =
            (intScan % 2 == 0) ? evenScanNoteContainer
            : oddScanNoteContainer;
        noteContainer.Add(elements.templateContainer);

        // Set position.
        PlaceElementHorizontally(elements.templateContainer,
            relativeScan: floatScan - intScan,
            (intScan % 2 == 0) ? evenScanDirection : oddScanDirection);
        PlaceElementVertically(elements.templateContainer,
            intScan, elements.note.lane);
    }

    private void PlaceElementHorizontally(VisualElement element,
        float relativeScan, ScanDirection scanDirection)
    {
        float marginBeforeScan = Ruleset.instance
            .scanMarginBeforeFirstBeat;
        float marginAfterScan = Ruleset.instance
            .scanMarginAfterLastBeat;
        float relativeX = Mathf.LerpUnclamped(marginBeforeScan,
                1f - marginAfterScan, relativeScan);
        if (scanDirection == ScanDirection.Left)
        {
            relativeX = 1f - relativeX;
        }
        element.style.left = new StyleLength(new Length(
            relativeX * 100f, LengthUnit.Percent));
    }

    private void PlaceElementVertically(VisualElement element,
        int scan, int lane)
    {
        int playableLanes = pattern.patternMetadata.playableLanes;
        float relativeLaneHeight = (1f -
            Ruleset.instance.scanMarginTopBottom[playableLanes - 2] -
            Ruleset.instance.scanMarginMiddle[playableLanes - 2])
            / playableLanes;

        HalfElements half = (scan % 2 == 0) ? evenHalf : oddHalf;
        float topOfLaneZero = (half == topHalf) ?
            Ruleset.instance.scanMarginTopBottom[playableLanes - 2] :
            Ruleset.instance.scanMarginMiddle[playableLanes - 2];
        float relativeY = topOfLaneZero + relativeLaneHeight *
            (lane + 0.5f);
        element.style.top = new StyleLength(new Length(
            relativeY * 100f, LengthUnit.Percent));
    }

    public void Begin()
    {
        layoutContainer.style.visibility = Visibility.Visible;
        Update(scan: 0f);
    }

    public void Dispose()
    {
        layoutContainer.RemoveFromHierarchy();
        scanElements.Clear();
    }

    public void Update(float scan)
    {
        // Update scanline.
        // TODO: respond to scanline opacity.
        // TODO: respond to auto play.

        GameUISkin skin = GlobalResource.gameUiSkin;
        Sprite scanlineSprite = skin.scanline.GetSpriteAtFloatIndex(
            scan);

        foreach (ScanElements s in scanElements)
        {
            float relativeScan = scan - s.scanNumber;
            PlaceElementHorizontally(s.anchor, relativeScan,
                s.direction);
            s.scanline.style.backgroundImage = new StyleBackground(
                scanlineSprite);
        }

        // Update countdown.

        // Because modulo doesn't work on negative numbers.
        while (scan < 0f) scan += 2f;
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

    #region Point to lane number
    public const int kOutsideAllLanes = -1;

    public int ScreenPointToLaneNumber(Vector2 screenPoint)
    {
        Vector2 localPoint = ThemeApi.VisualElementTransform
            .ScreenSpaceToElementLocalSpace(
            layoutContainer, screenPoint);
        if (!layoutContainer.ContainsPoint(localPoint))
        {
            return kOutsideAllLanes;
        }

        float normalizedY = localPoint.y * 2f /
            layoutContainer.contentRect.height;  // In [0, 2]
        int playableLanes = pattern.patternMetadata.playableLanes;

        float marginTopBottom = Ruleset.instance.scanMarginTopBottom
            [playableLanes - 2];
        float marginMiddle = Ruleset.instance.scanMarginMiddle
            [playableLanes - 2];
        if (normalizedY < marginTopBottom)
            return 0;
        if (normalizedY >= 1f - marginMiddle
            && normalizedY < 1f)
            return playableLanes - 1;
        if (normalizedY >= 1f
            && normalizedY < 1f + marginMiddle)
            return 0;
        if (normalizedY >= 2f - marginTopBottom)
            return playableLanes - 1;

        float normalizedLaneHeight =
            (1f - marginTopBottom - marginMiddle) / playableLanes;
        if (normalizedY < 1f)
        {
            return Mathf.FloorToInt(
                (normalizedY - marginTopBottom)
                / normalizedLaneHeight);
        }
        else
        {
            return Mathf.FloorToInt(
                (normalizedY - 1f - marginMiddle)
                / normalizedLaneHeight);
        }
    }
    #endregion
}
