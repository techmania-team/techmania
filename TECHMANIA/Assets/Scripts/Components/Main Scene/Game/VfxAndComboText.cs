using System;
using System.Collections.Generic;
using ThemeApi;
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
                sizeUnit * GlobalResource.comboSkin.distanceToNote);
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
            if (judgementSpriteSheet.additiveShader)
            {
                elements.judgement.style.unityMaterial = additiveMaterial;
            }
            else
            {
                elements.judgement.style.unityMaterial = null;
            }

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
                    if (comboDigitSpriteSheet[i].additiveShader)
                    {
                        elements.digits[i].style.unityMaterial = additiveMaterial;
                    }
                    else
                    {
                        elements.digits[i].style.unityMaterial = null;
                    }
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

            Vector2 worldPosition = elementToFollow.worldBound.center;
            Vector2 localPosition = elements.center.parent.WorldToLocal(worldPosition);
            elements.center.style.left = new StyleLength(localPosition.x);
            elements.center.style.top = new StyleLength(localPosition.y);
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

        #region Animation
        private void SetTranslationX(float value)
        {
            elements.distanceToNote.style.left = new StyleLength(
                value * sizeUnit);
        }

        private void SetTranslationY(float value)
        {
            elements.distanceToNote.style.bottom = new StyleLength(
                (GlobalResource.comboSkin.distanceToNote + value) * sizeUnit);
        }

        private void SetRotationInDegrees(float value)
        {
            // Negate the value to rotate in the opposite direction, for backwards
            // compatibility w/ UGUI combo text.
            elements.layoutContainer.style.rotate = new StyleRotate(new Rotate(
                Angle.Degrees(-value)));
        }

        private void SetScaleX(float value)
        {
            elements.layoutContainer.style.scale = new StyleScale(new Vector2(
                value, elements.layoutContainer.style.scale.value.value.y));
        }

        private void SetScaleY(float value)
        {
            elements.layoutContainer.style.scale = new StyleScale(new Vector2(
                elements.layoutContainer.style.scale.value.value.x, value));
        }

        private void SetAlpha(float value)
        {
            elements.layoutContainer.style.opacity = new StyleFloat(value);
        }

        private void ResetAllAnimationAttributes()
        {
            SetTranslationX(0f);
            SetTranslationY(0f);
            SetRotationInDegrees(0f);
            SetScaleX(1f);
            SetScaleY(1f);
            SetAlpha(1f);
        }

        private void UpdateAnimationCurves()
        {
            float time = Time.time - startTime;

            foreach (Tuple<AnimationCurve, string> tuple in
                GlobalResource.comboAnimationCurvesAndAttributes)
            {
                AnimationCurve curve = tuple.Item1;
                string attribute = tuple.Item2;

                float value = curve.Evaluate(time);
                switch (attribute)
                {
                    case "translationX":
                        SetTranslationX(value);
                        break;
                    case "translationY":
                        SetTranslationY(value);
                        break;
                    case "rotationInDegrees":
                        SetRotationInDegrees(value);
                        break;
                    case "scaleX":
                        SetScaleX(value);
                        break;
                    case "scaleY":
                        SetScaleY(value);
                        break;
                    case "alpha":
                        SetAlpha(value);
                        break;
                    default:
                        Debug.LogWarning("Unknown attribute in combo animation: " + attribute);
                        break;
                }
            }
        }
        #endregion
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
