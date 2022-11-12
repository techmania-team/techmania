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
            // With each file loaded, fires onLoadProgress.
            // When loading fails, fires both onStateChange and
            // onLoadError; onLoadError will contain a Status.
            Loading,
            LoadError,
            // Theme can start the game now, which transitions to
            // Ongoing state.
            LoadComplete,
            // Transitions to Paused and Complete.
            Ongoing,
            // Transitions to Ongoing.
            Paused,
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

        #region Other theme APIs
        public void UpdateBgBrightness()
        {
            CheckState(
                new List<State>
                { State.Ongoing, State.Paused, State.Complete },
                "UpdateBgBrightness");
            GameController.instance.UpdateBgBrightness();
        }

        public void ResetElementSizes()
        {
            CheckState(
                new List<State>
                { State.Ongoing, State.Paused, State.Complete },
                "ResetSize");
            GameController.instance.ResetElementSizes();
        }

        public void ActivateFever()
        {
            CheckState(State.Ongoing, "ActivateFever");
            GameController.instance.ActivateFever();
        }

        public ScoreKeeper scoreKeeper => GameController.instance
            .scoreKeeper;
        #endregion
    }
}