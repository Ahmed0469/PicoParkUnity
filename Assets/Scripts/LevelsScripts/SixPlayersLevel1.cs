using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visyde;
using Fusion;

public class SixPlayersLevel1 : NetworkBehaviour
{
    public Animator wallAnimator;
    public ContactFilter2D playerContactFilter;
    float timeElapsed = 0;
    int lastHitValue = 0;
    private void Update()
    {
        //if (hits != 0 && hits != 10)
        //{
        //    timeElapsed += Time.deltaTime;
        //    lastHitValue = hits;
        //    //if (timeElapsed > 2 && hits == lastHitValue)
        //    //{
        //    //    timeElapsed = 0;
        //    //    lastHitValue = 0;
        //    //    hits = 0;
        //    //    wallAnimator.SetInteger("Hits", hits);
        //    //}
        //}
    }
    public void BoostPlayerSpeed(GameObject boosterTriggerObj)
    {
        Collider2D[] colliders = new Collider2D[3];
        boosterTriggerObj.GetComponent<BoxCollider2D>().OverlapCollider(playerContactFilter,colliders);
        for (int i = 0; i < colliders.Length; i++)
        {
            Debug.Log("Boooost Before");
            if (colliders[i] != null /*&& colliders[i].GetComponent<Photon.Pun.PhotonView>().IsMine*/)
            {
                Debug.Log("Boooost");
                colliders[i].GetComponent<PlayerController>().stopRegularVelocity = true;
            }
        }
    }
    int hits = 0;
    public void SwitchWallStates(GameObject wallStateManagingCollider)
    {
        if (!Object.Runner.IsServer) return;

        Collider2D[] colliders = new Collider2D[1];
        wallStateManagingCollider.GetComponent<BoxCollider2D>().OverlapCollider(playerContactFilter, colliders);
        Debug.Log("Outside");
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null /*&& colliders[i].GetComponent<Photon.Pun.PhotonView>().IsMine*/)
            {
                var movementController = colliders[i].GetComponent<PlayerController>();
                if (movementController.stopRegularVelocity)
                {
                    hits++;
                    timeElapsed = 0;
                    wallAnimator.SetInteger("Hits", hits);
                    Debug.Log("Inside");
                    colliders[i].GetComponent<PlayerController>().stopRegularVelocity = false;
                }
            }
        }
    }
}
