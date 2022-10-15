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

    private TemplateContainer layout;
    private class ScanElements
    {
        public VisualElement scanlineAnchor;
        public VisualElement scanline;
        public VisualElement countdownBg;
        public VisualElement countdown;
    }
    private ScanElements topScan;
    private ScanElements bottomScan;

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
        // TODO: respond to swap & scan direction
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

    }
}
