using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class HatController : NetworkBehaviour
{
    public override void Spawned()
    {
        var players = FindObjectsOfType<Visyde.PlayerController>();
        transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).character.hatPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
