using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[MoonSharpUserData]
// Keeps track of all ScoreKeepers in a setlist, and provides
// a similar interface to query score / combo / HP / fever for the
// entire setlist.
public class SetlistScoreKeeper
{
    public SetlistScoreKeeper()
    {
        children = new List<ScoreKeeper>();
    }

    #region Children
    private List<ScoreKeeper> children;

    // stage is [0, 3]. Returns null if the stage has not started yet.
    public ScoreKeeper GetScoreKeeperForStage(int stage)
    {
        if (children.Count <= stage) return null;
        return children[stage];
    }

    public ScoreKeeper GetCurrentScoreKeeper() => children[^1];

    [MoonSharpHidden]
    public void AddChild(ScoreKeeper child, int stageNumber)
    {
        if (children.Count != stageNumber)
        {
            throw new System.ArgumentException("Added ScoreKeeper with unexpected stage number.");
        }

        // Prepare stats to send to new child.
        int currentCombo = children.Count == 0 ? 0 : this.currentCombo;
        int maxCombo = children.Count == 0 ? 0 : this.maxCombo;
        int hp = children.Count == 0 ? maxHp : this.hp;
        float feverAmount = children.Count == 0 ? 0 : this.feverAmount;

        children.Add(child);

        // Send stats to new child.
        child.OverrideStatsFromSetlist(
            currentCombo, maxCombo, hp, feverAmount);
    }
    #endregion

    public bool stageFailed => children.Any(x => x.stageFailed);

    #region Score
    public int totalNotes => children.Sum(x => x.totalNotes);
    public int totalFeverBonus => children.Sum(x => x.totalFeverBonus);
    public int maxScore => children.Sum(x => x.maxScore);
    public int NumNotesWithJudgement(Judgement j) => children.Sum(
        x => x.NumNotesWithJudgement(j));
    public bool AllNotesResolved() => children.All(
        x => x.AllNotesResolved());
    public int ScoreFromNotes() => children.Sum(
        x => x.ScoreFromNotes());
    public int ComboBonus() => children.Sum(x => x.ComboBonus());
    public int TotalScore() => children.Sum(x => x.TotalScore());

    public PerformanceMedal Medal()
    {
        if (NumNotesWithJudgement(Judgement.Miss) +
            NumNotesWithJudgement(Judgement.Break) > 0)
        {
            return PerformanceMedal.NoMedal;
        }
        if (NumNotesWithJudgement(Judgement.Cool) +
            NumNotesWithJudgement(Judgement.Good) > 0)
        {
            return PerformanceMedal.AllCombo;
        }
        if (NumNotesWithJudgement(Judgement.Max) > 0)
        {
            return PerformanceMedal.PerfectPlay;
        }
        return PerformanceMedal.AbsolutePerfect;
    }

    public static string ScoreToRankAssumingStageClear(int score)
    {
        if (score > 295000 * 4) return "S++";
        if (score > 290000 * 4) return "S+";
        if (score > 285000 * 4) return "S";
        if (score > 280000 * 4) return "A++";
        if (score > 270000 * 4) return "A+";
        if (score > 260000 * 4) return "A";
        if (score > 220000 * 4) return "B";
        return "C";
    }

    public string Rank()
    {
        if (stageFailed) return "F";
        return ScoreToRankAssumingStageClear(TotalScore());
    }
    #endregion

    #region Combo
    public int currentCombo => GetCurrentScoreKeeper().currentCombo;
    public int maxCombo => GetCurrentScoreKeeper().maxCombo;
    #endregion

    #region HP
    public int maxHp => Ruleset.instance.maxHp;
    public int hp => GetCurrentScoreKeeper().hp;
    #endregion

    #region Fever
    public ScoreKeeper.FeverState feverState =>
        GetCurrentScoreKeeper().feverState;
    public float feverAmount => GetCurrentScoreKeeper().feverAmount;
    #endregion
}
