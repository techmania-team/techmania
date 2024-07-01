using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Manages the repeat paths. Used by repeat head, repeat head hold and
// repeat path extensions.
public class RepeatPathElements
{
    private TemplateContainer templateContainer;
    private VisualElement path;
    private VisualElement pathEnd;

    // Acquired on construction

    private Note headNote;
    private int intScan;
    private int bps;
    private GameLayout layout;

    // Calculated

    private int pulsesPerScan => bps * Pattern.pulsesPerBeat;
    private GameLayout.ScanDirection scanDirection;
    private float startRelativeX;
    private float endRelativeX;
    private float relativeWidth;

    public RepeatPathElements(NoteElements noteElements,
        int intScan, int bps, GameLayout layout)
    {
        this.headNote = noteElements.note;
        this.intScan = intScan;
        this.bps = bps;
        this.layout = layout;
    }

    // At time of initialization, at least for the note head,
    // the last managed note is not known yet.
    // For heads, the templateContainer is of the head template;
    // for repeat path extensions, the templateContainer is of the
    // extension template.
    public void Initialize(TemplateContainer templateContainer)
    {
        this.templateContainer = templateContainer;
        templateContainer.AddToClassList("note-anchor");
        templateContainer.pickingMode = PickingMode.Ignore;

        VisualElement pathContainer = templateContainer.Q(
            "path-container");
        path = pathContainer.Q("repeat-path");
        pathEnd = path.Q("repeat-path-end");

        scanDirection = (intScan % 2 == 0) ?
            layout.evenScanDirection : layout.oddScanDirection;
        bool scansToLeft = scanDirection ==
            GameLayout.ScanDirection.Left;
        pathContainer.EnableInClassList(NoteElements.hFlippedClass,
            scansToLeft);

        // Calculate startPulse and endPulse.
        int startPulseOfIntScan = intScan * pulsesPerScan;
        int startPulseOfNote = headNote.pulse;

        int startPulse = Mathf.Max(
            startPulseOfIntScan, startPulseOfNote);
        bool startsBeforeIntScan =
            startPulseOfNote < startPulseOfIntScan;

        // Special case: if head is precisely on the start of this
        // int scan and it's end-of-scan, then it's considered to be
        // in the previous scan.
        if (startPulseOfNote == startPulseOfIntScan &&
            headNote.endOfScan)
        {
            startsBeforeIntScan = true;
        }

        // Calculate startRelativeX. endRelativeX will be calculated
        // in InitializeWithLastManagedNote.
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
    }

    // The second part of initialization when the last managed note
    // is known.
    public void InitializeWithLastManagedNote(
        int pulseOfLastManagedNote, int intScanOfLastManagedNote)
    {
        bool endsPastIntScan =
            intScanOfLastManagedNote > intScan;

        // Calculate relative width. This never changes even with
        // resetting size.
        if (endsPastIntScan)
        {
            endRelativeX = scanDirection switch
            {
                GameLayout.ScanDirection.Left => 0f,
                GameLayout.ScanDirection.Right => 1f,
                _ => 0f
            };
        }
        else
        {
            endRelativeX = layout.RelativeScanToRelativeX(
                (float)pulseOfLastManagedNote / pulsesPerScan - intScan,
                scanDirection);
        }
        relativeWidth = Mathf.Abs(endRelativeX - startRelativeX);
    }

    public void InitializeSize()
    {
        // Path
        path.style.width = layout.gameContainerWidth *
            relativeWidth;
        float pathHeight = layout.laneHeight *
            GlobalResource.noteSkin.repeatPath.scale;
        path.style.height = pathHeight;

        // Path end
        Rect pathEndRect = GlobalResource.noteSkin.repeatPathEnd
            .sprites[0].rect;
        pathEnd.style.width = pathHeight *
            pathEndRect.width / pathEndRect.height;
    }

    // Because this adjusts the template container, it works on both
    // heads and extensions.
    public void PlaceBehindManagedNotes(List<RepeatNoteElementsBase>
        managedNotes)
    {
        TemplateContainer lastElementUnderSameParent = null;
        System.Action<TemplateContainer> checkTemplateContainer =
            (TemplateContainer other) =>
            {
                if (templateContainer.parent == other.parent)
                {
                    lastElementUnderSameParent = other;
                }
            };
        foreach (RepeatNoteElementsBase e in managedNotes)
        {
            checkTemplateContainer(e.templateContainer);
            if (e.holdTrailAndExtensions != null)
            {
                foreach (HoldExtension extension in
                    e.holdTrailAndExtensions.extensions)
                {
                    checkTemplateContainer(
                        extension.trail.templateContainer);
                }
            }
        }

        if (lastElementUnderSameParent != null)
        {
            templateContainer.PlaceBehind(lastElementUnderSameParent);
        }
    }

    public void SetVisibility(NoteElements.Visibility v)
    {
        path.visible = v != NoteElements.Visibility.Hidden;
    }

    public void UpdateSprites(GameTimer timer)
    {
        float beat = timer.beat;
        path.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.repeatPath
            .GetSpriteAtFloatIndex(beat));
        pathEnd.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.repeatPathEnd
            .GetSpriteAtFloatIndex(beat));
    }
}
