using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;

public class EightPlayersLevel4 : NetworkBehaviour
{
    public GameObject shieldPrefabForward;
    public GameObject shieldPrefabUpward;
    public List<LineRenderer> lineRenderers = new List<LineRenderer>();
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public override void Spawned()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            StartCoroutine(WaitSpawn());
        }
    }
    IEnumerator WaitSpawn()
    {
        yield return new WaitForSecondsRealtime(3f);
        SpawnPlanes();
    }
    async void SpawnPlanes()
    {
        var players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < 2; i++)
        {                                                
            await Connector.instance.networkRunner.SpawnAsync
                (
                i == 0? shieldPrefabForward : shieldPrefabUpward,
                players[i].transform.position,
                Quaternion.identity,
                players[i].Object.InputAuthority
                );
        }
    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < lineRenderers.Count; i++)
        {
            lineRenderers[i].SetPosition(0, lineRenderers[i].transform.position);
            RaycastHit2D hit;
            if (lineRenderers[i].name == "HorizontalLazer")
            {
                hit = Physics2D.Raycast(lineRenderers[i].transform.position - new Vector3(0.15f, 0), lineRenderers[i].transform.up);
            }
            else
            {
                hit = Physics2D.Raycast(lineRenderers[i].transform.position - new Vector3(0.67f, 0), lineRenderers[i].transform.up);
            }
            lineRenderers[i].SetPosition(1, hit.point);
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                //hit.collider.GetComponent<PlayerController>().Die();
                RPC_GameOver();
            }
        }        
    }
    [Rpc]
    public void RPC_GameOver()
    {
        GameManager.instance.isGameOver = true;
    }
}
