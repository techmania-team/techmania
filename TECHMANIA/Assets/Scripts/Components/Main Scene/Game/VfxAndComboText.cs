using FMOD;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VfxAndComboText
{
    private TemplateContainer templateInstance;

    private VisualElement vfxContainer;
    private float laneHeight;

    private class ComboTextElements
    {
        public VisualElement center;
        public VisualElement distanceToNote;
        public VisualElement layoutContainer;
        public VisualElement judgement;
        public VisualElement space;
        public List<VisualElement> digits;
    }
    private ComboTextElements comboTextElements;
    private VisualElement comboTextElementToFollow;
    private float comboTextSizeUnit;
    private float comboTextStartTime;
    private SpriteSheet judgementSpriteSheet;
    private List<SpriteSheet> comboDigitSpriteSheet;

    private Material additiveMaterial;

    // To be passed to
    // HoldTrailAndExtensions.GetOngoingTrailEndPosition.
    private GameTimer timer;
    // To query scanline position when placing ongoing VFX.
    private GameLayout layout;

    public VfxAndComboText(VisualTreeAsset vfxAndComboTemplate,
        VisualElement vfxAndComboContainer,
        Material additiveMaterial,
        GameTimer timer,
        GameLayout layout)
    {
        templateInstance = vfxAndComboTemplate.Instantiate();
        vfxAndComboContainer.Add(templateInstance);

        vfxContainer = templateInstance.Q<VisualElement>("vfx-container");

        comboTextElements = new ComboTextElements
        {
            center = templateInstance.Q<VisualElement>("combo-text-center")
        };
        comboTextElements.distanceToNote = comboTextElements.center.
            Q<VisualElement>("distance-to-note");
        comboTextElements.layoutContainer = comboTextElements.distanceToNote.
            Q<VisualElement>("layout-container");
        comboTextElements.judgement = comboTextElements.layoutContainer.
            Q<VisualElement>("judgement");
        comboTextElements.space = comboTextElements.layoutContainer.
            Q<VisualElement>("space");
        comboTextElements.digits = new List<VisualElement>()
        {
            comboTextElements.layoutContainer.Q<VisualElement>("digit-1"),
            comboTextElements.layoutContainer.Q<VisualElement>("digit-2"),
            comboTextElements.layoutContainer.Q<VisualElement>("digit-3"),
            comboTextElements.layoutContainer.Q<VisualElement>("digit-4")
        };
        comboTextElementToFollow = null;

        this.additiveMaterial = additiveMaterial;
        this.timer = timer;
        this.layout = layout;
    }

    public void Update()
    {
        // VFX: TODO

        // Combo text
        if (comboTextElementToFollow != null)
        {
            ComboTextFollow();
            ComboTextUpdateAnimationCurves();
            ComboTextUpdateSprites();
        }
    }

    public void ResetSize(float laneHeight, float scanHeight)
    {
        // VFX
        this.laneHeight = laneHeight;

        // Combo text
        if (GlobalResource.comboSkin == null) return;

        comboTextSizeUnit = scanHeight / 500f;
        comboTextElements.distanceToNote.style.bottom = new StyleLength(
            comboTextSizeUnit * GlobalResource.comboSkin.distanceToNote + 100);
        comboTextElements.layoutContainer.style.height = new StyleLength(
            comboTextSizeUnit * GlobalResource.comboSkin.height);
        comboTextElements.space.style.width = new StyleLength(
            comboTextSizeUnit * GlobalResource.comboSkin.spaceBetweenJudgementAndCombo);
    }

    public void Dispose()
    {
        templateInstance.RemoveFromHierarchy();
    }

    public void ShowComboText(VisualElement noteImage, Judgement judgement,
        ScoreKeeper scoreKeeper)
    {
        ShowComboText(noteImage, judgement,
            scoreKeeper.feverState == ScoreKeeper.FeverState.Active,
            scoreKeeper.currentCombo);
    }

    public void ShowComboText(VisualElement noteImage, Judgement judgement,
        bool fever, int combo)
    {
        if (noteImage != null)
        {
            comboTextElementToFollow = noteImage;
        }
        ComboTextFollow();
        comboTextElements.center.style.display = DisplayStyle.Flex;

        Func<SpriteSheet, float> getSpriteWidth = (SpriteSheet spriteSheet) =>
        {
            if (spriteSheet.sprites.Count == 0) return 0f;
            float height = GlobalResource.comboSkin.height * comboTextSizeUnit;
            float ratio = spriteSheet.sprites[0].rect.height /
                spriteSheet.sprites[0].rect.width;
            return height / ratio;
        };

        // Draw judgement.

        List<SpriteSheet> comboDigitSpriteSheetList = null;
        if (fever &&
            (judgement == Judgement.RainbowMax ||
             judgement == Judgement.Max ||
             judgement == Judgement.Cool))
        {
            judgementSpriteSheet = GlobalResource.comboSkin.feverMaxJudgement;
            comboDigitSpriteSheetList = GlobalResource.comboSkin.feverMaxDigits;
        }
        else
        {
            switch (judgement)
            {
                case Judgement.RainbowMax:
                    judgementSpriteSheet = GlobalResource.comboSkin.rainbowMaxJudgement;
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
                        .rainbowMaxDigits;
                    break;
                case Judgement.Max:
                    judgementSpriteSheet = GlobalResource.comboSkin.maxJudgement;
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
                        .maxDigits;
                    break;
                case Judgement.Cool:
                    judgementSpriteSheet = GlobalResource.comboSkin.coolJudgement;
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
                        .coolDigits;
                    break;
                case Judgement.Good:
                    judgementSpriteSheet = GlobalResource.comboSkin.goodJudgement;
                    comboDigitSpriteSheetList = GlobalResource.comboSkin
                        .goodDigits;
                    break;
                case Judgement.Miss:
                    judgementSpriteSheet = GlobalResource.comboSkin.missJudgement;
                    break;
                case Judgement.Break:
                    judgementSpriteSheet = GlobalResource.comboSkin.breakJudgement;
                    break;
            }
        }
        comboTextElements.judgement.style.height = new StyleLength(
            getSpriteWidth(judgementSpriteSheet));

        // Draw combo, if applicable.

        if (judgement != Judgement.Miss &&
            judgement != Judgement.Break &&
            combo > 0)
        {
            comboTextElements.space.style.display = DisplayStyle.Flex;

            List<int> digits = new List<int>();
            int remainingCombo = combo;
            for (int i = 0; i < comboTextElements.digits.Count; i++)
            {
                digits.Insert(0, remainingCombo % 10);
                remainingCombo /= 10;
            }
            for (int i = 0; i < comboTextElements.digits.Count; i++)
            {
                comboDigitSpriteSheet[i] = comboDigitSpriteSheetList[digits[i]];
            }

            // Turn off the left-most 0 digits.
            comboTextElements.digits.ForEach(e =>
                e.style.display = DisplayStyle.Flex);
            for (int i = 0; i < comboTextElements.digits.Count; i++)
            {
                if (digits[i] == 0)
                {
                    comboTextElements.digits[i].style.display = DisplayStyle.None;
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < comboTextElements.digits.Count; i++)
            {
                if (comboTextElements.digits[i].style.display == DisplayStyle.None)
                    continue;
                comboTextElements.digits[i].style.width = new StyleLength(
                    getSpriteWidth(comboDigitSpriteSheet[i]));
            }
        }
        else
        {
            comboTextElements.space.style.display = DisplayStyle.None;
            comboTextElements.digits.ForEach(e => e.style.display = DisplayStyle.None);
        }

        comboTextStartTime = Time.time;
        ComboTextResetAllAnimationAttributes();
        ComboTextUpdateAnimationCurves();
        ComboTextUpdateSprites();
    }

    private void ComboTextFollow()
    {
        if (comboTextElementToFollow == null) return;

        Vector2 position = comboTextElementToFollow.worldBound.center;
        comboTextElements.center.style.left = new StyleLength(position.x);
        comboTextElements.center.style.top = new StyleLength(position.y);
    }

    private void ComboTextResetAllAnimationAttributes()
    {

    }

    private void ComboTextUpdateAnimationCurves()
    {

    }

    private void ComboTextUpdateSprites()
    {
        float time = Time.time - comboTextStartTime;

        if (judgementSpriteSheet != null)
        {
            comboTextElements.judgement.style.backgroundImage = new StyleBackground(
                judgementSpriteSheet.GetSpriteForTime(time, loop: true));
        }
        for (int i = 0; i < comboTextElements.digits.Count; i++)
        {
            if (comboTextElements.digits[i].style.display == DisplayStyle.Flex)
            {
                comboTextElements.digits[i].style.backgroundImage = new StyleBackground(
                    comboDigitSpriteSheet[i].GetSpriteForTime(time, loop: true));
            }
        }
    }

    public void HideComboText()
    {
        comboTextElementToFollow = null;  // To stop ComboTextFollow()
        comboTextElements.center.style.display = DisplayStyle.None;
    }

    public void JumpToScan()
    {

    }

    public void SpawnOngoingVFX(NoteElements elements, Judgement judgement)
    {

    }

    public void SpawnResolvedVFX(NoteElements elements, Judgement judgement)
    {

    }
}
