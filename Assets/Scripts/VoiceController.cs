using Visyde;
//using Photon.Pun;
//using Photon.Voice.PUN;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VoiceController : NetworkBehaviour/*MonoBehaviourPun,IPunObservable*/
{
    [Networked] private bool isTalking { get; set; }
    public bool isMuted = false;
    CustomGamePlayerItem item;
    PlayerController controller;
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Connector.onVoiceDetected += voiceDetected;
            Connector.onVoiceNotDetected += voiceNotDetected;
        }
        if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Game"))
        {
            controller = GetComponent<PlayerController>();
        }
        if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("MainMenu"))
        {
            item = GetComponent<CustomGamePlayerItem>();
        }
    }
    private void OnDestroy()
    {
        Connector.onVoiceDetected -= voiceDetected;
        Connector.onVoiceNotDetected -= voiceNotDetected;
    }
    public void voiceDetected()
    {
        RPC_Istalking(/*Object.InputAuthority,*/ true);
    }
    public void voiceNotDetected()
    {
        RPC_Istalking(/*Object.InputAuthority,*/ false);
    }
    [Rpc]
    public void RPC_Istalking(/*[RpcTarget] PlayerRef player,*/ bool istalking)
    {
        //if (Object.InputAuthority == player)
        //{
        isTalking = istalking;
        //}
    }
    //Update is called once per frame
    void Update()
    {        
        if (!isMuted)
        {
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Game"))
            {
                if (controller != null)
                {
                    controller.talkingIndicator.SetActive(isTalking);
                }
            }
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("MainMenu"))
            {
                if (item != null)
                {
                    item.talkingIndicator.SetActive(isTalking);
                }
            }
        }
    }
}
