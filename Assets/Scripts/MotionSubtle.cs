using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionSubtle : MonoBehaviour {
    public float speedMovement = 20f;   // how many seconds for full cycle?
    public float distHover = 0f;
    public bool allowRotate = true;
    protected float randStart = 0.0f;

	// Use this for initialization
	void Start () {
		//randomize rotation in global Y
        if (allowRotate) 
        {
            Vector3 euler = transform.eulerAngles;
            euler.y = Random.Range(0f, 360f);
            transform.eulerAngles = euler;            
        }
        if (distHover > 0.0f)
        {
            randStart = Random.Range(0f, 10f);
        }
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		// ...also rotate around the World's Y axis
        if (allowRotate) 
        {
            transform.Rotate(Vector3.up * Time.fixedDeltaTime * speedMovement, Space.World);
        }
        if (distHover > 0.0f)
        {
            //https://forum.unity.com/threads/sin-movement-on-y-axis.10357/
            transform.position += distHover * (Mathf.Sin(2*Mathf.PI*speedMovement*(Time.fixedTime+randStart)) 
                                - Mathf.Sin(2*Mathf.PI*speedMovement*((Time.fixedTime+randStart) - Time.fixedDeltaTime)))*transform.up;
        }
		//Debug.Log(string.Format("Rotate: {0}", transform.eulerAngles));
	}
	
}
