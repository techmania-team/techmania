using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class StyleHelper
    {
        public StyleLength LengthInPixel(float p) =>
            new StyleLength(new Length(p, LengthUnit.Pixel));
        public StyleLength LengthInPercent(float p) =>
            new StyleLength(new Length(p, LengthUnit.Percent));
        public StyleTranslate TranslateInPixel(float x, float y) =>
            new StyleTranslate(new Translate(
                new Length(x, LengthUnit.Pixel),
                new Length(y, LengthUnit.Pixel),
                z: 0f));

        public StyleFloat Float(float f) => new StyleFloat(f);
    }
}