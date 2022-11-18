using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DragNoteElements : NoteElements
{
    private VisualElement curve;
    private float curveWidth;  // lane height * scale
    private Texture curveTexture;

    public DragNoteElements(Note n) : base(n) { }

    protected override void TypeSpecificInitialize()
    {
        curve = templateContainer.Q("curve");
        curve.generateVisualContent = DrawCurve;
    }

    protected override void TypeSpecificReset()
    {
        InitializeCurve();
    }

    public override void SetOngoing()
    {
        base.SetOngoing();
        SetHitboxScale(new Vector2(
            Ruleset.instance.ongoingDragHitboxWidth,
            Ruleset.instance.ongoingDragHitboxHeight));
    }

    private void SetCurveVisibility(Visibility v)
    {
        if (curve == null) return;
        curve.visible = v != Visibility.Hidden;
        curve.style.opacity = VisibilityToAlpha(v);
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                SetCurveVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetCurveVisibility(Visibility.Transparent);
                break;
            case State.Active:
            case State.Ongoing:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetCurveVisibility(Visibility.Visible);
                break;
        }
    }

    protected override void TypeSpecificUpdate(GameTimer timer)
    {
        // UpdateOngoingCurve();
        // PlaceNoteImageAndHitboxOnCurve();
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.dragHead.scale;
    }

    protected override void TypeSpecificInitializeSize()
    {
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
        return new Vector2(Ruleset.instance.dragHitboxWidth,
            Ruleset.instance.dragHitboxHeight);
    }

    #region Curve
    // All positions relative to note head. This "list"
    // (actually a view) updates while the note is ongoing, and
    // gets converted to the curve's mesh.
    private ListView<Vector2> visiblePointsOnCurve;
    // All positions relative to note head. This list is generated
    // on initialization and never changes. It's used to place
    // hitbox while the note is ongoing.
    private List<Vector2> pointsOnCurve;
    // -1 or 1.
    private float curveXDirection;

    private void InitializeCurve()
    {
        DragNote dragNote = note as DragNote;
        visiblePointsOnCurve = new ListView<Vector2>();
        pointsOnCurve = new List<Vector2>();

        //Vector2 headPosition = GetComponent<RectTransform>()
        //    .anchoredPosition;
        //foreach (FloatPoint p in dragNote.Interpolate())
        //{
        //    Vector2 pointOnCurve = new Vector2(
        //        scanRef.FloatPulseToXPosition(
        //            dragNote.pulse + p.pulse)
        //        - headPosition.x,
        //        scanRef.FloatLaneToYPosition(
        //            dragNote.lane + p.lane)
        //        - headPosition.y);
        //    visiblePointsOnCurve.Add(pointOnCurve);
        //    pointsOnCurve.Add(pointOnCurve);
        //}

        //curveEnd.anchoredPosition =
        //    visiblePointsOnCurve[visiblePointsOnCurve.Count - 1];
        //curveXDirection = Mathf.Sign(
        //    visiblePointsOnCurve[visiblePointsOnCurve.Count - 1].x
        //    - visiblePointsOnCurve[0].x);
        //curve.SetVerticesDirty();

        //noteImage.rectTransform.anchoredPosition = Vector2.zero;
        //feverOverlay.GetComponent<RectTransform>().anchoredPosition
        //    = Vector2.zero;
        //approachOverlay.GetComponent<RectTransform>().anchoredPosition
        //    = Vector2.zero;
        //hitbox.anchoredPosition = Vector2.zero;
        //UIUtils.RotateToward(noteImage.rectTransform,
        //        selfPos: pointsOnCurve[0],
        //        targetPos: pointsOnCurve[1]);
    }

    private void DrawCurve(MeshGenerationContext context)
    {
        MeshWriteData data = context.Allocate(3, 6, curveTexture);
        // The texture passed in may be integrated with a larger
        // atlas. When writing uv in vertices, make sure to scale
        // them inside data.uvRegion.
        Vertex v;
        v.tint = new Color32();
    }
    #endregion
}
