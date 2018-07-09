using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mouse3D : MonoBehaviour {
	private Vector3 screenPoint;
	private Vector3 offset;
    private Rigidbody rb;
    public string nameType;

    public float rotationSpeed = 15f;
    protected bool stateClicked = false;

    public Transform transformCreate = null;
    protected GameObject objIndicator = null;
    public GameObject objPrefab = null;
    protected Mouse3D apiCreator = null;

    protected List<GameObject> pooledItem = null; //the object pool
    protected const int poolCount = 20;

   
    void Start()
    {
        if (objIndicator) 
        {
            objIndicator.SetActive(stateClicked);
        }
        rb = GetComponent<Rigidbody>();

        if (transformCreate && objPrefab)        // if we had a link to where to transform, create itme pool
        {
            pooledItem = new List<GameObject>();
            Mouse3D apiMouse = null;
            for (int i=0; i<poolCount; i++)
            {
                GameObject objNew = Instantiate(objPrefab, transformCreate.position, 
                    transformCreate.rotation, transformCreate);
                objNew.SetActive(false);
                apiMouse = objNew.GetComponent<Mouse3D>();
                if (apiMouse)
                {
                    apiMouse.apiCreator = this;
                    apiMouse.stateClicked = false;
                    if (objNew.transform.childCount==1)
                    {
                        objNew.transform.GetChild(0).gameObject.SetActive(false);
                    }
                }
                pooledItem.Add(objNew);
            }
        }
    }

    void Update() 
    {
        // if item is "clicked" and mouse button is down
        if (stateClicked && Input.GetMouseButton(1))
        {
            RotateObject();
        }
    }

     void RotateObject()
     {
        //Get mouse position
        Vector3 mousePos = Input.mousePosition;
         
        //Adjust mouse z position
        mousePos.z = Camera.main.transform.position.y - transform.position.y;   
 
        //Get a world position for the mouse
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);   
 
        //Get the angle to rotate and rotate
        float angle = -Mathf.Atan2(transform.position.z - mouseWorldPos.z, transform.position.x - mouseWorldPos.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, angle, 0), rotationSpeed * Time.deltaTime);
        //Debug.Log(string.Format("Rotate {0}, rotation {1}", angle, transform.rotation));
     }

	void OnMouseDown()
    {
        if (transformCreate)    // indicates this object is there to be cloned
        {
            if (pooledItem != null && pooledItem.Count != 0) 
            {
                GameObject objClone = pooledItem[0];
                pooledItem.RemoveAt(0);
                objClone.transform.position = transformCreate.position;
                objClone.SetActive(true);
            }
        }
        else    // else, just move this object
        {
            screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(
                        new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
            bool newStateClicked = !stateClicked;
            if (newStateClicked)   //if activating this one, deactivate others
            {
                foreach (Mouse3D apiMouse in transform.parent.gameObject.GetComponentsInChildren<Mouse3D>())
                {
                    apiMouse.stateClicked = false;
                    if (apiMouse.objIndicator != null)
                    {
                        apiMouse.objIndicator.SetActive(false);
                    }
                }
            }
            //finally, update th estate for our self object
            stateClicked = newStateClicked;
            if (objIndicator==null && transform.childCount==1)
            {
                objIndicator = transform.GetChild(0).gameObject;
            }
            if (objIndicator != null)
            {
                objIndicator.SetActive(newStateClicked);
            }

        }
	}
		
    void OnMouseUp()    // mouse release, stop all rigid body rotations
    {
        if (transformCreate == null)        // indicates it should be deleted
        {
            foreach (Mouse3D apiMouse in transform.parent.gameObject.GetComponentsInChildren<Mouse3D>())
            {
                apiMouse.RotateStop();
            }
        }
    }

    void RotateStop()
    {
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

	void OnMouseDrag()
    {
        if (transformCreate == null)
        {
            Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;
            transform.position = cursorPosition;
        }
	}

    private void OnTriggerEnter(Collider other)
    {
        if (transformCreate == null)        // indicates it should be deleted
        {
            Mouse3D apiOther = other.gameObject.GetComponent<Mouse3D>();
            if (apiOther && apiOther.apiCreator)
            {
                other.gameObject.SetActive(false);  //disable
                apiOther.apiCreator.pooledItem.Add(other.gameObject);   // insert item back into list
            }
        }
    }

    public void RemoveFromBoard()
    {
        if (apiCreator)
        {
            gameObject.SetActive(false);  //disable
            apiCreator.pooledItem.Add(gameObject);   // insert item back into list
        }
    }


}
