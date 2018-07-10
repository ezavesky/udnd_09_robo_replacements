using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskInteractionBase : MonoBehaviour {
    protected IKControl ikControl;
    protected DialogTrigger dialogTrigger;

	// Use this for initialization
	void Start () {
		ikControl = GetComponentInChildren<IKControl>();
        dialogTrigger = GetComponentInChildren<DialogTrigger>();

        // GameManager.instance.DialogTrigger(nameTrigger, typeTrigger);
	}
	
}
