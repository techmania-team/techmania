using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static HashSet<MoonSharp.Interpreter.Coroutine> 
            luaCoroutines;

        public static void Prepare()
        {
            luaCoroutines = new 
                HashSet<MoonSharp.Interpreter.Coroutine>();
        }

        public static void Add(MoonSharp.Interpreter.Coroutine c)
        {
            luaCoroutines.Add(c);
        }

        // Update is called once per frame
        void Update()
        {
            if (luaCoroutines == null) return;

            bool anyCoroutineFinished = false;
            foreach (var c in luaCoroutines)
            {
                if (c.State == CoroutineState.Dead)
                {
                    anyCoroutineFinished = true;
                }
                else
                {
                    c.Resume();  // Run until the next yield
                }
            }

            if (anyCoroutineFinished)
            {
                HashSet<MoonSharp.Interpreter.Coroutine> remaining = 
                    new HashSet<MoonSharp.Interpreter.Coroutine>();
                foreach (var c in luaCoroutines)
                {
                    if (c.State != CoroutineState.Dead)
                    {
                        remaining.Add(c);
                    }
                }
                luaCoroutines = remaining;
            }
        }
    }
}