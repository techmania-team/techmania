using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragNoteAppearance : NoteAppearance,
    IPointsOnCurveProvider
{
    public CurvedImage curve;
    public RectTransform curveEnd;

    public override void SetOngoing()
    {
        base.SetOngoing();
        SetHitboxSize(Ruleset.instance.ongoingDragHitboxWidth,
            Ruleset.instance.ongoingDragHitboxHeight);
    }

    protected void SetCurveVisibility(Visibility v)
    {
        if (curve == null) return;
        curve.gameObject.SetActive(v != Visibility.Hidden);
        curve.color = new Color(1f, 1f, 1f, VisibilityToAlpha(v));
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageVisibility(Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                SetCurveVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetCurveVisibility(Visibility.Transparent);
                break;
            case State.Active:
            case State.Ongoing:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetCurveVisibility(Visibility.Visible);
                break;
        }
    }

    protected override void TypeSpecificUpdate()
    {
        UpdateOngoingCurve();
        PlaceNoteImageAndHitboxOnCurve();
    }

    protected override void GetNoteImageScale(
        out float x, out float y)
    {
        x = GlobalResource.noteSkin.dragHead.scale;
        y = GlobalResource.noteSkin.dragHead.scale;
    }

    protected override void TypeSpecificInitializeScale()
    {
        curve.scale = GlobalResource.noteSkin.dragCurve.scale;
    }

    protected override void UpdateSprites()
    {
        noteImage.sprite = GlobalResource.noteSkin.dragHead
            .GetSpriteAtFloatIndex(Game.FloatBeat);
        curve.sprite = GlobalResource.noteSkin.dragCurve
            .GetSpriteAtFloatIndex(Game.FloatBeat);
    }

    protected override Vector2 GetHitboxSizeFromRuleset()
    {
        return new Vector2(Ruleset.instance.dragHitboxWidth,
            Ruleset.instance.dragHitboxHeight);
    }

    #region Curve
    // All positions relative to note head.
    private ListView<Vector2> visiblePointsOnCurve;
    private List<Vector2> pointsOnCurve;
    private float curveXDirection;

    public IList<Vector2> GetVisiblePointsOnCurve()
    {
        return visiblePointsOnCurve;
    }

    private void InitializeCurve()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;
        visiblePointsOnCurve = new ListView<Vector2>();
        pointsOnCurve = new List<Vector2>();

        Vector2 headPosition = GetComponent<RectTransform>()
            .anchoredPosition;
        foreach (FloatPoint p in dragNote.Interpolate())
        {
            Vector2 pointOnCurve = new Vector2(
                scanRef.FloatPulseToXPosition(
                    dragNote.pulse + p.pulse)
                - headPosition.x,
                scanRef.FloatLaneToYPosition(
                    dragNote.lane + p.lane)
                - headPosition.y);
            visiblePointsOnCurve.Add(pointOnCurve);
            pointsOnCurve.Add(pointOnCurve);
        }

        curveEnd.anchoredPosition =
            visiblePointsOnCurve[visiblePointsOnCurve.Count - 1];
        curveXDirection = Mathf.Sign(
            visiblePointsOnCurve[visiblePointsOnCurve.Count - 1].x
            - visiblePointsOnCurve[0].x);
        curve.SetVerticesDirty();

        noteImage.rectTransform.anchoredPosition = Vector2.zero;
        feverOverlay.GetComponent<RectTransform>().anchoredPosition
            = Vector2.zero;
        approachOverlay.GetComponent<RectTransform>().anchoredPosition
            = Vector2.zero;
        hitbox.anchoredPosition = Vector2.zero;
        UIUtils.RotateToward(noteImage.rectTransform,
                selfPos: pointsOnCurve[0],
                targetPos: pointsOnCurve[1]);
    }

    protected override void TypeSpecificInitialize()
    {
        InitializeCurve();
    }

    public void UpdateOngoingCurve()
    {
        if (visiblePointsOnCurve.Count < 2)
        {
            return;
        }
        float scanlineX = scanlineRef
            .GetComponent<RectTransform>().anchoredPosition.x -
            GetComponent<RectTransform>().anchoredPosition.x;
        // Make sure scanline is before pointsOnCurve[1]; remove
        // points if necessary.
        while ((scanlineX - visiblePointsOnCurve[1].x)
            * curveXDirection >= 0f)
        {
            if (visiblePointsOnCurve.Count < 3) break;
            visiblePointsOnCurve.RemoveFirst();
        }
        // Interpolate visiblePointsOnCurve[0] and
        // visiblePointsOnCurve[1].
        float t = (scanlineX - visiblePointsOnCurve[0].x) /
            (visiblePointsOnCurve[1].x - visiblePointsOnCurve[0].x);
        visiblePointsOnCurve[0] = Vector2.Lerp(
            visiblePointsOnCurve[0],
            visiblePointsOnCurve[1], t);
        curve.SetVerticesDirty();
    }

    public void PlaceNoteImageAndHitboxOnCurve()
    {
        RectTransform imageRect = noteImage
            .GetComponent<RectTransform>();
        imageRect.anchoredPosition = visiblePointsOnCurve[0];
        feverOverlay.GetComponent<RectTransform>()
            .anchoredPosition = visiblePointsOnCurve[0]; 
        approachOverlay.GetComponent<RectTransform>()
             .anchoredPosition = visiblePointsOnCurve[0];
        if (visiblePointsOnCurve.Count > 1)
        {
            UIUtils.RotateToward(imageRect,
                selfPos: visiblePointsOnCurve[0],
                targetPos: visiblePointsOnCurve[1]);
        }

        // To calculate the hitbox's position, we need to compensate
        // for latency.
        float compensatedTime = Game.Time;
        if (!Game.autoPlay)
        {
            if (InternalGameSetup.patternAfterModifier.patternMetadata.controlScheme
                == ControlScheme.Touch)
            {
                compensatedTime -= Options.instance.touchLatencyMs * 0.001f;
            }
            else
            {
                compensatedTime -= Options.instance.
                    keyboardMouseLatencyMs * 0.001f;
            }
        }
        float compensatedPulse = InternalGameSetup.patternAfterModifier.TimeToPulse(
            compensatedTime);
        float compensatedScanlineX = scanRef.FloatPulseToXPosition(
            compensatedPulse) -
            GetComponent<RectTransform>().anchoredPosition.x;

        // Find the first point after the compensated scanline's
        // position.
        int pointIndexAfterHitbox = -1;
        for (int i = 0; i < pointsOnCurve.Count; i++)
        {
            if ((pointsOnCurve[i].x - compensatedScanlineX) *
                curveXDirection >= 0f)
            {
                pointIndexAfterHitbox = i;
                break;
            }
        }
        if (pointIndexAfterHitbox < 0)
        {
            // All points are before the compensated scanline.
            pointIndexAfterHitbox = pointsOnCurve.Count - 1;
        }
        else if (pointIndexAfterHitbox == 0)
        {
            // All points are after the compensated scanline.
            pointIndexAfterHitbox = 1;
        }
        // Interpolate pointsOnCurve[pointIndexAfterHitbox - 1]
        // and pointsOnCurve[pointIndexAfterHitbox].
        Vector2 pointBeforeHitbox =
            pointsOnCurve[pointIndexAfterHitbox - 1];
        Vector2 pointAfterHitbox =
            pointsOnCurve[pointIndexAfterHitbox];
        float t = (compensatedScanlineX - pointBeforeHitbox.x) /
            (pointAfterHitbox.x - pointBeforeHitbox.x);
        hitbox.anchoredPosition = Vector2.Lerp(pointBeforeHitbox,
            pointAfterHitbox, t);
    }

    public Vector3 GetCurveEndPosition()
    {
        return curveEnd.position;
    }
    #endregion
}
