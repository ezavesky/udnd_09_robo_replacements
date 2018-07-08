using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogController : MonoBehaviour 
{
    protected System.Random randGen = new System.Random();
    public GameObject objDialog = null;
    protected UnityEngine.UI.Text textDialog = null;
    protected RectTransform transformDialog = null;
    protected float distSlide = 0.0f;

	// Use this for initialization
	void Start () {
		// attach to OSD mechanism (done via editor, once)
        if (objDialog) 
        {
            transformDialog = objDialog.GetComponent<RectTransform>();
            textDialog = objDialog.GetComponentInChildren<UnityEngine.UI.Text>();
            ToggleUtterance(null);
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
    protected void ToggleUtterance(Utterance uttNew)
    {
        //  TODO: slide off screen via timer event
        if (uttNew == null)
        {
            if (textDialog != null)
            {
                textDialog.text = "(hidden)";
            }
        }
        else 
        {
            Debug.Log(string.Format("[DialogController]: UtteranceToggle '{1}' (from {0})", uttNew.name, uttNew.text));
            if (textDialog != null)
            {
                textDialog.text = uttNew.text;
            }
        }
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
        { "doing_enter_0", new Utterance(null, 0.0f, "I don't eat, but I think you do. Can you help me cook?") }, 
        { "doing_exit_0", new Utterance(null, 0.0f, "No problem. I will prepare tonights meal from space worms and purified gray water!") }, 
        { "seeing_enter_0", new Utterance(null, 0.0f, "As your world's future architect, I must learn about 'circles'. Can you help?") }, 
        { "seeing_enter_1", new Utterance(null, 0.5f, "As a simulator, I could add quite well.  I don't think that's the same as building a bridge. Can you help?") }, 
        { "reading_enter_0", new Utterance(null, 0.0f, "I am writing the next best selling novel.  It must have 'words'. Can you help?") }, 
    };

    static protected Dictionary<string, List<string>> DICT_RANDOMIZER = new Dictionary<string, List<string> >();
	
}
