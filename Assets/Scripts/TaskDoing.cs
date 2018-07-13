using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

public class TaskDoing : TaskInteractionBase {
    public ButtonController buttonA;
    public ButtonController buttonB;
    
    public Text textLeaderBoard;
    public Text textHelp;
    public Text textTimeLeft;
    public Text textAccuracy;
    protected float intervalScoreboard = 0.5f;
    protected float timeGame = 120.0f;
    protected float timeRemain = 0.0f;
    protected float matchScore = 0.0f;
    protected float biasDistance = 1.0f;    //closer to 1 is distance, closer to 0 is rotation-based

    public Transform rootPlateReference;
    public Transform rootPlatePlayer;
    public Transform rootPlateRobot;
    public Transform rootResourcesPlayer;
    public Transform rootResourcesRobot;

    public GameObject objFoodSource;
    protected List<GameObject> listFoods = new List<GameObject>();
    public GameObject rootAnimationFreeze = null;

    protected GameObject objLeftGrab = null;
    protected GameObject objRightGrab = null;
    
    protected int idxPrefabActive = 0;
    protected Dictionary<int, GameObject> dictObjectRobot = new Dictionary<int, GameObject>();
    protected Dictionary<int, GameObject> dictObjectReference = new Dictionary<int, GameObject>();
    protected Dictionary<int, List<GameObject>> dictPrefabSets = new Dictionary<int, List<GameObject>>();

    protected enum TASK_STATE { STATE_RESET, STATE_START, STATE_START_NEXT, STATE_GAME_START, STATE_PIECE, STATE_FINISH };
    protected Dictionary<string, TASK_STATE> dictNextState = new Dictionary<string, TASK_STATE>();
    protected TASK_STATE stateLast;


    // main flow for program
    //      enable prefab objct to copy - STATE_START
    //              Next -> cycle to next prfab - STATE_START_NEXT
    //              Go -> proceed for this prefab
    //          start the clock, animate pieces for user - STATE_GAME_START
    //          user grabs one part at a time - STATE_PIECE
    //              robot copies the object stacking - 
    //              user can look at cooking views of robot
    //              compute similarity between objects by square distance difference
    //                  present matching score for entire presentation
    //          user determines object is finished - STATE_FINISH

    public override void Awake() 
    {
        base.Awake();
        ClonePrefabSets();
    }

    public override void Start()
    {
        SetTaskState(TASK_STATE.STATE_RESET);
    }

