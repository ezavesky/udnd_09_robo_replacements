using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))] 

public class IKControl : MonoBehaviour {
    
    protected Animator animator;
    
    protected bool ikActive = true;
    protected Transform rightHandObj = null;
    protected Transform lookObj = null;

    void Awake () 
    {
        animator = GetComponent<Animator>();
    }
    
    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if(animator && ikActive) 
        {
            if(lookObj == null)     // if no specific look, try to look at the player
            {
                animator.SetLookAtWeight(0.6f);
                animator.SetLookAtPosition(Camera.main.transform.position);
            }    
            /*
            // Set the right hand target position and rotation, if one has been assigned
            if(rightHandObj != null) {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand,1);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand,1);  
                animator.SetIKPosition(AvatarIKGoal.RightHand,rightHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand,rightHandObj.rotation);
            }        
            */
        }
        
    }    
}

