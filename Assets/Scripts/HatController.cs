using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using Visyde;
using UnityEngine.SceneManagement;

public class HatController : NetworkBehaviour
{
    public CosmeticType type;
    public override void Spawned()
    {
        var players = FindObjectsOfType<Visyde.PlayerController>();
        switch (type)
        {
            case CosmeticType.Hat:
                transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).character.hatPoint);
                transform.localScale = Vector3.one;
                break;
            case CosmeticType.Glasses:
                transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).character.glassesPoint);
                transform.localScale = Vector3.one;
                break;
            case CosmeticType.Necklace:
                transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).character.necklacePoint);
                break;
        }
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            var players = FindObjectsOfType<Visyde.PlayerController>();
            switch (type)
            {
                case CosmeticType.Hat:
                    transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).character.hatPoint);
                    transform.localScale = Vector3.one;
                    break;
                case CosmeticType.Glasses:
                    transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).character.glassesPoint);
                    transform.localScale = Vector3.one;
                    break;
                case CosmeticType.Necklace:
                    transform.SetParent(players.ToList().Find(playerr => playerr.Object.InputAuthority == Object.InputAuthority).character.necklacePoint);
                    break;
            }
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
