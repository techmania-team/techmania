using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner instance;

        public void Start()
        {
            instance = this;
        }

        public static void Add(MoonSharp.Interpreter.Coroutine c)
        {
            instance.StartCoroutine(instance.RunLuaCoroutine(c));
        }

        private IEnumerator RunLuaCoroutine(
            MoonSharp.Interpreter.Coroutine c)
        {
            while (c.State != CoroutineState.Dead)
            {
                c.Resume();
                yield return null;
            }
        }
    }
}