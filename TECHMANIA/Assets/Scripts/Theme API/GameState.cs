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
        public State stateEnum { get; private set; }
        public string state => stateEnum.ToString();

        [MoonSharpHidden]
        public GameState()
        {
            stateEnum = State.Idle;
            GameController.instance.SetStateInstance(this);
        }

        private void CheckState(State expectedState, string methodName)
        {
            if (stateEnum == expectedState) return;
            throw new System.Exception($"{methodName} expects {expectedState} state, but the current state is {stateEnum}.");
        }

        public void BeginLoading()
        {
            CheckState(State.Idle, "BeginLoading");
            stateEnum = State.Loading;
            GameController.instance.BeginLoading();
        }

        public void Begin()
        {
            CheckState(State.LoadComplete, "Begin");
            stateEnum = State.Ongoing;
            GameController.instance.Begin();
        }

        public void Pause()
        {
            CheckState(State.Ongoing, "Pause");
            stateEnum = State.Paused;
            GameController.instance.Pause();
        }

        public void Unpause()
        {
            CheckState(State.Paused, "Unpause");
            stateEnum = State.Ongoing;
            GameController.instance.Unpause();
        }

        public void Conclude()
        {
            // Any state => Idle
            stateEnum = State.Idle;
        }

        [MoonSharpHidden]
        public void SetState(State newState)
        {
            stateEnum = newState;
        }

        public float feverValue;
        // TODO: make the Score class read-only to Lua.
        public Score score;
    }
}