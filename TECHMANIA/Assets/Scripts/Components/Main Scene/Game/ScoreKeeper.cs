using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public enum Judgement
{
    RainbowMax,
    Max,
    Cool,
    Good,
    Miss,
    Break
}

public struct JudgementAndTimeDifference
{
    public Judgement judgement;
    public float timeDifference;  // game time - correct time

    public static JudgementAndTimeDifference Miss()
    {
        return new JudgementAndTimeDifference()
        {
            judgement = Judgement.Miss,
            timeDifference = float.PositiveInfinity
        };
    }

    public static JudgementAndTimeDifference Break()
    {
        return new JudgementAndTimeDifference()
        {
            judgement = Judgement.Break,
            timeDifference = float.PositiveInfinity
        };
    }
}

public enum PerformanceMedal
{
    NoMedal,
    AllCombo,
    PerfectPlay,
    AbsolutePerfect
}

[MoonSharpUserData]
// Keeps track of score, combo, HP and fever.
public class ScoreKeeper
{
    // Reference to GameSetup so we can call Fever callbacks.
    private ThemeApi.GameSetup gameSetup;
    private ThemeApi.GameState state;

    private WindowsAndDeltas legacyRulesetOverride;

    public bool stageFailed
    {
        get;
        [MoonSharpHidden]
        set;
    }

    // Score
    public int totalNotes { get; private set; }
    [MoonSharpHidden]  // Lua can access with NumNotesWithJudgement
    public Dictionary<Judgement, int> notesPerJudgement
        { get; private set; }
    private int oneTimeFeverBonus;
    public int totalFeverBonus { get; private set; }
    public int maxScore => Ruleset.instance.comboBonus ?
        290000 : 300000;

    // Combo
    public int currentCombo { get; private set; }
    public int maxCombo { get; private set; }

    // HP
    public int maxHp => Ruleset.instance.maxHp;
    public int hp { get; private set; }

    // Fever
    public enum FeverState
    {
        Building,  // Accummulates with MAXes
        Ready,  // No longer accummulates, awaiting activation
        Active  // Decreases with time
    }
    private float feverCoefficient;
    private System.Diagnostics.Stopwatch feverTimer;
    public FeverState feverState { get; private set; }
    public float feverAmount { get; private set; }  // [0, 1]

    [MoonSharpHidden]
    public ScoreKeeper(ThemeApi.GameSetup gameSetup,
        ThemeApi.GameState state)
    {
        this.gameSetup = gameSetup;
        this.state = state;
    }

    [MoonSharpHidden]
    public void Prepare(Pattern pattern, int firstScan, int lastScan,
        int playableNotes)
    {
        legacyRulesetOverride = pattern.legacyRulesetOverride;
        if (gameSetup.setlist.enabled)
        {
            legacyRulesetOverride = pattern.legacySetlistOverride?[
                state.setlist.currentStage];
        }
        stageFailed = false;

        // Score.
        totalNotes = playableNotes;
        notesPerJudgement = new Dictionary<Judgement, int>();
        foreach (Judgement j in System.Enum.GetValues(
            typeof(Judgement)))
        {
            notesPerJudgement.Add(j, 0);
        }
        oneTimeFeverBonus = 0;
        totalFeverBonus = 0;

        // Combo.
        currentCombo = 0;
        maxCombo = 0;

        // HP.
        hp = maxHp;

        // Calculate Fever coefficient. The goal is for the Fever bar
        // to fill up in around 12.5 seconds.
        if (Ruleset.instance.constantFeverCoefficient)
        {
            feverCoefficient = 8f;
        }
        else
        {
            int lastPulse = (lastScan + 1 - firstScan) *
                pattern.patternMetadata.bps *
                Pattern.pulsesPerBeat;
            float trackLength = pattern.PulseToTime(lastPulse);
            feverCoefficient = trackLength / 12.5f;
        }
        Debug.Log("Fever coefficient is: " + feverCoefficient);

        // Other Fever fields.
        feverState = FeverState.Building;
        feverAmount = 0;
    }

    [MoonSharpHidden]
    public void Pause()
    {
        feverTimer?.Stop();
    }

    [MoonSharpHidden]
    public void Unpause()
    {
        feverTimer?.Start();
    }

