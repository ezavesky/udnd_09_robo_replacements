using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskReading : TaskInteractionBase 
{
    public ButtonController buttonStart;
    public ButtonController buttonA;
    public ButtonController buttonB;
    public ButtonController buttonC;

    public Text textQuestion;
    public Text textMedia;
    public Text textJokes;
    public Image imageMedia;

    protected enum READ_STATE { STATE_RESET, STATE_PLAYBACK_EXIT, STATE_TRAIN, STATE_TRAIN_EXIT, STATE_OPTION, STATE_PLAYBACK };
    protected READ_STATE stateLast;
    protected bool hasTrained = false;

    protected Dictionary<string, READ_STATE> dictNextState = new Dictionary<string, READ_STATE>();

    // flow for reading process (with some button names and dialog enters)
    //  Start -> reading_start_enter
    //      reading_training_enter (if not seen already)
    //          Yes -> reading_training_exit
    //          No -> reading_training_more_enter
    //      reading_training_exit
    //      select_option_enter
    //          A, B, C -> thanks, option recorded
    //      reading_playback_enter
    //          (custom reading of text)
    //      reading_playback_exit
    //          Replay -> reading_playback_enter
    //          Start -> reading_start_enter

    void Start() 
    {
        LoadImageExamples();
        SetReadState(READ_STATE.STATE_RESET);
    }

    protected void SetReadState(READ_STATE stateNew)
    {
        string strQuestion = null;
        string strExample = null;

        //init buttons to just show start
        switch(stateNew) 
        {
            default:
                //nothing
                break;

            case READ_STATE.STATE_RESET:
                buttonStart.name = "Start";
                buttonStart.buttonEnabled = true;
                strQuestion = GameManager.instance.DialogTrigger("reading_start_enter", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                if (hasTrained)
                {
                    dictNextState[buttonStart.name] = READ_STATE.STATE_TRAIN;
                }
                else 
                {
                    dictNextState[buttonStart.name] = READ_STATE.STATE_TRAIN_EXIT;
                }
                buttonA.buttonEnabled = false;
                buttonB.buttonEnabled = false;
                buttonC.buttonEnabled = false;
                strExample = "welcome";
                break;

            case READ_STATE.STATE_PLAYBACK:
                buttonB.buttonEnabled = true;
                buttonB.name = "Replay";

                // TODO: other state tracking!
                break;
        }

        if (string.IsNullOrEmpty(strQuestion)) 
        {
            textQuestion.text = "";
        }
        else 
        {
            textQuestion.text = strQuestion;
        }
        ExampleUpdate(strExample);

        stateLast = stateNew;
    }

    // method to start story

    // method to offer options

    // method to replay story with options

    // method to update story with spoken text

    // database for reading/substituting story?

    // method to update screen with example graphics
    protected void ExampleUpdate(string uniImage)
    {
        if (DICT_IMAGE_EX.ContainsKey(uniImage))
        {
            ImageExample exItem = DICT_IMAGE_EX[uniImage];
            imageMedia.sprite = exItem.sprite;
            textMedia.text = exItem.text;
        }
        else 
        {
            textMedia.text = "";
            imageMedia.sprite = null;
        }
    }

    public void ReceiveButtonPress(object o, string buttonName) 
    {
        // do anything?
        Debug.Log(string.Format("[TaskReading]: {0} PRESS {1}", buttonName, o));
    }

    public void ReceiveButtonRelease(object o, string buttonName) 
    {
        if (dictNextState.ContainsKey(buttonName))
        {
            SetReadState(dictNextState[buttonName]);
        }
        else 
        {
            // do anything?        
            Debug.LogWarning(string.Format("[TaskReading]: {0} RELEASE but no state information! (last state: {1})", buttonName, stateLast));
        }
    }

    // ------------------------------------
    // methods for image content management

    [SerializeField]
    public class ImageExample 
    {
        public string text;             // text of what is being said
        public Sprite sprite = null;   // clip for audio of what is said

        public ImageExample(string _path, string _text) 
        {
            text = _text;
            sprite = Resources.Load<Sprite>(string.Format("Sprites/{0}", _path));
        }
    }

    // static reference for image examples
    static protected Dictionary<string, ImageExample> DICT_IMAGE_EX = null;

    protected void LoadImageExamples()
    {
        if (DICT_IMAGE_EX != null)
            return;
        DICT_IMAGE_EX = new Dictionary<string, ImageExample>{
            { "welcome", new ImageExample("read_welcome", "Welcome!") }
        };
    }

}