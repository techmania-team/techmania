using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// For get the state of the saved checkbox (SavedChecked property).
    /// Because to be able to register in the inspector (SavedCheckedSwitcher), 
    /// make it an abstract class instead of an interface.
    /// </summary>
    public abstract class SavedCheckedBehaviour : LocalizableBehaviour
    {
        virtual public bool SavedChecked { get { return false; } }
    }
}
