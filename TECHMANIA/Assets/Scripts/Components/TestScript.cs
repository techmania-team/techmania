using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DragNote dragNote = new DragNote();
        dragNote.nodes.Add(new DragNode()
        {
            anchor = new IntPoint(0, 0),
            controlLeft = new FloatPoint(0f, 0f),
            controlRight = new FloatPoint(480f, 0f)
        });
        dragNote.nodes.Add(new DragNode()
        {
            anchor = new IntPoint(960, 2),
            controlLeft = new FloatPoint(-480f, 0f),
            controlRight = new FloatPoint(480f, 0f)
        });
        dragNote.nodes.Add(new DragNode()
        {
            anchor = new IntPoint(1920, 0),
            controlLeft = new FloatPoint(-480f, 0f),
            controlRight = new FloatPoint(0f, 0f)
        });

        GetComponent<NoteObject>().note = dragNote;
    }
}
