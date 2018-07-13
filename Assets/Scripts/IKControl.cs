using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))] 

public class IKControl : MonoBehaviour 
{
    protected float weightLookCamera = 0.6f;
    protected float weightLookObject = 0.9f;
    protected float weightHandCopy = 1.0f;      // mirror as close as possible
    protected Animator animator;

    protected bool ikActive = true;
    public Transform lookObj = null;
    public Transform rightHandObj = null;
    public Transform leftHandObj = null;

    public Transform sentryA = null;
    public Transform sentryB = null;
    protected object tweenMovement = null;
    public string sentryState = "walk";
    public float intervalSentry = 4.0f;     //number of seconds for whole 

    void Awake () 
    {
        animator = GetComponent<Animator>();
    }

    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if(animator && ikActive && Camera.main!=null) 
        {
            Transform transTarget = lookObj;

            // Set the right hand target position and rotation, if one has been assigned
            if(rightHandObj != null) 
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weightHandCopy);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weightHandCopy);  
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandObj.rotation);
                //TODO: something smarter about which hand gets precedence?!
                transTarget = rightHandObj;
            }
            
            // Set the left hand target position and rotation, if one has been assigned
            if(leftHandObj != null) 
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weightHandCopy);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weightHandCopy);  
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
                transTarget = leftHandObj;
            }

            if(transTarget == null)     // if no specific look, try to look at the player
            {
                animator.SetLookAtWeight(weightLookCamera);
                animator.SetLookAtPosition(Camera.main.transform.position);
            }  
            else
            {
                animator.SetLookAtWeight(weightLookObject);
                animator.SetLookAtPosition(transTarget.position);
            } 

            // finally, execute our sentry routine (alternate between two points)
            if (sentryA!=null && sentryB!=null)
            {
                if (tweenMovement == null) 
                {
                    tweenMovement = TargetNextSentry();
                    animator.SetInteger("Speed", 1);
                }
            }
        }
    } 

    protected object TargetNextSentry()
    {
        float distA = Vector3.Distance(sentryA.position, transform.position);
        float distB = Vector3.Distance(sentryB.position, transform.position);
        float distTotal = Vector3.Distance(sentryB.position, sentryA.position);
        Transform targetNext;
        float intervalNext = 0f;

        if (distA > distB)
        {
            targetNext = sentryA;
            intervalNext = distA/distTotal*intervalSentry;
        }
        else
        {
            targetNext = sentryB;
            intervalNext = distB/distTotal*intervalSentry;
        }
        
        //turn the acting animator towards our new target
        transform.LookAt(targetNext);
        //start a tween of invisible object between animator and first sentry point
        object ltReturn = LeanTween.move(gameObject, targetNext.position, intervalNext).setOnComplete(
            () => { TargetNextSentry(); }
        );
        //Debug.Log(string.Format("[IKControl]: Turning to new target {0}", targetNext.name));
        return ltReturn;
    }


}

