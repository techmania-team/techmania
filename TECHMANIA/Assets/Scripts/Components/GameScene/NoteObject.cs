using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum Judgement
{
    RainbowMax,
    Max,
    Cool,
    Good,
    Miss,
    Break
}

public class NoteObject : MonoBehaviour
{
    [HideInInspector]
    public Note note;
    [HideInInspector]
    public string sound;

    private static List<List<KeyCode>> keysForLane;

    public static void InitializeKeysForLane()
    {
        keysForLane = new List<List<KeyCode>>();
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

    public static event UnityAction<NoteObject, Judgement> NoteResolved;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Game.Time > note.time + Game.kBreakThreshold)
        {
            NoteResolved?.Invoke(this, Judgement.Break);
        }

        switch (GameSetup.pattern.patternMetadata.controlScheme)
        {
            case ControlScheme.Touch:
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        Touch t = Input.GetTouch(i);
                        if (t.phase != TouchPhase.Began) continue;
                        if (RectTransformUtility.RectangleContainsScreenPoint(
                            GetComponent<RectTransform>(),
                            t.position))
                        {
                            OnHit();
                            break;
                        }
                    }
                }
                break;
            case ControlScheme.Keys:
                {
                    foreach (KeyCode k in keysForLane[note.lane])
                    {
                        if (Input.GetKeyDown(k))
                        {
                            OnHit();
                            break;
                        }
                    }
                }
                break;
            case ControlScheme.KM:
                {
                    if (Input.GetMouseButtonDown(0) &&
                        RectTransformUtility.RectangleContainsScreenPoint(
                            GetComponent<RectTransform>(),
                            Input.mousePosition))
                    {
                        OnHit();
                    }
                }
                break;
        }
    }

    private void OnHit()
    {
        float correctTime = note.time;
        float currentTime = Game.Time;
        float difference = Mathf.Abs(currentTime - correctTime);

        if (difference > Game.kBreakThreshold)
        {
            // Do nothing.
        }
        else if (difference <= Game.kRainbowMaxThreshold)
        {
            NoteResolved?.Invoke(this, Judgement.RainbowMax);
        }
        else if (difference <= Game.kMaxThreshold)
        {
            NoteResolved?.Invoke(this, Judgement.Max);
        }
        else if (difference <= Game.kCoolThreshold)
        {
            NoteResolved?.Invoke(this, Judgement.Cool);
        }
        else if (difference <= Game.kGoodThreshold)
        {
            NoteResolved?.Invoke(this, Judgement.Good);
        }
        else
        {
            NoteResolved?.Invoke(this, Judgement.Miss);
        }
    }
}
