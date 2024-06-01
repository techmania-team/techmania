using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
            guid = Guid.NewGuid().ToString(),
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
        SmallerThan,
        LargerThan
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

    [MoonSharpHidden]
    // Returns the index in hiddenPatterns.
    public int ChooseHiddenPattern(
        int totalIndex, int totalLevel,
        SetlistScoreKeeper scoreKeeper)
    {
        for (int i = 0; i < hiddenPatterns.Count - 1; i++)
        {
            HiddenPattern h = hiddenPatterns[i];
            StringBuilder log = new StringBuilder();
            log.AppendLine($"Evaluating criteria for hidden pattern #{i}: {h.reference.trackTitle}, {h.reference.patternName}, level {h.reference.patternLevel}.");
            log.AppendLine($"Criteria: {h.criteriaType} {h.criteriaDirection} {h.criteriaValue}");

            int actualValue = h.criteriaType switch
            {
                HiddenPatternCriteriaType.Index => totalIndex,
                HiddenPatternCriteriaType.Level => totalLevel,
                HiddenPatternCriteriaType.HP =>
                    (scoreKeeper.hp * 100 / scoreKeeper.maxHp),
                HiddenPatternCriteriaType.Score =>
                    scoreKeeper.TotalScore(),
                HiddenPatternCriteriaType.Combo =>
                    scoreKeeper.currentCombo,
                HiddenPatternCriteriaType.MaxCombo =>
                    scoreKeeper.maxCombo,
                HiddenPatternCriteriaType.D100 =>
                    UnityEngine.Random.Range(1, 100),
                _ => throw new ArgumentException("Unknown criteria type: " + h.criteriaType)
            };
            log.AppendLine("Actual value: " + actualValue);

            bool met = h.criteriaDirection switch
            {
                HiddenPatternCriteriaDirection.SmallerThan =>
                    actualValue < h.criteriaValue,
                HiddenPatternCriteriaDirection.LargerThan =>
                    actualValue > h.criteriaValue,
                _ => throw new ArgumentException("Unknown criteria direction: " + h.criteriaDirection)
            };
            if (met)
            {
                log.AppendLine("Criteria is met, choosing this hidden pattern.");
            }
            else
            {
                log.AppendLine("Criteria is not met, will try the next one.");
            }
            Debug.Log(log.ToString());

            if (met)
            {
                return i;
            }
        }
        Debug.Log("All hidden pattern criterias are not met, choosing the last hidden pattern.");
        return hiddenPatterns.Count - 1;
    }
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