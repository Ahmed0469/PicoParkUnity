using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;
public class TwoPlayersLevel5 : NetworkBehaviour
{
    public List<Transform> weightBlocksTransforms = new();
    public GameObject weightBlockPrefab;
    public override void Spawned()
    {
        if (Connector.instance.networkRunner.IsServer)
        {
            SpawnWeightBlocks();
        }
    }
    public async void SpawnWeightBlocks()
    {
        for (int i = 0; i < weightBlocksTransforms.Count; i++)
        {
            var block = await Connector.instance.networkRunner.SpawnAsync
                (
                weightBlockPrefab,
                weightBlocksTransforms[i].position,
                weightBlocksTransforms[i].rotation,
                Object.InputAuthority
                );
            block.transform.localScale = weightBlocksTransforms[i].localScale;
        }
    }
}
