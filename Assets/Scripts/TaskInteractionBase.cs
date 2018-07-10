using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskInteractionBase : MonoBehaviour {
    protected IKControl ikControl;
    protected DialogTrigger dialogTrigger;

	// Use this for initialization
	public virtual void Awake () {
		ikControl = GetComponentInChildren<IKControl>();
        dialogTrigger = GetComponentInChildren<DialogTrigger>();

        // GameManager.instance.DialogTrigger(nameTrigger, typeTrigger);
        if (dialogTrigger)
        {
            dialogTrigger.OnTrigger.AddListener(ReceiveDialogTrigger);
        }
	}

	public virtual void Start() {
        // dO something?
    }

    virtual protected void ReceiveDialogTrigger(string nameTrigger, DialogTrigger.TRIGGER_TYPE typeTrigger)
    {
        //TODO: add something to interpret dialog trigger notifications?
        Debug.Log("IN PARENT!");
    }
}
