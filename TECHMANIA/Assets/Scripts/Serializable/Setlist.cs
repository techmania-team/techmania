using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A setlist is a collection of at least 3 selectable patterns and
// at least 1 hidden patterns. The player chooses 3 selectable patterns,
// plays through them in one go, and the game chooses a hidden pattern
// as the 4th pattern to play.

[Serializable]
[FormatVersion(Setlist.kVersion, typeof(Setlist), isLatest: true)]
public class SetlistBase : SerializableClass<SetlistBase> { }

[Serializable]
[MoonSharpUserData]
public class Setlist : SetlistBase
{
    public const string kVersion = "1";

    public Setlist(string title)
    {
        version = kVersion;
        setlistMetadata = new SetlistMetadata()
        {
            title = title
        };
        selectablePatterns = new List<PatternReference>();
        hiddenPatterns = new List<HiddenPattern>();
    }

    public SetlistMetadata setlistMetadata;

    [Serializable]
    [MoonSharpUserData]
    public class PatternReference
    {
        public string trackTitle;
        public string trackGuid;

        public string patternName;
        public int patternLevel;
        public int patternPlayableLanes;
        public string patternGuid;
    }

    // When choosing a hidden pattern, the game will consider each
    // hidden pattern in order, and choose the first hidden pattern
    // whose criteria is fully met. Each criteria has a type, a
    // direction, and a value.
    //
    // The criteria of the last hidden pattern is ignored.
    public enum HiddenPatternCriteriaType
    {
        // The total index of the 3 selected patterns in 1-index
        Index,
        // The total level of the 3 selected patterns
        Level,
        // The remaining HP percentage in [0, 100] after the 3rd pattern
        HP,
        // The total score, including bonuses, after the 3rd pattern
        Score,
        // The current combo after the 3rd pattern
        Combo,
        // The max combo after the 3rd pattern
        MaxCombo,
        // A random number in [1, 100]
        D100
    }

    public enum HiddenPatternCriteriaDirection
    {
        LargerThan,
        SmallerThan
    }

    [Serializable]
    [MoonSharpUserData]
    public class HiddenPattern
    {
        public PatternReference reference;
        public HiddenPatternCriteriaType criteriaType;
        public HiddenPatternCriteriaDirection criteriaDirection;
        public int criteriaValue;
    }

    public List<PatternReference> selectablePatterns;
    public List<HiddenPattern> hiddenPatterns;
}

[Serializable]
[MoonSharpUserData]
public class SetlistMetadata
{
    public string guid;

    public string title;
    public string description;
    public string eyecatchImage;
    public string backImage;
    public ControlScheme controlScheme;
}