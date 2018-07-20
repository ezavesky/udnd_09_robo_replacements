using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskSeeing : TaskInteractionBase {
    public UnityEngine.UI.Text textSystem;
    protected List<string> listLines = new List<string>();
    protected const int maxLines = 17;

    // TODO : specific for seeing

    // method for story instruction

    // method for clearing board & resetting pieces

    // method for loading board

    // method for gatering objects on the board

    // method for classification of task as it is completed

    protected void AppendSystem(string strNew)
    {
        if (string.IsNullOrEmpty(strNew) || textSystem == null)
        {
            return;
        }
        if (listLines.Count >= maxLines) 
        {
            listLines.RemoveAt(listLines.Count-1);
        }
        listLines.Insert(0, strNew);
        textSystem.text = string.Join("\n", listLines.ToArray());
    }


    // methods receieving events from external callers

    override protected void ReceiveDialogTrigger(string nameTrigger, DialogTrigger.TRIGGER_TYPE typeTrigger)
    {
        string strEcho = null;
        if (typeTrigger != DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT)     //enter -> start
        {
            strEcho = string.Format("[TaskSeeing]: Trigger '{0}' type {1}", nameTrigger, typeTrigger);
            Debug.Log(strEcho);
        }
        else    // exit -> reset
        {
            strEcho = string.Format("[TaskSeeing]: Trigger '{0}' type {1}", nameTrigger, typeTrigger);
            Debug.Log(string.Format("[TaskSeeing]: Trigger '{0}' type {1}", nameTrigger, typeTrigger));
        }
        AppendSystem(strEcho);
    }

    public void ReceiveButtonRelease(object o, string buttonName) 
    {
        // do anything?        
        string strEcho = string.Format("[TaskSeeing]: {0} RELEASE but no state information! ", buttonName);
        Debug.LogWarning(strEcho);
        AppendSystem(strEcho);
    }


}
