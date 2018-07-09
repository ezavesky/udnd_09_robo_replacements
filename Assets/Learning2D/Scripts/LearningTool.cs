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

	[NonSerialized]
	protected static System.Object thisLock = new System.Object ();	//lock for matrix update

	// Use this for initialization
	void Start () {
		
	}
	
    public void ClassArchive(string nameClass) 
    {
        lock (LearningTool.thisLock) {
            PrimitiveSet localSet = new PrimitiveSet();
            for (int i=0; i<transform.childCount; i++)  // loop over all available points
            {
                GameObject objTarget = transform.GetChild(i).gameObject;
                if (objTarget.activeSelf) {                
                    Mouse3D apiOther = objTarget.GetComponent<Mouse3D>();
                    localSet.listPoints.Add(new PrimitivePoint(apiOther.nameType, objTarget.transform.localPosition));
                }
            } 
            localSet.setName = nameClass;       // append the class name 
            listSets.Add(localSet);     // add our last set member

            Debug.Log(string.Format("[LearningTool]: new class '{0}' contained {1} entires; {2} total sets", 
                nameClass, localSet.listPoints.Count, listSets.Count));

    	    SetsSave();
        }
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

        public PrimitivePoint() 
        {
            ptType = "(unknown)";
            ptPosition = Vector3.zero;
        }

        public PrimitivePoint(string _type, Vector3 _pos) 
        {
            ptType = _type;
            ptPosition = _pos;
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

	protected void SetsSave() {
		lock (LearningTool.thisLock) {
			// fusion of two c# scripts (currently just saves in XML)
			//   https://answers.unity.com/questions/972594/trying-to-learn-to-save-can-i-please-see-some-basi.html
			//   https://docs.unity3d.com/Manual/JSONSerialization.html

			// string filepath = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location)
			// 						.DirectoryName + "/game_configuration.xml";
			string filepath = Application.persistentDataPath + "/shape_sets.xml";
			Debug.Log(String.Format("[LearningTool]: {0} items saved to '{1}'", listSets.Count, filepath));

			System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(List<PrimitiveSet>));
			System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
			writer.Serialize(file, listSets);
			file.Close();
		}
     }

}
