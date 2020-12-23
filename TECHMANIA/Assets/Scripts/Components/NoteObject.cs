using UnityEngine;

public class NoteObject : MonoBehaviour
{
    [HideInInspector]
    // This usually is a reference to a note in a pattern, but
    // in the editor, the cursor contains a made-up Note to help
    // with repositioning.
    public Note note;
}
