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
            UpdateOngoingCurve(timer.scan);
            PlaceNoteImageAndHitboxOnCurve(timer.gameTime);
        }
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.dragHead.scale;
    }

    protected override void TypeSpecificInitializeSizeExceptHitbox()
    {
        noteImage.EnableInClassList(hFlippedClass,
            scanDirection == GameLayout.ScanDirection.Left &&
            GlobalResource.noteSkin.dragHead.flipWhenScanningLeft);

        gameContainerWidthCopy = layout.gameContainerWidth;
        scanHeightCopy = layout.scanHeight;
        curveWidth = layout.laneHeight *
            GlobalResource.noteSkin.dragCurve.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.dragHead
            .GetSpriteAtFloatIndex(timer.beat));
        curveSprite = GlobalResource.noteSkin.dragCurve
            .GetSpriteAtFloatIndex(timer.beat);
        curve.MarkDirtyRepaint();
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
    // Top and Left of the note in layout.
    private Vector2 headPosition;
    private Sprite curveSprite;

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
                float scan = pulse / Pattern.pulsesPerBeat /
                    pattern.patternMetadata.bps;
                float relativeX = layout.RelativeScanToRelativeX(
                    scan - intScan, scanDirection);
                float relativeY = layout.LaneToRelativeY(lane,
                    intScan);
                return new Vector2(relativeX, relativeY);
            };
        headPosition = pulseLaneToPoint(note.pulse, note.lane);
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
        if (curveSprite == null) return;
        bool hFlipped = scanDirection == GameLayout.ScanDirection.Left
            && GlobalResource.noteSkin.dragCurve.flipWhenScanningLeft;

        // Convert relative positions to absolute ones.
        Vector2[] points = new Vector2[pointList.Count];
        for (int i = 0; i < pointList.Count; i++)
        {
            points[i].x = pointList[i].x
                * gameContainerWidthCopy;
            points[i].y = pointList[i].y
                * scanHeightCopy;
        }

        // Calculate the curve sprite's normalized rect
        // inside its texture.
        Rect spriteRectInTexture = new Rect(
            curveSprite.rect.xMin / curveSprite.texture.width,
            curveSprite.rect.yMin / curveSprite.texture.height,
            curveSprite.rect.width / curveSprite.texture.width,
            curveSprite.rect.height / curveSprite.texture.height);

        // Calculate number of vertices and indices.
        // Counting the additional point (for curve end)
        // after the curve, each point generates 2 vertices.
        int numVertices = (points.Length + 1) * 2;
        Vertex[] vertices = new Vertex[numVertices];
        // Each group of 2 points (4 vertices) generates 2 triangles,
        // therefore 6 incides.
        int numIndices = points.Length * 6;
        MeshWriteData data = context.Allocate(
            numVertices, numIndices, curveSprite.texture);

        float halfWidth = curveWidth * 0.5f;

        System.Func<float, float, Vector2> projectUv =
            (float u, float v) =>
            {
                if (hFlipped) v = 1f - v;

                // First project from sprite to texture.
                float uInTexture = Mathf.Lerp(
                    spriteRectInTexture.xMin,
                    spriteRectInTexture.xMax, u);
                float vInTexture = Mathf.Lerp(
                    spriteRectInTexture.yMin,
                    spriteRectInTexture.yMax, v);
                return new Vector2(
                    Mathf.Lerp(
                        data.uvRegion.xMin, data.uvRegion.xMax,
                        uInTexture),
                    Mathf.Lerp(
                        data.uvRegion.yMin, data.uvRegion.yMax,
                        vInTexture));
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

    private void UpdateOngoingCurve(float floatScan)
    {
        if (pointList.Count < 2)
        {
            return;
        }
        float relativeX =
            layout.RelativeScanToRelativeX(floatScan - intScan,
            scanDirection);
        pointList.SetStart(relativeX - headPosition.x);
        curve.MarkDirtyRepaint();
    }

    private void PlaceNoteImageAndHitboxOnCurve(float gameTime)
    {
        if (pointList.Count < 1) return;

        System.Action<VisualElement, Vector2> setTranslate =
            (VisualElement e, Vector2 translate) =>
            {
                e.style.translate = new StyleTranslate(
                    new Translate(
                        new Length(translate.x *
                            gameContainerWidthCopy),
                        new Length(translate.y *
                            scanHeightCopy),
                        0f));
            };
        setTranslate(noteImage, pointList[0]);
        setTranslate(feverOverlay, pointList[0]);
        setTranslate(approachOverlay, pointList[0]);
        RotateNoteImage();

        // To calculate the hitbox's position, we need to compensate
        // for latency.
        float compensatedTime = gameTime;
        if (!GameController.instance.autoPlay)
        {
            if (pattern.patternMetadata.controlScheme
                == ControlScheme.Touch)
            {
                compensatedTime -= Options.instance
                    .touchLatencyMs * 0.001f;
            }
            else
            {
                compensatedTime -= Options.instance
                    .keyboardMouseLatencyMs * 0.001f;
            }
        }
        float compensatedScan = pattern.TimeToPulse(
            compensatedTime) / Pattern.pulsesPerBeat /
            pattern.patternMetadata.bps;
        float hitboxX = layout.RelativeScanToRelativeX(
            compensatedScan - intScan,
            scanDirection) - headPosition.x;
        float hitboxY = pointList.InterpolateForY(hitboxX);
        setTranslate(hitbox, new Vector2(hitboxX, hitboxY));
    }

    private void RotateNoteImage()
    {
        if (pointList.Count < 2) return;
        RotateElementToward(noteImage,
            pointList[0], pointList[1]);
    }
    #endregion
}