    [MoonSharpHidden]
    public void ResolveNote(NoteType noteType, Judgement judgement)
    {
        bool missOrBreak = judgement == Judgement.Miss ||
            judgement == Judgement.Break;

        // Combo
        if (!missOrBreak)
        {
            SetCombo(currentCombo + 1);
        }
        else
        {
            SetCombo(0);
        }

        if (GameController.instance.modifiers.mode == 
            Modifiers.Mode.Practice)
        {
            // Score, HP and Fever don't update in Practice mode.
            return;
        }

        // Score
        notesPerJudgement[judgement]++;
        if (feverState == FeverState.Active)
        {
            switch (judgement)
            {
                case Judgement.Max:
                    oneTimeFeverBonus +=
                        Ruleset.instance.feverBonusOnMax;
                    break;
                case Judgement.Cool:
                    oneTimeFeverBonus +=
                        Ruleset.instance.feverBonusOnCool;
                    break;
                case Judgement.Good:
                    oneTimeFeverBonus +=
                        Ruleset.instance.feverBonusOnGood;
                    break;
            }
        }

        // HP
        int? setlistStageNumber = gameSetup.setlist.enabled ?
            state.setlist.currentStage : null;
        hp += Ruleset.instance.GetHpDelta(
            judgement, noteType,
            feverState == FeverState.Active,
            legacyRulesetOverride,
            setlistStageNumber);
        if (Modifiers.instance.suddenDeath == 
            Modifiers.SuddenDeath.SuddenDeath &&
            (judgement == Judgement.Miss ||
            judgement == Judgement.Break))
        {
            hp = 0;
        }
        // It's up to GameController to set stage failed.
        if (hp < 0) hp = 0;
        if (hp >= maxHp) hp = maxHp;

        // Fever
        if (!missOrBreak)
        {
            if (feverState == FeverState.Building &&
                (judgement == Judgement.RainbowMax ||
                judgement == Judgement.Max))
            {
                float feverDelta = feverCoefficient / totalNotes;
                if (GameController.instance.autoPlay) feverDelta = 0f;
                if (GameController.instance.modifiers.fever == 
                    Modifiers.Fever.FeverOff)
                {
                    feverDelta = 0;
                }

                feverAmount += feverDelta;
                gameSetup.onFeverUpdate?.Function?.Call(feverAmount);
                if (feverAmount >= 1f)
                {
                    OnFeverFull();
                }
            }
        }
        else  // MISS or BREAK
        {
            if (feverState == FeverState.Building ||
                feverState == FeverState.Ready)
            {
                bool wasReady = feverState == FeverState.Ready;
                switch (judgement)
                {
                    case Judgement.Miss:
                        feverAmount *= 0.75f;
                        break;
                    case Judgement.Break:
                        feverAmount *= 0.5f;
                        break;
                }
                feverState = FeverState.Building;
                if (wasReady)
                {
                    gameSetup.onFeverUnready?.Function?.Call();
                }
                gameSetup.onFeverUpdate?.Function?.Call(feverAmount);
            }
        }
    }

    private void OnFeverFull()
    {
        feverState = FeverState.Ready;
        feverAmount = 1f;
        gameSetup.onFeverReady?.Function?.Call();
        if (GameController.instance.modifiers.fever
            == Modifiers.Fever.AutoFever)
        {
            ActivateFever();
        }
    }

    [MoonSharpHidden]
    public void JumpToScan()
    {
        if (GameController.instance.modifiers.mode ==
            Modifiers.Mode.Practice)
        {
            SetCombo(0);
        }
    }

    #region Score
    public int NumNotesWithJudgement(Judgement j)
    {
        if (notesPerJudgement.ContainsKey(j))
        {
            return notesPerJudgement[j];
        }
        else
        {
            return 0;
        }
    }

    public bool AllNotesResolved()
    {
        int numNotesResolved = 0;
        foreach (KeyValuePair<Judgement, int> pair in
            notesPerJudgement)
        {
            numNotesResolved += pair.Value;
        }
        return numNotesResolved >= totalNotes;
    }

