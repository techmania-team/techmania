using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RepeatHoldElements : RepeatNoteElementsBase
{
    public RepeatHoldElements(Note n) : base(n) { }

    protected override void TypeSpecificInitializeSizeExceptHitbox()
    {
        noteImage.EnableInClassList(hFlippedClass,
            scanDirection == GameLayout.ScanDirection.Left &&
            GlobalResource.noteSkin.repeat.flipWhenScanningLeft);
    }
}
