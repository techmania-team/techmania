using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class ThemeL10n
    {
        private VisualElement root;
        [MoonSharpHidden]
        public ThemeL10n(VisualElement root)
        {
            this.root = root;
        }
        
        public void Initialize(string stringTable)
        {
            L10n.Initialize(stringTable, L10n.Instance.Theme);
        }

        public void ApplyLocale(string locale)
        {
            L10n.SetLocale(locale, L10n.Instance.Theme);
            foreach (KeyValuePair<string, string> s in
                L10n.themeInstance.current.strings)
            {
                if (!s.Key.Contains('#')) continue;

                VisualElement element = root;
                foreach (string name in s.Key.Split(' '))
                {
                    string nameWithoutPound = name.TrimStart('#');
                    element = element.Q(nameWithoutPound);
                    if (element == null)
                    {
                        Debug.LogError($"Element at l10n key '{s.Key}' is not found; unable to apply l10n value.");
                        break;
                    }
                }
                if (element == null) continue;
                if (!(element is TextElement))
                {
                    Debug.LogError($"Element at l10n key '{s.Key}' is not a TextElement; unable to apply l10n value.");
                    continue;
                }
                (element as TextElement).text = s.Value;
            }
        }

        public string GetString(string key)
        {
            return L10n.GetString(key, L10n.Instance.Theme);
        }
    }
}
