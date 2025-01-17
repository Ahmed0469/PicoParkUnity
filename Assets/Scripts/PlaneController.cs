using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
public class PlaneController : NetworkBehaviour
{
    public bool isShield = false;
    public override void Spawned()
    {
        var players = FindObjectsOfType<Visyde.PlayerController>();
        transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).transform);
        if (isShield)
        {
            transform.position = Vector3.zero;
        }
        else
        {
            transform.localPosition = Vector3.zero;
        }
        transform.localRotation = Quaternion.identity;
    }
}
