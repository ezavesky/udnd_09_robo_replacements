using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRTK;

public class SceneController : MonoBehaviour {
    //public string[] nameLevels = new string[0];
    public TeleportObjToggle teleporterController = null;
    protected string nameSceneMain = null;
    protected string nameSceneLast = null;
    protected GameObject objToolParent = null;
	
    // [Header("Body Collision Settings")]
    //[Tooltip("If checked then the body collider and rigidbody will be used to check for rigidbody collisions.")]
    protected VRTK_HeadsetFade headsetFade = null;
    public string nameSceneNext = null;
    protected float timeSceneLoadFade = 1.0f;

	// Use this for initialization
	void Start () {
        GameManager.instance.RegisterSceneController(this);
		headsetFade = GetComponent<VRTK_HeadsetFade>();
        nameSceneMain = gameObject.scene.name;
        if (headsetFade) 
        {
        	headsetFade.HeadsetFadeComplete += new HeadsetFadeEventHandler(HeadsetFadeComplete);
        }

        // create holder object for tools 
        objToolParent = new GameObject("Runtime GameTools"); 
        objToolParent.transform.parent = gameObject.transform;
        GameManager.instance.toolParentTransform = objToolParent.transform;
	
        // finish scene with our current scene
        SceneLoad();
	}

    // called to prep next stage
    public void SceneStage(string strName) 
    {
        if (!string.IsNullOrEmpty(strName)) 
        {
            nameSceneNext = strName;
            Debug.Log(string.Format("[SceneController]: Staging next scene '{0}'", strName));
        }
    }

    // called by a goal or game manager
    public void SceneLoad(string strName = null) 
    {
        if (!string.IsNullOrEmpty(strName)) 
        {
            nameSceneNext = strName;
        }
        if (string.IsNullOrEmpty(nameSceneNext) && !string.IsNullOrEmpty(nameSceneMain)) 
        {
            nameSceneNext = nameSceneMain;
            Debug.Log(string.Format("[SceneController]: Loading from main scene '{0}'", nameSceneMain));
        }

        if (headsetFade)        // if we have a valid fade, attempt to do that first
        {
            Invoke("HeadsetFadeBeforeLevel", 0.001f);     //for delay for correct op
        }
        else                    // if we do not, just jump right to scene transition
        {
            StartCoroutine(LoadAsyncScene(nameSceneNext));
        }
    }

    protected void HeadsetFadeBeforeLevel() 
    {
        headsetFade.Fade(new Color(0f, 0f, 0f, 1f), string.IsNullOrEmpty(nameSceneNext) ? 0f : timeSceneLoadFade);
    }

    // event complete for end of fade, proceed to scene load
	protected void HeadsetFadeComplete(object sender, HeadsetFadeEventArgs args) 
    {
        StartCoroutine(LoadAsyncScene(nameSceneNext));
	}

    // enumeror for scene load completion
    protected IEnumerator LoadAsyncScene(string strName)
    {
        Scene sceneNew = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(strName))
        {
            sceneNew = SceneManager.GetSceneByName(strName);
            if (!sceneNew.isLoaded)     //avoid loading if already there
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(strName, LoadSceneMode.Additive);    //load scene
                AsyncOperation asyncUnload = null;  
                if (!string.IsNullOrEmpty(nameSceneLast) && !string.Equals(nameSceneLast, nameSceneMain)) //unload old scenegj
                {
                    SceneManager.UnloadSceneAsync(nameSceneLast);
                }
                nameSceneLast = strName;

                // Wait until the asynchronous scene fully loads
                while (!asyncLoad.isDone || (asyncUnload!=null && !asyncUnload.isDone))
                {
                    yield return null;
                }   //end wait for scene load
                sceneNew = SceneManager.GetSceneByName(strName); 
            }
       }
        if (!sceneNew.IsValid())
        {
            Debug.LogError(string.Format("[SceneController]: Attempted to load scene '{0}', but returned invalid!", strName));
            yield break;            
        }
      
        // if we loaded the same scene as the initial scene, clear all play objects
        if (/* string.Equals(sceneNew.name, nameSceneMain) && */  objToolParent!=null) 
        {
            foreach (Transform child in objToolParent.transform) {
                GameObject.Destroy(child.gameObject);
            }
        }

        // finally change the state back to initial
        GameManager.instance.state = GameManager.GAME_STATE.STATE_INITIAL;

        GoalController sceneGoal = null;

        // on complete, find all of the collectables under new scene
        foreach (GameObject objRoot in sceneNew.GetRootGameObjects()) 
        {
            // start teleporter rediscover
            if (teleporterController != null) 
            {
                teleporterController.RediscoverTeleporters(objRoot);
            }

            GoalController localSceneGoal = objRoot.GetComponentInChildren<GoalController>();
            if (localSceneGoal != null) 
            {
                sceneGoal = localSceneGoal;
            }

        }   //end search of goal


        // teleport user to spawn within new scene
        if (sceneGoal) 
        {
            sceneGoal.TeleportUser(true);
        }

        // unfade the screen
        headsetFade.Unfade(timeSceneLoadFade*2);

    }   //end async scene load
}
