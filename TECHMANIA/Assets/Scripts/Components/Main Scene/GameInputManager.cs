using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInputManager
{
    private Pattern pattern;
    private ControlScheme scheme;
    private int lanes;

    private NoteManager noteManager;

    private List<List<KeyCode>> keysForLane;

    public GameInputManager(
        Pattern pattern,
        NoteManager noteManager)
    {
        this.pattern = pattern;
        scheme = pattern.patternMetadata.controlScheme;
        lanes = pattern.patternMetadata.playableLanes;

        this.noteManager = noteManager;
    }

    public void Prepare()
    {
        // Prepare keycodes for keyboard input.
        keysForLane = new List<List<KeyCode>>();
        if (lanes >= 4)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.BackQuote,
                KeyCode.Alpha0,
                KeyCode.Alpha1,
                KeyCode.Alpha2,
                KeyCode.Alpha3,
                KeyCode.Alpha4,
                KeyCode.Alpha5,
                KeyCode.Alpha6,
                KeyCode.Alpha7,
                KeyCode.Alpha8,
                KeyCode.Alpha9,
                KeyCode.Minus,
                KeyCode.Equals,
                KeyCode.KeypadDivide,
                KeyCode.KeypadMultiply,
                KeyCode.KeypadMinus
            });
        }
        if (lanes >= 3)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.Q,
                KeyCode.W,
                KeyCode.E,
                KeyCode.R,
                KeyCode.T,
                KeyCode.Y,
                KeyCode.U,
                KeyCode.I,
                KeyCode.O,
                KeyCode.P,
                KeyCode.LeftBracket,
                KeyCode.RightBracket,
                KeyCode.Backslash,
                KeyCode.Keypad7,
                KeyCode.Keypad8,
                KeyCode.Keypad9
            });
        }
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.Semicolon,
            KeyCode.Quote,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6
        });
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.M,
            KeyCode.Comma,
            KeyCode.Period,
            KeyCode.Slash,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3
        });
    }

    public void Update(int scan)
    {

    }

    public void Dispose()
    {

    }
}
