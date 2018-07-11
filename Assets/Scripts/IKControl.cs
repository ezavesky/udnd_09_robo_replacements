using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))] 

public class IKControl : MonoBehaviour {
    protected float weightLookCamera = 0.6f;
    protected float weightLookObject = 0.9f;
    protected float weightHandCopy = 1.0f;      // mirror as close as possible
    protected Animator animator;

    protected bool ikActive = true;
    protected Transform rightHandObj = null;
    protected Transform leftHandObj = null;
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
                animator.SetLookAtWeight(weightLookCamera);
                animator.SetLookAtPosition(Camera.main.transform.position);
            }  
            else
            {
                animator.SetLookAtWeight(weightLookObject);
                animator.SetLookAtPosition(lookObj.position);
            }  

            // Set the right hand target position and rotation, if one has been assigned
            if(rightHandObj != null) 
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weightHandCopy);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weightHandCopy);  
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandObj.rotation);
            }        

            // Set the right hand target position and rotation, if one has been assigned
            if(leftHandObj != null) 
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weightHandCopy);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weightHandCopy);  
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
            }        

        }
        
    }    
}

