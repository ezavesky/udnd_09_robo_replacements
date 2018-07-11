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
    public float timeGame = 30.0f;
    protected float timeRemain = 0.0f;

    public Transform rootPlatePlayer;
    public Transform rootPlateRobot;
    public Transform rootResourcesPlayer;
    public Transform rootResourcesRobot;

    public GameObject[] objPrefabExamples = new GameObject[0];      // objects used for games
    public GameObject rootAnimationFreeze = null;

    public GameObject objLeftGrab = null;
    public GameObject objRightGrab = null;
    
    protected int idxPrefabActive = 0;
    protected Dictionary<int, GameObject> dictObjectPair = new Dictionary<int, GameObject>();
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
                ActivatePrefabSet(-1, false);
                ChildrenSetActive(rootAnimationFreeze.transform, false);    //deactivate cameras, lazy susan
                break;

            case TASK_STATE.STATE_START_NEXT:
                idxPrefabActive = (idxPrefabActive + 1) % objPrefabExamples.Length;
                goto case TASK_STATE.STATE_START;    //fall through

            case TASK_STATE.STATE_START:
                dictNextState.Clear();
                objPrefabExamples[idxPrefabActive].SetActive(true);
                ChildrenSetActive(rootAnimationFreeze.transform, true);     //activate cameras, lazy susan
                buttonA.buttonName = "Next";
                dictNextState[buttonA.buttonName] = TASK_STATE.STATE_START_NEXT;
                buttonB.buttonName = "Go";
                dictNextState[buttonB.buttonName] = TASK_STATE.STATE_GAME_START;
                buttonA.buttonEnabled = buttonB.buttonEnabled = true;
                textHelp.text = GameManager.instance.DialogTrigger("doing_intro", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                break;                

            case TASK_STATE.STATE_GAME_START:
                ActivatePrefabSet(idxPrefabActive, true);
                buttonB.buttonName = "Done!";
                dictNextState[buttonB.buttonName] = TASK_STATE.STATE_FINISH;
                buttonA.buttonEnabled = false;
                buttonB.buttonEnabled = true;
                timeRemain = timeGame;
                textHelp.text = GameManager.instance.DialogTrigger("doing_game", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
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

    protected void ActivatePrefabSet(int idxPrefab, bool bEnable=false)
    {
        if (idxPrefab == -1)    //helper mode to take action for all prefabs
        {
            for (int i=0; i<objPrefabExamples.Length; i++)
            {
                ActivatePrefabSet(i, bEnable);
            }
            return;
        }
        if (idxPrefab >= objPrefabExamples.Length)  //check bounds
            return;
        objPrefabExamples[idxPrefab].SetActive(bEnable);
        foreach (GameObject objClone in dictPrefabSets[idxPrefab])  //disable all objects for this prefab
        {
            if (!bEnable)
            {
                objClone.SetActive(false);
            }
            else    //activating, so we need to check if player or robot 
            {
                if (dictObjectPair.ContainsKey(objClone.GetInstanceID()))       //has instance match, it's player object
                {
                    objClone.transform.position = rootResourcesPlayer.position;
                }
                else
                {
                    objClone.transform.position = rootResourcesRobot.position;
                }
                objClone.SetActive(true);   // let object fall into basket from anchor
            }
        }
    } 

    protected void ClonePrefabSets() 
    {
        //tasks: clone sets of prefab examples into two pairs: player and robobt ones
        //       create a nested object under this one to contain all sets
        GameObject cloneRoot = new GameObject();
        cloneRoot.SetActive(false);
        cloneRoot.name = "_cloneRoot";
        cloneRoot.transform.parent = transform;

        GameObject newPlayerHandle = null;
        GameObject newRobotHandle = null;
        
        for (int i=0; i<objPrefabExamples.Length; i++) 
        {
            dictPrefabSets[i] = new List<GameObject>();
            for (int j=0; j<objPrefabExamples[0].transform.childCount; j++)
            {
                newPlayerHandle = Instantiate(objPrefabExamples[0].transform.GetChild(j).gameObject);
                newPlayerHandle.transform.parent = cloneRoot.transform;
                newPlayerHandle.isStatic = false;
                newRobotHandle = Instantiate(objPrefabExamples[0].transform.GetChild(j).gameObject);
                newRobotHandle.transform.parent = cloneRoot.transform;
                newRobotHandle.isStatic = false;

                dictPrefabSets[i].Add(newPlayerHandle);     //save in list for easy retrieval
                dictPrefabSets[i].Add(newRobotHandle);
                dictObjectPair[newPlayerHandle.GetInstanceID()] = newRobotHandle;   //establish link to robot object
            }
            objPrefabExamples[0].SetActive(false);
        }
    }

    // TODO : specific for doing task

    // method to switch to tracking food object in person's hands

    // method to switch to tracking food object in robot's hands

    //every so often update joke panel with new text
    IEnumerator ScoreboardUpdate()
    {
        bool hasBuzzed = false;
        while (true)
        {
            if (stateLast == TASK_STATE.STATE_GAME_START)       // proceed to update time remaining and score
            {
                textTimeLeft.text = string.Format("{0:.2f} s", timeRemain);

                //TODO compute similarity score
            }
            else {
                textTimeLeft.text = "(game over)";
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
        // do anything?        
        Debug.LogWarning(string.Format("[TaskDoing]: {0} RELEASE (last state: {1})", buttonName, stateLast));
    }

}
