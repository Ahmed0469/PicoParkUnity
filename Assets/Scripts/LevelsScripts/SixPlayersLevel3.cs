using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;

public class SixPlayersLevel3 : NetworkBehaviour
{
    public LineRenderer lineRenderer;
    public PlayerController[] players;
    public override void Spawned()
    {
        StartCoroutine(WaitSpawn());
    }
    IEnumerator WaitSpawn()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        players = FindObjectsOfType<PlayerController>();
        for (int i = players.Length - 1; i >= 0; i--)
        {
            var playerSpringJoint = players[i].gameObject.GetComponent<SpringJoint2D>();
            playerSpringJoint.enabled = true;
            if (i == 0)
            {
                playerSpringJoint.connectedBody = players[i + 1].rg;
            }
            else
            {
                playerSpringJoint.connectedBody = players[i - 1].rg;
            }
            playerSpringJoint.autoConfigureDistance = false;
            playerSpringJoint.distance = 1.34f;
            playerSpringJoint.dampingRatio = 1;
            playerSpringJoint.frequency = 1.5f;
            playerSpringJoint.enableCollision = true;
        }
        lineRenderer.positionCount = players.Length;
    }
    // Start is called before the first frame update
    void Start()
    {
        GameManager.instance.deadZone = true;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < players.Length; i++)
        {
            lineRenderer.SetPosition(i,players[i].transform.position + new Vector3(0,0.36f));
        }
    }
}
