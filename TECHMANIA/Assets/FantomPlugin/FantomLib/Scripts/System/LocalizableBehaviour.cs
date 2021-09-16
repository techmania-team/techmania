using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// For apply localize string by specific language
    /// Because to be able to register in the inspector (LocalizeLanguageChanger), 
    /// and use 'FindObjectsOfType()' method,
    /// make it an abstract class instead of an interface.
    /// </summary>
    public abstract class LocalizableBehaviour : MonoBehaviour
    {
        virtual public void ApplyLocalize(SystemLanguage language) { }
    }
}
