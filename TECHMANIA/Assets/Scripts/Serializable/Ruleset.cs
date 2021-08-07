using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Each format version is a derived class of RulesetBase.

[Serializable]
[FormatVersion(RulesetV1.kVersion, typeof(RulesetV1), isLatest: false)]
[FormatVersion(Ruleset.kVersion, typeof(Ruleset), isLatest: true)]
public class RulesetBase : SerializableClass<RulesetBase> {}

// Updates in version 2:
// - Allows defining timing window in pulses instead of seconds.
// - Allows defining HP delta by each judgement.
[Serializable]
public class Ruleset : RulesetBase
{
    public const string kVersion = "2";

    [NonSerialized]
    public bool isCustom;

    // Time windows

    // 5 time windows for Rainbow MAX, MAX, COOL, GOOD and MISS,
    // respectively. No input after the MISS window = BREAK.
    public List<float> timeWindows;
    // True: time windows are in pulses.
    // False: time windows are in seconds.
    public bool timeWindowsInPulses;
    // TODO: should this be affected by timeWindowInPulses too?
    public float longNoteGracePeriod;

    // Hitbox sizes

    public float scanMargin;
    public float hitboxWidth;
    public float chainHeadHitboxWidth;
    public float chainNodeHitboxWidth;
    public float ongoingDragHitboxWidth;
    public float ongoingDragHitboxHeight;

    // HP

    public int maxHp;
    // 6 values for Rainbow MAX, MAX, COOL, GOOD, MISS and BREAK,
    // respectively. hpDelta and hpDeltaDuringFever are for basic
    // notes; hpDeltaNonBasic and hpDeltaNonBasicDuringFever are for
    // all other types.
    public List<int> hpDelta;
    public List<int> hpDeltaNonBasic;
    public List<int> hpDeltaDuringFever;
    public List<int> hpDeltaNonBasicDuringFever;

    // Score
    public bool comboBonus;

    // Fever
    public bool constantFeverCoefficient;
    public int feverBonusOnMax;
    public int feverBonusOnCool;
    public int feverBonusOnGood;

    public Ruleset()
    {
        version = kVersion;
    }

    #region Accessors
    public int GetHpDelta(Judgement j, NoteType type, bool fever)
    {
        List<int> list = null;
        if (type == NoteType.Basic)
        {
            if (fever)
                list = hpDeltaDuringFever;
            else
                list = hpDelta;
        }
        else
        {
            if (fever)
                list = hpDeltaNonBasicDuringFever;
            else
                list = hpDeltaNonBasic;
        }

        switch (j)
        {
            case Judgement.RainbowMax:
                return list[0];
            case Judgement.Max:
                return list[1];
            case Judgement.Cool:
                return list[2];
            case Judgement.Good:
                return list[3];
            case Judgement.Miss:
                return list[4];
            case Judgement.Break:
                return list[5];
            default:
                return 0;
        }
    }
    #endregion

    #region Instances
    public static readonly Ruleset standard;
    public static readonly Ruleset legacy;
    public static Ruleset custom;

    static Ruleset()
    {
        standard = new Ruleset()
        {
            timeWindows = new List<float>()
            { 0.04f, 0.07f, 0.1f, 0.15f, 0.2f },
            timeWindowsInPulses = false,
            longNoteGracePeriod = 0.15f,

            scanMargin = 0.05f,
            hitboxWidth = 1.5f,
            chainHeadHitboxWidth = 1.5f,
            chainNodeHitboxWidth = 3f,
            ongoingDragHitboxWidth = 2f,
            ongoingDragHitboxHeight = 2f,

            maxHp = 1000,
            hpDelta = new List<int>()
            { 3, 3, 3, 3, -50, -50 },
            hpDeltaNonBasic = new List<int>()
            { 3, 3, 3, 3, -50, -50 },
            hpDeltaDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 },
            hpDeltaNonBasicDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 },

            comboBonus = false,

