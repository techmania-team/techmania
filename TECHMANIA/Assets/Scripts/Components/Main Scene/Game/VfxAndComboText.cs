using FMOD;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VfxAndComboText
{
    private class ComboText
    {
        private class ComboTextElements
        {
            public VisualElement center;
            public VisualElement distanceToNote;
            public VisualElement layoutContainer;
            public VisualElement judgement;
            public VisualElement space;
            public List<VisualElement> digits;
        }
        private ComboTextElements elements;
        private VisualElement elementToFollow;
        private float sizeUnit;
        private float startTime;
        private SpriteSheet judgementSpriteSheet;
        private List<SpriteSheet> comboDigitSpriteSheet;

        private Material additiveMaterial;

        public ComboText(TemplateContainer vfxAndComboTextTemplateInstance,
            Material additiveMaterial)
        {
            elements = new ComboTextElements
            {
                center = vfxAndComboTextTemplateInstance.Q<VisualElement>("combo-text-center")
            };
            elements.distanceToNote = elements.center.
                Q<VisualElement>("distance-to-note");
            elements.layoutContainer = elements.distanceToNote.
                Q<VisualElement>("layout-container");
            elements.judgement = elements.layoutContainer.
                Q<VisualElement>("judgement");
            elements.space = elements.layoutContainer.
                Q<VisualElement>("space");
            elements.digits = new List<VisualElement>()
            {
                elements.layoutContainer.Q<VisualElement>("digit-1"),
                elements.layoutContainer.Q<VisualElement>("digit-2"),
                elements.layoutContainer.Q<VisualElement>("digit-3"),
                elements.layoutContainer.Q<VisualElement>("digit-4")
            };
            elementToFollow = null;
            comboDigitSpriteSheet = new List<SpriteSheet>();
            foreach (VisualElement e in elements.digits)
            {
                comboDigitSpriteSheet.Add(null);
            }

            this.additiveMaterial = additiveMaterial;
        }

        public void Update()
        {
            if (elementToFollow != null)
            {
                Follow();
                UpdateAnimationCurves();
                UpdateSprites();
            }
        }

        public void ResetSize(float scanHeight)
        {
            if (GlobalResource.comboSkin == null) return;

            sizeUnit = scanHeight / 500f;
            elements.distanceToNote.style.bottom = new StyleLength(
                sizeUnit * GlobalResource.comboSkin.distanceToNote + 200);
            elements.layoutContainer.style.height = new StyleLength(
                sizeUnit * GlobalResource.comboSkin.height);
            elements.space.style.width = new StyleLength(
                sizeUnit * GlobalResource.comboSkin.spaceBetweenJudgementAndCombo);
        }

        public void Hide()
        {
            elementToFollow = null;  // To stop Follow()
            elements.center.style.display = DisplayStyle.None;
        }

        public void Show(VisualElement noteImage, Judgement judgement,
        ScoreKeeper scoreKeeper)
        {
            Show(noteImage, judgement,
                scoreKeeper.feverState == ScoreKeeper.FeverState.Active,
                scoreKeeper.currentCombo);
        }

        public void Show(VisualElement noteImage, Judgement judgement,
            bool fever, int combo)
        {
            if (noteImage != null)
            {
                elementToFollow = noteImage;
            }
            Follow();
            elements.center.style.display = DisplayStyle.Flex;

            Func<SpriteSheet, float> getSpriteWidth = (SpriteSheet spriteSheet) =>
            {
                if (spriteSheet.sprites.Count == 0) return 0f;
                float height = GlobalResource.comboSkin.height * sizeUnit;
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
            elements.judgement.style.width = new StyleLength(
                getSpriteWidth(judgementSpriteSheet));

            // Draw combo, if applicable.

            if (judgement != Judgement.Miss &&
                judgement != Judgement.Break &&
                combo > 0)
            {
                elements.space.style.display = DisplayStyle.Flex;

                List<int> digits = new List<int>();
                int remainingCombo = combo;
                for (int i = 0; i < elements.digits.Count; i++)
                {
                    digits.Insert(0, remainingCombo % 10);
                    remainingCombo /= 10;
                }
                for (int i = 0; i < elements.digits.Count; i++)
                {
                    comboDigitSpriteSheet[i] = comboDigitSpriteSheetList[digits[i]];
                }

                // Turn off the left-most 0 digits.
                elements.digits.ForEach(e =>
                    e.style.display = DisplayStyle.Flex);
                for (int i = 0; i < elements.digits.Count; i++)
                {
                    if (digits[i] == 0)
                    {
                        elements.digits[i].style.display = DisplayStyle.None;
                    }
                    else
                    {
                        break;
                    }
                }

                for (int i = 0; i < elements.digits.Count; i++)
                {
                    if (elements.digits[i].style.display == DisplayStyle.None)
                        continue;
                    elements.digits[i].style.width = new StyleLength(
                        getSpriteWidth(comboDigitSpriteSheet[i]));
                }
            }
            else
            {
                elements.space.style.display = DisplayStyle.None;
                elements.digits.ForEach(e => e.style.display = DisplayStyle.None);
            }

            startTime = Time.time;
            ResetAllAnimationAttributes();
            UpdateAnimationCurves();
            UpdateSprites();
        }

        private void Follow()
        {
            if (elementToFollow == null) return;

            Vector2 position = elementToFollow.worldBound.center;
            elements.center.style.left = new StyleLength(position.x);
            elements.center.style.top = new StyleLength(position.y);
        }

        private void ResetAllAnimationAttributes()
        {

        }

        private void UpdateAnimationCurves()
        {

        }

        private void UpdateSprites()
        {
            float time = Time.time - startTime;

            if (judgementSpriteSheet != null)
            {
                elements.judgement.style.backgroundImage = new StyleBackground(
                    judgementSpriteSheet.GetSpriteForTime(time, loop: true));
            }
            for (int i = 0; i < elements.digits.Count; i++)
            {
                if (elements.digits[i].style.display == DisplayStyle.Flex)
                {
                    elements.digits[i].style.backgroundImage = new StyleBackground(
                        comboDigitSpriteSheet[i].GetSpriteForTime(time, loop: true));
                }
            }
        }

        
    }

    private TemplateContainer templateInstance;

    private ComboText comboText;

    private VisualElement vfxContainer;
    private float laneHeight;

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

        this.additiveMaterial = additiveMaterial;
        this.timer = timer;
        this.layout = layout;

        comboText = new ComboText(templateInstance, additiveMaterial);
    }

    public void Update()
    {
        // VFX: TODO

        comboText.Update();
    }

    public void ResetSize(float laneHeight, float scanHeight)
    {
        // VFX
        this.laneHeight = laneHeight;

        comboText.ResetSize(scanHeight);
    }

    public void Dispose()
    {
        templateInstance.RemoveFromHierarchy();
    }

    public void ShowComboText(VisualElement noteImage, Judgement judgement,
        ScoreKeeper scoreKeeper)
    {
        comboText.Show(noteImage, judgement, scoreKeeper);
    }

    public void HideComboText()
    {
        comboText.Hide();
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
