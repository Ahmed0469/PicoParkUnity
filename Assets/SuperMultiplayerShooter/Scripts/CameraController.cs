using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Camera Controller
    /// - controls the in-game camera that follows the player.
    /// </summary>

    public class CameraController : MonoBehaviour
    {
        [Header("Settings:")]
        public float xFollowSpeed = 7;
        public float yFollowSpeed = 7;
        public Vector2 followOffset;
        [Space]
        public float normalZoom;
        public float noPlayerZoom;
        public float zoomChangeFactor;
        public float zoomSpeed;
        public float shakeDamping = 1f;

        [Space]
        [Header("References:")]
        public Camera theCamera;

        // Internal:
        public PlayerController target;
        /*[HideInInspector] */public GameManager gm;
        float zPos;
        Vector3 curPos;
        float camWidth;
        float camHeight;
        Vector3 lastPlayerPos;                              // for death cam
        //Vector3 lastMousePos;                               // for death cam
        Vector3 initCamPos;                                 // for cam shake
        float shakeAmount;
        float curShakeDur;

        void OnEnable()
        {
            // Save the initial position of the main camera for the cam shake:
            initCamPos = theCamera.transform.localPosition;
        }

        // Use this for initialization
        void Start()
        {
            // Initial Z position:
            zPos = transform.position.z;
        }

        void LateUpdate()
        {
            if (target)
            {
                // Forget target if dead:
                if (target.isDead)
                {
                    target = null;
                    return;
                }

                lastPlayerPos = target.transform.position;
                //lastMousePos = target.mousePos;
            }

            // Get target and mouse position:
            Vector3 targetPos = target ? target.transform.position : lastPlayerPos;
            //Vector3 targetMousePos = target ? gm.gameStarted ? target.mousePos : target.transform.position : lastMousePos;

            // Get camera view's width and height:
            camHeight = theCamera.orthographicSize * 2;
            camWidth = camHeight * theCamera.aspect;

            // Zoom amount:
            float finalZoom = noPlayerZoom;
            if (target)
            {
                if (gm.gameStarted)
                {
                    finalZoom = normalZoom + (zoomChangeFactor * target.rg.velocity.magnitude) * 0.4f;
                }
                else
                {
                    finalZoom = normalZoom;
                }
            }
            theCamera.orthographicSize = Mathf.Lerp(theCamera.orthographicSize, gm.countdownStarted ? finalZoom : noPlayerZoom, Time.deltaTime * zoomSpeed);

            // Do cam shake:
            if (curShakeDur > 0)
            {
                curShakeDur -= Time.deltaTime * shakeDamping;
                // Generate a random position:
                Vector3 randomShake = initCamPos + Random.insideUnitSphere * shakeAmount;
                randomShake.z = 0;

                // Move to that position:
                theCamera.transform.localPosition = Vector3.Lerp(theCamera.transform.localPosition, randomShake, Time.deltaTime * shakeAmount * 40f);
            }
            else
            {
                theCamera.transform.localPosition = initCamPos;
            }

            // Camera movement:
            if (gm.countdownStarted)
            {
                // *How far the player can see is determined by the 'sightRange' variable of the weapon:
                float sR = 1;
                if (target)
                {
                    sR = 1f;
                }
                Vector3 finalPos = targetPos /*+ (targetMousePos - targetPos).normalized*/ * sR + new Vector3(followOffset.x, followOffset.y);
                curPos.x = Mathf.Lerp(curPos.x, finalPos.x, Time.deltaTime * xFollowSpeed);
                curPos.y = Mathf.Lerp(curPos.y, finalPos.y, Time.deltaTime * yFollowSpeed);
                curPos.z = zPos;
                transform.position = curPos;
            }

            // Restrict camera inside an active map's bounds:
            GameMap map = gm.getActiveMap;
            if (map)
            {
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, map.boundOffset.x - (map.bounds.x - camWidth / 2), map.boundOffset.x + (map.bounds.x - camWidth / 2)), Mathf.Clamp(transform.position.y, map.boundOffset.y - (map.bounds.y - camHeight / 2), map.boundOffset.y + (map.bounds.y - camHeight / 2)), zPos);
            }
        }

        // Call to do a cam shake:
        public void DoShake(float amount, float duration)
        {
            shakeAmount = amount;
            curShakeDur = duration;
        }
    }


    //using UnityEngine;
    //using System.Collections.Generic;
    //using Fusion;

    //[ExecuteInEditMode]
    //public class CameraController : MonoBehaviour
    //{
    //    public float smoothTime = 0.3f;   // Camera movement smooth time
    //    public float minZoom = 5f;        // Minimum camera zoom level
    //    public float maxZoom = 15f;       // Maximum camera zoom level
    //    public float zoomLimiter = 50f;   // Zoom sensitivity

    //    private Vector3 velocity;         // Used for smooth movement
    //    private Camera cam;               // Main camera reference

    //    void Start()
    //    {
    //        //cam = GetComponent<Camera>();
    //    }

    //    void LateUpdate()
    //    {
    //        // Get all active players
    //        List<Transform> players = GetAllPlayerTransforms();

    //        if (players.Count == 0)
    //            return;

    //        // Update camera position and zoom
    //        MoveCamera(players);
    //        //ZoomCamera(players);
    //    }

    //    // Step 1: Get all Photon Player Transforms
    //    List<Transform> GetAllPlayerTransforms()
    //    {
    //        List<Transform> playerTransforms = new List<Transform>();
    //        foreach (var view in FindObjectsOfType<NetworkObject>())
    //        {
    //            if (view.CompareTag("Player"))  // Add only players
    //            {
    //                playerTransforms.Add(view.transform);
    //            }
    //        }
    //        return playerTransforms;
    //    }

    //    // Step 2: Smoothly Move the Camera
    //    void MoveCamera(List<Transform> players)
    //    {
    //        Vector3 centerPoint = GetCenterPoint(players); // Get center point
    //        Vector3 newPosition = centerPoint + new Vector3(0, 5, -10f); // Offset for 2D

    //        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    //    }

    //    // Step 3: Adjust Camera Zoom
    //    void ZoomCamera(List<Transform> players)
    //    {
    //        float greatestDistance = GetGreatestDistance(players);
    //        float newZoom = Mathf.Lerp(maxZoom, minZoom, greatestDistance / zoomLimiter);
    //        //cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newZoom, Time.deltaTime);
    //    }

    //    // Get Center Point of all players
    //    Vector3 GetCenterPoint(List<Transform> players)
    //    {
    //        if (players.Count == 1)
    //            return players[0].position;

    //        Bounds bounds = new Bounds(players[0].position, Vector3.zero);
    //        foreach (Transform player in players)
    //        {
    //            bounds.Encapsulate(player.position);
    //        }
    //        return bounds.center;
    //    }

    //    // Get the greatest distance between players
    //    float GetGreatestDistance(List<Transform> players)
    //    {
    //        Bounds bounds = new Bounds(players[0].position, Vector3.zero);
    //        foreach (Transform player in players)
    //        {
    //            bounds.Encapsulate(player.position);
    //        }
    //        return bounds.size.x; // Use horizontal distance for 2D
    //    }
    //}

}