    // Does not take Fever bonus and combo bonus into account.
    public int ScoreFromNotes()
    {
        if (totalNotes == 0) return 0;

        // Rainbow Max = maxScore / totalNotes
        // Max = Rainbow Max - 1
        // Cool = Rainbow Max * 0.7
        // Good = Rainbow Max * 0.4
        // Miss/Break = 0
        float maxMultiplier =
            notesPerJudgement[Judgement.RainbowMax] * 1f
            + notesPerJudgement[Judgement.Max] * 1f
            + notesPerJudgement[Judgement.Cool] * 0.7f
            + notesPerJudgement[Judgement.Good] * 0.4f;
        int score = Mathf.FloorToInt(
            maxScore * maxMultiplier / totalNotes);
        score -= notesPerJudgement[Judgement.Max];
        return score;
    }

    public int ComboBonus()
    {
        if (!Ruleset.instance.comboBonus)
        {
            return 0;
        }
        if (notesPerJudgement[Judgement.RainbowMax] +
            notesPerJudgement[Judgement.Max] == totalNotes)
        {
            return 10000;
        }

        int missAndBreaks = notesPerJudgement[Judgement.Miss] +
            notesPerJudgement[Judgement.Break];
        // Why, just why.
        return Mathf.FloorToInt(
            (float)(7800 * missAndBreaks + 9800) *
            (totalNotes - missAndBreaks) /
            (missAndBreaks + 1) /
            totalNotes);
    }

    public int TotalScore()
    {
        return ScoreFromNotes() + totalFeverBonus +
            ComboBonus();
    }

    public PerformanceMedal Medal()
    {
        if (notesPerJudgement[Judgement.Miss] +
            notesPerJudgement[Judgement.Break] > 0)
        {
            return PerformanceMedal.NoMedal;
        }
        if (notesPerJudgement[Judgement.Cool] +
            notesPerJudgement[Judgement.Good] > 0)
        {
            return PerformanceMedal.AllCombo;
        }
        if (notesPerJudgement[Judgement.Max] > 0)
        {
            return PerformanceMedal.PerfectPlay;
        }
        return PerformanceMedal.AbsolutePerfect;
    }

    public static string ScoreToRankAssumingStageClear(int score)
    {
        if (score > 295000) return "S++";
        if (score > 290000) return "S+";
        if (score > 285000) return "S";
        if (score > 280000) return "A++";
        if (score > 270000) return "A+";
        if (score > 260000) return "A";
        if (score > 220000) return "B";
        return "C";
    }

    public string Rank()
    {
        if (stageFailed) return "F";
        return ScoreToRankAssumingStageClear(TotalScore());
    }
    #endregion

    #region Combo
    private void SetCombo(int combo)
    {
        currentCombo = combo;
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
    }

    // For combo ticks.
    [MoonSharpHidden]
    public void IncrementCombo()
    {
        SetCombo(currentCombo + 1);
    }
    #endregion

    #region Fever
    [MoonSharpHidden]
    public void UpdateFever()
    {
        if (feverState != FeverState.Active) return;
        feverAmount = 1f -
            (float)feverTimer.Elapsed.TotalSeconds * 0.1f;
        gameSetup.onFeverUpdate?.Function?.Call(feverAmount);

        if (feverAmount < 0f)
        {
            DeactivateFever();
        }
    }

    [MoonSharpHidden]
    public void ActivateFever()
    {
        if (feverState != FeverState.Ready) return;

        oneTimeFeverBonus = 0;
        feverState = FeverState.Active;
        feverTimer = new System.Diagnostics.Stopwatch();
        feverTimer.Start();

        gameSetup.onFeverActivated?.Function?.Call();
    }

    [MoonSharpHidden]
    public void DeactivateFever()
    {
        if (feverState == FeverState.Active)
        {
            feverAmount = 0f;
            feverTimer.Stop();
            feverTimer = null;
            feverState = FeverState.Building;

            int feverBonus = oneTimeFeverBonus;
            totalFeverBonus += feverBonus;
            gameSetup.onFeverEnd?.Function?.Call(feverBonus);
        }
    }
    #endregion

    [MoonSharpHidden]
    public void OverrideStatsFromSetlist(
        int currentCombo, int maxCombo, int hp, float feverAmount)
    {
        this.currentCombo = currentCombo;
        this.maxCombo = maxCombo;
        this.hp = hp;
        this.feverAmount = feverAmount;
        if (this.feverAmount >= 1f)
        {
            OnFeverFull();
        }
    }
}
