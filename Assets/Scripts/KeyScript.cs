using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visyde;
using Fusion;

public class KeyScript : NetworkBehaviour
{
    [Networked] private Vector3 networkPos { get; set; }
    Vector2 velocity = Vector3.zero;
    public PlayerController player;
    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
        {
            transform.position = networkPos;
        }
    }
    private void Update()
    {
        if(GameManager.gameMode == Visyde.GameMode.Multiplayer)
        {
            if (Object.HasStateAuthority)
            {
                if (player != null)
                {
                    transform.position = Vector2.Lerp
                        (
                        transform.position,
                        player.transform.position,
                        Time.deltaTime * 5
                        );
                }
                networkPos = transform.position;
            }
            if (!Object.HasStateAuthority)
            {
                transform.position = Vector2.SmoothDamp(transform.position, networkPos, ref velocity, 0.1f);
            }
        }
        else
        {
            if (player != null)
            {
                transform.position = Vector2.Lerp
                    (
                    transform.position,
                    player.transform.position,
                    Time.deltaTime * 5
                    );
            }
        }       
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(GameManager.gameMode == Visyde.GameMode.Multiplayer)
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }
            if (GameManager.instance.gameStarted && collision.gameObject.CompareTag("Player"))
            {
                if (player == null)
                {
                    player = collision.gameObject.GetComponent<PlayerController>();
                    player.hasDoorKey = true;
                    player.keyObj = this.gameObject;
                }
            }
        }
        else
        {
            if (GameManager.instance.gameStarted && collision.gameObject.CompareTag("Player"))
            {
                if (player == null)
                {
                    player = collision.gameObject.GetComponent<PlayerController>();
                    player.hasDoorKey = true;
                    player.keyObj = this.gameObject;
                }
            }
        }      
    }
}
