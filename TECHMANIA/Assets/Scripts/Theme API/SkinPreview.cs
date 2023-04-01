using MoonSharp.Interpreter;
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
    public class SkinPreview : MonoBehaviour
    {
        [MoonSharpHidden]
        public static SkinPreview instance;

        [MoonSharpHidden]
        public VisualTreeAsset skinPreviewTemplate;
        [MoonSharpHidden]
        public VFXManager vfxManager;
        [MoonSharpHidden]
        public ComboText comboText;

        [HideInInspector]
        public VisualElementWrap previewContainer;
        [HideInInspector]
        public float bpm;
        [HideInInspector]
        public int lanes;
        [HideInInspector]
        public Judgement judgement;
        [HideInInspector]
        public int combo;
        [HideInInspector]
        public bool fever;

        private bool running;
        private float prevFrameBeat;
        private float scanHeight => 
            previewContainer.resolvedStyle.height;
        private float laneHeight => scanHeight / lanes;
        private TemplateContainer previewBg;
        private VisualElement scanlineAnchor;
        private VisualElement scanline;
        private VisualElement noteAnchor;
        private VisualElement noteImage;
        private Stopwatch stopwatch;

        private void Start()
        {
            running = false;
            instance = this;
        }

        // Render a skin preview in the specified previewContainer.
        // The scanline will move from left to right, scanning over
        // a note in the middle in the process, and resolving it
        // with the specified judgement and combo count.
        //
        // One scan consists of 4 beats, at the specified bpm. The
        // preview area will be seen as the specified number of
        // lanes, with the single note in the 2nd lane from the top.
        //
        // If the user changes skins, Conclude() and Begin() to
        // restart the preview with the newly selected skins.
        public void Begin()
        {
            previewBg = skinPreviewTemplate.Instantiate();
            previewBg.style.flexGrow = new StyleFloat(1f);
            previewContainer.inner.Add(previewBg);

            scanlineAnchor = previewBg.Q("scanline-anchor");
            scanline = scanlineAnchor.Q("scanline");
            noteAnchor = previewBg.Q("note-anchor");
            noteImage = previewBg.Q("note-image");
            ResetSize();

            stopwatch = new Stopwatch();
            stopwatch.Start();
            prevFrameBeat = 0f;

            running = true;
        }

        public void ResetSize()
        {
            Rect spriteSize = GlobalResource.gameUiSkin
                .scanline.sprites[0].rect;
            scanline.style.width = scanHeight
                * spriteSize.width / spriteSize.height;

            float noteAnchorTop = 1.5f / lanes;
            noteAnchor.style.top = new StyleLength(new Length(
                noteAnchorTop * 100f, LengthUnit.Percent));

            float noteScale = GlobalResource.noteSkin.basic.scale;
            noteImage.style.width = laneHeight * noteScale;
            noteImage.style.height = laneHeight * noteScale;

            vfxManager.ResetSize(laneHeight);
            comboText.ResetSize();
        }

        private void Update()
        {
            if (!running) return;

            // Calculate time
            const int bps = 4;
            float beat = (float)stopwatch.Elapsed.TotalMinutes * bpm;
            beat = beat % bps;
            float scan = beat / bps;
            
            // Move scanline
            scanlineAnchor.style.left = new StyleLength(new Length(
                scan * 100f, LengthUnit.Percent));

            // Animate scanline and note
            scanline.style.backgroundImage = new StyleBackground(
                GlobalResource.gameUiSkin.scanline
                .GetSpriteAtFloatIndex(scan));
            noteImage.visible = beat < bps / 2;
            noteImage.style.backgroundImage = new StyleBackground(
                GlobalResource.noteSkin.basic.GetSpriteAtFloatIndex(
                    beat % 1f));

            // Spawn VFX and combo on beat 2
            if (beat >= bps / 2 && prevFrameBeat < bps / 2)
            {
                vfxManager.SpawnOneShotVFX(noteAnchor, judgement);
                comboText.Show(noteImage, judgement, fever, combo);
            }
            prevFrameBeat = beat;
        }

        public void Conclude()
        {
            previewBg.RemoveFromHierarchy();
            stopwatch.Stop();
            vfxManager.Dispose();
            comboText.Hide();

            running = false;
        }
    }
}
