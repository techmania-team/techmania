using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionToScene : MonoBehaviour
{
    public string target;

    public void Invoke()
    {
        Curtain.DrawCurtainThenGoToScene(target);
    }
}
