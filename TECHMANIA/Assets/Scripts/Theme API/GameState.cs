using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class GameState
    {
        public enum State
        {
            // Waiting for Lua to fill in GameSetup and call
            // BeginLoading.
            // Any state can transition to Idle by calling Conclude().
            Idle,
            // Transitions to LoadError or LoadComplete.
            // With each file loaded, calls onLoadProgress.
            // When loading fails, calls onLoadError with a Status.
            Loading,
            LoadError,
            // Theme can start the game now, which transitions to
            // Ongoing state.
            LoadComplete,
            // Transitions to Paused, CompletePattern and Complete.
            Ongoing,
            // Transitions to Ongoing.
            Paused,
            // Setlist only: completed one pattern with HP above
            // stage threshold, but not completed the whole setlist yet.
            //
            // If completed the hidden pattern, game will treat it
            // as stage clear and enter Complete state, skipping
            // CompletePattern.
            //
            // If the pattern ended with HP below stage threshold,
            // game will treat it as stage failed and enter Complete
            // state, skipping CompletePattern.
            //
            // In this state, the game no longer updates or
            // responds to input, and waits for Lua to call
            // LoadNextPattern().
            //
            // Transitions to Loading.
            CompletePattern,
            // The game is complete by either stage clear or
            // stage failed. The game no longer updates or responds
            // to input, and waits for Lua to call Conclude().
            // Transitions to Idle.
            Complete
        }
        public State state { get; private set; }

        [MoonSharpHidden]
        public GameState()
        {
            state = State.Idle;
            GameController.instance.SetStateInstance(this);
        }

        #region State changes
        private void CheckState(State expectedState, string methodName)
        {
            if (state == expectedState) return;
            throw new System.Exception($"{methodName} expects {expectedState} state, but the current state is {state}.");
        }

        private void CheckState(List<State> states, string methodName)
        {
            foreach (State s in states)
            {
                if (state == s) return;
            }
            throw new System.Exception($"{methodName} expects one of the following states: {string.Join(',', states)}, but the current states is {state}.");
        }

        public void BeginLoading()
        {
            CheckState(State.Idle, "BeginLoading");
            state = State.Loading;
            GameController.instance.BeginLoading();
        }

        public void Begin()
        {
            CheckState(State.LoadComplete, "Begin");
            state = State.Ongoing;
            GameController.instance.Begin();
        }

        public void Pause()
        {
            CheckState(State.Ongoing, "Pause");
            state = State.Paused;
            GameController.instance.Pause();
        }

        public void Unpause()
        {
            CheckState(State.Paused, "Unpause");
            state = State.Ongoing;
            GameController.instance.Unpause();
        }

        public void LoadNextPattern()
        {
            CheckState(State.CompletePattern, "LoadNextPattern");
            state = State.Loading;
            // TODO
        }

        public void Conclude()
        {
            state = State.Idle;
            GameController.instance.Conclude();
        }

        [MoonSharpHidden]
        public void SetLoadError()
        {
            state = State.LoadError;
        }

        [MoonSharpHidden]
        public void SetLoadComplete()
        {
            state = State.LoadComplete;
        }

        [MoonSharpHidden]
        public void SetComplete()
        {
            state = State.Complete;
        }
        #endregion

        #region Game control
        // Available in states LoadComplete, Ongoing,
        // Paused, CompletePattern and Complete.
        public ScoreKeeper scoreKeeper => GameController.instance
            .scoreKeeper;
        // Available in states LoadComplete, Ongoing and Paused.
        public GameTimer timer => GameController.instance.timer;

        public void ActivateFever()
        {
            CheckState(State.Ongoing, "ActivateFever");
            GameController.instance.ActivateFever();
        }
        #endregion

        #region Background
        public void UpdateBgBrightness()
        {
            CheckState(
                new List<State>
                { State.Ongoing, State.Paused, State.Complete },
                "UpdateBgBrightness");
            GameController.instance.UpdateBgBrightness();
        }

        // It's up to the theme to wait 1 frame for layout to update.
        public void ResetElementSizes()
        {
            CheckState(
                new List<State>
                { State.Ongoing, State.Paused, State.Complete },
                "ResetElementSizes");
            GameController.instance.ResetElementSizes();
        }

        // Covers backing track and keysounds.
        public void StopAllGameAudio()
        {
            GameController.instance.StopAllGameAudio();
        }

        public void StopBga()
        {
            GameController.instance.StopBga();
        }
        #endregion

        #region Score and record
        public bool ScoreIsValid()
        {
            return GameController.instance.ScoreIsValid();
        }

        // Returns true if the current score is valid, AND it's
        // greater than the current record on the pattern,
        // if one exists.
        public bool ScoreIsNewRecord()
        {
            CheckState(State.Complete, "ScoreIsNewRecord");
            return GameController.instance.ScoreIsNewRecord();
        }

        // Updates the score and medal on the current pattern separately.
        // Does not save to disk; call Records.SaveToFile() to do that.
        public void UpdateRecord()
        {
            CheckState(State.Complete, "UpdateRecord");
            GameController.instance.UpdateRecord();
        }
        #endregion

        #region Practice mode APIs
        private void CheckPracticeMode(string methodName)
        {
            if (GameController.instance.modifiers.mode != 
                Modifiers.Mode.Practice)
            {
                throw new System.Exception($"{methodName} can only be called in practice mode.");
            }
        }

        // scan will be clamped into the pattern's bounds.
        public void JumpToScan(int scan)
        {
            CheckPracticeMode("JumpToScan");
            GameController.instance.JumpToScan(scan);
        }

        public void SetSpeed(int speedPercent)
        {
            CheckPracticeMode("SetSpeed");
            GameController.instance.SetSpeed(speedPercent);
        }

        public bool autoPlay
        {
            get => GameController.instance.autoPlay;
            set
            {
                CheckPracticeMode("autoPlay");
                GameController.instance.autoPlay = value;
                ResetElementSizes();
            }
        }

        public bool showHitbox
        {
            get => GameController.instance.showHitbox;
            set
            {
                CheckPracticeMode("showHitbox");
                GameController.instance.showHitbox = value;
            }
        }
        #endregion
    }
}