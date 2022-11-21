using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DragNoteElements : NoteElements
{
    private VisualElement curve;
    // VFXManager uses this to spawn VFX on resolve.
    public VisualElement curveEnd { get; private set; }

    public DragNoteElements(Note n) : base(n) { }

    protected override void TypeSpecificInitialize()
    {
        curve = templateContainer.Q("curve");
        curveEnd = templateContainer.Q("curve-end");
        curve.generateVisualContent = DrawCurve;

        InitializeCurve();
    }

    protected override void TypeSpecificResetToInactive()
    {
        ResetCurve();
    }

    public override void SetOngoing()
    {
        base.SetOngoing();
        ResetHitbox();
    }

    protected override void TypeSpecificUpdateState()
    {
        System.Action<Visibility> setCurveVisibility =
            (Visibility v) =>
            {
                if (curve == null) return;
                curve.visible = v != Visibility.Hidden;
                curve.style.opacity = VisibilityToAlpha(v);
            };

        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                setCurveVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                setCurveVisibility(Visibility.Transparent);
                break;
            case State.Active:
            case State.Ongoing:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                setCurveVisibility(Visibility.Visible);
                break;
        }
    }

    protected override void TypeSpecificUpdate(GameTimer timer)
    {
        if (state == State.Ongoing)
        {
            UpdateOngoingCurve();
            PlaceNoteImageAndHitboxOnCurve();
        }
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.dragHead.scale;
    }

    protected override void TypeSpecificInitializeSizeExceptHitbox()
    {
        gameContainerWidthCopy = layout.gameContainerWidth;
        scanHeightCopy = layout.scanHeight;
        curveWidth = layout.laneHeight *
            GlobalResource.noteSkin.dragCurve.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.dragHead
            .GetSpriteAtFloatIndex(Game.FloatBeat));
        curveTexture = GlobalResource.noteSkin.dragCurve
            .GetSpriteAtFloatIndex(Game.FloatBeat).texture;
    }

    protected override Vector2 GetHitboxScaleFromRuleset()
    {
        if (state == State.Ongoing)
        {
            return new Vector2(
                Ruleset.instance.ongoingDragHitboxWidth,
                Ruleset.instance.ongoingDragHitboxHeight);
        }
        else
        {
            return new Vector2(
                Ruleset.instance.dragHitboxWidth,
                Ruleset.instance.dragHitboxHeight);
        }
    }

    #region Curve
    // All positions relative to note head; units are relative
    // lengths to a scan's width and height.
    private DragPointList pointList;
    // -1 or 1.
    private float curveXDirection;
    private Texture curveTexture;

    private float scanHeightCopy;
    private float gameContainerWidthCopy;
    // lane height * scale
    private float curveWidth;

    private void InitializeCurve()
    {
        DragNote dragNote = note as DragNote;
        List<Vector2> points = new List<Vector2>();

        System.Func<float, float, Vector2> pulseLaneToPoint =
            (float pulse, float lane) =>
            {
                float scan = pulse / Pattern.pulsesPerBeat / bps;
                float relativeX = layout.RelativeScanToRelativeX(
                    scan - intScan, scanDirection);
                float relativeY = layout.LaneToRelativeY(lane,
                    intScan);
                return new Vector2(relativeX, relativeY);
            };
        Vector2 headPosition = pulseLaneToPoint(note.pulse, note.lane);
        foreach (FloatPoint p in dragNote.Interpolate())
        {
            points.Add(pulseLaneToPoint(
                note.pulse + p.pulse,
                note.lane + p.lane) - headPosition);
        }
        pointList = new DragPointList(points);

        // Don't use gameContainerWidthCopy and scanHeightCopy here,
        // as they are not yet set at this stage of initialization.
        curveEnd.style.translate = new StyleTranslate(
            new Translate(
                new Length(points[^1].x * layout.gameContainerWidth),
                new Length(points[^1].y * layout.scanHeight),
                0f));
        curveXDirection = Mathf.Sign(
            points[^1].x - points[0].x);
    }

    private void ResetCurve()
    {
        pointList.Reset();
        curve.MarkDirtyRepaint();

        System.Action<VisualElement> resetPosition =
            (VisualElement e) =>
            {
                e.style.translate = new StyleTranslate(
                    new Translate(
                        new Length(0f), new Length(0f), 0f));
            };
        resetPosition(noteImage);
        resetPosition(feverOverlay);
        resetPosition(approachOverlay);
        resetPosition(hitbox);
        RotateNoteImage();
    }

    private void DrawCurve(MeshGenerationContext context)
    {
        if (pointList == null ||
            pointList.Count < 2) return;

        // Convert relative positions to absolute ones.
        Vector2[] points = new Vector2[pointList.Count];
        for (int i = 0; i < pointList.Count; i++)
        {
            points[i].x = pointList[i].x
                * gameContainerWidthCopy;
            points[i].y = pointList[i].y
                * scanHeightCopy;
        }

        // Calculate number of vertices and indices.
        // Counting the additional point (for curve end)
        // after the curve, each point generates 2 vertices.
        int numVertices = (points.Length + 1) * 2;
        Vertex[] vertices = new Vertex[numVertices];
        // Each group of 2 points (4 vertices) generates 2 triangles,
        // therefore 6 incides.
        int numIndices = points.Length * 6;
        MeshWriteData data = context.Allocate(
            numVertices, numIndices, curveTexture);

        float halfWidth = curveWidth * 0.5f;

        System.Func<float, float, Vector2> projectUv =
            (float u, float v) =>
            {
                return new Vector2(
                    Mathf.Lerp(
                        data.uvRegion.xMin, data.uvRegion.xMax, u),
                    Mathf.Lerp(
                        data.uvRegion.yMin, data.uvRegion.yMax, v));
            };

        // Calculate left vector on each point. Then generate
        // vertices.
        Vector2 forward, left;
        for (int i = 0; i < points.Length; i++)
        {
            forward = Vector2.zero;
            if (i < points.Length - 1)
            {
                forward += (points[i + 1] -
                    points[i]).normalized;
            }
            if (i > 0)
            {
                forward += (points[i] -
                    points[i - 1]).normalized;
            }
            forward.Normalize();
            left = new Vector2(-forward.y, forward.x);

            float u = (float)i / (points.Length - 1);
            u = u * 0.5f;

            vertices[i * 2].position = points[i] + halfWidth * left;
            vertices[i * 2].position.z = Vertex.nearZ;
            vertices[i * 2].uv = projectUv(u, 1f);
            vertices[i * 2].tint = Color.white;

            vertices[i * 2 + 1].position =
                points[i] - halfWidth * left;
            vertices[i * 2 + 1].position.z = Vertex.nearZ;
            vertices[i * 2 + 1].uv = projectUv(u, 0f);
            vertices[i * 2 + 1].tint = Color.white;
        }

        // Add 2 points after the curve.
        forward = (points[^1] -
            points[^2]).normalized;
        left = new Vector2(-forward.y, forward.x);
        vertices[^2].position = points[^1]
            + halfWidth * forward
            + halfWidth * left;
        vertices[^2].position.z = Vertex.nearZ;
        vertices[^2].uv = projectUv(1f, 1f);
        vertices[^2].tint = Color.white;

        vertices[^1].position = points[^1]
            + halfWidth * forward
            - halfWidth * left;
        vertices[^1].position.z = Vertex.nearZ;
        vertices[^1].uv = projectUv(1f, 0f);
        vertices[^1].tint = Color.white;

        data.SetAllVertices(vertices);

        // Triangles.
        for (int i = 0; i < points.Length; i++)
        {
            // #2i: left
            // #2i+1: right
            // #2i+2: next left
            // #2i+3: next right
            data.SetNextIndex((ushort)(2 * i + 1));
            data.SetNextIndex((ushort)(2 * i + 2));
            data.SetNextIndex((ushort)(2 * i + 0));
            data.SetNextIndex((ushort)(2 * i + 3));
            data.SetNextIndex((ushort)(2 * i + 2));
            data.SetNextIndex((ushort)(2 * i + 1));
        }
    }

    private void UpdateOngoingCurve()
    {
        if (pointList.Count < 2)
        {
            return;
        }
        //float scanlineX = scanlineRef
        //    .GetComponent<RectTransform>().anchoredPosition.x -
        //    GetComponent<RectTransform>().anchoredPosition.x;
        // Make sure scanline is before visiblePointsOnCurve[1]; remove
        // points if necessary.
        //while ((scanlineX - visiblePointsOnCurve[1].x)
        //    * curveXDirection >= 0f)
        //{
        //    if (visiblePointsOnCurve.Count < 3) break;
        //    visiblePointsOnCurve.RemoveFirst();
        //}
        // Interpolate visiblePointsOnCurve[0] and
        // visiblePointsOnCurve[1].
        //float t = (scanlineX - visiblePointsOnCurve[0].x) /
        //    (visiblePointsOnCurve[1].x - visiblePointsOnCurve[0].x);
        //visiblePointsOnCurve[0] = Vector2.Lerp(
        //    visiblePointsOnCurve[0],
        //    visiblePointsOnCurve[1], t);
        curve.MarkDirtyRepaint();
    }

    private void PlaceNoteImageAndHitboxOnCurve()
    {
        //RectTransform imageRect = noteImage
        //    .GetComponent<RectTransform>();
        //imageRect.anchoredPosition = visiblePointsOnCurve[0];
        //feverOverlay.GetComponent<RectTransform>()
        //    .anchoredPosition = visiblePointsOnCurve[0];
        //approachOverlay.GetComponent<RectTransform>()
        //     .anchoredPosition = visiblePointsOnCurve[0];
        RotateNoteImage();

        // To calculate the hitbox's position, we need to compensate
        // for latency.
        //float compensatedTime = Game.Time;
        //if (!Game.autoPlay)
        //{
        //    if (InternalGameSetup.patternAfterModifier.patternMetadata.controlScheme
        //        == ControlScheme.Touch)
        //    {
        //        compensatedTime -= Options.instance.touchLatencyMs * 0.001f;
        //    }
        //    else
        //    {
        //        compensatedTime -= Options.instance.
        //            keyboardMouseLatencyMs * 0.001f;
        //    }
        //}
        //float compensatedPulse = InternalGameSetup.patternAfterModifier.TimeToPulse(
        //    compensatedTime);
        //float compensatedScanlineX = scanRef.FloatPulseToXPosition(
        //    compensatedPulse) -
        //    GetComponent<RectTransform>().anchoredPosition.x;

        // Find the first point after the compensated scanline's
        // position.
        //int pointIndexAfterHitbox = -1;
        //for (int i = 0; i < pointsOnCurve.Count; i++)
        //{
        //    if ((pointsOnCurve[i].x - compensatedScanlineX) *
        //        curveXDirection >= 0f)
        //    {
        //        pointIndexAfterHitbox = i;
        //        break;
        //    }
        //}
        //if (pointIndexAfterHitbox < 0)
        //{
            // All points are before the compensated scanline.
        //    pointIndexAfterHitbox = pointsOnCurve.Count - 1;
        //}
        //else if (pointIndexAfterHitbox == 0)
        //{
            // All points are after the compensated scanline.
        //    pointIndexAfterHitbox = 1;
        //}
        // Interpolate pointsOnCurve[pointIndexAfterHitbox - 1]
        // and pointsOnCurve[pointIndexAfterHitbox].
        //Vector2 pointBeforeHitbox =
        //    pointsOnCurve[pointIndexAfterHitbox - 1];
        //Vector2 pointAfterHitbox =
        //    pointsOnCurve[pointIndexAfterHitbox];
        //float t = (compensatedScanlineX - pointBeforeHitbox.x) /
        //    (pointAfterHitbox.x - pointBeforeHitbox.x);
        //hitbox.anchoredPosition = Vector2.Lerp(pointBeforeHitbox,
        //    pointAfterHitbox, t);
    }

    private void RotateNoteImage()
    {
        if (pointList.Count < 2) return;
        RotateElementToward(noteImage,
            pointList[0], pointList[1]);
    }
    #endregion
}
