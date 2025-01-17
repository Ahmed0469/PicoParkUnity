using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelItem : MonoBehaviour
{
    public UnityEvent onCollisionEnter = new();
    public UnityEvent<Collision2D,GameObject> onCollisionEnterNew = new();
    public UnityEvent onCollisionStay = new();
    public UnityEvent<Collision2D, GameObject> onCollisionStayNew = new();
    public UnityEvent onCollisionExit = new();
    public UnityEvent<Collision2D, GameObject> onCollisionExitNew = new();
    public UnityEvent onTriggerEnter = new();
    public UnityEvent<Collider2D, GameObject> onTriggerEnterNew = new();
    public UnityEvent onTriggerStay = new();
    public UnityEvent<Collider2D, GameObject> onTriggerStayNew = new();
    public UnityEvent onTriggerExit = new();     
    public UnityEvent<Collider2D, GameObject> onTriggerExitNew = new();
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (onCollisionEnter != null)
        {
            onCollisionEnter.Invoke();
        }
        if (onCollisionEnterNew != null)
        {
            onCollisionEnterNew.Invoke(collision, gameObject);
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (onCollisionStay != null)
        {
            onCollisionStay.Invoke();
        }
        if (onCollisionStayNew != null)
        {
            onCollisionStayNew.Invoke(collision, gameObject);
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (onCollisionExit != null)
        {
            onCollisionExit.Invoke();
        }
        if (onCollisionExitNew != null)
        {
            onCollisionExitNew.Invoke(collision, gameObject);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (onTriggerEnter != null)
        {
            onTriggerEnter.Invoke();
        }
        if (onTriggerEnterNew != null)
        {
            onTriggerEnterNew.Invoke(collision, gameObject);
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (onTriggerStay != null)
        {
            onTriggerStay.Invoke();
        }
        if (onTriggerStayNew != null)
        {
            onTriggerStayNew.Invoke(collision, gameObject);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (onTriggerExit != null)
        {
            onTriggerExit.Invoke();
        }
        if (onTriggerExitNew != null)
        {
            onTriggerExitNew.Invoke(collision, gameObject);
        }
    }
}
