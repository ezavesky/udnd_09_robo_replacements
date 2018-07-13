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
    protected float timeGame = 60.0f;
    protected float timeRemain = 0.0f;
    protected float matchScore = 0.0f;
    protected float biasDistance = 0.95f;    //closer to 1 is distance, closer to 0 is rotation-based

    public Transform rootPlateReference;
    public Transform rootPlatePlayer;
    public Transform rootPlateRobot;
    public Transform rootResourcesPlayer;
    public Transform rootResourcesRobot;

    protected GameObject objCloneRoot = null;
    public GameObject objFoodSource;
    protected List<GameObject> listFoods = new List<GameObject>();
    public GameObject rootAnimationFreeze = null;
    public AudioClip clipTimeup = null;

    protected GameObject objLeftGrab = null;
    protected GameObject objRightGrab = null;
    
    protected int idxPrefabActive = 0;
    protected Dictionary<int, GameObject> dictObjectRobot = new Dictionary<int, GameObject>();
    protected Dictionary<int, GameObject> dictObjectReference = new Dictionary<int, GameObject>();
    protected Dictionary<int, List<GameObject>> dictPrefabSets = new Dictionary<int, List<GameObject>>();

    protected enum TASK_STATE { RESET, START, START_NEXT, GAME_START, GAME_RESET, PIECE, FINISH };
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
        SetTaskState(TASK_STATE.RESET);
        idxPrefabActive = GameManager.instance.rand.Next(listFoods.Count);  // different starter food
    }

    protected void SetTaskState(TASK_STATE stateNew)
    {
        bool ranNested = false;

        //init buttons to just show start
        switch(stateNew) 
        {
            default:
                Debug.LogError(string.Format("[TaskReading]: Life-systems restarting! State {0} received, but unexpected!", stateNew));
                SetTaskState(TASK_STATE.START_NEXT);
                return;

            case TASK_STATE.RESET:
                buttonA.buttonEnabled = buttonB.buttonEnabled = false;
                textAccuracy.text = "";
                textTimeLeft.text = "";
                Invoke("FreezeDisplay", 0.5f);     // delay for camera textures to grab a snapshot
                dictNextState.Clear();
                break;

            case TASK_STATE.START_NEXT:
                ActivatePrefabSet(idxPrefabActive, PREFAB_STATE.INACTIVE);
                idxPrefabActive = (idxPrefabActive + 1) % listFoods.Count;
                goto case TASK_STATE.START;    //fall through

            case TASK_STATE.START:
                listFoods[idxPrefabActive].SetActive(true);
                ChildrenSetActive(rootAnimationFreeze.transform, true);     //activate cameras, lazy susan
                buttonA.buttonName = "Next";
                dictNextState[buttonA.buttonName] = TASK_STATE.START_NEXT;
                buttonB.buttonName = "Go";
                dictNextState[buttonB.buttonName] = TASK_STATE.GAME_START;
                buttonA.buttonEnabled = buttonB.buttonEnabled = true;
                textHelp.text = GameManager.instance.DialogTrigger("doing_intro", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                break;                

            case TASK_STATE.GAME_RESET:
                ActivatePrefabSet(idxPrefabActive, PREFAB_STATE.ACTIVE);
                textHelp.text = GameManager.instance.DialogTrigger("doing_game", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                ranNested = true;
                goto case TASK_STATE.GAME_START;

            case TASK_STATE.GAME_START:
                ActivatePrefabSet(idxPrefabActive, PREFAB_STATE.ACTIVE);
                buttonB.buttonName = "Done!";
                dictNextState[buttonB.buttonName] = TASK_STATE.FINISH;
                buttonA.buttonName = "Reset Food";
                dictNextState[buttonA.buttonName] = TASK_STATE.GAME_RESET;
                buttonA.buttonEnabled = buttonB.buttonEnabled = true;
                if (!ranNested)
                {
                    timeRemain = timeGame;
                    textHelp.text = GameManager.instance.DialogTrigger("doing_game", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                }
                Invoke("StabalizePrefabs", 1.0f);     // delay for correct op
                break;                

            case TASK_STATE.FINISH:
                buttonB.buttonName = "Again!";
                dictNextState[buttonB.buttonName] = TASK_STATE.START;
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
                objClone.transform.parent = objCloneRoot.transform;
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
                        objClone.transform.parent = rootPlatePlayer.transform;
                    }
                }
                else
                {
                    rb = objClone.GetComponent<Rigidbody>();
                    rb.isKinematic = (stateNew!=PREFAB_STATE.ACTIVE);                        
                    if (stateNew==PREFAB_STATE.ACTIVE)
                    {
                        objClone.transform.position = rootResourcesRobot.position;
                        objClone.transform.parent = rootPlateRobot.transform;
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
        if (objCloneRoot == null)
        {
            objCloneRoot = new GameObject();
        }
        objCloneRoot.name = "_cloneRoot";
        objCloneRoot.transform.parent = transform;
        if (objCloneRoot.transform.childCount > 0)
        {
            return;
        }

        //TODO: clobber this cloneroot, too?
        listFoods.Clear();

        GameObject objSource = null;
        GameObject objFoodBase = null;
        GameObject[] newObjs = new GameObject[2];
        Rigidbody rb = null;
        VRTK.VRTK_InteractableObject apiInteract;
        int i, j, k;

        for (i=0; i<objFoodSource.transform.childCount; i++) 
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
                    newObjs[k].transform.parent = objCloneRoot.transform;
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
        // use this trick (local transforms only) because the we have normalized by the parent's space via transform
        Vector3 ptNew = objTarget.transform.localPosition;
        // Debug.Log(string.Format("[TaskDoing]: Move from {0} to new {1}; source {2}", objTracking.transform.position, ptNew, objTarget.transform.position));
        
        // smoothly transition the object to its new position...
        LeanTween.moveLocal(objTracking, ptNew, intervalScoreboard*0.8f);
        LeanTween.rotateLocal(objTracking, objTarget.transform.localEulerAngles, intervalScoreboard*0.8f);
        //objTracking.transform.localPosition = ptNew;
        //objTracking.transform.localRotation = objTarget.transform.localRotation;

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

        //foreach item in the reference
        for (int i=0; i < listFoods[idxPrefabActive].transform.childCount; i++)
        {
            objRef = listFoods[idxPrefabActive].transform.GetChild(i).gameObject;
            if (!dictObjectReference.ContainsKey(objRef.GetInstanceID()))
            {
                Debug.LogWarning(string.Format("[TaskDoing]: Warning, reference object {0} (id {1}) not found in compare hierarchy.", objRef.name, objRef.GetInstanceID()));
            }
            else {
                objCompare = dictObjectReference[objRef.GetInstanceID()];
                
                //  compute the position (plate normalized) differences
                localDist = Vector3.Distance(objRef.transform.localPosition, objCompare.transform.localPosition);
                // Debug.Log(string.Format("[TaskDoing]: PTS {3} @ {0} - REF {1}, PLAYER {2}", objRef.GetInstanceID(), ptRef, ptPlayer, localDist));
                userDist += localDist;

                //  compute the angular differences
                localDist = Vector3.Distance(objRef.transform.localEulerAngles, objCompare.transform.localEulerAngles);
                // Debug.Log(string.Format("[TaskDoing]: ANG {3} @ {0} - REF {1}, PLAYER {2}", objRef.GetInstanceID(), ptRef, ptPlayer, localDist));
                userAngle += localDist/360f;
            }
        }

        //return the weighted total of user components
        float distCombined = (userDist * biasDistance) + (userAngle * (1-biasDistance));

        // h(x) = Min(Max(0,(2^(-x/100+8))/300), 100)
        //      good distance function
        // http://www.wolframalpha.com/input/?x=0&y=0&i=h(x)+%3D+2%5E(-x%2B8) - plot example

        // some normalization scheme to get to 0-1 position
        float matchNorm = 1.0f - Mathf.Min(Mathf.Max(0,Mathf.Pow(2,(-distCombined+8))/250), 1);
        // Debug.Log(string.Format("[TaskDoing]: Dist {0}, Angle {1}, Combined {2}, Norm {3}", userDist, userAngle, distCombined, matchNorm));
        return matchNorm;
    }


    //every so often update joke panel with new text
    IEnumerator ScoreboardUpdate()
    {
        while (true)
        {
            // proceed to update time remaining and score
            if (stateLast == TASK_STATE.GAME_START || stateLast == TASK_STATE.GAME_RESET)
            {
                textTimeLeft.text = string.Format("{0:F1} sec", timeRemain);
                timeRemain -= intervalScoreboard; //(Time.fixedTime-timeLast);  //so what, it's nto time accurate
                
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
                    UpdateMirroredPosition(objRightGrab);
                }

                if (timeRemain <= 0) 
                {
                    timeRemain = 0.0f;
                    if (clipTimeup) 
                    {
                        AudioSource.PlayClipAtPoint(clipTimeup, Camera.main.transform.position);
                    }
                    SetTaskState(TASK_STATE.FINISH);
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
            SetTaskState(TASK_STATE.START);
            StartCoroutine("ScoreboardUpdate");
        }
        else    // exit -> reset
        {
            SetTaskState(TASK_STATE.RESET);
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
