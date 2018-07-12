using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public Transform rootPlateReference;
    public Transform rootPlatePlayer;
    public Transform rootPlateRobot;
    public Transform rootResourcesPlayer;
    public Transform rootResourcesRobot;

    public GameObject objFoodSource;
    protected List<GameObject> listFoods = new List<GameObject>();
    public GameObject rootAnimationFreeze = null;

    public GameObject objLeftGrab = null;
    public GameObject objRightGrab = null;
    
    protected int idxPrefabActive = 0;
    protected Dictionary<int, GameObject> dictObjectPair = new Dictionary<int, GameObject>();
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
        listFoods[idxPrefab].SetActive(stateNew==PREFAB_STATE.ACTIVE);
        foreach (GameObject objClone in dictPrefabSets[idxPrefab])  //disable all objects for this prefab
        {
            if (stateNew==PREFAB_STATE.INACTIVE)
            {
                objClone.SetActive(false);
            }
            else    //activating, so we need to check if player or robot 
            {
                if (dictObjectPair.ContainsKey(objClone.GetInstanceID()))       //has instance match, it's player object
                {
                    if (stateNew==PREFAB_STATE.ACTIVE)
                    {
                        objClone.transform.position = rootResourcesPlayer.position;
                    }
                    else 
                    {
                        rb = objClone.GetComponent<Rigidbody>();
                        rb.isKinematic = true;                        
                    }
                }
                else
                {
                    if (stateNew==PREFAB_STATE.ACTIVE)
                    {
                        objClone.transform.position = rootResourcesRobot.position;
                    }
                    else 
                    {
                        rb = objClone.GetComponent<Rigidbody>();
                        rb.isKinematic = true;                        
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
        GameObject newPlayerHandle = null;
        GameObject newRobotHandle = null;
        Rigidbody rb = null;
        
        for (int i=0; i<objFoodSource.transform.GetChildCount(); i++) 
        {
            objFoodBase = objFoodSource.transform.GetChild(i).gameObject;
            listFoods.Add(objFoodBase);
            dictPrefabSets[i] = new List<GameObject>();
            for (int j=0; j<objFoodBase.transform.childCount; j++)
            {
                objSource = objFoodBase.transform.GetChild(j).gameObject;
                rb = objSource.GetComponent<Rigidbody>();
                if (rb) 
                {
                    rb.isKinematic = true;
                }

                newPlayerHandle = Instantiate(objSource);
                newPlayerHandle.SetActive(false);
                newPlayerHandle.transform.parent = cloneRoot.transform;
                newPlayerHandle.isStatic = false;
                rb = newPlayerHandle.GetComponent<Rigidbody>();
                if (rb) 
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                newRobotHandle = Instantiate(objSource);
                newRobotHandle.SetActive(false);
                newRobotHandle.transform.parent = cloneRoot.transform;
                newRobotHandle.isStatic = false;
                rb = newRobotHandle.GetComponent<Rigidbody>();
                if (rb) 
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                dictPrefabSets[i].Add(newPlayerHandle);     //save in list for easy retrieval
                dictPrefabSets[i].Add(newRobotHandle);
                dictObjectReference[objSource.GetInstanceID()] = newPlayerHandle;   //establish link to reference object
                dictObjectPair[newPlayerHandle.GetInstanceID()] = newRobotHandle;   //establish link to robot object
            }
            objFoodBase.SetActive(false);
        }
    }

    // TODO : specific for doing task

    // method to switch to tracking food object in person's hands

    // method to switch to tracking food object in robot's hands



    protected float ComputeMatch(float weightDistance, float weightAngle)  
    {
        //method to compute distance and rotation from reference/user items
        float userDist = 0f;
        float userAngle = 0f;
        GameObject objRef = null;
        GameObject objCompare = null;

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
                //  compute the angular differences

            }
        }

        //return the weighted total of user components
        return (userDist * weightDistance) + (userAngle * weightAngle);
    }


    //every so often update joke panel with new text
    IEnumerator ScoreboardUpdate()
    {
        float timeLast = Time.fixedTime;
        while (true)
        {
            if (stateLast == TASK_STATE.STATE_GAME_START)       // proceed to update time remaining and score
            {
                textTimeLeft.text = string.Format("{0:F1} sec", timeRemain);
                timeRemain -= intervalScoreboard; //(Time.fixedTime-timeLast);  //so what, it's nto time accurate
                timeLast = Time.fixedTime;

                //TODO compute similarity score

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
}
