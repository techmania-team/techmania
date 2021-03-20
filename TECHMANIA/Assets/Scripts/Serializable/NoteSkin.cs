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

    public SpriteSheet()
    {
        rows = 1;
        columns = 1;
        firstIndex = 0;
        lastIndex = 0;
    }
}

[Serializable]
public class SpriteSheetWithScale : SpriteSheet
{
    public float scale;  // Relative to 1x lane height

    public SpriteSheetWithScale() : base()
    {
        scale = 1f;
    }
}

[Serializable]
[FormatVersion(NoteSkin.kVersion, typeof(NoteSkin), isLatest: true)]
public class NoteSkinBase : Serializable<NoteSkinBase> {}

public class NoteSkin : NoteSkinBase
{
    public const string kVersion = "1";

    public string name;

    public SpriteSheetWithScale basic;

    public SpriteSheetWithScale chainHead;
    public SpriteSheetWithScale chainNode;
    public SpriteSheetWithScale chainPath;

    public SpriteSheetWithScale holdHead;
    public SpriteSheetWithScale holdTrail;
    public SpriteSheetWithScale holdTrailEnd;
    public SpriteSheetWithScale holdOngoingTrail;
    public SpriteSheetWithScale holdOngoingTrailEnd;

    public SpriteSheetWithScale dragHead;
    public SpriteSheetWithScale dragCurve;

    public SpriteSheetWithScale repeatHead;
    public SpriteSheetWithScale repeat;
    public SpriteSheetWithScale repeatHoldTrail;
    public SpriteSheetWithScale repeatHoldTrailEnd;
    public SpriteSheetWithScale repeatPath;

    public NoteSkin()
    {
        version = kVersion;
    }
}