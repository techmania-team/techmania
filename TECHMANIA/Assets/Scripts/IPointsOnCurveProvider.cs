using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// There was a time when the game and editor generated points on
// drag notes from different components, but rendered them with the
// same. This interface allows the renderer (CurvedImage) to be
// decoupled from how the points are generated.
//
// Since then, the game part moved to UI Toolkit (DragNoteElements),
// and thus a different renderer. CurvedImage now only serves the
// editor, so there isn't really a need for this interface anymore.
//
// But I'm too lazy to remove it.
public interface IPointsOnCurveProvider
{
    IList<Vector2> GetVisiblePointsOnCurve();
}
