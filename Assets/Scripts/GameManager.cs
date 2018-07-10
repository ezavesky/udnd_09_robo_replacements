using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Source: https://forum.unity.com/threads/creating-a-proper-game-manager.378606/#post-2480596
public class Singleton<T> : MonoBehaviour where T:Singleton<T>
{
    private static volatile T _instance = null;
    private static object _lock = new object();

    public static T instance
    {
        get
        {
            if(_instance == null)
            {
                lock(_lock)
                {
                    if(_instance == null)
                    {
                        GameObject go = new GameObject();
                        _instance = go.AddComponent<T>();
                        go.name = typeof(T).ToString() + " singleton";
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return _instance;
        }
    }
}

public class GameManager : Singleton<GameManager> 
{
    public enum GAME_STATE { STATE_INITIAL, STATE_NORMAL, STATE_TESTING, STATE_HINTS, STATE_FINISHED, STATE_NEXT_LEVEL, STATE_RETURN_TO_LAST=1000 };
    protected GAME_STATE _state = GAME_STATE.STATE_INITIAL;
    protected GAME_STATE _stateLast = GAME_STATE.STATE_INITIAL;
    protected SceneController sceneController = null;  //allow manipulation of the scene
    protected DialogController dialogController = null;  //allow manipulation of the dialog
    public Transform toolParentTransform = null;  // transform to use for created tools

    // validate star/collectable possiblity when teleported from a valid location
    public bool normalPlay { 
        get 
        { 
            return _state == GAME_STATE.STATE_NORMAL; 
        } 
        private set {} 
    }
    public bool finishedLevel { 
        get 
        { 
            // state jump if we're in normal but no collectables (like in training level)
            if ((_state == GAME_STATE.STATE_FINISHED) || (_state == GAME_STATE.STATE_NORMAL))
            {
                state = GAME_STATE.STATE_FINISHED;
                return true;
            }
            return false;
        } 
        private set {} 
    }

    public GAME_STATE state {
        set
        {
            //TODO: other game mechanics when state changes?
            if (value == GAME_STATE.STATE_RETURN_TO_LAST) {
                _state = _stateLast;    
            }
            else if (value == GAME_STATE.STATE_NEXT_LEVEL) {
                Debug.Log(string.Format("[GameManager] Switching to next scene with scene manager {0}", sceneController));
                _stateLast = _state;
                _state = GAME_STATE.STATE_INITIAL;
            }
            else {
                // guarantee that we have only one unique object (e.g. a ball)
                Debug.Log(string.Format("[GameManager]: New state {0}, previous state {1}", value, _state));
                _stateLast = _state;
                _state = value;
            }
        }
        get 
        {
            return _state;
        }
    }

    // helper methods for scene/level controll

    public void RegisterSceneController(SceneController sceneControllerNew) 
    {
         sceneController = sceneControllerNew;
    }

    public void StageNewScene(string nameSceneNext) 
    {
        sceneController.SceneStage(nameSceneNext);
    }

    public bool LoadNewScene(string nameSceneNext) 
    {
        if (state != GAME_STATE.STATE_FINISHED && !string.IsNullOrEmpty(nameSceneNext))
        {
            Debug.LogWarning(string.Format("[GameManager] Attempting to load new scene '{0}' but not in finished state", nameSceneNext));
            return false;
        }
        state = GAME_STATE.STATE_NEXT_LEVEL;
        sceneController.SceneLoad(nameSceneNext);
        return true;
    }

    // methods for management of dialog
    public void RegisterDialogController(DialogController dialogControllerNew) 
    {
         dialogController = dialogControllerNew;
    }

    public string DialogTrigger(string nameTrigger, DialogTrigger.TRIGGER_TYPE typeTrigger) 
    {
        if (dialogController)
        {
            return dialogController.TriggerUtterance(nameTrigger, typeTrigger);
        }
        return null;
    }


}
