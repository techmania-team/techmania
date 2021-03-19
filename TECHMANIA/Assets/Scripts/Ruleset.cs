using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Each format version is a derived class of RulesetBase.

[Serializable]
[FormatVersion(Ruleset.kVersion, typeof(Ruleset), isLatest: true)]
public class RulesetBase : Serializable<RulesetBase> {}

[Serializable]
public class Ruleset : RulesetBase
{
    public const string kVersion = "1";

    [NonSerialized]
    public bool isCustom;

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

    public Ruleset()
    {
        version = kVersion;

        rainbowMaxWindow = 0.04f;
        maxWindow = 0.07f;
        coolWindow = 0.1f;
        goodWindow = 0.15f;
        breakThreshold = 0.3f;
        longNoteGracePeriod = 0.15f;

        scanMargin = 0.04f;
        hitboxWidth = 1.5f;
        chainHeadHitboxWidth = 1.5f;
        chainNodeHitboxWidth = 3f;
        ongoingDragHitboxWidth = 2f;
        ongoingDragHitboxHeight = 2f;

        maxHp = 1000;
        hpLoss = 50;
        hpRecovery = 3;
        hpLossDuringFever = 50;
        hpRecoveryDuringFever = 5;

        comboBonus = false;

        constantFeverCoefficient = false;
        feverBonusOnMax = 1;
        feverBonusOnCool = 1;
        feverBonusOnGood = 0;
    }

    #region Instance
    public static Ruleset instance { get; private set; }
    public static void RefreshInstance()
    {
        try
        {
            instance = LoadFromFile(
                Paths.GetRulesetFilePath()) as Ruleset;
            instance.isCustom = true;
        }
        catch (IOException)
        {
            instance = new Ruleset();
            instance.isCustom = false;
        }
        catch (Exception ex)
        {
            instance = new Ruleset();
            instance.isCustom = false;
            throw ex;
        }
    }
    #endregion
}