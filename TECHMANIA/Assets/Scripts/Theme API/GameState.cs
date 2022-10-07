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
            // Transitions to Paused and AllNotesResolved.
            Ongoing,
            // Transitions to Ongoing.
            Paused,
            // The game is complete, the score is available.
            // Transitions to Idle state.
            Complete
        }
        public State state { get; private set; }

        [MoonSharpHidden]
        public GameState()
        {
            state = State.Idle;
            GameController.instance.SetStateInstance(this);
        }

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
            // Any state => Idle
            state = State.Idle;
        }

        public void UpdateBgBrightness()
        {
            CheckState(
                new List<State> { State.Ongoing, State.Paused },
                "UpdateBgBrightness");
            GameController.instance.UpdateBgBrightness();
        }

        [MoonSharpHidden]
        public void SetState(State newState)
        {
            state = newState;
        }

        public float feverValue;
        // TODO: make the Score class read-only to Lua.
        public Score score;
    }
}