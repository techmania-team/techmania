using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;

namespace ThemeApi
{
    // Wraps around Painter2D, used in custom mesh painting.
    [MoonSharpUserData]
    public class PainterWrap
    {
        public Painter2D inner { get; private set; }

        [MoonSharpHidden]
        public PainterWrap(Painter2D p)
        {
            inner = p;
        }

        #region Colors and width
        // Colors in [0, 1]
        public void SetStrokeColor(float r, float g, float b,
            float a = 1f)
        {
            inner.strokeColor = new Color(r, g, b, a);
        }

        // Colors in [0, 1]
        public void SetFillColor(float r, float g, float b,
            float a = 1f)
        {
            inner.fillColor = new Color(r, g, b, a);
        }

        public void SetLineWidth(float width)
        {
            inner.lineWidth = width;
        }
        #endregion

        #region Caps and joins
        // "Butt" or "Round"
        public void SetLineCap(string lineCap)
        {
            inner.lineCap = System.Enum.Parse<LineCap>(lineCap);
        }

        // "Miter", "Bevel" or "Round"
        public void SetLineJoin(string lineJoin)
        {
            inner.lineJoin = System.Enum.Parse<LineJoin>(lineJoin);
        }

        public void SetMiterLimit(float limit)
        {
            inner.miterLimit = limit;
        }
        #endregion

        #region Operations and lines
        public void BeginPath()
        {
            inner.BeginPath();
        }

        public void ClosePath()
        {
            inner.ClosePath();
        }

        public void MoveTo(float x, float y)
        {
            inner.MoveTo(new Vector2(x, y));
        }
        public void LineTo(float x, float y)
        {
            inner.LineTo(new Vector2(x, y));
        }

        public void Stroke()
        {
            inner.Stroke();
        }

        // "NonZero" or "OddEven".
        public void Fill(string fillRule = "NonZero")
        {
            inner.Fill(System.Enum.Parse<FillRule>(fillRule));
        }
        #endregion

        #region Arcs and curves
        // Direction is "Clockwise" or "CounterClockwise".
        public void Arc(float centerX, float centerY, float radius,
            float startAngleDegrees, float endAngleDegrees,
            string direction)
        {
            inner.Arc(new Vector2(centerX, centerY),
                radius,
                new Angle(startAngleDegrees, AngleUnit.Degree),
                new Angle(endAngleDegrees, AngleUnit.Degree),
                System.Enum.Parse<ArcDirection>(direction));
        }

        public void ArcTo(float controlX, float controlY,
            float pX, float pY,
            float radius)
        {
            inner.ArcTo(new Vector2(controlX, controlY),
                new Vector2(pX, pY),
                radius);
        }

        public void QuadraticCurveTo(float controlX, float controlY,
            float pX, float pY)
        {
            inner.QuadraticCurveTo(
                new Vector2(controlX, controlY),
                new Vector2(pX, pY));
        }

        public void BezierCurveTo(float control1X, float control1Y,
            float control2X, float control2Y,
            float pX, float pY)
        {
            inner.BezierCurveTo(
                new Vector2(control1X, control1Y),
                new Vector2(control2X, control2Y),
                new Vector2(pX, pY));
        }

        #endregion
    }
}