using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner instance;
        private Dictionary<int, UnityEngine.Coroutine> coroutines;
        private int nextId;

        public void Start()
        {
            instance = this;
            coroutines = new Dictionary<int, UnityEngine.Coroutine>();
            nextId = 0;
        }

        public static int Start(
            MoonSharp.Interpreter.Coroutine luaCoroutine)
        {
            int id = instance.nextId;

            UnityEngine.Coroutine unityCoroutine =
                instance.StartCoroutine(
                    instance.RunLuaCoroutine(luaCoroutine, id));
            instance.coroutines[id] = unityCoroutine;
            instance.nextId++;

            return id;
        }

        public static void Stop(int id)
        {
            instance.StopCoroutine(instance.coroutines[id]);
            instance.coroutines.Remove(id);
        }

        private IEnumerator RunLuaCoroutine(
            MoonSharp.Interpreter.Coroutine c, int id)
        {
            while (c.State != CoroutineState.Dead)
            {
                c.Resume();
                yield return null;
            }
            coroutines.Remove(id);
        }
    }
}