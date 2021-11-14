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
// - Allows defining HP delta by each judgement.
[Serializable]
public class Ruleset : RulesetBase
{
    public const string kVersion = "2";

    // Time windows

    // 5 time windows for Rainbow MAX, MAX, COOL, GOOD and MISS,
    // respectively. No input after the MISS window = BREAK.
    public List<float> timeWindows;
    // True: time windows are in pulses.
    // False: time windows are in seconds.
    public bool timeWindowsInPulses;
    public float longNoteGracePeriod;
    public bool longNoteGracePeriodInPulses;

    // Hitbox sizes

    public List<float> scanMarginTopBottom;
    public List<float> scanMarginMiddle;
    public float scanMarginBeforeFirstBeat;
    public float scanMarginAfterLastBeat;
    public float hitboxWidth;
    public float hitboxHeight;
    public float chainHeadHitboxWidth;
    public float chainNodeHitboxWidth;
    public float dragHitboxWidth;
    public float dragHitboxHeight;
    public float ongoingDragHitboxWidth;
    public float ongoingDragHitboxHeight;

    // HP

    public int maxHp;
    // 6 values for Rainbow MAX, MAX, COOL, GOOD, MISS and BREAK,
    // respectively.
    public List<int> hpDeltaBasic;
    public List<int> hpDeltaChain;
    public List<int> hpDeltaHold;
    public List<int> hpDeltaDrag;
    public List<int> hpDeltaRepeat;
    public List<int> hpDeltaBasicDuringFever;
    public List<int> hpDeltaChainDuringFever;
    public List<int> hpDeltaHoldDuringFever;
    public List<int> hpDeltaDragDuringFever;
    public List<int> hpDeltaRepeatDuringFever;

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

        // The constructor constructs the standard ruleset, so
        // custom ruleset can selectively override fields.
        timeWindows = new List<float>()
            { 0.04f, 0.07f, 0.1f, 0.15f, 0.2f };
        timeWindowsInPulses = false;
        longNoteGracePeriod = 0.15f;
        longNoteGracePeriodInPulses = false;

        scanMarginTopBottom = new List<float>()
            { 0.05f, 0.05f, 0.05f };
        scanMarginMiddle = new List<float>()
            { 0.05f, 0.05f, 0.05f };
        scanMarginBeforeFirstBeat = 0.15f;
        scanMarginAfterLastBeat = 0.1f;
        hitboxWidth = 1.5f;
        hitboxHeight = 1f;
        chainHeadHitboxWidth = 1.5f;
        chainNodeHitboxWidth = 3f;
        dragHitboxWidth = 1.5f;
        dragHitboxHeight = 1f;
        ongoingDragHitboxWidth = 2f;
        ongoingDragHitboxHeight = 2f;

