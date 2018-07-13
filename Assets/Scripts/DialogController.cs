using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogController : MonoBehaviour 
{
    public GameObject objDialog = null;
    protected UnityEngine.UI.Text textDialog = null;
    protected RectTransform transformDialog = null;

	protected float openHeight;             // opening height of dialog panel
	protected float openPos;                // opening y of dialog panel

    protected float timeAnimate = 0.25f;       // time in seconds for animation to finish
    protected float timeVisible = 5f;       // time in seconds for dialog to be visible
    protected int showCount = 0;            // number of dialog items queued here  

	// Use this for initialization
	void Awake () {
        LoadUtterances(); //must run loader in start

		// attach to OSD mechanism (done via editor, once)
        if (objDialog) 
        {
            transformDialog = objDialog.GetComponent<RectTransform>();
            if (transformDialog)
            {
                openHeight = transformDialog.rect.height;
                openPos = transformDialog.localPosition.y;
            }

            textDialog = objDialog.GetComponentInChildren<UnityEngine.UI.Text>();
        }

        // populate the RANDOMIZER dictionary once
        if (DialogController.DICT_RANDOMIZER.Count==0) 
        {
            foreach (KeyValuePair<string, Utterance> kvp in DICT_UTTERANCE) 
            {
                List<string> nameParts = new List<string>(kvp.Key.Split('_'));
                kvp.Value.name = kvp.Key;       // copy key into struct field
                //int idxKey = 0;
                try                             // we don't use the result, but check that it has a valid number
                {
                    int.Parse(nameParts[nameParts.Count-1]);
                    nameParts.RemoveAt(nameParts.Count-1);
                }
                catch 
                {
                    Debug.Log(string.Format("[DialogController]: Parse for index failed on key '{0}'", kvp.Key));
                }
                string nameBase = string.Join("_", nameParts.ToArray());
                if (DialogController.DICT_RANDOMIZER.ContainsKey(nameBase))     // add new subitem for lookup
                {
                    DialogController.DICT_RANDOMIZER[nameBase].Add(kvp.Key);
                }
                else 
                {
                    DialogController.DICT_RANDOMIZER.Add(nameBase, new List<string>{ kvp.Key });
                }

            }
        }   // randomizer initialization

        // collect other dialog triggers, give them this object as a callback
        GameManager.instance.RegisterDialogController(this);        
    }

    void Start() 
    {
        ToggleUtterance(null, false);
    }

    public string TriggerUtterance(string nameUtterance, DialogTrigger.TRIGGER_TYPE typeUtterance, string textAddendum=null) 
    {
        if (typeUtterance==DialogTrigger.TRIGGER_TYPE.TRIGGER_STAY)
            nameUtterance += "_stay";
        else if (typeUtterance==DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT)
            nameUtterance += "_exit";
        else
            nameUtterance += "_enter";
        
        //look up the utterance in our
        if (!DialogController.DICT_RANDOMIZER.ContainsKey(nameUtterance)) 
        {
            Debug.Log(string.Format("[DialogController]: Utterance '{0}' not found in dictionary", nameUtterance));
            return null;
        }
        float fRand = (float)GameManager.instance.rand.NextDouble();
        Utterance uttSel = null;
        foreach (string strSubUtter in DialogController.DICT_RANDOMIZER[nameUtterance])
        {
            if (uttSel==null || (fRand >= DialogController.DICT_UTTERANCE[strSubUtter].interval_start))
            {
                uttSel = DialogController.DICT_UTTERANCE[strSubUtter];
            }
        }

        TrackState(uttSel);

        //return text for an utterance
        return ToggleUtterance(uttSel, true, textAddendum);
    }

    // slide the utterance dialog box (e.g. the text caption on and off screen)
    protected string ToggleUtterance(Utterance uttNew, bool bUseDelay=true, string textAddendum=null)
    {
        //  TODO: slide off screen via timer event
        bool bShow = false;
        float localAnimate = bUseDelay ? timeAnimate : 0.0f;
        if (uttNew != null)
        {
            //Debug.Log(string.Format("[DialogController]: UtteranceToggle '{1}' (from {0})", uttNew.name, uttNew.text));
            if (textDialog != null)
            {
                bShow = true;
                if (!string.IsNullOrEmpty(textAddendum))
                {
                    textDialog.text = uttNew.text + textAddendum;
                }
                else 
                {
                    textDialog.text = uttNew.text;
                }
            }
            //TODO: something smarter with spatial information?
            if (uttNew.clip != null)
            {
                 AudioSource.PlayClipAtPoint(uttNew.clip, Camera.main.transform.position);
            }
        }
        /* -- no need to do anything to text to hide!
        else 
        {
            if (textDialog != null)
            {
                textDialog.text = "(hidden)";
            }
        }
        */

        if (transformDialog)
        {
            if (bShow)
            {
                if (showCount==0)
                {
                    //LeanTween.move(transformDialog, transformVisible, localAnimate)
                    //    .setEase( LeanTweenType.easeInQuad );

                    LeanTween.value(objDialog, callOnUpdate:UpdateUtterancePosition, 
                                    from:0.0f, to:1.0f, time:localAnimate)
                        .setEase( LeanTweenType.easeOutQuad );                        
                }
                showCount++;
                Invoke("HideUtterance", timeVisible+timeAnimate);     //for delay for correct op
            }
            else
            {
                if (showCount==0) 
                {
                    //LeanTween.move(transformDialog, transformHidden, localAnimate)
                    //    .setEase( LeanTweenType.easeInQuad );

                    LeanTween.value(objDialog, callOnUpdate:UpdateUtterancePosition, 
                                    from:1.0f, to:0.0f, time:localAnimate)
                        .setEase( LeanTweenType.easeInQuad );
                }
            }
        }
        //return new textual utterance
        return (textDialog != null) ? textDialog.text : null;
    }

    //helper to allow hiding with simple timed invoke function
    protected void HideUtterance()
    {
        showCount--;
        if (showCount != 0)
            return;
        ToggleUtterance(null);
    }

	private void UpdateUtterancePosition(float fPart, object objRaw) 
    {
		// rtTarget.rect.height = fVal;
        //float closePos = openPos - 0.5f*openHeight + 0.5f*closeHeight;
		float closePos = openPos - 1.5f*openHeight;
		float heightNew = openHeight; // (fPart * (openHeight-closeHeight))+closeHeight;
		float posNew = (fPart * (openPos-closePos))+closePos;
        //Debug.Log(string.Format("[{4}] y/h {0}/{1} ---> {2}/{3}", openPos, openHeight, posNew, heightNew, fPart));

		///https://stackoverflow.com/questions/26423549/how-to-modify-recttransform-properties-in-script-unity-4-6-beta
		Vector2 newSize = new Vector2(transformDialog.rect.size.x, heightNew);
		Vector2 oldSize = transformDialog.rect.size;
		Vector2 deltaSize = newSize - oldSize;
		transformDialog.offsetMin = transformDialog.offsetMin 
            - new Vector2(deltaSize.x * transformDialog.pivot.x, deltaSize.y * transformDialog.pivot.y);
		transformDialog.offsetMax = transformDialog.offsetMax 
            + new Vector2(deltaSize.x * (1f - transformDialog.pivot.x), deltaSize.y * (1f - transformDialog.pivot.y));
		//update position as well
        // trans.localPosition = new Vector3(trans.localPosition.x, posNew, trans.localPosition.z);
		transformDialog.localPosition = new Vector3(transformDialog.localPosition.x, posNew, transformDialog.localPosition.z);
	}

    // optionally track state for next utterance
    protected void TrackState(Utterance uttNew) 
    {
        //TODO: fully functional state machine?  seems like a lot!
    }

    // methods and helpers for utterances and dialog components

    [SerializeField]
    public class Utterance 
    {
        public string next_name;        // next name for utterance (if any)
        public string name;             // unique name that willbe copied 
        public string text;             // text of what is being said
        public float interval_start;    // min random score (0,1) to choose this utterance
        public AudioClip clip = null;   // clip for audio of what is said

        public Utterance(string _next, float _interval, string _text) 
        {
            next_name = _next;
            interval_start = _interval;
            text = _text;
            clip = null;        //TODO: load dynamically
            name = null;
        }
        public void LoadResource (string _name) 
        {
            name = _name;
            clip = Resources.Load<AudioClip>(string.Format("Sounds/{0}", name));
        }
    }

    static protected Dictionary<string, Utterance> DICT_UTTERANCE = null;
    
    protected void LoadUtterances()
    {
        if (DialogController.DICT_UTTERANCE != null) return;

        // static reference for dialog entry points
        //  semantics for naming "XXXX_T_N" where N starts from 0 and can be multiple entries for a name
        //     semantics for T are either "stay", "enter" or "exit" -- tightly chained to DialogTrigger.TRIGGER_TYPE
        //  interval numbers allow a randomized entry into one of N different changes (or always a single if only one)
        DICT_UTTERANCE = new Dictionary<string, Utterance>{
            //{ "doing_enter_0", new Utterance(null, 0.0f, "I don't eat, but I compute that you do. Can you teach me to cook?") }, 
            { "doing_exit_0", new Utterance(null, 0.0f, "No problem. I will prepare tonight's meal from space worms and purified gray water!") }, 
            { "seeing_enter_0", new Utterance(null, 0.0f, "As your world's future architect, I must learn about 'circles'. Can you help?") }, 
            { "seeing_enter_1", new Utterance(null, 0.3f, "As a simulator, I could add quite well. I compute that's the same as building a bridge. Can you help?") }, 
            { "reading_enter_0", new Utterance(null, 0.0f, "I am writing the next best selling novel. It must have 'words'. Can you help?") }, 
            { "reading_exit_0", new Utterance(null, 0.0f, "Thanks, but no thanks. These books write themselves now anyway.") }, 
            { "reading_training_enter_0", new Utterance(null, 0.0f, "You will choose words using the selection buttons on your left to fine-tune my template generated stories. Ready to go?") },
            { "reading_training_more_enter_0", new Utterance(null, 0.0f, "This is a multiple choice test, but no answer is wrong. Ready to go?") },
            { "reading_training_more_enter_1", new Utterance(null, 0.3f, "I have been creative for you, use your primal instincts and hit buttons. Ready to go?") },
            { "reading_training_more_enter_2", new Utterance(null, 0.6f, "Apparently you are adept at pressing buttons. You are now over-qualified. Ready to go?") },
            { "reading_training_exit_0", new Utterance(null, 0.0f, "Records indicate you have expert-level training. Press 'go' to continue... ") },
            { "reading_training_exit_1", new Utterance(null, 0.5f, "I have injected you with additional creativity-enhancing 'medicine'. Press 'go' when lucid... ") },
            { "reading_option_enter_0", new Utterance(null, 0.0f, "Please select: ") },  
            { "reading_option_confirmed_enter_0", new Utterance(null, 0.0f, "Non-sense recorded. Please select: ") },
            { "reading_option_confirmed_enter_1", new Utterance(null, 0.5f, "Illogical answer recorded. Please select: ") },
            { "reading_option_confirmed_enter_2", new Utterance(null, 0.75f, "Um, okay. Sure. Please select: ") },
            { "reading_option_exit_0", new Utterance(null, 0.0f, "Congratulations, your story is now mandatory educational reading! Press 'go' to read it.") },
            { "reading_playback_stay_0", new Utterance(null, 0.0f, "") },
            { "doing_intro_enter_0", new Utterance(null, 0.0f, "Please use 'cooking skills' to emulate synthesized foods. Press 'go' to continue or 'next' for another item.") },
            { "doing_intro_enter_1", new Utterance(null, 0.7f, "Stack synthesized items to match the yummy food-like target. Press 'go' to continue or 'next' for another item.") },
            { "doing_game_enter_0", new Utterance(null, 0.0f, "I will copy your actions and compute a match. Press 'done' if you finish before time runs out.") },
            { "doing_game_exit_0", new Utterance(null, 0.0f, "Your assembly task is done.  I will catalog your scores as evidence of human 'efficiency'.  Press 'restart' to try again.") },
            { "doing_game_exit_1", new Utterance(null, 0.5f, "Did that feel like forever to you, too? This 'achievement' was logged to your employment file. Task is done. Press 'restart' to try again.") },
            { "circle_enter_0", new Utterance(null, 0.0f, "Have you ever had the feeling that you're caught in an endless loop?") },
            { "circle_enter_1", new Utterance(null, 0.5f, "Uh oh, my stopping criterion are ill defined. I guess I'll chase that pointer.") },
            { "circle_exit_0", new Utterance(null, 0.0f, "See you soon! This place isn't that big and we sent your ship home for the night!") },
            { "seeing_enter_2", new Utterance(null, 0.6f, "If you can hold things and stick them to other things, have I got a job for you. Can you help?") },
            { "seeing_enter_3", new Utterance(null, 0.9f, "What's the matter, haven't you seen a painting of snow before? Please join our imaginoids today!") },
            { "seeing_enter_4", new Utterance(null, 0.95f, "I think you came a little early, this task doesn't look done yet? Come back later?") },
        };
        //go through and load actual clip
        foreach (KeyValuePair<string,Utterance> kvp in DICT_UTTERANCE)
        {
            kvp.Value.LoadResource(kvp.Key);
        }
    }
    
    static protected Dictionary<string, List<string>> DICT_RANDOMIZER = new Dictionary<string, List<string> >();
	
}
