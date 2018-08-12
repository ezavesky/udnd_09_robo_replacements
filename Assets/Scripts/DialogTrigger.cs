using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRTK;

public class DialogTrigger : MonoBehaviour {
	public AudioClip soundHit = null;
    public string nameTrigger; //name of trigger
    protected bool triggerStay = false;
    public bool onlyPlayerTrigger = true;
    public enum TRIGGER_TYPE { TRIGGER_STAY, TRIGGER_EXIT, TRIGGER_ENTER };
    protected TRIGGER_TYPE typeLast = TRIGGER_TYPE.TRIGGER_EXIT;

    [System.Serializable]
    public sealed class DialogTriggerEvent : UnityEvent<string, TRIGGER_TYPE>{};
    [SerializeField]
    public DialogTriggerEvent OnTrigger = new DialogTriggerEvent();

    public bool IsUserEngaged() 
    {
        return typeLast != TRIGGER_TYPE.TRIGGER_EXIT;
    }

	protected void PlayClip(AudioSource audioSrc, AudioClip audioClip) {
		if (audioClip==null || audioSrc==null) {
			return;
		}
		//Valve.VR.InteractionSystem.Player.instance.audioListener	
		audioSrc.PlayOneShot(audioClip);
	}

	protected virtual void OnHit(AudioSource audioSrc, GameObject objOther, TRIGGER_TYPE typeTrigger=TRIGGER_TYPE.TRIGGER_ENTER) {
        if (onlyPlayerTrigger)
        {
            VRTK_PlayerObject apiPlayer = objOther.GetComponent<VRTK_PlayerObject>();
            if (apiPlayer == null || apiPlayer.transform.childCount!=1)      //only check for player intersects
            {
                return;
            }
        }

		if (soundHit != null && audioSrc != null) {
            PlayClip(audioSrc, soundHit);
        }
        typeLast = typeTrigger;
        GameManager.instance.DialogTrigger(nameTrigger, typeTrigger, null, true);
        OnTrigger.Invoke(nameTrigger, typeTrigger);
	}

	protected void OnCollisionEnter(Collision other)
    {
        OnTriggerEnter(other.collider);
	}

    protected void OnCollisionExit(Collision other)
    {
        OnTriggerExit(other.collider);
    }

    protected void OnCollisionStay(Collision other)
    {
        OnTriggerStay(other.collider);
    }

	protected void OnTriggerStay(Collider other)
    {
        if (triggerStay) 
        {
            AudioSource audioSource = other.gameObject.GetComponent<AudioSource>();
            OnHit(audioSource, other.gameObject, TRIGGER_TYPE.TRIGGER_STAY);
        }
	}

    protected void OnTriggerEnter(Collider other)
    {
		AudioSource audioSource = other.gameObject.GetComponent<AudioSource>();
        OnHit(audioSource, other.gameObject, TRIGGER_TYPE.TRIGGER_ENTER);
  	}

    protected void OnTriggerExit(Collider other)
    {
		AudioSource audioSource = other.gameObject.GetComponent<AudioSource>();
        OnHit(audioSource, other.gameObject, TRIGGER_TYPE.TRIGGER_EXIT);
  	}
	


}
