using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class RulesetBase
{
    public string version;

    private string Serialize()
    {
        return UnityEngine.JsonUtility.ToJson(this, prettyPrint: true);
    }
    private static RulesetBase Deserialize(string json)
    {
        string version = UnityEngine.JsonUtility.FromJson<RulesetBase>(json).version;
        switch (version)
        {
            case Ruleset.kVersion:
                return UnityEngine.JsonUtility.FromJson<Ruleset>(json);
            // For non-current versions, maybe attempt conversion?
            default:
                throw new Exception($"Unknown version: {version}");
        }
    }

    public static RulesetBase LoadFromFile(string path)
    {
        string fileContent = System.IO.File.ReadAllText(path);
        return Deserialize(fileContent);
    }
}

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

    // Hitbox size
    public float hitboxWidth;
    public float chainHeadHitboxWidth;
    public float chainNodeHitboxWidth;
    public float ongoingDragHitboxWidth;
    public float ongoingDragHitboxHeight;

    // HP
    public int maxHp;
    public int hpLoss;
    public int hpRecovery;

    public Ruleset()
    {
        version = kVersion;

        rainbowMaxWindow = 0.03f;
        maxWindow = 0.05f;
        coolWindow = 0.1f;
        goodWindow = 0.15f;
        breakThreshold = 0.3f;

        hitboxWidth = 1.5f;
        chainHeadHitboxWidth = 1.5f;
        chainNodeHitboxWidth = 3f;
        ongoingDragHitboxWidth = 2f;
        ongoingDragHitboxHeight = 1.5f;

        maxHp = 1000;
        hpLoss = 50;
        hpRecovery = 4;
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