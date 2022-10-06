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
        public static StyleLength LengthInPixel(float p) =>
            new StyleLength(new Length(p, LengthUnit.Pixel));
        public static StyleLength LengthInPercent(float p) =>
            new StyleLength(new Length(p, LengthUnit.Percent));
        public static StyleTranslate TranslateInPixel(
            float x, float y) =>
            new StyleTranslate(new Translate(
                new Length(x, LengthUnit.Pixel),
                new Length(y, LengthUnit.Pixel),
                z: 0f));
        public static StyleRotate Rotate(float degree) =>
            new StyleRotate(new Rotate(Angle.Degrees(degree)));
        public static StyleScale Scale(float x, float y) =>
            new StyleScale(new Scale(new Vector3(x, y, 1f)));

        public static StyleFloat Float(float f) => new StyleFloat(f);
    }
}