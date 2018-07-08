using UnityEngine;
using System.Collections.Generic;
using VRTK;

public class TeleportObjToggle : MonoBehaviour
{
    protected List<VRTK_DestinationPoint> listTeleporters = new List<VRTK_DestinationPoint>();
    public AudioClip clipStart = null;
    public AudioClip clipStop = null;

    public bool RediscoverTeleporters(GameObject objParent) 
    {
        VRTK_DestinationPoint[] addObjs = objParent.GetComponentsInChildren<VRTK_DestinationPoint>(true);  //  .FindGameObjectsWithTag(retoggleTags[i]);
        if ((addObjs == null) || (addObjs.Length == 0)) 
        {
            return false;
        }
        listTeleporters.Clear();
        listTeleporters.AddRange(addObjs);
        ToggleObjects(false);
        return true;
    }

    public virtual void ToggleObjects(bool newState)
    {
        foreach (VRTK_DestinationPoint objTeleport in listTeleporters)
        {
            if (objTeleport != null)
            {
                objTeleport.gameObject.SetActive(newState);
            }
        }
        if (newState) {
            if (clipStart) //play the start sound
            {
                AudioSource.PlayClipAtPoint(clipStart, Camera.main.transform.position);
            }
            //TODO: ? start playing back a 'pending' loop
        }
        else {
            if (clipStop) //play the stop sound
            {
                AudioSource.PlayClipAtPoint(clipStop, Camera.main.transform.position);
            }
        }
    }

    
}
