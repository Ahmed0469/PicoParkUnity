using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Visyde{

    /// <summary>
    /// Loading Screen Manager
    /// - A simple loading screen manager that displays load progress.
    /// </summary>

    public class LoadingScreenManager : MonoBehaviour
    {
        [Header("References:")]
        public Slider loadingBar;

        void Start()
        {
            //PhotonNetwork.LoadLevel(DataCarrier.sceneToLoad);
            //Connector.instance.networkRunner.LoadScene(DataCarrier.sceneToLoad);
            SceneManager.LoadScene(DataCarrier.sceneToLoad);
        }

        void Update()
        {
            loadingBar.value = Mathf.Lerp(loadingBar.value,1,Time.deltaTime);
        }
    }
}