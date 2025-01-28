using Fusion;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.ProfilesModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Visyde;
using UnityEngine.SceneManagement;
//using Google.Play.Review;
//using UnityEngine.iOS;

public class PlayfabLogin : MonoBehaviour
{
    private string userEmail;
    private string userPassword;
    private string username;
    private string guestId;
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject mainPanel;
    public Text loginFailedText;
    public Text registerFailedText;
    public InputField friendUserNameInputField;
    public Transform firendsUIParentTransform;
    public FriendsUI friendsUIPrefab;
    public Transform firendsInviteUIParentTransform;
    public FriendsUI friendsInviteUIPrefab;
    public FriendsUI friendRequestUIPrefab;
    public static GetFriendsListResult playFabFriends;
    public List<FriendsUI> lobbyInvitesList;
    public List<FriendsUI> friendsRequestsObjList;
    public GameObject addFriendInputField;
    public GameObject addFriendButton;
    private string friendUserName;
    public GameObject friendInteractionsRedCircle;
    public Text totalFriendInteractionsText;
    int totalFriendInteractions;
    public GameObject lobbyMessageIndicator;
    public GameObject friendRequestMessageIndicator;
    public GameObject micdisabledObject; public GameObject MusicOnBtn;
    public GameObject MusicOffBtn;
    public GameObject SoundOnBtn;
    public GameObject SoundOffBtn;
    public GameObject settingsPanel;
    public List<string> inAppIds = new();
    private void OnEnable()
    {
        ChatSystem.OnRoomInvite += HandleRoomInvitation;
        ChatSystem.OnFriendRequestSent += HandleFriendRequest;
        ChatSystem.OnFriendRequestAccept += HandleFriendRequestAccept;
    }
    private void OnDisable()
    {
        ChatSystem.OnRoomInvite -= HandleRoomInvitation;
        ChatSystem.OnFriendRequestSent -= HandleFriendRequest;
        ChatSystem.OnFriendRequestAccept -= HandleFriendRequestAccept;
    }
    public void Start()
    {        
        Application.targetFrameRate = 60;
        GameManager.levelInt = 0;
        if (PlayerPrefs.GetInt("RememberMe") == 1 && !string.IsNullOrEmpty(PlayerPrefs.GetString("EMAIL")))
        {
            userEmail = PlayerPrefs.GetString("EMAIL");
            userPassword = PlayerPrefs.GetString("PASSWORD");
            username = PlayerPrefs.GetString("USERNAME");
            var request = new LoginWithEmailAddressRequest
            {
                Email = userEmail,
                Password = userPassword,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetUserAccountInfo = true }
            };
            PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
        }
        else if (PlayerPrefs.GetInt("GuestLogin") == 1)
        {
            username = PlayerPrefs.GetString("USERNAME");
            guestId = PlayerPrefs.GetString("GUESTUSERNAME");
            var request = new LoginWithCustomIDRequest
            {
                CustomId = guestId,
                TitleId = PlayFabSettings.TitleId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetUserAccountInfo = true }
            };
            PlayFabClientAPI.LoginWithCustomID(request, OnGuestLoginSuccess, OnLoginFailure);
        }
    }
    public void LooadScene()
    {
        SceneManager.LoadScene(3);
    }
    #region PlayerData
    public void GetPlayerData(string playFabId)
    {
        PlayFabClientAPI.GetUserData
            (
            new GetUserDataRequest()
            {
                PlayFabId = playFabId,
                Keys = null
            },
            (resultsuccess) =>
            {
                if (resultsuccess.Data == null || !resultsuccess.Data.ContainsKey("InAppData"))
                {
                    SetPlayerData();
                }
                else
                {
                    InAppData inAppDataClass = JsonUtility.FromJson<InAppData>(resultsuccess.Data["InAppData"].Value);
                    for (int i = 0; i < inAppIds.Count; i++)
                    {
                        int value = int.Parse(inAppDataClass.inAppData.Find(item => item.key == inAppIds[i]).value);
                        PlayerPrefs.SetInt(inAppIds[i], value);
                        PlayerPrefs.Save();
                        FindObjectOfType<CharacterCustomizer>().RefreshSlots();
                    }
                }                
            },
            (resultError) =>
            {
                Debug.Log(resultError.ErrorMessage);
            }
            );
    }
    public void SetPlayerData()
    {
        InAppData inAppDataClass = new InAppData();
        for (int i = 0; i < inAppIds.Count; i++)
        {
            inAppDataClass.inAppData.Add(new InAppDataItem { key = inAppIds[i], value = PlayerPrefs.GetInt(inAppIds[i], 0).ToString() });
        }
        string jsonData = JsonUtility.ToJson(inAppDataClass);
        PlayFabClientAPI.UpdateUserData
            (
            new UpdateUserDataRequest() 
            {
                Data = new Dictionary<string, string>()
                {
                    {"InAppData",jsonData }
                }
            },
            (resultSuccess) => 
            {
                Debug.Log("SetDataSuccesfull");
            },
            (resultError) => 
            {
                Debug.Log("SetData Error " + resultError.ErrorMessage);
            }
            );
    }    
    #endregion
    #region Login    
    public void OnClickRateUsBtn()
    {
        StartCoroutine(ReviewCR());
    }
    IEnumerator ReviewCR()
    {
        yield return null;
#if PLATFORM_ANDROID
        //var _reviewManager = new ReviewManager();
        //var requestFlowOperation = _reviewManager.RequestReviewFlow();
        //yield return requestFlowOperation;
        //if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        //{
        //    yield break;
        //}
        //var _playReviewInfo = requestFlowOperation.GetResult();

        //var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
        //yield return launchFlowOperation;
        //_playReviewInfo = null; // Reset the object
        //if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        //{
        //    // Log error. For example, using launchFlowOperation.Error.ToString().
        //    yield break;
        //}
        ////The flow has finished. The API does not indicate whether the user
        //// reviewed or not, or even whether the review dialog was shown. Thus, no
        //// matter the result, we continue our app flow.
#else
        //Device.RequestStoreReview();
#endif

    }
    public void ChangeRegion()
    {
        Connector.regionSelected = false;
        Connector.instance.Disconnect();
        Connector.instance.RefreshRegionDropdown();
        settingsPanel.SetActive(false);
    }
    public void TurnOnSettingsPanel()
    {
        settingsPanel.SetActive(true);
        if (PlayerPrefs.GetInt("Music", 1) == 0)
        {
            MusicOffBtn.SetActive(true);
            MusicOnBtn.SetActive(false);
        }
        else
        {
            MusicOffBtn.SetActive(false);
            MusicOnBtn.SetActive(true);
        }
        if (PlayerPrefs.GetInt("Sound", 1) == 0)
        {
            SoundOffBtn.SetActive(true);
            SoundOnBtn.SetActive(false);
        }
        else
        {
            SoundOffBtn.SetActive(false);
            SoundOnBtn.SetActive(true);
        }
    }
    public void OnClickMusicOn(bool turnOff)
    {
        if (turnOff)
        {
            PlayerPrefs.SetInt("Music",0);
            MusicOffBtn.SetActive(true);
            MusicOnBtn.SetActive(false);
            SoundManager.instance.StopMusic();
        }
        else
        {
            PlayerPrefs.SetInt("Music", 1);
            MusicOffBtn.SetActive(false);
            MusicOnBtn.SetActive(true);
            SoundManager.instance.PlayMusic();
        }
    }
    public void OnClickSound(bool turnOff)
    {
        if (turnOff)
        {
            PlayerPrefs.SetInt("Sound", 0);
            SoundOffBtn.SetActive(true);
            SoundOnBtn.SetActive(false);
        }
        else
        {
            PlayerPrefs.SetInt("Sound", 1);
            SoundOffBtn.SetActive(false);
            SoundOnBtn.SetActive(true);
        }
    }
    public void SignOut()
    {
        PlayerPrefs.SetInt("RememberMe", 0);
        PlayerPrefs.SetInt("GuestLogin",0);
        SceneManager.LoadScene(0);
    }
    public void RememberMeCheck(GameObject tickObj)
    {
        var rememberMe = !tickObj.activeInHierarchy;
        tickObj.SetActive(rememberMe);
        PlayerPrefs.SetInt("RememberMe", rememberMe?1:0);
    }
    private void OnGuestLoginSuccess(LoginResult result)
    {
        PlayerPrefs.SetInt("GuestLogin", 1);
        username = guestId;
        Debug.Log(guestId + " Working");
        PlayerPrefs.SetString("USERNAME", guestId);
        PlayerPrefs.SetString("GUESTUSERNAME", guestId);
        SuccesSigning();        
        GetPlayers();
    }
    private void OnLoginSuccess(LoginResult result)
    {
        username = result.InfoResultPayload.AccountInfo.Username;
        Debug.Log(result.InfoResultPayload.AccountInfo.Username + " Working");
        PlayerPrefs.SetString("USERNAME", result.InfoResultPayload.AccountInfo.Username);
        SuccesSigning();
        GetPlayerData(result.PlayFabId);
        GetPlayers();
    }
    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        PlayerPrefs.SetString("USERNAME", username);
        SuccesSigning();
        GetPlayerData(result.PlayFabId);
        GetPlayers();
    }
    private void SuccesSigning()
    {
        Debug.Log("Congratulations, you made your first successful API call!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
        registerPanel.SetActive(false);
        mainPanel.SetActive(true);
        if (!Connector.regionSelected)
        {
            Connector.instance.RefreshRegionDropdown();
        }
        else
        {
            StartCoroutine(Connector.instance.Reconnection());
        }
    }
    private void OnLoginFailure(PlayFabError error)
    {
        StartCoroutine(loginFailed());
    }
    IEnumerator loginFailed()
    {
        if (!loginFailedText.gameObject.activeInHierarchy)
        {
            loginFailedText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(3f);
            loginFailedText.gameObject.SetActive(false);
        }
    }
    private void OnRegisterFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        registerFailedText.gameObject.SetActive(true);
        registerFailedText.text = "Password length must be greater than 6\nEmail must be Valid and a Unique Username";
    }
    public void GetUserEmail(InputField emailIn)
    {
        userEmail = emailIn.text;
    }
    public void GetUserPassword(InputField passwordIn)
    {
        userPassword = passwordIn.text;
    }
    public void GetUsername(InputField usernameIn)
    {
        username = usernameIn.text;
    }
    public void OnClickForgetPassword()
    {
        SendAccountRecoveryEmailRequest requestt = new SendAccountRecoveryEmailRequest { Email = userEmail, TitleId = PlayFabSettings.TitleId };
        PlayFabClientAPI.SendAccountRecoveryEmail(requestt, (r) => { Debug.Log("emailSucces"); }, (r) => { Debug.Log("email error " + r.ErrorMessage); });
    }
    public void OnClickGuestLogin()
    {
        if (PlayerPrefs.HasKey("GUESTUSERNAME"))
        {
            guestId = PlayerPrefs.GetString("GUESTUSERNAME");
        }
        else
        {
            guestId = "GuestPlayer" + DateTime.UtcNow.Ticks;
        }
        var request = new LoginWithCustomIDRequest
        {            
            CustomId = guestId,
            TitleId = PlayFabSettings.TitleId,
            CreateAccount = true,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetUserAccountInfo = true }
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnGuestLoginSuccess, OnLoginFailure);
    }
    public void OnClickLogin()
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = userEmail,
            Password = userPassword,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetUserAccountInfo = true }
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }
    public void OnCloickRegister()
    {
        var registerRequest = new RegisterPlayFabUserRequest { Email = userEmail, Password = userPassword, Username = username };
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, OnRegisterSuccess, OnRegisterFailure);
    }
    #endregion
    #region Friends
    public void FriendUserName(InputField userName)
    {
        friendUserName = userName.text;
    }
    public void AddFriend()
    {
        if (!string.IsNullOrEmpty(friendUserName))
        {
            string messageID = UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + "";
            ChatSystem.chatClient.SendPrivateMessage(friendUserName, "ADMINFRIENDREQUESTFUNCTION" + "?" + messageID);
            friendUserName = "";
            friendUserNameInputField.text = "";
        }
    }
    public void OnAddFriendSucces(AddFriendResult result)
    {
        Debug.Log(result.Created + " Friend Added");
        if (Connector.instance.networkRunner.IsCloudReady)
        {
            GetFriendsListRequest getFriendsListRequest = new GetFriendsListRequest { };
            PlayFabClientAPI.GetFriendsList(getFriendsListRequest,
                (result) =>
                {
                    //PhotonNetwork.FindFriends(result.Friends.Select(y => y.Username).ToArray());
                    OnGetFriendsListSucces(result);
                }
                , OnGetFriendsListFailure);
        }
    }

    public void GetPlayers()
    {
        GetFriendsListRequest getFriendsListRequest = new GetFriendsListRequest { };
        PlayFabClientAPI.GetFriendsList(getFriendsListRequest, OnGetFriendsListSucces, OnGetFriendsListFailure);
    }
    public void MicEnableDisable()
    {
        int val = PlayerPrefs.GetInt("MICDISABLE");
        val = PlayerPrefs.GetInt("MICDISABLE") == 0 ? 1 : 0;
        PlayerPrefs.SetInt("MICDISABLE", val);
        Connector.instance.recorder.TransmitEnabled = PlayerPrefs.GetInt("MICDISABLE") == 0 ? true : false;
        micdisabledObject.SetActive(PlayerPrefs.GetInt("MICDISABLE") != 0);
    }
    public void OnGetFriendsListSucces(GetFriendsListResult result)
    {
        micdisabledObject.SetActive(PlayerPrefs.GetInt("MICDISABLE") != 0);
        if (Connector.instance.networkRunner != null && Connector.instance.recorder == null)
        {
            Connector.instance.recorder = FindObjectOfType<Photon.Voice.Unity.Recorder>();
        }
        Connector.instance.recorder.TransmitEnabled = PlayerPrefs.GetInt("MICDISABLE") == 0 ? true : false;
        for (int i = 0; i < firendsUIParentTransform.childCount; i++)
        {
            Destroy(firendsUIParentTransform.GetChild(i).gameObject);
        }

        for (int i = 0; i < result.Friends.Count; i++)
        {
            Debug.Log(result.Friends.Count + " Count Friend");
            var playerUIObj = Instantiate(friendsUIPrefab, firendsUIParentTransform);
            playerUIObj.userNameText.text = result.Friends[i].Username;
            //if (Connector.instance.photonFriends != null && Connector.instance.photonFriends.Count > 0)
            //{
            //    Debug.Log("PlayfabFriendsListDebug");
            //    Photon.Realtime.FriendInfo player = Connector.instance.photonFriends.FirstOrDefault(fr => fr.UserId == result.Friends[i].Username);
            //    if (player != null && !player.IsOnline)
            //    {
            //        playerUIObj.offlinePanel.gameObject.SetActive(true);
            //        playerUIObj.inviteBtn.interactable = false;
            //        playerUIObj.inviteBtn.GetComponentInChildren<Text>().text = "OFFLINE";
            //        playerUIObj.transform.SetAsLastSibling();
            //    }
            //    else
            //    {
            //        playerUIObj.transform.SetAsFirstSibling();
            //    }
            //}
            playerUIObj.inviteBtn.onClick.AddListener(() =>
            {
                string messageID = UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + "";
                ChatSystem.chatClient.SendPrivateMessage(playerUIObj.userNameText.text, Connector.instance.networkRunner.SessionInfo.Name + "?" + "ADMININVITELOBBYFUNCTION" + "?" + messageID);
                Debug.Log("Sending Invite");
            });
        }
        playFabFriends = result;
    }
    private void HandleRoomInvitation(string sender, string roomName)
    {
        if (lobbyInvitesList.Any(invite => invite.userNameText.text == sender))
        {

        }
        else
        {
            FriendsUI friendsUI = Instantiate(friendsInviteUIPrefab, firendsInviteUIParentTransform);
            lobbyInvitesList.Add(friendsUI);
            lobbyMessageIndicator.SetActive(true);
            totalFriendInteractions++;
            friendInteractionsRedCircle.SetActive(true);
            totalFriendInteractionsText.text = totalFriendInteractions.ToString();
            friendsUI.userNameText.text = sender;
            friendsUI.roomName = roomName;
            if (panelState != PanelState.Lobby)
            {
                friendsUI.gameObject.SetActive(false);
            }
            friendsUI.inviteAcceptBtn.onClick.AddListener(() =>
            {
                if (Connector.instance.networkRunner.IsInSession)
                {
                    Connector.instance.Disconnect();
                    Connector.instance.JoinSession(roomName);
                    //PhotonNetwork.JoinRoom(roomName);
                }
                else
                {
                    Connector.instance.JoinSession(roomName);
                    //PhotonNetwork.JoinRoom(roomName);
                }
                Destroy(friendsUI.gameObject);
                totalFriendInteractions--;
                friendInteractionsRedCircle.SetActive(totalFriendInteractions <= 0 ? false : true);
                totalFriendInteractions = totalFriendInteractions <= 0 ? 0 : totalFriendInteractions;
                totalFriendInteractionsText.text = totalFriendInteractions.ToString();
                lobbyInvitesList.Remove(friendsUI);
                lobbyMessageIndicator.SetActive(lobbyInvitesList.Count > 0);
            });
            friendsUI.inviteRejectBtn.onClick.AddListener(() =>
            {
                Destroy(friendsUI.gameObject);
                totalFriendInteractions--;
                friendInteractionsRedCircle.SetActive(totalFriendInteractions <= 0 ? false : true);
                totalFriendInteractions = totalFriendInteractions <= 0 ? 0 : totalFriendInteractions;
                totalFriendInteractionsText.text = totalFriendInteractions.ToString();
                lobbyInvitesList.Remove(friendsUI);
                lobbyMessageIndicator.SetActive(lobbyInvitesList.Count > 0);
            });
        }
    }
    private void HandleFriendRequest(string sender)
    {
        if (friendsRequestsObjList.Any(requests => requests.userNameText.text == sender))
        {

        }
        else
        {
            if (playFabFriends.Friends.All(friend => friend.Username != sender))
            {
                FriendsUI friendsUI = Instantiate(friendRequestUIPrefab, firendsInviteUIParentTransform);
                friendsRequestsObjList.Add(friendsUI);
                friendRequestMessageIndicator.gameObject.SetActive(true);
                totalFriendInteractions++;
                friendInteractionsRedCircle.SetActive(true);
                totalFriendInteractionsText.text = totalFriendInteractions.ToString();
                friendsUI.userNameText.text = sender;
                if (panelState != PanelState.FriendRequests)
                {
                    friendsUI.gameObject.SetActive(false);
                }
                friendsUI.friendRequestAcceptBtn.onClick.AddListener(() =>
                {
                    AddFriendRequest addFriendRequest = new AddFriendRequest { FriendUsername = friendsUI.userNameText.text };
                    PlayFabClientAPI.AddFriend(addFriendRequest, OnAddFriendSucces, OnAddFriendFailure);
                    string messageID = UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + "";
                    ChatSystem.chatClient.SendPrivateMessage(friendsUI.userNameText.text, "ADMINFRIENDREQUESTACCEPTFUNCTION" + "?" + messageID);
                    Destroy(friendsUI.gameObject);
                    totalFriendInteractions--;
                    friendInteractionsRedCircle.SetActive(totalFriendInteractions <= 0 ? false : true);
                    totalFriendInteractions = totalFriendInteractions <= 0 ? 0 : totalFriendInteractions;
                    totalFriendInteractionsText.text = totalFriendInteractions.ToString();
                    friendsRequestsObjList.Remove(friendsUI);
                    friendRequestMessageIndicator.SetActive(friendsRequestsObjList.Count > 0);
                });
                friendsUI.friendRequestRejectBtn.onClick.AddListener(() =>
                {
                    Destroy(friendsUI.gameObject);
                    totalFriendInteractions--;
                    friendInteractionsRedCircle.SetActive(totalFriendInteractions <= 0 ? false : true);
                    totalFriendInteractions = totalFriendInteractions <= 0 ? 0 : totalFriendInteractions;
                    totalFriendInteractionsText.text = totalFriendInteractions.ToString();
                    friendsRequestsObjList.Remove(friendsUI);
                    friendRequestMessageIndicator.SetActive(friendsRequestsObjList.Count > 0);
                });
            }
            else
            {
                string messageID = UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + UnityEngine.Random.Range(0, 1000) + "";
                ChatSystem.chatClient.SendPrivateMessage(sender, "ADMINFRIENDREQUESTACCEPTFUNCTION" + "?" + messageID);
            }
        }
    }
    enum PanelState
    {
        Lobby,
        FriendRequests,
        AddFriend
    }
    PanelState panelState;
    public void LobbyPanelOpenBtn()
    {
        for (int i = 0; i < friendsRequestsObjList.Count; i++)
        {
            friendsRequestsObjList[i].gameObject.SetActive(false);
        }
        panelState = PanelState.Lobby;
        addFriendInputField.SetActive(false);
        addFriendButton.SetActive(false);
        for (int i = 0; i < lobbyInvitesList.Count; i++)
        {
            lobbyInvitesList[i].gameObject.SetActive(true);
        }
    }
    public void FriendRequestsPanelOpenBtn()
    {
        for (int i = 0; i < lobbyInvitesList.Count; i++)
        {
            lobbyInvitesList[i].gameObject.SetActive(false);
        }
        addFriendInputField.SetActive(false);
        addFriendButton.SetActive(false);
        panelState = PanelState.FriendRequests;
        for (int i = 0; i < friendsRequestsObjList.Count; i++)
        {
            friendsRequestsObjList[i].gameObject.SetActive(true);
        }
    }
    public void AddFriendPanelOpenBtn()
    {
        for (int i = 0; i < lobbyInvitesList.Count; i++)
        {
            lobbyInvitesList[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < friendsRequestsObjList.Count; i++)
        {
            friendsRequestsObjList[i].gameObject.SetActive(false);
        }
        panelState = PanelState.AddFriend;
        addFriendInputField.SetActive(true);
        addFriendButton.SetActive(true);
    }
    private void HandleFriendRequestAccept(string sender)
    {
        AddFriendRequest addFriendRequest = new AddFriendRequest { FriendUsername = sender };
        PlayFabClientAPI.AddFriend(addFriendRequest, OnAddFriendSucces, OnAddFriendFailure);
        if (Connector.instance.networkRunner.IsCloudReady)
        {
            GetFriendsListRequest getFriendsListRequest = new GetFriendsListRequest { };
            PlayFabClientAPI.GetFriendsList(getFriendsListRequest,
                (result) =>
                {
                    //PhotonNetwork.FindFriends(result.Friends.Select(y => y.Username).ToArray());
                    OnGetFriendsListSucces(result);
                }
                , OnGetFriendsListFailure);
        }
    }
    public void OnGetFriendsListFailure(PlayFabError error)
    {
        Debug.Log("GetFriend Error " + error.Error.ToString());
    }
    public void OnAddFriendFailure(PlayFabError error)
    {
        Debug.Log("AddFriend Error " + error.Error.ToString());
    }
    #endregion
}
[Serializable]
public class InAppData
{
    public List<InAppDataItem> inAppData = new List<InAppDataItem>();
}
[Serializable]
public class InAppDataItem
{
    public string key;
    public string value;
}
