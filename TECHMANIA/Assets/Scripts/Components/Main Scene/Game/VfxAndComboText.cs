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

    private class VfxManager
    {
        /* One-shot VFX vs Looping VFX
         * 
         *                |      One-shot       |        Looping
         *  -------------------------------------------------------------
         *  Managed       | Update(),           | Update(), ResetSize(),
         *  once spawned? | ResetSize()         | SetCenter(), Dispose()
         *  -------------------------------------------------------------
         *  Removed when  |        No           |          Yes
         *  jumping scan? |                     |
         *  -------------------------------------------------------------
         *  End of life   | Removes itself once | Removed by VfxManager
         *                | animation ends      | once note resolves
         *  -------------------------------------------------------------
         */
        private class VfxLayer
        {
            // If false, VfxManager should remove this object.
            public bool active;

            private TemplateContainer templateContainer;
            private VisualElement centerElement;
            private VisualElement layerElement;

            private SpriteSheet spriteSheet;
            private bool loop;

            private float startTime;

            public void Initialize(VisualElement vfxContainer,
                VisualTreeAsset vfxLayerTemplate,
                Material additiveMaterial, Vector2 center,
                SpriteSheet spriteSheet, bool loop)
            {
                this.spriteSheet = spriteSheet;
                this.loop = loop;

                if (spriteSheet.sprites == null ||
                    spriteSheet.sprites.Count == 0)
                {
                    active = false;
                    templateContainer = null;
                    centerElement = null;
                    layerElement = null;
                    return;
                }

                active = true;
                templateContainer = vfxLayerTemplate.Instantiate();
                vfxContainer.Add(templateContainer);
                centerElement = templateContainer.Q<VisualElement>("center");
                layerElement = centerElement.Q<VisualElement>("vfx-layer");

                SetCenter(center);

                // Set initial sprite and material.
                layerElement.style.backgroundImage = new StyleBackground(
                    spriteSheet.sprites[0]);
                if (spriteSheet.additiveShader)
                {
                    layerElement.style.unityMaterial = additiveMaterial;
                }

                startTime = Time.time;
            }

            public void ResetSize(float laneHeight)
            {
                if (!active) return;

                float height = laneHeight * spriteSheet.scale;
                float width = spriteSheet.sprites[0].rect.width /
                    spriteSheet.sprites[0].rect.height * height;
                layerElement.style.width = new StyleLength(width);
                layerElement.style.height = new StyleLength(height);
            }

            public void Update()
            {
                float time = Time.time - startTime;
                Sprite sprite = spriteSheet.GetSpriteForTime(time, loop);
                if (sprite == null)
                {
                    Dispose();
                }
                else
                {
                    layerElement.style.backgroundImage = new StyleBackground(sprite);
                }
            }

            public void Dispose()
            {
                templateContainer.RemoveFromHierarchy();
                active = false;
            }

            // center is in vfxContainer's local space.
            public void SetCenter(Vector2 center)
            {
                if (!active) return;

                centerElement.style.left = new StyleLength(center.x + 200);
                centerElement.style.top = new StyleLength(center.y);
            }
        }

        private VisualElement vfxContainer;
        private VisualTreeAsset vfxLayerTemplate;
        private Material additiveMaterial;

        // Track all the one-shot and looping VfxLayers.
        private List<VfxLayer> oneShotLayers;
        private Dictionary<NoteElements, List<VfxLayer>> holdNoteToOngoingHeadVfx;
        private Dictionary<NoteElements, List<VfxLayer>> holdNoteToOngoingTrailVfx;
        private Dictionary<NoteElements, List<VfxLayer>> dragNoteToOngoingVfx;

        private float laneHeight;
        // To be passed to HoldTrailAndExtensions.GetOngoingTrailEndPosition.
        private GameTimer timer;
        // To query scanline position when placing ongoing VFX.
        private GameLayout layout;

        public VfxManager(TemplateContainer vfxAndComboTextTemplateInstance,
            VisualTreeAsset vfxLayerTemplate, Material additiveMaterial,
            GameTimer timer, GameLayout layout)
        {
            this.vfxContainer = vfxAndComboTextTemplateInstance.Q<VisualElement>(
                "vfx-container");
            this.vfxLayerTemplate = vfxLayerTemplate;
            this.additiveMaterial = additiveMaterial;
            this.timer = timer;
            this.layout = layout;

            oneShotLayers = new List<VfxLayer>();
            holdNoteToOngoingHeadVfx = new Dictionary<NoteElements, List<VfxLayer>>();
            holdNoteToOngoingTrailVfx = new Dictionary<NoteElements, List<VfxLayer>>();
            dragNoteToOngoingVfx = new Dictionary<NoteElements, List<VfxLayer>>();
        }

        public void ResetSize(float laneHeight)
        {
            foreach (VfxLayer l in oneShotLayers)
            {
                l.ResetSize(laneHeight);
            }
            foreach (List<VfxLayer> list in holdNoteToOngoingHeadVfx.Values)
            {
                foreach (VfxLayer l in list) l.ResetSize(laneHeight);
            }
            foreach (List<VfxLayer> list in holdNoteToOngoingTrailVfx.Values)
            {
                foreach (VfxLayer l in list) l.ResetSize(laneHeight);
            }
            foreach (List<VfxLayer> list in dragNoteToOngoingVfx.Values)
            {
                foreach (VfxLayer l in list) l.ResetSize(laneHeight);
            }
        }

        // center is in vfxContainer's local space.
        private List<VfxLayer> SpawnVfxAt(Vector2 center,
            List<SpriteSheet> spriteSheetLayers, bool loop = false)
        {
            List<VfxLayer> vfxLayers = new List<VfxLayer>();
            foreach (SpriteSheet spriteSheetLayer in spriteSheetLayers)
            {
                VfxLayer vfxLayer = new VfxLayer();
                vfxLayer.Initialize(vfxContainer, vfxLayerTemplate,
                    additiveMaterial, center, spriteSheetLayer, loop);
                vfxLayer.ResetSize(laneHeight);
                vfxLayers.Add(vfxLayer);
            }
            return vfxLayers;
        }

        private List<VfxLayer> SpawnVfxAt(VisualElement element,
            List<SpriteSheet> spriteSheetLayers, bool loop = false)
        {
            Vector2 worldPosition = element.worldBound.center;
            Vector2 localPosition = vfxContainer.WorldToLocal(worldPosition);
            return SpawnVfxAt(localPosition, spriteSheetLayers, loop);
        }

        private List<VfxLayer> SpawnVfxAt(NoteElements noteElements,
            List<SpriteSheet> spriteSheetLayers, bool loop = false)
        {
            return SpawnVfxAt(noteElements.templateContainer,
                spriteSheetLayers, loop);
        }

        public void SpawnOngoingVfx(NoteElements noteElements, Judgement judgement)
        {
            if (judgement == Judgement.Miss ||
                judgement == Judgement.Break)
            {
                return;
            }

            switch (noteElements.note.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    // Do nothing. VFX is spawned on resolve.
                    break;
                case NoteType.Hold:
                    holdNoteToOngoingHeadVfx.Add(noteElements,
                        SpawnVfxAt(noteElements,
                            GlobalResource.vfxSkin.holdOngoingHead,
                            loop: true));
                    holdNoteToOngoingTrailVfx.Add(noteElements,
                        SpawnVfxAt(noteElements,
                            GlobalResource.vfxSkin.holdOngoingTrail,
                            loop: true));
                    break;
                case NoteType.Drag:
                    dragNoteToOngoingVfx.Add(noteElements,
                        SpawnVfxAt(noteElements,
                            GlobalResource.vfxSkin.dragOngoing,
                            loop: true));
                    break;
                case NoteType.RepeatHeadHold:
                    holdNoteToOngoingHeadVfx.Add(noteElements,
                        SpawnVfxAt(noteElements,
                            GlobalResource.vfxSkin.repeatHoldOngoingHead,
                            loop: true));
                    holdNoteToOngoingTrailVfx.Add(noteElements,
                        SpawnVfxAt(noteElements,
                            GlobalResource.vfxSkin.repeatHoldOngoingTrail,
                            loop: true));
                    break;
                case NoteType.RepeatHold:
                    // Spawn the head VFX on repeat head.
                    NoteElements head = (noteElements as RepeatNoteElementsBase)
                        .head;
                    holdNoteToOngoingHeadVfx.Add(head,
                        SpawnVfxAt(head,
                            GlobalResource.vfxSkin.repeatHoldOngoingHead,
                            loop: true));
                    holdNoteToOngoingTrailVfx.Add(noteElements,
                        SpawnVfxAt(noteElements,
                            GlobalResource.vfxSkin.repeatHoldOngoingTrail,
                            loop: true));
                    break;
            }
        }

        public void SpawnResolvedVfx(NoteElements noteElements, Judgement judgement)
        {
            // Even if judgement is Miss or Break, we still need
            // to despawn ongoing VFX, if any.

            Action<Dictionary<NoteElements, List<VfxLayer>>, NoteElements> despawnVfx =
                (Dictionary<NoteElements, List<VfxLayer>> dictionary,
                NoteElements elements) =>
                {
                    if (!dictionary.ContainsKey(elements)) return;
                    dictionary[elements].ForEach(l => l.Dispose());
                    dictionary.Remove(elements);
                };

            bool missOrBreak = judgement == Judgement.Miss ||
                judgement == Judgement.Break;
            List<VfxLayer> newLayers = null;

            switch (noteElements.note.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                    switch (judgement)
                    {
                        case Judgement.RainbowMax:
                        case Judgement.Max:
                            newLayers = SpawnVfxAt(noteElements,
                                GlobalResource.vfxSkin.basicMax);
                            break;
                        case Judgement.Cool:
                            newLayers = SpawnVfxAt(noteElements,
                                GlobalResource.vfxSkin.basicCool);
                            break;
                        case Judgement.Good:
                            newLayers = SpawnVfxAt(noteElements,
                                GlobalResource.vfxSkin.basicGood);
                            break;
                    }
                    break;
                case NoteType.Hold:
                    despawnVfx(holdNoteToOngoingHeadVfx, noteElements);
                    despawnVfx(holdNoteToOngoingTrailVfx, noteElements);
                    if (!missOrBreak)
                    {
                        newLayers = SpawnVfxAt(
                            noteElements.holdTrailAndExtensions
                            .GetDurationTrailEndPosition(),
                            GlobalResource.vfxSkin.holdComplete);
                    }
                    break;
                case NoteType.Drag:
                    despawnVfx(dragNoteToOngoingVfx, noteElements);
                    if (!missOrBreak)
                    {
                        newLayers = SpawnVfxAt(
                            (noteElements as DragNoteElements).curveEnd,
                            GlobalResource.vfxSkin.dragComplete);
                    }
                    break;
                case NoteType.RepeatHead:
                    if (missOrBreak) break;
                    newLayers = SpawnVfxAt(noteElements,
                        GlobalResource.vfxSkin.repeatHead);
                    break;
                case NoteType.Repeat:
                    if (missOrBreak) break;
                    newLayers = SpawnVfxAt(noteElements,
                        GlobalResource.vfxSkin.repeatNote);
                    newLayers = SpawnVfxAt((noteElements as RepeatNoteElementsBase).head,
                        GlobalResource.vfxSkin.repeatHead);
                    break;
                case NoteType.RepeatHeadHold:
                    despawnVfx(holdNoteToOngoingHeadVfx, noteElements);
                    despawnVfx(holdNoteToOngoingTrailVfx, noteElements);
                    if (!missOrBreak)
                    {
                        newLayers = SpawnVfxAt(
                            noteElements.holdTrailAndExtensions
                            .GetDurationTrailEndPosition(),
                            GlobalResource.vfxSkin.repeatHoldComplete);
                    }
                    break;
                case NoteType.RepeatHold:
                    // Despawn VFX on repeat head.
                    NoteElements head = (noteElements as RepeatNoteElementsBase)
                        .head;
                    despawnVfx(holdNoteToOngoingHeadVfx, head);
                    despawnVfx(holdNoteToOngoingTrailVfx, noteElements);
                    if (!missOrBreak)
                    {
                        newLayers = SpawnVfxAt(
                            noteElements.holdTrailAndExtensions
                            .GetDurationTrailEndPosition(),
                            GlobalResource.vfxSkin.repeatHoldComplete);
                    }
                    break;
            }
            if (newLayers != null)
            {
                foreach (VfxLayer l in newLayers) oneShotLayers.Add(l);
            }
        }

        public void SpawnOneShotVfx(VisualElement element, Judgement judgement)
        {
            List<VfxLayer> newLayers = null;

            switch (judgement)
            {
                case Judgement.RainbowMax:
                case Judgement.Max:
                    newLayers = SpawnVfxAt(element,
                        GlobalResource.vfxSkin.basicMax);
                    break;
                case Judgement.Cool:
                    newLayers = SpawnVfxAt(element,
                        GlobalResource.vfxSkin.basicCool);
                    break;
                case Judgement.Good:
                    newLayers = SpawnVfxAt(element,
                        GlobalResource.vfxSkin.basicGood);
                    break;
            }
            if (newLayers != null)
            {
                foreach (VfxLayer l in newLayers) oneShotLayers.Add(l);
            }
        }

        public void Update()
        {
            // Move ongoing VFX.
            float worldXOfScanline = layout.GetWorldXOfScanline(timer.intScan);
            foreach (KeyValuePair<NoteElements, List<VfxLayer>> pair in
                holdNoteToOngoingTrailVfx)
            {
                VisualElement ongoingTrailEnd = pair.Key.holdTrailAndExtensions
                    .GetOngoingTrailEndPosition(timer.intScan);
                Vector2 ongoingTrailEndWorldPosition = ongoingTrailEnd.worldBound.center;
                Vector2 vfxWorldPosition = new Vector2(
                    worldXOfScanline, ongoingTrailEndWorldPosition.y);
                Vector2 vfxLocalPosition = vfxContainer.WorldToLocal(vfxWorldPosition);
                foreach (VfxLayer l in pair.Value)
                {
                    l.SetCenter(vfxLocalPosition);
                }
            }
            foreach (KeyValuePair<NoteElements, List<VfxLayer>> pair in
                dragNoteToOngoingVfx)
            {
                Vector2 noteWorldPosition = pair.Key.noteImage.worldBound.center;
                Vector2 vfxLocalPosition = vfxContainer.WorldToLocal(noteWorldPosition);
                foreach (VfxLayer l in pair.Value)
                {
                    l.SetCenter(vfxLocalPosition);
                }
            }

            // Find and collect one-shot layers, if any.
            List<VfxLayer> remainingOneShotLayers = new List<VfxLayer>();
            foreach (VfxLayer l in oneShotLayers)
            {
                if (l.active) remainingOneShotLayers.Add(l);
            }
            if (remainingOneShotLayers.Count < oneShotLayers.Count)
            {
                oneShotLayers = remainingOneShotLayers;
            }
        }

        // The caller should remove all VFX and combo from hierarchy.
        public void Dispose()
        {
            oneShotLayers.Clear();
            holdNoteToOngoingHeadVfx.Clear();
            holdNoteToOngoingTrailVfx.Clear();
            dragNoteToOngoingVfx.Clear();
        }

        public void JumpToScan()
        {
            foreach (VfxLayer l in oneShotLayers)
            {
                l.Dispose();
            }
            oneShotLayers.Clear();
            foreach (List<VfxLayer> list in holdNoteToOngoingHeadVfx.Values)
            {
                foreach (VfxLayer l in list) l.Dispose();
            }
            holdNoteToOngoingHeadVfx.Clear();
            foreach (List<VfxLayer> list in holdNoteToOngoingTrailVfx.Values)
            {
                foreach (VfxLayer l in list) l.Dispose();
            }
            holdNoteToOngoingTrailVfx.Clear();
            foreach (List<VfxLayer> list in dragNoteToOngoingVfx.Values)
            {
                foreach (VfxLayer l in list) l.Dispose();
            }
            dragNoteToOngoingVfx.Clear();
        }
    }

    private TemplateContainer templateInstance;

    private VfxManager vfxManager;
    private ComboText comboText;

    public VfxAndComboText(VisualTreeAsset vfxAndComboTemplate,
        VisualTreeAsset vfxLayerTemplate,
        VisualElement vfxAndComboContainer,
        Material additiveMaterial,
        // To be passed to
        // HoldTrailAndExtensions.GetOngoingTrailEndPosition.
        GameTimer timer,
        // To query scanline position when placing ongoing VFX.
        GameLayout layout)
    {
        templateInstance = vfxAndComboTemplate.Instantiate();
        vfxAndComboContainer.Add(templateInstance);

        comboText = new ComboText(templateInstance, additiveMaterial);
        vfxManager = new VfxManager(templateInstance, vfxLayerTemplate,
            additiveMaterial, timer, layout);
    }

    public void Update()
    {
        vfxManager.Update();
        comboText.Update();
    }

    public void ResetSize(float laneHeight, float scanHeight)
    {
        vfxManager.ResetSize(laneHeight);
        comboText.ResetSize(scanHeight);
    }

    public void Dispose()
    {
        templateInstance.RemoveFromHierarchy();
        vfxManager.Dispose();
    }

    public void ShowComboText(VisualElement noteImage, Judgement judgement,
        ScoreKeeper scoreKeeper)
    {
        comboText.Show(noteImage, judgement, scoreKeeper);
    }

    public void ShowComboText(VisualElement noteImage, Judgement judgement,
        bool fever, int combo)
    {
        comboText.Show(noteImage, judgement, fever, combo);
    }

    public void HideComboText()
    {
        comboText.Hide();
    }

    public void JumpToScan()
    {
        vfxManager.JumpToScan();
    }

    public void SpawnOngoingVfx(NoteElements elements, Judgement judgement)
    {
        vfxManager.SpawnOngoingVfx(elements, judgement);
    }

    public void SpawnResolvedVfx(NoteElements elements, Judgement judgement)
    {
        vfxManager.SpawnResolvedVfx(elements, judgement);
    }

    public void SpawnOneShotVfx(VisualElement element, Judgement judgement)
    {
        vfxManager.SpawnOneShotVfx(element, judgement);
    }
}
