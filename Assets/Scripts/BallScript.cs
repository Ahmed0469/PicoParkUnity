using UnityEngine;
using Fusion;
using Visyde;
using System;

public class BallScript : NetworkBehaviour
{
    [Networked] public Vector2 networkPos { get; set; }
    Vector2 velocity = Vector3.zero;
    Vector2 lastPos;
    float lag;
    Rigidbody2D rb;
    public Action<Collision2D,GameObject> OnCollisionEnter;
    public Action<Collision2D,GameObject> OnCollisionExit;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
        {
            transform.position = networkPos;
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            networkPos = transform.position;
        }
    }
    private void FixedUpdate()
    {
        if (!Object.HasStateAuthority)
        {
            transform.position = Vector2.SmoothDamp(transform.position, networkPos, ref velocity, 0.1f);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (OnCollisionEnter != null)
        {
            OnCollisionEnter(collision,gameObject);
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (OnCollisionExit != null)
        {
            OnCollisionExit(collision, gameObject);
        }
    }
}
