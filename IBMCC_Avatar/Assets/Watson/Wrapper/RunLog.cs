using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RunLog : MonoBehaviour
{
    [SerializeField]
    private string myText;

    [SerializeField]
    private Color myColour;

    [SerializeField]
     private TextLogControl logControl;

    public void LogText()
    {
        logControl.LogText(myText, myColour);
    }
}