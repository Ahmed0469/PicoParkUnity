using UnityEngine;
using UnityEngine.UI;
//using Photon.Pun;
//using Photon.Realtime;
using System.Linq;
using UnityEngine.SceneManagement;
using Fusion;
using Photon.Voice.Unity;
//using Photon.Voice.Unity;

namespace Visyde
{
    /// <summary>
    /// Custom Game Player Item
    /// - The script for the UI item that represents players in the custom game lobby
    /// </summary>

    public class CustomGamePlayerItem : NetworkBehaviour
    {
        [Networked] public string NickName { get; set; }
        [Networked] public int Character { get; set; }
        [Networked] public int ChoosenHat { get; set; }
        [Networked] public int ChoosenGlasses { get; set; }
        [Networked] public int ChoosenNecklace { get; set; }

        [Header("Settings:")]
        public Color ownerColor;

        [Header("References:")]
        public GameObject hostIndicator;
        public Text playerNameText;
        public Button kickBTN;
        public GameObject talkingIndicator;
        public Button muteBtn;
        public Button colorSelectorBtn;
        public GameObject colorsListObj;
		//public PlayerRef owner;
        private void Start()
        {
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("MainMenu"))
            {
                transform.SetParent(FindObjectOfType<LobbyBrowserUI>().playerItemHandler,false);
            }
            else
            {
                transform.SetParent(FindObjectOfType<ChatSystem>().playerItemHandler, false);
            }
            if (Object.HasInputAuthority)
            {
                RPC_Set(PlayerPrefs.GetString("USERNAME"), DataCarrier.chosenCharacter,DataCarrier.chosenHat,DataCarrier.chosenGlasses,DataCarrier.chosenNecklace);
                NickName = PlayerPrefs.GetString("USERNAME");
                Set();
            }
            Connector.onPlayerLeave += Resettle;
        }
        private void FixedUpdate()
        {
            //if (!Object.HasInputAuthority)
            //{

            //}
            Set();
            if (!Connector.instance.playerData.ContainsKey(Connector.instance.playersList.IndexOf(Object.InputAuthority)) && !string.IsNullOrEmpty(NickName))
            {
                //Debug.Log("Actor = " + Connector.instance.playersList.IndexOf(Object.InputAuthority) +
                //    "nickName = " + NickName + "Character = " + Character);
                Connector.instance.playerData.Add(Connector.instance.playersList.IndexOf(Object.InputAuthority), new PlayerData
                { nickName = NickName, characterData = Character, choosenHat = ChoosenHat, choosenGlasses = ChoosenGlasses,choosenNecklace = ChoosenNecklace });
            }
            else
            {
                //Debug.Log("ReEnter Actor = " + Connector.instance.playersList.IndexOf(Object.InputAuthority) +
                //    "nickName = " + NickName + "Character = " + Character);
                Connector.instance.playerData.Remove(Connector.instance.playersList.IndexOf(Object.InputAuthority));
                Connector.instance.playerData.Add(Connector.instance.playersList.IndexOf(Object.InputAuthority), new PlayerData
                { nickName = NickName, characterData = Character, choosenHat = ChoosenHat, choosenGlasses = ChoosenGlasses, choosenNecklace = ChoosenNecklace });
            }
        }
        public void OpenCloseColorPalett()
        {
            colorsListObj.SetActive(!colorsListObj.activeInHierarchy);
        }
        public void OnColorSwitchBtn(int colorId)
        {
            DataCarrier.chosenCharacter = colorId;
            RPC_Set(PlayerPrefs.GetString("USERNAME"), DataCarrier.chosenCharacter, DataCarrier.chosenHat, DataCarrier.chosenGlasses, DataCarrier.chosenNecklace);
            colorsListObj.SetActive(false);
        }
        private void OnDestroy()
        {
            //if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("MainMenu"))
            //{
            Connector.onPlayerLeave -= Resettle;
            //}
        }
        public void Resettle(PlayerRef player)
        {
            if (Object.InputAuthority == player)
            {
                Connector.instance.lobbyItems.Remove(this);
                Destroy(gameObject);
            }
            Set();
        }
        public void Set()
        {
            playerNameText.text = NickName;
            if (Object.HasInputAuthority)
            {
                playerNameText.color = ownerColor;
                if (SceneManager.GetActiveScene().name == "MainMenu")
                {
                    colorSelectorBtn.gameObject.SetActive(true);
                }
                //kickBTN.gameObject.SetActive(false);
            }

            // Host indicator, position in list, and the kick buttons:
            //Debug.Log(Object.InputAuthority.PlayerId + "ID");
            hostIndicator.SetActive(Object.InputAuthority.PlayerId == 1);
            if (Object.InputAuthority.PlayerId == 1) transform.SetAsFirstSibling(); else transform.SetAsLastSibling();
            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("MainMenu"))
            {
                //kickBTN.gameObject.SetActive(PhotonNetwork.IsMasterClient && !owner.IsLocal);
            }
            if (Object.HasInputAuthority)
            {
                muteBtn.gameObject.SetActive(false);
            }
        }
        [Rpc/*(sources:RpcSources.InputAuthority,targets:RpcTargets.All)*/]
        public void RPC_Set(string nickName,int character,int hat,int glasses,int necklace)
        {   
            NickName = nickName;
            Character = character;
            ChoosenHat = hat;
            ChoosenGlasses = glasses;
            ChoosenNecklace = necklace;
        }

        public void Kick()
        {
            Object.Runner.Disconnect(Object.InputAuthority);
        }
        public void MuteUnmute()
        {
            //NetworkObject[] photonViews = FindObjectsOfType<NetworkObject>();
            //Speaker speaker = null;
            Speaker speaker = GetComponentInChildren<Speaker>();
            var voiceController = GetComponent<VoiceController>();
            //photonViews.Any(view => speaker = view.Owner.NickName == owner.NickName ? view.GetComponent<Speaker>() : null);
            //VoiceController voiceController = null;
            if (speaker != null)
            {
                //voiceController = speaker.GetComponent<VoiceController>();
                voiceController.isMuted = !voiceController.isMuted;
                speaker.enabled = !voiceController.isMuted;
            }

            if (voiceController.isMuted)
            {
                //muteBtn.image.color = Color.green;
                muteBtn.transform.GetChild(0).gameObject.SetActive(true);
            }
            else
            {
                //muteBtn.image.color = Color.red;
                muteBtn.transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }
}