using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Manages the repeat paths. Used by repeat head, repeat head hold and
// repeat path extensions.
public class RepeatPathElements
{
    private VisualElement path;
    private VisualElement pathEnd;

    // Acquired on construction

    private Note note;
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
        this.note = noteElements.note;
        this.intScan = intScan;
        this.bps = bps;
        this.layout = layout;
    }

    // At time of initialization, at least for the note head,
    // the last managed note is not known yet.
    public void Initialize(TemplateContainer templateContainer)
    {
        templateContainer.AddToClassList("note-anchor");
        templateContainer.pickingMode = PickingMode.Ignore;

        VisualElement pathContainer = templateContainer.Q(
            "path-container");
        path = pathContainer.Q("repeat-path");
        pathEnd = path.Q("path-end");

        scanDirection = (intScan % 2 == 0) ?
            layout.evenScanDirection : layout.oddScanDirection;
        bool scansToLeft = scanDirection ==
            GameLayout.ScanDirection.Left;
        pathContainer.EnableInClassList("h-flipped", scansToLeft);

        // Calculate startPulse and endPulse.
        int startPulseOfIntScan = intScan * pulsesPerScan;
        int startPulseOfNote = note.pulse;

        int startPulse = Mathf.Max(
            startPulseOfIntScan, startPulseOfNote);
        bool startsBeforeIntScan =
            startPulseOfNote < startPulseOfIntScan;

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
        NoteElements lastManagedNote)
    {
        int endPulseOfIntScan = (intScan + 1) * pulsesPerScan;
        int pulseOfLastManagedNote = lastManagedNote.note.pulse;
        if (lastManagedNote.note.type == NoteType.RepeatHold)
        {
            pulseOfLastManagedNote +=
                (lastManagedNote.note as HoldNote).pulse;
        }

        int endPulse = Mathf.Min(
            endPulseOfIntScan, pulseOfLastManagedNote);
        bool endsPastIntScan =
            pulseOfLastManagedNote > endPulseOfIntScan;

        // A special case: if the last managed note is a repeat note
        // (no hold), is on a scan boundary, and it's not end-of-scan,
        // then this path should extend past the scan.
        if (lastManagedNote.note.type == NoteType.Repeat &&
            lastManagedNote.note.pulse == endPulseOfIntScan &&
            !lastManagedNote.note.endOfScan)
        {
            endsPastIntScan = true;
        }

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
                (float)endPulse / pulsesPerScan - intScan,
                scanDirection);
        }
        relativeWidth = Mathf.Abs(endRelativeX - startRelativeX);

        // Reset size now that relativeWidth is known.
        InitializeSize();
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

    public void SetVisibility(NoteElements.Visibility v)
    {
        path.visible = v != NoteElements.Visibility.Hidden;
    }

    public void UpdateSprites(GameTimer timer)
    {
        float beat = timer.Beat;
        path.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.repeatPath
            .GetSpriteAtFloatIndex(beat));
        pathEnd.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.repeatPathEnd
            .GetSpriteAtFloatIndex(beat));
    }
}
