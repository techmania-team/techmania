using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpriteSheet
{
    public string filename;
    public int rows;
    public int columns;
    public int firstIndex;
    public int lastIndex;
}

[Serializable]
public class SpriteSheetWithSize : SpriteSheet
{
    public float scale;  // Relative to 1x lane height
}

[Serializable]
[FormatVersion(NoteSkin.kVersion, typeof(NoteSkin), isLatest: true)]
public class NoteSkinBase : Serializable<NoteSkinBase> {}

public class NoteSkin : NoteSkinBase
{
    public const string kVersion = "1";

    public string name;

    public SpriteSheetWithSize basic;

    public SpriteSheetWithSize chainHead;
    public SpriteSheetWithSize chainNode;
    public SpriteSheetWithSize chainPath;

    public SpriteSheetWithSize holdHead;
    public SpriteSheetWithSize holdTrail;
    public SpriteSheetWithSize holdTrailEnd;
    public SpriteSheetWithSize holdOngoingTrail;
    public SpriteSheetWithSize holdOngoingTrailEnd;

    public SpriteSheetWithSize dragHead;
    public SpriteSheetWithSize dragCurve;

    public SpriteSheetWithSize repeatHead;
    public SpriteSheetWithSize repeat;
    public SpriteSheetWithSize repeatHoldTrail;
    public SpriteSheetWithSize repeatHoldTrailEnd;
    public SpriteSheetWithSize repeatPath;

    public NoteSkin()
    {
        version = kVersion;
    }
}