        maxHp = 1000;
        hpDeltaBasic = new List<int>()
            { 3, 3, 3, 3, -50, -50 };
        hpDeltaChain = new List<int>()
            { 3, 3, 3, 3, -50, -50 };
        hpDeltaHold = new List<int>()
            { 3, 3, 3, 3, -50, -50 };
        hpDeltaDrag = new List<int>()
            { 3, 3, 3, 3, -50, -50 };
        hpDeltaRepeat = new List<int>()
            { 3, 3, 3, 3, -50, -50 };
        hpDeltaBasicDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 };
        hpDeltaChainDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 };
        hpDeltaHoldDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 };
        hpDeltaDragDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 };
        hpDeltaRepeatDuringFever = new List<int>()
            { 5, 5, 5, 5, -50, -50 };

        comboBonus = false;

        constantFeverCoefficient = false;
        feverBonusOnMax = 1;
        feverBonusOnCool = 1;
        feverBonusOnGood = 0;
    }

    #region Accessors
    public int GetHpDelta(Judgement j, NoteType type, bool fever)
    {
        List<int> list = null;
        switch (type)
        {
            case NoteType.Basic:
                if (fever)
                    list = hpDeltaBasicDuringFever;
                else
                    list = hpDeltaBasic;
                break;
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                if (fever)
                    list = hpDeltaChainDuringFever;
                else
                    list = hpDeltaChain;
                break;
            case NoteType.Hold:
                if (fever)
                    list = hpDeltaHoldDuringFever;
                else
                    list = hpDeltaHold;
                break;
            case NoteType.Drag:
                if (fever)
                    list = hpDeltaDragDuringFever;
                else
                    list = hpDeltaDrag;
                break;
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                if (fever)
                    list = hpDeltaRepeatDuringFever;
                else
                    list = hpDeltaRepeat;
                break;
        }

        List<int> overrideList = null;
        if (Options.instance.ruleset == Options.Ruleset.Legacy &&
            GameSetup.pattern.legacyRulesetOverride != null)
        {
            LegacyRulesetOverride o =
                GameSetup.pattern.legacyRulesetOverride;
            switch (type)
            {
                case NoteType.Basic:
                    if (fever)
                        overrideList = o.hpDeltaBasicDuringFever;
                    else
                        overrideList = o.hpDeltaBasic;
                    break;
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                    if (fever)
                        overrideList = o.hpDeltaChainDuringFever;
                    else
                        overrideList = o.hpDeltaChain;
                    break;
                case NoteType.Hold:
                    if (fever)
                        overrideList = o.hpDeltaHoldDuringFever;
                    else
                        overrideList = o.hpDeltaHold;
                    break;
                case NoteType.Drag:
                    if (fever)
                        overrideList = o.hpDeltaDragDuringFever;
                    else
                        overrideList = o.hpDeltaDrag;
                    break;
                case NoteType.RepeatHead:
                case NoteType.RepeatHeadHold:
                case NoteType.Repeat:
                case NoteType.RepeatHold:
                    if (fever)
                        overrideList = o.hpDeltaRepeatDuringFever;
                    else
                        overrideList = o.hpDeltaRepeat;
                    break;
            }
        }

        if (overrideList != null && overrideList.Count > 0)
        {
            list = overrideList;
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

    public void GetScanMargin(int playableLanes,
        Scan.Position scanPosition,
        out float marginAbove, out float marginBelow)
    {
        int index = playableLanes - 2;
        float marginTopBottom = scanMarginTopBottom[index];
        float marginMiddle = scanMarginMiddle[index];
        switch (scanPosition)
        {
            case Scan.Position.Top:
                marginAbove = marginTopBottom;
                marginBelow = marginMiddle;
                break;
            case Scan.Position.Bottom:
                marginAbove = marginMiddle;
                marginBelow = marginTopBottom;
                break;
            default:
                throw new NotImplementedException();
        }
    }
    #endregion

    #region Instances
    public static readonly Ruleset standard;
    public static readonly Ruleset legacy;
    public static Ruleset custom;

    static Ruleset()
    {
        standard = new Ruleset();
        legacy = new Ruleset()
        {
            timeWindows = new List<float>()
            { 12.5f, 37.5f, 51.25f, 65f, 83.75f },
            timeWindowsInPulses = true,
            longNoteGracePeriod = 0.1f,
            longNoteGracePeriodInPulses = false,

            scanMarginTopBottom = new List<float>()
                { 0.05f, 0.05f, 0.05f },
            scanMarginMiddle = new List<float>()
                { 0.05f, 0.05f, 0.05f },
            scanMarginBeforeFirstBeat = 0.157f,
            scanMarginAfterLastBeat = 0.093f,
            hitboxWidth = 1.25f,
            hitboxHeight = 1.15f,
            chainHeadHitboxWidth = 100f,
            chainNodeHitboxWidth = 100f,
            dragHitboxWidth = 3f,
            dragHitboxHeight = 1.15f,
            ongoingDragHitboxWidth = 3f,
            ongoingDragHitboxHeight = 1.15f,

            maxHp = 10000,
            hpDeltaBasic = new List<int>()
            { 30, 30, 15, 0, -300, -600 },
            hpDeltaChain = new List<int>()
            { 30, 30, 15, 0, -350, -500 },
            hpDeltaHold = new List<int>()
            { 30, 30, 15, 0, -350, -500 },
            hpDeltaDrag = new List<int>()
            { 30, 30, 15, 0, -350, -500 },
            hpDeltaRepeat = new List<int>()
            { 30, 30, 15, 0, -350, -500 },
            hpDeltaBasicDuringFever = new List<int>()
            { 30, 30, 30, 0, -300, -600 },
            hpDeltaChainDuringFever = new List<int>()
            { 30, 30, 30, 0, -350, -500 },
            hpDeltaHoldDuringFever = new List<int>()
            { 30, 30, 30, 0, -350, -500 },
            hpDeltaDragDuringFever = new List<int>()
            { 30, 30, 30, 0, -350, -500 },
            hpDeltaRepeatDuringFever = new List<int>()
            { 30, 30, 30, 0, -350, -500 },

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
        switch (Options.instance.ruleset)
        {
            case Options.Ruleset.Standard:
                return standard;
            case Options.Ruleset.Legacy:
                return legacy;
            case Options.Ruleset.Custom:
                return custom;
        }
        return null;
    }

    // May throw exceptions.
    public static void LoadCustomRuleset()
    {
        custom = LoadFromFile(Paths.GetRulesetFilePath()) as Ruleset;
    }
    #endregion
}

[Serializable]
public class RulesetV1 : RulesetBase
{
    public const string kVersion = "1";

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

            scanMarginTopBottom = new List<float>()
                { scanMargin, scanMargin, scanMargin },
            scanMarginMiddle = new List<float>()
                { scanMargin, scanMargin, scanMargin },
            hitboxWidth = hitboxWidth,
            chainHeadHitboxWidth = chainHeadHitboxWidth,
            chainNodeHitboxWidth = chainNodeHitboxWidth,
            ongoingDragHitboxWidth = ongoingDragHitboxWidth,
            ongoingDragHitboxHeight = ongoingDragHitboxHeight,

            maxHp = maxHp,
            hpDeltaBasic = new List<int>()
            { 
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpLoss,
                hpLoss
            },
            hpDeltaChain = new List<int>()
            {
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpLoss,
                hpLoss
            },
            hpDeltaHold = new List<int>()
            {
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpLoss,
                hpLoss
            },
            hpDeltaDrag = new List<int>()
            {
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpLoss,
                hpLoss
            },
            hpDeltaRepeat = new List<int>()
            {
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpRecovery,
                hpLoss,
                hpLoss
            },
            hpDeltaBasicDuringFever = new List<int>()
            { 
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpLossDuringFever,
                hpLossDuringFever
            },
            hpDeltaChainDuringFever = new List<int>()
            {
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpLossDuringFever,
                hpLossDuringFever
            },
            hpDeltaHoldDuringFever = new List<int>()
            {
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpLossDuringFever,
                hpLossDuringFever
            },
            hpDeltaDragDuringFever = new List<int>()
            {
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpRecoveryDuringFever,
                hpLossDuringFever,
                hpLossDuringFever
            },
            hpDeltaRepeatDuringFever = new List<int>()
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