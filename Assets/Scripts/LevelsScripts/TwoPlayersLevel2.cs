using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Visyde;
using Fusion;

public class TwoPlayersLevel2 : NetworkBehaviour
{
    [Networked] private Vector3 networkElevatorPos { get; set; }
    Vector2 veloc = Vector3.zero;
    public TextMeshPro launchText;
    public TextMeshPro elevatorText;
    public ContactFilter2D contactFilter;
    public BoxCollider2D playersLaunchPad;
    public BoxCollider2D playerElevator;
    public Transform fallPosOne;
    public Transform fallPosTwo;
    Vector2 elevatorDefaultPos;
    float elapsedTime = 0;
    public override void Spawned()
    {
        elevatorDefaultPos = playerElevator.transform.parent.position;
    }
    // Update is called once per frame
    void Update()
    {
        if (!Object.HasStateAuthority)
        {
            playerElevator.transform.parent.position = Vector2.SmoothDamp(playerElevator.transform.parent.position, networkElevatorPos, ref veloc, 0.1f);
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            var colliders = new Collider2D[10];
            playerElevator.OverlapCollider(contactFilter, colliders);
            var collideds = colliders.ToList().FindAll(col => col != null);
            RPC_ElevatorText(collideds.Count);
            if (collideds.Count == Connector.instance.networkRunner.SessionInfo.PlayerCount)
            {
                playerElevator.transform.parent.position = Vector2.Lerp(playerElevator.transform.parent.position, new Vector3(playerElevator.transform.parent.position.x, 5), Time.deltaTime / 2);
                elapsedTime = 0;
            }
            else
            {
                if (elapsedTime > 1)
                {
                    playerElevator.transform.parent.position = Vector2.Lerp(playerElevator.transform.parent.position, elevatorDefaultPos, Time.deltaTime * 1.3f);
                }
                else
                {
                    elapsedTime += Time.deltaTime;
                }
            }
            networkElevatorPos = playerElevator.transform.parent.position;
        }
    }
    public void OnLaunchBtn(GameObject btn)
    {
        if (!Connector.instance.networkRunner.IsServer)
        {
            return;
        }
        var colliders = new Collider2D[1];
        btn.GetComponent<BoxCollider2D>().OverlapCollider(contactFilter,colliders);
        Debug.Log("Before");
        if (colliders[0] != null)
        {
            Debug.Log("After");
            StartCoroutine(Launch());
        }
    }
    public void OnFallTriggerOne(GameObject trigger)
    {
        var colliders = new Collider2D[1];
        trigger.GetComponent<Collider2D>().OverlapCollider(contactFilter, colliders);
        if (colliders[0] != null)
        {
            colliders[0].transform.position = fallPosOne.position;
        }
    }
    public void OnFallTriggerTwo(GameObject trigger)
    {
        var colliders = new Collider2D[1];
        trigger.GetComponent<Collider2D>().OverlapCollider(contactFilter, colliders);
        if (colliders[0] != null)
        {
            colliders[0].transform.position = fallPosTwo.position;
        }
    }
    public float launchForce;
    IEnumerator Launch()
    {
        RPC_LaunchingText(0);
        yield return new WaitForSecondsRealtime(3);        
        RPC_LaunchingText(1);
        yield return new WaitForSecondsRealtime(1);
        RPC_LaunchingText(2);
        yield return new WaitForSecondsRealtime(1);
        RPC_LaunchingText(3);
        var colliders = new Collider2D[10];
        playersLaunchPad.OverlapCollider(contactFilter, colliders);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                var playerController = colliders[i].GetComponent<PlayerController>();
                //playerController.rg.AddForce(Vector2.right * launchForce);
                Vector2 veloc = playerController.rg.velocity;
                //veloc.y = 22;
                veloc.x = 700;
                RPC_PlayerLaunchSound();
                playerController.rg.velocity = veloc;

                // Don't allow jumping right after a jump:
                playerController.allowJump = false;
            }
        }
    }
    [Rpc]
    public void RPC_PlayerLaunchSound()
    {
        SoundManager.instance.PlayOneShot(SoundManager.instance.JumperSFX);
    }
    [Rpc]
    public void RPC_ElevatorText(int collidesCount)
    {
        elevatorText.text = Connector.instance.networkRunner.SessionInfo.PlayerCount - collidesCount + "";
    }
    [Rpc]
    public void RPC_LaunchingText(int textState)
    {
        switch (textState)
        {
            case 0:
                launchText.gameObject.SetActive(true);
                launchText.text = "LAUNCHING in 3...";
                break;
            case 1:
                launchText.text = "LAUNCHING in 2...";
                break;
            case 2:
                launchText.text = "LAUNCHING in 1...";
                break;
            case 3:
                launchText.gameObject.SetActive(false);
                break;
        }
    }
}
