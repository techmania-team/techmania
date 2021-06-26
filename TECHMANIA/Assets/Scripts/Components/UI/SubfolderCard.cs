using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SubfolderCard : MonoBehaviour
{
    public EyecatchSelfLoader eyecatch;
    public TextMeshProUGUI folderName;
    
    // Eyecatch path may be null.
    public void Initialize(string name, string eyecatchFullPath)
    {
        eyecatch.LoadImage(eyecatchFullPath);
        folderName.text = name;
    }
}
