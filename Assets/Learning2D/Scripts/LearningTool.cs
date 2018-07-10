using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


public class LearningTool : MonoBehaviour {

    public Text textClassifier;     //output for classifier
    public Text textLabels;     //output for classifier

    public Text textNextButton;     //trigger next pattern load
    public Text textPrevButton;     //trigger prev pattern load
    protected int idxSet = 0;       //index for current set

    protected string pathArchive;

	[NonSerialized]
	protected static System.Object thisLock = new System.Object ();	//lock for matrix update

	// Use this for initialization
	void Start () {
		pathArchive = Application.persistentDataPath + "/shape_sets.xml";
        SetsLoad();
	}
	
    public void ClassSave(string nameClass) 
    {
        lock (LearningTool.thisLock) {
            PrimitiveSet localSet = new PrimitiveSet();
            for (int i=0; i<transform.childCount; i++)  // loop over all available points
            {
                GameObject objTarget = transform.GetChild(i).gameObject;
                if (objTarget.activeSelf) {                
                    Mouse3D apiOther = objTarget.GetComponent<Mouse3D>();
                    localSet.listPoints.Add(new PrimitivePoint(apiOther.nameType, objTarget.transform.localPosition, objTarget.transform.localEulerAngles));
                }
            } 
            localSet.setName = nameClass;       // append the class name 
            listSets.Add(localSet);     // add our last set member

            Debug.Log(string.Format("[LearningTool]: new class '{0}' contained {1} entires; {2} total sets", 
                nameClass, localSet.listPoints.Count, listSets.Count));

    	    SetsSave();
        }
    }

    //reload a sample in a specific direction (-1=previous, +1=next, 0=current)
    public void ClassReload(int idxDirection) 
    {
        ClassLoad((idxSet + idxDirection + listSets.Count) % listSets.Count);

    }

    public void ClassLoad(int idxNew) 
    {
        Debug.Log(string.Format("[LearningTools]: Class index load {0}", idxNew));
        //TODO: method for reloading class sample
        //  clear board
        //  walk through each shape in set
        //      reset position to local position
        //      reset angle by local euler angle
        //  reset a color/text indicator for what you voted
    }

    public void ClearBoard()
    { 
        for (int i=0; i<transform.childCount; i++)
        {
            GameObject objTarget = transform.GetChild(i).gameObject;
            if (objTarget.activeSelf) {                
                Mouse3D apiOther = objTarget.GetComponent<Mouse3D>();
                if (apiOther)
                {
                    apiOther.RemoveFromBoard();
                }
            }
        }
    }


    // components to serialize data
    
    [Serializable]
    public class PrimitivePoint
    {
        public string ptType;
        public Vector3 ptPosition;
        public Vector3 ptRotate;

        public PrimitivePoint() 
        {
            ptType = "(unknown)";
            ptPosition = Vector3.zero;
            ptRotate = Vector3.zero;
        }

        public PrimitivePoint(string _type, Vector3 _pos, Vector3 _angle) 
        {
            ptType = _type;
            ptPosition = _pos;
            ptRotate = _angle;
        }
    }

    [Serializable]
    public class PrimitiveSet
    {
        public List<PrimitivePoint> listPoints;
        public string setName;
        public string setUni;
        public string setDatetime;

        public PrimitiveSet() 
        {
            setUni = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}",DateTime.Now); 
            setDatetime = DateTime.Now.ToString("o");
            listPoints = new List<PrimitivePoint>();
        }
    }

    protected List<PrimitiveSet> listSets = new List<PrimitiveSet>();

    public void SetsRecount() 
    {
        // Labels: cars:x, flowers:y, house:z
        Dictionary<string, int> dictCounts = new Dictionary<string, int>();
        for (int i=0; i<listSets.Count; i++) 
        {
            if (!dictCounts.ContainsKey(listSets[i].setName))
            {
                dictCounts[listSets[i].setName] = 0;
            }
            dictCounts[listSets[i].setName]++;
        }
        if (textLabels != null)
        {
            string strCounts = "Labels: ";
            foreach (KeyValuePair<string,int> kvp in dictCounts)
            {
                strCounts += string.Format("{0} {1},", kvp.Key, kvp.Value);
            }
            textLabels.text = strCounts;
        }

        //update previous/next buttons
        int idxTest;
        if (textNextButton)
        {
            idxTest = (idxSet+1) % listSets.Count;
            textNextButton.text = string.Format("{0} >", idxTest);
        }
        if (textPrevButton)
        {
            idxTest = (idxSet-1 + listSets.Count) % listSets.Count;
            textPrevButton.text = string.Format("< {0}", idxTest);
        }
    }

	public void SetsSave() {
		lock (LearningTool.thisLock) {
			// fusion of two c# scripts (currently just saves in XML)
			//   https://answers.unity.com/questions/972594/trying-to-learn-to-save-can-i-please-see-some-basi.html
			//   https://docs.unity3d.com/Manual/JSONSerialization.html

			// string filepath = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location)
			// 						.DirectoryName + "/game_configuration.xml";

			Debug.Log(String.Format("[LearningTool]: {0} items being saved to '{1}'", listSets.Count, pathArchive));
			System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(List<PrimitiveSet>));
			System.IO.StreamWriter file = new System.IO.StreamWriter(pathArchive);
			writer.Serialize(file, listSets);
			file.Close();

            SetsRecount();
		}
     }

    public void SetsLoad() {
		lock (LearningTool.thisLock) {
            listSets = new List<PrimitiveSet>();    //start with prior
			try {
				System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(List<PrimitiveSet>));
				System.IO.StreamReader file = new System.IO.StreamReader(pathArchive);
				listSets = (List<PrimitiveSet>)reader.Deserialize(file);
				file.Close();
    			Debug.Log(String.Format("[LearningTool]: {0} items loaded from '{1}'", listSets.Count, pathArchive));
			}
			catch (System.IO.FileNotFoundException e)  {  
				Debug.LogWarning("[HistoryLoad]: File not found... "+e);
			}  
			catch (System.Xml.XmlException e)  {  
				Debug.LogWarning("[HistoryLoad]: File corrupt... "+e);
			}  
			catch (Exception e)  {  
				Debug.LogWarning("[HistoryLoad]: File not found... "+e);
			}  

            SetsRecount();
		}
     }

}
