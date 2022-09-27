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
            // Transitions to LoadError or LoadComplete. Fires
            // onLoadProgress with each file loaded; fires
            // onLoadError when load fails.
            Loading,
            LoadError,
            // Theme can start the game now, which transitions to
            // Ongoing state.
            LoadComplete,
            // Transitions to Paused and AllNotesResolved.
            Ongoing,
            // Transitions to Ongoing.
            Paused,
            // All notes are resolved but some audio or video may
            // still be playing. Score is available. Transitions to
            // Complete with time.
            AllNotesResolved,
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
        }

        private void CheckState(State expectedState, string methodName)
        {
            if (stateEnum == expectedState) return;
            throw new System.Exception($"{methodName} expects {expectedState} state, but the current state is {stateEnum}.");
        }

        public void BeginLoading()
        {
            CheckState(State.Idle, "BeginLoading");
            Debug.Log("BeginLoading called. Track folder: " +
                Techmania.instance.gameSetup.trackFolder +
                " pattern GUID: " +
                Techmania.instance.gameSetup.patternGuid);
            // Idle => Loading
            // Lock down the track and pattern so theme can't change
            // them later.
        }

        public void Begin()
        {
            CheckState(State.LoadComplete, "Begin");
            // LoadComplete => Ongoing
        }

        public void Pause()
        {
            CheckState(State.Ongoing, "Pause");
            // Ongoing => Paused
        }

        public void Unpause()
        {
            CheckState(State.Paused, "Unpause");
            // Paused => Ongoing
        }

        public void Conclude()
        {
            // Any state => Pending
        }

        [MoonSharpHidden]
        public void SetState(State newState)
        {
            State oldState = stateEnum;
            stateEnum = newState;
            if (oldState != newState)
            {
                Techmania.instance.gameSetup.onStateChange?
                    .Function.Call(newState.ToString());
            }
        }

        public float feverValue;
        // TODO: make the Score class read-only to Lua.
        public Score score;
    }
}