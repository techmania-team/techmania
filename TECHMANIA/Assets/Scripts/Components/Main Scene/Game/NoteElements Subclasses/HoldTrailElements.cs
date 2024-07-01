using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Manages the ongoing and duration trails. Used by both
// hold notes and hold extensions.
public class HoldTrailElements
{
    public TemplateContainer templateContainer { get; private set; }
    private VisualElement trailContainer;
    private VisualElement totalTrail;
    private VisualElement ongoingTrail;
    public VisualElement ongoingTrailEndPosition { get; private set; }
    private VisualElement durationTrail;
    private VisualElement durationTrailEnd;
    public VisualElement durationTrailEndPosition { get; private set; }

    // Acquired on construction

    private NoteElements noteElements;
    private Note note;
    // May not be equal to the note's starting scan, in the case
    // of extensions.
    private int intScan;
    private int bps;
    private GameLayout layout;

    // Calculated

    private GameLayout.ScanDirection scanDirection;
    private float startRelativeX;
    private float endRelativeX;
    private float relativeWidth;

    public HoldTrailElements(
        NoteElements noteElements, int intScan, int bps,
        GameLayout layout)
    {
        this.noteElements = noteElements;
        this.note = noteElements.note;
        this.intScan = intScan;
        this.bps = bps;
        this.layout = layout;
    }

    public void Initialize(TemplateContainer templateContainer)
    {
        this.templateContainer = templateContainer;
        templateContainer.AddToClassList("note-anchor");
        templateContainer.pickingMode = PickingMode.Ignore;

        trailContainer = templateContainer.Q("trail-container");
        totalTrail = trailContainer.Q("total-trail");
        ongoingTrail = totalTrail.Q("ongoing");
        ongoingTrailEndPosition = ongoingTrail.Q(
            "ongoing-trail-end-position");
        durationTrail = totalTrail.Q("duration");
        durationTrailEnd = durationTrail.Q("duration-trail-end");
        durationTrailEndPosition = durationTrail.Q(
            "duration-trail-end-position");

        scanDirection = (intScan % 2 == 0) ?
            layout.evenScanDirection : layout.oddScanDirection;
        bool scansToLeft = scanDirection ==
            GameLayout.ScanDirection.Left;
        trailContainer.EnableInClassList(NoteElements.hFlippedClass,
            scansToLeft);

        // Calculate startPulse and endPulse.
        int pulsesPerScan = bps * Pattern.pulsesPerBeat;
        int startPulseOfIntScan = intScan * pulsesPerScan;
        int endPulseOfIntScan = (intScan + 1) * pulsesPerScan;
        int startPulseOfNote = note.pulse;
        int endPulseOfNote = note.pulse +
            (note as HoldNote).duration;

        int startPulse = Mathf.Max(
            startPulseOfIntScan, startPulseOfNote);
        int endPulse = Mathf.Min(
            endPulseOfIntScan, endPulseOfNote);
        bool startsBeforeIntScan =
            startPulseOfNote < startPulseOfIntScan;
        bool endsPastIntScan =
            endPulseOfNote > endPulseOfIntScan;

        // Special case: if note is precisely on the start of this
        // int scan and it's end-of-scan, then it's considered to be
        // in the previous scan.
        if (startPulseOfNote == startPulseOfIntScan &&
            note.endOfScan)
        {
            startsBeforeIntScan = true;
        }

        // Calculate relative width. This never changes even with
        // resetting size.
        if (startsBeforeIntScan)
        {
            startRelativeX = scansToLeft ? 1f : 0f;
        }
        else
        {
            startRelativeX = layout.RelativeScanToRelativeX(
                (float)startPulse / pulsesPerScan - intScan,
                scanDirection);
        }
        if (endsPastIntScan)
        {
            endRelativeX = scansToLeft ? 0f : 1f;
        }
        else
        {
            endRelativeX = layout.RelativeScanToRelativeX(
                (float)endPulse / pulsesPerScan - intScan, 
                scanDirection);
        }
        relativeWidth = Mathf.Abs(endRelativeX - startRelativeX);
    }

    public void InitializeSize()
    {
        // Total trail
        totalTrail.style.width = layout.gameContainerWidth *
            relativeWidth;

        float laneHeight = layout.laneHeight;
        float scale = (note.type == NoteType.Hold) ?
            GlobalResource.noteSkin.holdTrail.scale :
            GlobalResource.noteSkin.repeatHoldTrail.scale;
        float trailHeight = laneHeight * scale;
        totalTrail.style.height = trailHeight;

        // Duration trail end
        Rect durationTrailEndRect = (note.type == NoteType.Hold) ?
            GlobalResource.noteSkin.holdTrailEnd.sprites[0].rect :
            GlobalResource.noteSkin.repeatHoldTrailEnd.sprites[0].rect;
        durationTrailEnd.style.width = trailHeight * 
            durationTrailEndRect.width /
            durationTrailEndRect.height;
    }

    public void ResetToInactive()
    {
        SetOngoingTrailProportion(0f);
    }

    private void SetOngoingTrailProportion(float proportion)
    {
        ongoingTrail.style.width = new StyleLength(
            new Length(proportion * 100f, LengthUnit.Percent));
    }

    public void SetVisibility(NoteElements.Visibility v)
    {
        totalTrail.visible = v != NoteElements.Visibility.Hidden;
        totalTrail.style.opacity = noteElements.VisibilityToAlpha(v);
    }

    // Assumes note is in ongoing state.
    public void UpdateTrails(GameTimer timer)
    {
        float currentTimeRelativeX = layout.RelativeScanToRelativeX(
            timer.scan - intScan, scanDirection);
        float proportion = (currentTimeRelativeX - startRelativeX)
            / (endRelativeX - startRelativeX);
        proportion = Mathf.Clamp01(proportion);
        SetOngoingTrailProportion(proportion);
    }

    public void UpdateSprites(GameTimer timer)
    {
        float beat = timer.beat;

        Sprite durationTrailSprite;
        Sprite durationTrailEndSprite;
        Sprite ongoingTrailSprite = null;
        if (note.type == NoteType.Hold)
        {
            durationTrailSprite = GlobalResource.noteSkin.holdTrail
                .GetSpriteAtFloatIndex(beat);
            durationTrailEndSprite = GlobalResource.noteSkin
                .holdTrailEnd.GetSpriteAtFloatIndex(beat);
            ongoingTrailSprite = GlobalResource.noteSkin
                .holdOngoingTrail.GetSpriteAtFloatIndex(beat);
        }
        else
        {
            durationTrailSprite = GlobalResource.noteSkin
                .repeatHoldTrail.GetSpriteAtFloatIndex(beat);
            durationTrailEndSprite =
                GlobalResource.noteSkin.repeatHoldTrailEnd
                .GetSpriteAtFloatIndex(beat);
        }

        if (ongoingTrailSprite != null)
        {
            ongoingTrail.style.backgroundImage = new StyleBackground(
                ongoingTrailSprite);
        }
        durationTrail.style.backgroundImage = new StyleBackground(
            durationTrailSprite);
        durationTrailEnd.style.backgroundImage = new StyleBackground(
            durationTrailEndSprite);
    }
}
