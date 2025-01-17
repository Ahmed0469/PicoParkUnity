using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
//using Photon.Voice.Unity;

namespace Visyde
{
    /// <summary>
    /// Controls Manager
    /// - manages player control inputs for different platforms (PC and mobile) in one place.
    /// </summary>

    public class ControlsManager : MonoBehaviour
    {
        [HideInInspector] public bool mobileControls = false;
        [Header("References:")]
        public Joystick moveStick;
        public Joystick shootStick;
        public GameObject chatBox;
        public GameObject micDisableObj;

        [Tooltip("The extent of the shootStick where shooting begins.")]
		[Range(0.1f, 1f)]
		public float shootingThreshold;

        [Header("Jumping using the Move Stick:")]
        public bool enableMoveStickJumping;
        [Tooltip("The Y value of moveStick where jumping begins.")]
        [Range(0.1f, 1f)]
        public float jumpingYStart;
        [Tooltip("The extent of the moveStick where jumping begins.")]
        [Range(0.1f, 1f)]
        public float jumpingThreshold;

        // Movement:
        [HideInInspector]
        public float horizontal;
        [HideInInspector]
        public float vertical;
        [HideInInspector]
        public float horizontalRaw;
        [HideInInspector]
        public float verticalRaw;
        public UnityAction jump;

        // Shooting:
        [HideInInspector]
        public bool shoot;              
        [HideInInspector]
        public float aimX;
        [HideInInspector]
        public float aimY;

        // UI:
        [HideInInspector]
        public bool showScoreboard;
        private void Start()
        {
            if (micDisableObj != null)
            {
                micDisableObj.SetActive(PlayerPrefs.GetInt("MICDISABLE") != 0);
                //recorder.TransmitEnabled = PlayerPrefs.GetInt("MICDISABLE") == 0 ? true : false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Movement input:
            float x = moveStick.xValue;
            float y = moveStick.yValue;
            horizontal = mobileControls ? x : Input.GetAxis("Horizontal");
            vertical = mobileControls ? y : Input.GetAxis("Vertical");
            horizontalRaw = mobileControls ? x == 0 ? 0 : x > 0 ? 1 : -1 : Input.GetAxisRaw("Horizontal");
            verticalRaw = mobileControls ? y == 0 ? 0 : y > 0 ? 1 : -1 : Input.GetAxisRaw("Vertical");

            // Jumping (Mobile controls have 2 options, either use move stick as a jumping control when the 
            // Y axis is over the 'jumpingYStart' and 'jumpingThreshold', or simply use an on-screen button): 
            if (mobileControls){
                if (enableMoveStickJumping && (y >= jumpingYStart && moveStick.progress >= jumpingThreshold)) Jump();
            }
            else{
                if (Input.GetButton("Jump")) Jump();
                if (Input.GetKeyDown(KeyCode.LeftShift)) ChangePlayer();
                if (Input.GetKeyDown(KeyCode.T))
                {
                    chatBox.SetActive(!chatBox.activeInHierarchy);
                }
                if (Input.GetKeyDown(KeyCode.M))
                {
                    MicEnableDisable();
                }
            }

            // Shooting input:
            aimX = shootStick.xValue;
            aimY = shootStick.yValue;

            // Show/hide scoreboard for PC:
            if (!mobileControls)
            {
                showScoreboard = Input.GetKey(KeyCode.Tab);
            }
        }
        void LateUpdate(){
            shoot = mobileControls? shootStick.progress >= shootingThreshold && shootStick.isHolding : Input.GetButton("Fire1");
        }

        // Jumping (can be called by an on-screen button):
        public void Jump()
        {
            if (jump != null)
            jump.Invoke();
        }
        public void MicEnableDisable()
        {
            if (micDisableObj != null)
            {
                int val = PlayerPrefs.GetInt("MICDISABLE");
                val = PlayerPrefs.GetInt("MICDISABLE") == 0 ? 1 : 0;
                PlayerPrefs.SetInt("MICDISABLE", val);
                Connector.instance.recorder.TransmitEnabled = PlayerPrefs.GetInt("MICDISABLE") == 0 ? true : false;
                micDisableObj.SetActive(PlayerPrefs.GetInt("MICDISABLE") != 0);
            }            
        }
        public void ChangePlayer()
        {
            GameManager.instance.ourPlayers[0].playerInstance.isMine = !GameManager.instance.ourPlayers[0].playerInstance.isMine;
            GameManager.instance.ourPlayers[1].playerInstance.isMine = !GameManager.instance.ourPlayers[1].playerInstance.isMine;
            GameManager.instance.ourPlayers[0].SubscribeJump();
            GameManager.instance.ourPlayers[1].SubscribeJump();
        }
        // Holding a button to show/hide the scoreboard.
        public void ShowScoreBoard(bool show)
        {
            showScoreboard = show;
        }
    }
}
