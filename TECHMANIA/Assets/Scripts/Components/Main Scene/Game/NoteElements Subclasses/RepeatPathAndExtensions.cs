using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using State = NoteElements.State;
using Visibility = NoteElements.Visibility;

// Common behavior between repeat head and repeat head hold notes.
// This manages the repeat path on the note itself, as well as
// all extensions.
public class RepeatPathAndExtensions
{
    private RepeatPathElements path;
    private List<RepeatPathExtension> extensions;

    public RepeatPathAndExtensions(NoteElements noteElements,
        int intScan, int bps, GameLayout layout)
    {
        path = new RepeatPathElements(noteElements, intScan,
            bps, layout);
        extensions = new List<RepeatPathExtension>();
    }

    public void Initialize(TemplateContainer templateContainer)
    {
        path.Initialize(templateContainer);
        // At this point extensions are not spawned yet. They will
        // be initialized when spawned.
    }

    public void InitializeWithLastManagedNote(
        int pulseOfLastManagedNote, int intScanOfLastManagedNote)
    {
        path.InitializeWithLastManagedNote(
            pulseOfLastManagedNote, intScanOfLastManagedNote);
        extensions.ForEach(e =>
            e.path.InitializeWithLastManagedNote(
                pulseOfLastManagedNote, intScanOfLastManagedNote));
    }

    public void PlaceAllPathsBehindManagedNotes(
        List<RepeatNoteElementsBase> managedNotes)
    {
        path.PlaceBehindManagedNotes(managedNotes);
        extensions.ForEach(e =>
            e.path.PlaceBehindManagedNotes(managedNotes));
    }

    public void InitializeSize()
    {
        path.InitializeSize();
        extensions.ForEach(e => e.path.InitializeSize());
    }

    public void RegisterPathExtension(RepeatPathExtension e)
    {
        extensions.Add(e);
    }

    public void UpdateState(State state)
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetPathVisibility(Visibility.Hidden);
                SetPathExtensionVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
            case State.Active:
            case State.PendingResolve:
                SetPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Ongoing:
                // Only applies to repeat head hold.
                SetPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
        }
    }

    private void SetPathVisibility(Visibility v)
    {
        path.SetVisibility(v);
    }

    private void SetPathExtensionVisibility(Visibility v)
    {
        extensions.ForEach(e => e.path.SetVisibility(v));
    }

    public void UpdateSprites(GameTimer timer)
    {
        path.UpdateSprites(timer);
        extensions.ForEach(e => e.path.UpdateSprites(timer));
    }
}
