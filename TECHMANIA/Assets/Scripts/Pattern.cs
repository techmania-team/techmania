using System.Collections;
using System.Collections.Generic;

public partial class Pattern
{
    // The "main" part is defined in Track.cs. This part contains
    // editing routines and timing calculations.
    #region Editing
    public bool HasNoteAt(int pulse, int lane)
    {
        return GetNoteAt(pulse, lane) != null;
    }

    public Note GetNoteAt(int pulse, int lane)
    {
        Note note = new Note() { pulse = pulse, lane = lane };
        SortedSet<Note> view = notes.GetViewBetween(
            note, note);
        if (view.Count > 0) return view.Min;
        return null;
    }
    #endregion

    #region Timing
    #endregion
}