            constantFeverCoefficient = false,
            feverBonusOnMax = 1,
            feverBonusOnCool = 1,
            feverBonusOnGood = 0
        };
        legacy = new Ruleset()
        {
            // TODO: fill these.
            timeWindows = new List<float>()
            { 20f, 40f, 60f, 120f, 240f },
            timeWindowsInPulses = true,
            longNoteGracePeriod = 0.15f,

            scanMargin = 0.05f,
            hitboxWidth = 1.5f,
            chainHeadHitboxWidth = 100f,
            chainNodeHitboxWidth = 100f,
            ongoingDragHitboxWidth = 2f,
            ongoingDragHitboxHeight = 2f,

            maxHp = 1000,
            hpDelta = new List<int>()
            { 3, 3, 3, 3, -50, -50 },
            hpDeltaNonBasic = new List<int>()
            { 3, 3, 3, 3, -50, -50 },
            hpDeltaDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 },
            hpDeltaNonBasicDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 },

            comboBonus = true,

            constantFeverCoefficient = true,
            feverBonusOnMax = 1,
            feverBonusOnCool = 1,
            feverBonusOnGood = 0
        };
    }

    public static Ruleset instance => GetInstance();

    private static Ruleset GetInstance()
    {
        // TODO: return the correct instance based on options and
        // overrides.
        return legacy;
    }

    public static void RefreshInstance()
    {
        //try
        //{
        //    instance = LoadFromFile(
        //        Paths.GetRulesetFilePath()) as Ruleset;
        //    instance.isCustom = true;
        //}
        //catch (IOException)
        //{
        //    instance = new Ruleset();
        //    instance.isCustom = false;
        //}
        //catch (Exception ex)
        //{
        //    instance = new Ruleset();
        //    instance.isCustom = false;
        //    throw ex;
        //}
    }
    #endregion
}

[Serializable]
public class RulesetV1 : RulesetBase
{
    public const string kVersion = "2";

    // Timing window
    public float rainbowMaxWindow;
    public float maxWindow;
    public float coolWindow;
    public float goodWindow;
    public float breakThreshold;
    public float longNoteGracePeriod;

    // Hitbox size
    public float scanMargin;
    public float hitboxWidth;
    public float chainHeadHitboxWidth;
    public float chainNodeHitboxWidth;
    public float ongoingDragHitboxWidth;
    public float ongoingDragHitboxHeight;

    // HP
    public int maxHp;
    public int hpLoss;
    public int hpRecovery;
    public int hpLossDuringFever;
    public int hpRecoveryDuringFever;

    // Score
    public bool comboBonus;

    // Fever
    public bool constantFeverCoefficient;
    public int feverBonusOnMax;
    public int feverBonusOnCool;
    public int feverBonusOnGood;

    protected override RulesetBase Upgrade()
    {
        return new Ruleset()
        {
            timeWindows = new List<float>()
            { 
                rainbowMaxWindow,
                maxWindow,
                coolWindow,
                goodWindow,
                breakThreshold
            },
            timeWindowsInPulses = false,
            longNoteGracePeriod = longNoteGracePeriod,

            scanMargin = scanMargin,
            hitboxWidth = hitboxWidth,
            chainHeadHitboxWidth = chainHeadHitboxWidth,
            chainNodeHitboxWidth = chainNodeHitboxWidth,
            ongoingDragHitboxWidth = ongoingDragHitboxWidth,
            ongoingDragHitboxHeight = ongoingDragHitboxHeight,

            maxHp = maxHp,
            hpDelta = new List<int>()
            { 
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpLoss,
                hpLoss
            },
            hpDeltaNonBasic = new List<int>()
            {
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpLoss,
                hpLoss
            },
            hpDeltaDuringFever = new List<int>()
            { 
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpLossDuringFever,
                hpLossDuringFever
            },
            hpDeltaNonBasicDuringFever = new List<int>()
            {
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpLossDuringFever,
                hpLossDuringFever
            },

            comboBonus = comboBonus,

            constantFeverCoefficient = constantFeverCoefficient,
            feverBonusOnMax = feverBonusOnMax,
            feverBonusOnCool = feverBonusOnCool,
            feverBonusOnGood = feverBonusOnGood
        };
    }
}