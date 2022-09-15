using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    public class GameState
    {
        public enum State
        {
            // The only state that allows Lua to change GameSetup.
            // Theme should fill the fields in GameSetup and begin
            // loading.
            Pending,
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
            // Transitions to Pending state.
            Complete
        }
        public State state { get; private set; }

        public void BeginLoading()
        {
            // Pending => Loading
        }

        public void Begin()
        {
            // LoadComplete => Ongoing
        }

        public void Pause()
        {
            // Ongoing => Paused
        }

        public void Unpause()
        {
            // Paused => Ongoing
        }

        public void Conclude()
        {
            // Any state => Pending
        }

        [MoonSharpHidden]
        public void SetState(State newState)
        {
            State oldState = state;
            state = newState;
            if (oldState != newState)
            {
                Techmania.gameSetup.onStateChange?.Function.Call(
                    newState.ToString());
            }
        }

        public float feverValue;
        // TODO: make the Score class read-only to Lua.
        public Score score;
    }
}