    protected void SetTaskState(TASK_STATE stateNew)
    {
        //init buttons to just show start
        switch(stateNew) 
        {
            default:
                Debug.LogError(string.Format("[TaskReading]: Life-systems restarting! State {0} received, but unexpected!", stateNew));
                SetTaskState(TASK_STATE.STATE_START_NEXT);
                return;

            case TASK_STATE.STATE_RESET:
                buttonA.buttonEnabled = buttonB.buttonEnabled = false;
                textAccuracy.text = "";
                textTimeLeft.text = "";
                Invoke("FreezeDisplay", 0.5f);     // delay for camera textures to grab a snapshot
                dictNextState.Clear();
                break;

            case TASK_STATE.STATE_START_NEXT:
                idxPrefabActive = (idxPrefabActive + 1) % listFoods.Count;
                goto case TASK_STATE.STATE_START;    //fall through

            case TASK_STATE.STATE_START:
                listFoods[idxPrefabActive].SetActive(true);
                ChildrenSetActive(rootAnimationFreeze.transform, true);     //activate cameras, lazy susan
                buttonA.buttonName = "Next";
                dictNextState[buttonA.buttonName] = TASK_STATE.STATE_START_NEXT;
                buttonB.buttonName = "Go";
                dictNextState[buttonB.buttonName] = TASK_STATE.STATE_GAME_START;
                buttonA.buttonEnabled = buttonB.buttonEnabled = true;
                textHelp.text = GameManager.instance.DialogTrigger("doing_intro", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                break;                

            case TASK_STATE.STATE_GAME_START:
                ActivatePrefabSet(idxPrefabActive, PREFAB_STATE.ACTIVE);
                buttonB.buttonName = "Done!";
                dictNextState[buttonB.buttonName] = TASK_STATE.STATE_FINISH;
                buttonA.buttonEnabled = false;
                buttonB.buttonEnabled = true;
                timeRemain = timeGame;
                textHelp.text = GameManager.instance.DialogTrigger("doing_game", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                Invoke("StabalizePrefabs", 1.0f);     // delay for correct op
                break;                

            case TASK_STATE.STATE_FINISH:
                buttonB.buttonName = "Restart";
                dictNextState[buttonB.buttonName] = TASK_STATE.STATE_START;
                buttonA.buttonEnabled = false;
                buttonB.buttonEnabled = true;
                
                // TODO: log the time and other info to leaderboard?
                
                textHelp.text = GameManager.instance.DialogTrigger("doing_game", DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT);
                break;

        }
        stateLast = stateNew;
    }

    protected void ChildrenSetActive(Transform transParent, bool newState)
    {
        for (int i=0; i<transParent.childCount; i++)
        {
            transParent.GetChild(i).gameObject.SetActive(newState);
        }
    }
    protected enum PREFAB_STATE { INACTIVE, ACTIVE, SETTLE };

    protected void ActivatePrefabSet(int idxPrefab, PREFAB_STATE stateNew=PREFAB_STATE.INACTIVE)
    {
        Rigidbody rb = null;
        if (idxPrefab == -1)    //helper mode to take action for all prefabs
        {
            for (int i=0; i<listFoods.Count; i++)
            {
                ActivatePrefabSet(i, stateNew);
            }
            return;
        }
        if (idxPrefab >= listFoods.Count)  //check bounds
            return;

        listFoods[idxPrefab].SetActive(stateNew!=PREFAB_STATE.INACTIVE);    //activate/deactivate main display
        foreach (GameObject objClone in dictPrefabSets[idxPrefab])  //disable all objects for this prefab
        {
            if (stateNew==PREFAB_STATE.INACTIVE)
            {
                objClone.SetActive(false);
            }
            else    //activating, so we need to check if player or robot 
            {
                if (dictObjectRobot.ContainsKey(objClone.GetInstanceID()))       //has instance match, it's player object
                {
                    rb = objClone.GetComponent<Rigidbody>();
                    rb.isKinematic = (stateNew!=PREFAB_STATE.ACTIVE);                        
                    if (stateNew==PREFAB_STATE.ACTIVE)
                    {
                        objClone.transform.position = rootResourcesPlayer.position;
                    }
                }
                else
                {
                    rb = objClone.GetComponent<Rigidbody>();
                    rb.isKinematic = (stateNew!=PREFAB_STATE.ACTIVE);                        
                    if (stateNew==PREFAB_STATE.ACTIVE)
                    {
                        objClone.transform.position = rootResourcesRobot.position;
                    }
                }
                objClone.SetActive(true);   // let object fall into basket from anchor
            }
        }
    } 

    protected void FreezeDisplay() 
    {
        //method for freezing display after reset or after camera textures have grabbed example
        ActivatePrefabSet(-1, PREFAB_STATE.INACTIVE);
        ChildrenSetActive(rootAnimationFreeze.transform, false);    //deactivate cameras, lazy susan
    }

    protected void StabalizePrefabs() 
    {
        //annoying but necessary step to make things not jump around as much after gravity-based fall
        ActivatePrefabSet(idxPrefabActive, PREFAB_STATE.SETTLE);
    }

    protected void ClonePrefabSets() 
    {
        //tasks: clone sets of prefab examples into two pairs: player and robobt ones
        //       create a nested object under this one to contain all sets
        GameObject cloneRoot = new GameObject();
        cloneRoot.name = "_cloneRoot";
        cloneRoot.transform.parent = transform;
        //TODO: clobber this cloneroot, too?
        listFoods.Clear();

        GameObject objSource = null;
        GameObject objFoodBase = null;
        GameObject[] newObjs = new GameObject[2];
        Rigidbody rb = null;
        VRTK.VRTK_InteractableObject apiInteract;
        int i, j, k;

        for (i=0; i<objFoodSource.transform.GetChildCount(); i++) 
        {
            objFoodBase = objFoodSource.transform.GetChild(i).gameObject;
            listFoods.Add(objFoodBase);
            dictPrefabSets[i] = new List<GameObject>();
            for (j=0; j<objFoodBase.transform.childCount; j++)
            {
                objSource = objFoodBase.transform.GetChild(j).gameObject;
                rb = objSource.GetComponent<Rigidbody>();
                if (rb) 
                {
                    rb.isKinematic = true;
                }

                for (k=0; k<2; k++)
                {
                    newObjs[k] = Instantiate(objSource);
                    newObjs[k].SetActive(false);
                    newObjs[k].transform.parent = cloneRoot.transform;
                    newObjs[k].isStatic = false;
                    rb = newObjs[k].GetComponent<Rigidbody>();
                    if (rb) 
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    dictPrefabSets[i].Add(newObjs[k]);     //save in list for easy retrieval
                }

                // only on the object htat the user is touching (item 0)
                newObjs[0].AddComponent(typeof(VRTK.VRTK_InteractableObject));
                apiInteract = newObjs[0].GetComponent<VRTK_InteractableObject>();
                if (apiInteract)
                {
                    apiInteract.isGrabbable = true;
                    apiInteract.InteractableObjectGrabbed += new InteractableObjectEventHandler(OnInteractableObjectGrabbed);
                    apiInteract.InteractableObjectUngrabbed += new InteractableObjectEventHandler(OnInteractableObjectUnGrabbed);
                }

                dictObjectReference[objSource.GetInstanceID()] = newObjs[0];   //establish link to reference object
                dictObjectRobot[newObjs[0].GetInstanceID()] = newObjs[1];   //establish link to robot object
            }
            objFoodBase.SetActive(false);
        }
    }

    // method to switch to tracking food object in robot's hands
    protected void UpdateMirroredPosition(GameObject objTarget) 
    {
        // find reference to the grabbed object and move it
        if (objTarget==null || !dictObjectRobot.ContainsKey(objTarget.GetInstanceID()))
        {
            return;
        }
        // at this point, robot should be tracking this object, so we don't need to add it again...
        GameObject objTracking = dictObjectRobot[objTarget.GetInstanceID()];
        
        // just smoothly transition the object to its new position...
        //      move point out of reference from 
        // Vector3 ptNew = objTracking.transform.InverseTransformPoint(
        //     objTracking.transform.TransformPoint(objTracking.transform.localPosition)
        //     - rootPlateRobot.transform.TransformPoint(rootPlateRobot.transform.localPosition)
        //     + (objTarget.transform.TransformPoint(objTarget.transform.localPosition)
        //     - rootPlatePlayer.transform.TransformPoint(rootPlatePlayer.transform.localPosition)) );
        // objTracking.transform.localPosition = ptNew;

        //METHOD 1: it offsets to the right place, but the X/Y and X/Z directions aren't right because
        //          the robot's table is rotated
        Vector3 ptNew = rootPlateRobot.transform.position
                             + (objTarget.transform.position - rootPlatePlayer.transform.position);

        // METHOD 2: comptue the offset, but sneak in as local position; rotate transform then retrieve new position
        // Transform transTurn = rootPlatePlayer.transform;
        // transTurn.localPosition = (objTarget.transform.position - rootPlatePlayer.transform.position);
        // transTurn.eulerAngles = rootPlateRobot.eulerAngles;
        // Vector3 ptNew = transTurn.localPosition + rootPlateRobot.position;

        Debug.Log(string.Format("[TaskDoing]: Move from {0} to new {1}; source {2}", objTracking.transform.position, ptNew, objTarget.transform.position));
        objTracking.transform.position = ptNew;

        // ptRef = objRef.transform.TransformDirection(objRef.transform.eulerAngles)
        //         - rootPlatePlayer.transform.TransformDirection(rootPlateReference.transform.eulerAngles);

        // update equivalent robot hand to track along with look
        // move object around in space, updating robot 

    }

    protected float ComputeMatch(float biasDistance = 1.0f)  
    {
        //method to compute distance and rotation from reference/user items
        float userDist = 0f;
        float userAngle = 0f;
        float localDist = 0f;
        GameObject objRef = null;
        GameObject objCompare = null;
        Vector3 ptPlayer, ptRef;

        //foreach item in the reference
        for (int i=0; i < listFoods[idxPrefabActive].transform.GetChildCount(); i++)
        {
            objRef = listFoods[idxPrefabActive].transform.GetChild(i).gameObject;
            if (!dictObjectReference.ContainsKey(objRef.GetInstanceID()))
            {
                Debug.LogWarning(string.Format("[TaskDoing]: Warning, reference object {0} (id {1}) not found in compare hierarchy.", objRef.name, objRef.GetInstanceID()));
            }
            else {
                objCompare = dictObjectReference[objRef.GetInstanceID()];
                
                //  compute the position (plate normalized) differences
                ptRef = objRef.transform.position - rootPlateReference.transform.position;
                ptPlayer = objCompare.transform.position - rootPlatePlayer.transform.position;
                localDist = Vector3.Distance(ptPlayer, ptRef);
                // Debug.Log(string.Format("[TaskDoing]: PTS {3} @ {0} - REF {1}, PLAYER {2}", objRef.GetInstanceID(), ptRef, ptPlayer, localDist));
                userDist += localDist;

                //  compute the angular differences
                // ptRef = objRef.transform.TransformDirection(objRef.transform.localEulerAngles)
                //         - rootPlatePlayer.transform.TransformDirection(rootPlateReference.transform.localEulerAngles);
                // ptPlayer = objCompare.transform.TransformDirection(objCompare.transform.localEulerAngles)
                //         - rootPlateReference.transform.TransformDirection(rootPlateReference.transform.localEulerAngles);
                // Debug.Log(string.Format("[TaskDoing]: ANG {3} @ {0} - REF {1}, PLAYER {2}", objRef.GetInstanceID(), ptRef, ptPlayer, localDist));
                userAngle += Vector3.Distance(ptPlayer, ptRef);
            }
        }

        //return the weighted total of user components
        float distCombined = (userDist * biasDistance) + (userAngle * (1-biasDistance));

        // TODO: some normalization scheme to get to 0-1 position
        return distCombined;
    }


    //every so often update joke panel with new text
    IEnumerator ScoreboardUpdate()
    {
        while (true)
        {
            if (stateLast == TASK_STATE.STATE_GAME_START)       // proceed to update time remaining and score
            {
                textTimeLeft.text = string.Format("{0:F1} sec", timeRemain);
                //timeRemain -= intervalScoreboard; //(Time.fixedTime-timeLast);  //so what, it's nto time accurate
                
                // compute similarity score within local variable
                matchScore = ComputeMatch(biasDistance);
                textAccuracy.text = string.Format("{0:F1} %", matchScore*100.0f);
                
                // if there is a right or left object available, track it
                if (objLeftGrab)
                {
                    UpdateMirroredPosition(objLeftGrab);
                }
                if (objRightGrab)
                {
                    UpdateMirroredPosition(objLeftGrab);
                }

                if (timeRemain <= 0) 
                {
                    timeRemain = 0.0f;
                    SetTaskState(TASK_STATE.STATE_FINISH);
                }
            }
            else {
                textTimeLeft.text = "Game Over";
            }
            // Yield execution of this coroutine and return to the main loop until next frame
            yield return new WaitForSeconds(intervalScoreboard);
        }
    }

    // methods receieving events from external callers

    override protected void ReceiveDialogTrigger(string nameTrigger, DialogTrigger.TRIGGER_TYPE typeTrigger)
    {
        if (typeTrigger != DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT)     //enter -> start
        {
            SetTaskState(TASK_STATE.STATE_START);
            StartCoroutine("ScoreboardUpdate");
        }
        else    // exit -> reset
        {
            SetTaskState(TASK_STATE.STATE_RESET);
            StopCoroutine("ScoreboardUpdate");
        }
    }

    public void ReceiveButtonRelease(object o, string buttonName) 
    {
        if (dictNextState.ContainsKey(buttonName))
        {
            TASK_STATE stateNew = dictNextState[buttonName];
            SetTaskState(dictNextState[buttonName]);
        }
        else 
        {
            // do anything?        
            Debug.LogWarning(string.Format("[TaskDoing]: {0} RELEASE but no state information! (last state: {1})", buttonName, stateLast));
        }
    }

    protected void OnInteractableObjectGrabbed(object o, InteractableObjectEventArgs args)
    {
        VRTK_InteractableObject api = (VRTK_InteractableObject)o;

        if (dictObjectRobot.ContainsKey(api.gameObject.GetInstanceID())) 
        {
            GameObject objCompare = dictObjectRobot[api.gameObject.GetInstanceID()];
            if (VRTK_DeviceFinder.IsControllerLeftHand(args.interactingObject))
            {
                if (ikControl)
                {
                    ikControl.leftHandObj = objCompare.transform;
                    objLeftGrab = api.gameObject;
                }
            }
            else if (VRTK_DeviceFinder.IsControllerRightHand(args.interactingObject))
            {
                if (ikControl)
                {
                    ikControl.rightHandObj = objCompare.transform;
                    objRightGrab = api.gameObject;
                }
            }
        }
        // Debug.Log(string.Format("[TaskDoing]: Grabbed {0} ", api.gameObject.name));
        UpdateMirroredPosition(api.gameObject);
    }
    protected void OnInteractableObjectUnGrabbed(object o, InteractableObjectEventArgs args)
    {
        VRTK_InteractableObject api = (VRTK_InteractableObject)o;

        UpdateMirroredPosition(api.gameObject);     // run last update
        if (VRTK_DeviceFinder.IsControllerLeftHand(args.interactingObject))
        {
            if (ikControl)
            {
                ikControl.leftHandObj = null;
                objLeftGrab = null;
            }
        }
        else if (VRTK_DeviceFinder.IsControllerRightHand(args.interactingObject))
        {
            if (ikControl)
            {
                ikControl.rightHandObj = null;
                objLeftGrab = null;
            }
        }
        // Debug.Log(string.Format("[TaskDoing]: UnGrabbed {0} ", api.gameObject.name));
    }

}
