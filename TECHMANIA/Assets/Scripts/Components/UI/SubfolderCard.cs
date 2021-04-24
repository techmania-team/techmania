using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SubfolderCard : MonoBehaviour
{
    public TextMeshProUGUI folderName;
    
    public void Initialize(string name)
    {
        folderName.text = name;
    }
}
