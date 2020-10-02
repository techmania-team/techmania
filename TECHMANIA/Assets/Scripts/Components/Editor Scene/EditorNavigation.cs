using UnityEngine;

// TODO: deprecate this.
public class EditorNavigation : MonoBehaviour
{
    public static string GetCurrentTrackPath()
    {
        return "";
    }

    public static Pattern GetCurrentPattern()
    {
        return null;
    }

    // Call this before making any change to currentTrack.
    public static void PrepareForChange()
    {

    }

    // Call this after making any change to currentTrack.
    public static void DoneWithChange()
    {

    }
}
