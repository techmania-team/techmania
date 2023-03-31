using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class SkinPreview
    {
        public VisualElementWrap previewContainer;
        public float bpm;
        public int lanes;
        public Judgement judgement;
        public int combo;

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

        }

        public void Conclude()
        {

        }
    }
}
