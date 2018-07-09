using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogController : MonoBehaviour 
{
    protected System.Random randGen = new System.Random();
    public GameObject objDialog = null;
    protected UnityEngine.UI.Text textDialog = null;
    protected RectTransform transformDialog = null;

	protected float openHeight;             // opening height of dialog panel
	protected float openPos;                // opening y of dialog panel

    protected float timeAnimate = 0.25f;       // time in seconds for animation to finish
    protected float timeVisible = 5f;       // time in seconds for dialog to be visible
    protected int showCount = 0;            // number of dialog items queued here  

	// Use this for initialization
	void Start () {
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
            ToggleUtterance(null, false);
        }

        // populate the RANDOMIZER dictionary once
        if (DialogController.DICT_RANDOMIZER.Count==0) 
        {
            foreach (KeyValuePair<string, Utterance> kvp in DICT_UTTERANCE) 
            {
                List<string> nameParts = new List<string>(kvp.Key.Split('_'));
                kvp.Value.name = kvp.Key;       // copy key into struct field
                int idxKey = 0;
                try 
                {
                    idxKey = int.Parse(nameParts[nameParts.Count-1]);
                    nameParts.RemoveAt(nameParts.Count-1);
                }
                catch 
                {
                    idxKey = 0;
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

    public void TriggerUtterance(string nameUtterance, DialogTrigger.TRIGGER_TYPE typeUtterance) 
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
            return;
        }
        float fRand = (float)randGen.NextDouble();
        Utterance uttSel = null;
        foreach (string strSubUtter in DialogController.DICT_RANDOMIZER[nameUtterance])
        {
            if (uttSel==null || (fRand >= DialogController.DICT_UTTERANCE[strSubUtter].interval_start))
            {
                uttSel = DialogController.DICT_UTTERANCE[strSubUtter];
            }
        }

        ToggleUtterance(uttSel);
        TrackState(uttSel);
    }

    // slide the utterance dialog box (e.g. the text caption on and off screen)
    protected void ToggleUtterance(Utterance uttNew, bool bUseDelay=true)
    {
        //  TODO: slide off screen via timer event
        bool bShow = false;
        float localAnimate = bUseDelay ? timeAnimate : 0.0f;
        float[] floatTrans = {0.0f, 0.0f};
        if (uttNew != null)
        {
            //Debug.Log(string.Format("[DialogController]: UtteranceToggle '{1}' (from {0})", uttNew.name, uttNew.text));
            if (textDialog != null)
            {
                bShow = true;
                textDialog.text = uttNew.text;
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
    }

    //helper to allow hiding with simple timed invoke function
    protected void HideUtterance()
    {
        showCount--;
        if (showCount != 0)
            return;
        ToggleUtterance(null);
    }

	private void UpdateUtterancePosition(float fPart, object objRaw) {
		GameObject objDialogLocal = (GameObject)objRaw;
        
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
            // clip = Resources.Load<AudioClip>("Sounds/cube_up");
        }
    }

    // static reference for dialog entry points
    //  semantics for naming "XXXX_T_N" where N starts from 0 and can be multiple entries for a name
    //     semantics for T are either "stay", "enter" or "exit" -- tightly chained to DialogTrigger.TRIGGER_TYPE
    //  interval numbers allow a randomized entry into one of N different changes (or always a single if only one)
    static protected Dictionary<string, Utterance> DICT_UTTERANCE = new Dictionary<string, Utterance>{
        { "doing_enter_0", new Utterance(null, 0.0f, "I don't eat, but I compute that you do. Can you teach me to cook?") }, 
        { "doing_exit_0", new Utterance(null, 0.0f, "No problem. I will prepare tonight's meal from space worms and purified gray water!") }, 
        { "seeing_enter_0", new Utterance(null, 0.0f, "As your world's future architect, I must learn about 'circles'. Can you help?") }, 
        { "seeing_enter_1", new Utterance(null, 0.5f, "As a simulator, I could add quite well. I compute that's the same as building a bridge. Can you help?") }, 
        { "reading_enter_0", new Utterance(null, 0.0f, "I am writing the next best selling novel. It must have 'words'. Can you help?") }, 
    };

    static protected Dictionary<string, List<string>> DICT_RANDOMIZER = new Dictionary<string, List<string> >();
	
}
