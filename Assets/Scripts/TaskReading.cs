using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskReading : TaskInteractionBase {
    public ButtonController buttonStart;
    public ButtonController buttonA;
    public ButtonController buttonB;
    public ButtonController buttonC;

    public Text textQuestion;
    public Text textMedia;
    public Text textJokes;
    public Image imageMedia;
    
    // TODO : specific for reading

    // method to start story

    // method to offer options

    // method to replay story with options

    // method to update screen with example graphics

    // method to update story with spoken text

    // database for reading/substituting story?

    public void ReceiveButtonPress(object o, string buttonName) 
    {
        // do anything?
        Debug.Log(string.Format("[TaskReading]: {0} PRESS {1}", buttonName, o));
    }

    public void ReceiveButtonRelease(object o, string buttonName) 
    {
        // do anything?        
        Debug.Log(string.Format("[TaskReading]: {0} RELEASE {1}", buttonName, o));
    }

}
