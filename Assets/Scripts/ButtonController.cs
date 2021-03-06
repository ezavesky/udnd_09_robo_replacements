﻿using UnityEngine;
using UnityEngine.UI;
using VRTK;
using UnityEngine.Events;

public class ButtonController : MonoBehaviour
{
    protected VRTK.Controllables.PhysicsBased.VRTK_PhysicsPusher controllable;
    protected Text displayText;

    public Color colorPushed;
    protected Color colorNormal;
    protected MeshRenderer renderObject;

    [System.Serializable]
    public sealed class ButtonPressed : UnityEvent<object, string>{};
    [SerializeField]
    public ButtonPressed OnPressed = new ButtonPressed();
    public ButtonPressed OnReleased = new ButtonPressed();
    protected bool wasPressed = false;

    public string buttonName
    {
        get
        {
            if (displayText) 
            {
                return displayText.text;
            }
            return null;
        }
        set
        {
            if (displayText) 
            {
                displayText.text = value;
            }
        }
    }

    public bool buttonEnabled
    {
        get
        {
            if (controllable)
            {
                return controllable.gameObject.activeSelf;
            }
            return false;
        }
        set
        {
            if (controllable)
            {
                controllable.gameObject.SetActive(value);
            }
        }
    }

    void Awake()
    {
        controllable = GetComponentInChildren<VRTK.Controllables.PhysicsBased.VRTK_PhysicsPusher>();
        if (controllable)
        {
            /*
            Rigidbody rb = controllable.gameObject.GetComponent<Rigidbody>();
            if (rb)     // forcibly update the rigid body
            {
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            }
            */
            controllable.ValueChanged += ValueChanged;
            renderObject = controllable.GetComponent<MeshRenderer>();
            colorNormal = renderObject.material.color;
        }
        displayText = GetComponentInChildren<Text>();
    }

    protected virtual void ValueChanged(object sender, VRTK.Controllables.ControllableEventArgs e)
    {
        if (controllable.AtMaxLimit() )
        {
            if (!wasPressed)
            {
                OnPressed.Invoke(sender, buttonName);
                wasPressed = true;
                renderObject.material.color = colorPushed;

                // Debug.Log(string.Format("[ButtonController]: PRESS {3} {0}, min {1}, max {2}", e.value, controllable.AtMinLimit(), controllable.AtMaxLimit(), buttonName));
            }
        }
        else if (wasPressed)
        {
            if (controllable.IsResting()) 
            {
                //Debug.Log(string.Format("[ButtonController]: RELEASE {3}, {0}, min {1}, max {2}, resting {4}", e.value, controllable.AtMinLimit(), controllable.AtMaxLimit(), buttonName, controllable.IsResting()));
                OnReleased.Invoke(sender, buttonName);
                wasPressed = false;
                renderObject.material.color = colorNormal;
            }
        }
        /*
        if (displayText != null)
        {
            displayText.text = e.value.ToString("F1");
        }
        */
    }

}