﻿using System.Collections;
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

    protected enum READ_STATE { STATE_RESET, STATE_START, STATE_TRAIN,  STATE_TRAIN_MORE, STATE_TRAIN_EXIT, STATE_OPTION, 
                                STATE_OPTION_CONFIRM, STATE_OPTION_EXIT, STATE_PLAYBACK, STATE_PLAYBACK_EXIT };
    protected READ_STATE stateLast;
    protected bool hasTrained = false;  // has gone through training dialog
    
    protected Dictionary<string, int> dictStoryComplete = new Dictionary<string, int>();   //track if user has completed story
    protected string storyActive = null;
    protected List<string> listAnswers = new List<string>();
    protected int sentenceActive = 0;

    protected Dictionary<string, READ_STATE> dictNextState = new Dictionary<string, READ_STATE>();

    // flow for reading process (with some button names and dialog enters)
    //  Go -> reading_start_enter
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

    public override void Awake() 
    {
        base.Awake();
        LoadImageExamples();
        LoadStoryExamples();
    }

    public override void Start()
    {
        SetReadState(READ_STATE.STATE_RESET);
    }

    protected void SetReadState(READ_STATE stateNew)
    {
        string strQuestion = null;
        string strExample = null;
        buttonStart.buttonEnabled = false;      // we may remove this button in the future!

        int idxExample = listAnswers.Count;     // current index of example to present

        //init buttons to just show start
        switch(stateNew) 
        {
            default:
                Debug.LogError(string.Format("[TaskReading]: Life-systems restarting! State {0} received, but unexpected!", stateNew));
                SetReadState(READ_STATE.STATE_START);
                return;

            case READ_STATE.STATE_RESET:
                dictNextState.Clear();
                buttonB.buttonName = "";
                buttonB.buttonEnabled = false;
                buttonA.buttonEnabled = false;
                buttonC.buttonEnabled = false;
                strExample = "welcome";
                break;


            case READ_STATE.STATE_START:
                dictNextState.Clear();
                buttonB.buttonName = "Start";
                buttonB.buttonEnabled = true;
                if (dialogTrigger && dialogTrigger.IsUserEngaged())
                {
                    strQuestion = GameManager.instance.DialogTrigger("reading", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                }
                if (hasTrained)
                {
                    dictNextState[buttonB.buttonName] = READ_STATE.STATE_TRAIN_EXIT;
                }
                else 
                {
                    dictNextState[buttonB.buttonName] = READ_STATE.STATE_TRAIN;
                }
                buttonA.buttonEnabled = false;
                buttonC.buttonEnabled = false;
                strExample = "welcome";
                break;

            case READ_STATE.STATE_TRAIN:
                buttonC.buttonName = "Yes";
                dictNextState[buttonC.buttonName] = READ_STATE.STATE_TRAIN_EXIT;
                buttonA.buttonName = "No";
                dictNextState[buttonA.buttonName] = READ_STATE.STATE_TRAIN_MORE;
                strQuestion = GameManager.instance.DialogTrigger("reading_training", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                buttonB.buttonEnabled = false;
                buttonA.buttonEnabled = buttonC.buttonEnabled = true;
                strExample = "training";
                break;

            case READ_STATE.STATE_TRAIN_MORE:
                buttonC.buttonName = "Yes";
                dictNextState[buttonC.buttonName] = READ_STATE.STATE_TRAIN_EXIT;
                buttonA.buttonName = "No";
                dictNextState[buttonA.buttonName] = READ_STATE.STATE_TRAIN_MORE;
                strQuestion = GameManager.instance.DialogTrigger("reading_training_more", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER);
                buttonB.buttonEnabled = false;
                buttonA.buttonEnabled = buttonC.buttonEnabled = true;
                strExample = "monkey";
                break;

            case READ_STATE.STATE_TRAIN_EXIT:
                buttonC.buttonName = "Go";
                dictNextState[buttonC.buttonName] = READ_STATE.STATE_OPTION;
                buttonA.buttonName = "New";
                dictNextState[buttonA.buttonName] = READ_STATE.STATE_TRAIN_EXIT;

                {
                    storyActive = null;
                    foreach (KeyValuePair<string, StoryExample> kvp in TaskReading.DICT_STORY_EX)
                    {
                        if (!dictStoryComplete.ContainsKey(kvp.Key) || string.IsNullOrEmpty(storyActive))   // start somewhere
                        {
                            storyActive = kvp.Key;
                        }
                        else if (dictStoryComplete[kvp.Key] < dictStoryComplete[storyActive])       // do it again!
                        {
                            storyActive = kvp.Key;
                        }
                    }
                    if (!dictStoryComplete.ContainsKey(storyActive))
                    {
                        dictStoryComplete[storyActive] = 0;
                    }
                    dictStoryComplete[storyActive] += 1;
                    listAnswers.Clear();
                }

                strQuestion = GameManager.instance.DialogTrigger("reading_training", 
                    DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT, storyActive+".");
                buttonA.buttonEnabled = buttonC.buttonEnabled = true;
                buttonB.buttonEnabled = false;
                // strExample = "";
                break;

            case READ_STATE.STATE_OPTION_CONFIRM:
                // add confirmation message
                strQuestion = GameManager.instance.DialogTrigger("reading_option_confirmed", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER, 
                                                                 FormulateOptions(storyActive, idxExample));
                goto case READ_STATE.STATE_OPTION;      // fall through                

            case READ_STATE.STATE_OPTION:               
                //populate the buttons and get our question text
                if (string.IsNullOrEmpty(strQuestion)) {
                    strQuestion = GameManager.instance.DialogTrigger("reading_option", DialogTrigger.TRIGGER_TYPE.TRIGGER_ENTER, 
                                                                     FormulateOptions(storyActive, idxExample));
                }

                // turn on all options to same destination
                if (TaskReading.DICT_STORY_EX[storyActive].listExamples.Count-1 == idxExample) 
                {
                    dictNextState[buttonA.buttonName] = READ_STATE.STATE_OPTION_EXIT;
                }
                else // more examples to get
                {
                    dictNextState[buttonA.buttonName] = READ_STATE.STATE_OPTION_CONFIRM;
                }
                dictNextState[buttonB.buttonName] = dictNextState[buttonA.buttonName];
                dictNextState[buttonC.buttonName] = dictNextState[buttonA.buttonName];
                buttonA.buttonEnabled = buttonC.buttonEnabled = buttonB.buttonEnabled = true;
                strExample = "idea";
                break;

            case READ_STATE.STATE_OPTION_EXIT:     // confirm new story mode 
                sentenceActive = 0;
                buttonB.buttonName = "Go";
                dictNextState[buttonB.buttonName] = READ_STATE.STATE_PLAYBACK;
                strQuestion = GameManager.instance.DialogTrigger("reading_option", DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT);
                buttonB.buttonEnabled = true;
                buttonA.buttonEnabled = buttonC.buttonEnabled = false;
                strExample = "idea";
                break;

            case READ_STATE.STATE_PLAYBACK:     // confirm new story mode 
                strQuestion = GameManager.instance.DialogTrigger("reading_playback", DialogTrigger.TRIGGER_TYPE.TRIGGER_STAY,
                                                                FormulateSentence(storyActive, sentenceActive));
                //start with generic, but move to individual if possible
                strExample = "printing";
                if (TaskReading.DICT_STORY_EX[storyActive].listExamples.Count > sentenceActive) 
                {
                    //NOTE: we will have some misalignment between word and sentence, but GIMMIE a BREAK! ;)
                    strExample = listAnswers[sentenceActive];
                }

                sentenceActive++;
                if (sentenceActive < TaskReading.DICT_STORY_EX[storyActive].listSentences.Count)
                {
                    buttonB.buttonName = "Next";
                    dictNextState[buttonB.buttonName] = READ_STATE.STATE_PLAYBACK;
                }
                else 
                {
                    buttonB.buttonName = "Done";
                    dictNextState[buttonB.buttonName] = READ_STATE.STATE_PLAYBACK_EXIT;
                }
                buttonB.buttonEnabled = true;
                buttonA.buttonEnabled = buttonC.buttonEnabled = false;
                break;

            case READ_STATE.STATE_PLAYBACK_EXIT:     // confirm new story mode 
                sentenceActive = 0;     // reset index for re-read
                buttonC.buttonName = "Restart";
                dictNextState[buttonC.buttonName] = READ_STATE.STATE_TRAIN_EXIT;
                buttonA.buttonName = "Replay";
                dictNextState[buttonA.buttonName] = READ_STATE.STATE_PLAYBACK;
                strQuestion = GameManager.instance.DialogTrigger("reading_option", DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT);
                buttonB.buttonEnabled = false;
                buttonA.buttonEnabled = buttonC.buttonEnabled = true;
                strExample = "recycle";
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

    // method to offer options
    protected string FormulateOptions(string nameStory, int idxExample) 
    {
        //randomize options for each round
        List<string> listRandAnswers = GameManager.instance.ShuffleList<string>(
            TaskReading.DICT_STORY_EX[nameStory].listExamples[idxExample].listOptions);
        int numItems = listRandAnswers.Count;
        if (numItems > 0)
            buttonC.buttonName = listRandAnswers[0];
        if (numItems > 1)
            buttonA.buttonName = listRandAnswers[1];
        if (numItems > 2)
            buttonB.buttonName = listRandAnswers[2];
        
        return string.Join(", ", listRandAnswers.ToArray());        
    }

    // method to replay story with options
    protected string FormulateSentence(string nameStory, int idxSentence)
    {
        return string.Format(TaskReading.DICT_STORY_EX[nameStory].listSentences[idxSentence], listAnswers.ToArray());
    }

    // method to update screen with example graphics
    protected void ExampleUpdate(string uniImage)
    {
        if (!string.IsNullOrEmpty(uniImage) && DICT_IMAGE_EX.ContainsKey(uniImage))
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

    override protected void ReceiveDialogTrigger(string nameTrigger, DialogTrigger.TRIGGER_TYPE typeTrigger)
    {
        if (typeTrigger != DialogTrigger.TRIGGER_TYPE.TRIGGER_EXIT)     //enter -> start
        {
            SetReadState(READ_STATE.STATE_START);
        }
        else    // exit -> reset
        {
            SetReadState(READ_STATE.STATE_RESET);
        }
    }

    public void ReceiveButtonPress(object o, string buttonName) 
    {
        // do anything?
        //Debug.Log(string.Format("[TaskReading]: PRESS '{0}' from {1}", buttonName, o));
    }

    public void ReceiveButtonRelease(object o, string buttonName) 
    {
        if (dictNextState.ContainsKey(buttonName))
        {
            READ_STATE stateNew = dictNextState[buttonName];
            if (READ_STATE.STATE_OPTION_CONFIRM == stateNew ||  stateNew == READ_STATE.STATE_OPTION_EXIT)    // save name as result
            {
                listAnswers.Add(buttonName);
            }
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
            { "welcome", new ImageExample("read_welcome", "Welcome!") },
            { "training", new ImageExample("read_training", "Corporate Training") },
            { "monkey", new ImageExample("read_monkey", "is this you?") },
            { "tigers", new ImageExample("read_tiger", "tiger") },
            { "cheetahs", new ImageExample("read_cheetahs", "cheetahs") },
            { "children", new ImageExample("read_children", "children") },
            { "rainforest", new ImageExample("read_rainforest", "rainforest") },
            { "grasslands", new ImageExample("read_grassland", "grasslands") },
            { "Martian dunes", new ImageExample("read_mars", "Martian dunes") },
            { "grass", new ImageExample("read_grassland", "grass") },
            { "trees", new ImageExample("read_trees", "trees") },
            { "sponges", new ImageExample("read_sponge", "sponges") },
            { "prey", new ImageExample("read_eagle", "prey") },
            { "Jupiters", new ImageExample("read_jupiter", "Jupiter") },
            { "phones", new ImageExample("read_phones", "phones") },
            { "animals", new ImageExample("read_kittens", "animals") },
            { "people", new ImageExample("read_people", "people") },
            { "pens", new ImageExample("read_pens", "pens") },
            { "sleeps on", new ImageExample("read_baby", "sleeps on") },
            { "eats", new ImageExample("read_pumpkin", "eats") },
            { "draws on", new ImageExample("read_children", "draws on") },
            { "India", new ImageExample("read_india", "India") },
            { "the Bahamas", new ImageExample("read_beach", "Bahamas") },
            { "the Amazon", new ImageExample("read_rainforest", "Amazon Rainforest") },
            { "idea", new ImageExample("read_idea", "question") },
            { "printing", new ImageExample("read_library", "mandatory reading") },
            { "recycle", new ImageExample("read_scrabble", "always recycle") },
        };
    }

    // ------------------------------------
    // methods for fill-in-the-blank method

    [SerializeField]
    public class SentenceExample 
    {
        public string textType;             // text of what is being said, use format references like {0} for blank
        public List<string> listOptions;    // list of options (should exist in image examples!)

        public SentenceExample(string _typeNew, List<string> _listOptions) 
        {
            textType = _typeNew;
            listOptions = _listOptions;
        }
    }

    [SerializeField]
    public class StoryExample 
    {
        public string nameStory;
        public List<string> listSentences;
        public List<SentenceExample> listExamples;
        
        public StoryExample(string _name, List<string> _listSentences, List<SentenceExample> _listExamples) 
        {
            nameStory = _name;
            listSentences = _listSentences;
            listExamples = _listExamples;
        }
    }

    // static reference for image examples
    static protected Dictionary<string, StoryExample> DICT_STORY_EX = null;

    protected void LoadStoryExamples()
    {
        if (TaskReading.DICT_STORY_EX != null)
            return;

        // NOTE: this is a nested data array!
        //  StoryExample -> (name + sentences (below)
        //      SentenceExample -> (text + options (images)
        //  The parser will unroll all of these options during runtime.
        TaskReading.DICT_STORY_EX = new Dictionary<string, StoryExample>{
            { "animals", new StoryExample(
                "animals", new List<string>{
                    "Many {0} live in the {1}.", "They can blend in with the {2} to help them catch {3}.",
                    "Different {3} are {4} that a hunter {5}.", "Many {0} are often found in {6}.", "I like {0}, don't you?" },
                new List<SentenceExample>{
                    new SentenceExample("animal", new List<string>{
                        "tigers", "cheetahs", "children" 
                    }),
                    new SentenceExample("place", new List<string>{
                        "rainforest", "grasslands", "Martian dunes" 
                    }),
                    new SentenceExample("environment", new List<string>{
                        "grass", "trees", "sponges" 
                    }),
                    new SentenceExample("object", new List<string>{
                        "prey", "Jupiters", "phones" 
                    }),
                    new SentenceExample("type", new List<string>{
                        "animals", "people", "pens" 
                    }),
                    new SentenceExample("action", new List<string>{
                        "sleeps on", "eats", "draws on" 
                    }),
                    new SentenceExample("place", new List<string>{
                        "India", "the Bahamas", "the Amazon" 
                    })
                })  //end examples within story
            },  //end a story
        };  //end dictionary init

    }   //end load function

}