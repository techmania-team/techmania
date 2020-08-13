using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum Judgement
{
    RainbowMax,
    Max,
    Cool,
    Good,
    Miss,
    Break
}

public class NoteObject : MonoBehaviour
{
    [HideInInspector]
    public Note note;
    [HideInInspector]
    public string sound;

    public static event UnityAction<NoteObject> NoteBreak;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Game.Time > note.time + Game.kBreakThreshold)
        {
            NoteBreak?.Invoke(this);
        }
    }
}
