using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;

namespace Visyde
{
    /// <summary>
    /// Player Controller
    /// - The player controller itself! Requires a 2D character controller (like the MovementController.cs) to work.
    /// </summary>

    public class PlayerController : NetworkBehaviour/*MonoBehaviourPunCallbacks, IPunObservable, IInRoomCallbacks*/
    {
        public bool hasDoorKey;
        public GameObject keyObj;
    public bool forPreview = false;                                 // used for non in-game such as character customization preview in the main menu

    [System.Serializable]
    public class Character
    {
        public CharacterData data;
        public Animator animator;

        // For cosmetics:
        public Transform hatPoint;
    }
    public Character[] characters;                                  // list of characters for the spawnable characters (modifying this will not change the main menu character selection screen

    [Header("References:")]                                    // the AI controller for this player (only gets enabled if this is a bot, disabled when not)
    public AudioSource aus;                                         // the AudioSource that will play the player sounds
    public GameObject spawnVFX;                                     // the effect that's shown on spawn
    public EmotePopup emotePopupPrefab;                             // emote prefab to spawn
    public int actorNumber;
    public GameObject talkingIndicator;
    public Transform playerHat;
    public CosmeticsManager cosmeticsManager;
    public int playersTotalWeight;
    private CameraController camController;

    // In-Game:
    //[Networked] public PlayerInstance networkPlayerInstance { get; set;}
    public PlayerInstance playerInstance;
    public int curCharacterID { get; protected set; }               // determines which character is used for this player
    public bool isDead { get; protected set; }                      // is this player dead?
    public Vector3 mousePos { get; protected set; }                 // the mouse position we're working on locally
    public EmotePopup curEmote { get; protected set; }              // this player's own emote popup
    [HideInInspector] public bool isOnJumpPad;                      // when true, jumping is disabled to not interfere with the jump pad
    [HideInInspector] public Vector3 nMousePos;                     // mouse position from network. We're gonna smoothly interpolate the mousePos' value to this one to prevent the jittering effect.
    [HideInInspector] public float xInput;                          // the X input for the movement controls (sent to other clients for animation speed control)
    [HideInInspector] public float yInput;                          // the Y input for the movement controls (sent to other clients for animation speed control)
    float jumpProgress;                                             // longer press means higher jump
    bool moving;                                                    // are we moving on ground?
    bool isFalling;                                                 // are we falling? (can be used for something like a falling animation)
    bool lastFrameGrounded;                                         // used for spawning landing vfx
    bool doneDeadZone;                                              // makes sure that DeadZoned() doesn't called repeatedly
    float lastGroundedTime;
    Vector3 lastAimPos;                                             // used for mobile controls
    GameManager gm;                                                 // GameManger instance reference for simplicity
    public Rigidbody2D rg;

    // Network:
    [HideInInspector] public Vector2 lastPos;
    float lag;

    // Returns the chosen character:
    public Character character
    {
        get
        {
            return characters[curCharacterID];
        }
    }
    // Check if this player is ours and not owned by a bot or another player:
    public bool isPlayerOurs
    {
        get
        {
            return !playerInstance.isBot && playerInstance.isMine;
        }
    }
    void Awake()
    {
        // Find essential references:
        gm = GameManager.instance;
    }
        public override void Spawned()
        {
            if (!Object.HasStateAuthority)
            {
                transform.position = networkPos;
                rg.velocity = networkVel;
                transform.localScale = new Vector3(networkScaleX, 1, 1);
                moving = networkMoving;
                isFalling = networkIsFalling;
                xInput = networkxInput;
                yInput = networkyInput;
                isGrounded = networkIsgrounded;
                allowJump = networkAllowJump;
            }
            if (Object.HasInputAuthority)
            {
                gm.ourPlayer = this;
                FindObjectOfType<CameraController>().target = this;
            }
        }
        public void OnEnable()
    {
        if (gm)
        {
            // Add this to the player controllers list:
            gm.playerControllers.Add(this);
        }
    }
    public void OnDisable()
    {
            if (gm)
        {
            // Unsubscibe to Controls Manager events (doesn't do anything if player isn't ours):
            gm.controlsManager.jump -= Jump;

            // Remove from the player controllers list
            gm.playerControllers.Remove(this);
        }
            Connector.onPlayerLeave -= OnPlayerLeft;
    }
    void Start()
    {
        if (forPreview) return;
            Connector.onPlayerLeave += OnPlayerLeft;
        // Spawn VFX:
        Instantiate(spawnVFX, transform);

            // Reset player stats and stuff:
            if (GameManager.gameMode == GameMode.Multiplayer)
            {
                if (Object.HasStateAuthority)
                {
                    RestartPlayer();
                }
            }
            else
            {
                camController = FindObjectOfType<CameraController>();
                RestartPlayer();
            }
            SubscribeJump();
            // Spawn our own emote popup:
            curEmote = Instantiate(emotePopupPrefab, Vector3.zero, Quaternion.identity);
        curEmote.owner = this;

        jumpForce = character.data.jumpForce;
        moveSpeed = character.data.moveSpeed;
    }
        public void OnPlayerLeft(PlayerRef player)
        {
            if (Object.InputAuthority == player)
            {
                Destroy(gameObject);
            }
        }

    [Header("Settings:")]
    public float groundCheckerRadius;
    public Vector2 groundCheckerOffset;
    public bool allowJump { get; set; }
    public bool isGrounded = false;
    public bool slidePlayer = false;
    [HideInInspector] public bool stopRegularVelocity = false;
        public override void FixedUpdateNetwork()
    {
        if (forPreview) return;

        if (!Object.HasStateAuthority) return;

            if (GameManager.instance.isGameOver || GameManager.instance.LevelWinScreen.activeInHierarchy)
            {
                character.animator.SetBool("Moving", false);
                character.animator.speed = 0;
                return;
            }

        Transform t = transform;

        if (!isDead)
        {
            // Check if we're currently falling:
            isFalling = rg.velocity.y < 0;
                // Dead zone interaction:
                if (gm.deadZone)
                {
                    if (t.position.y < gm.deadZoneOffset && !doneDeadZone)
                    {
                        RPC_TriggerDeadZone(rg.position);
                        //photonView.RPC("TriggerDeadZone", RpcTarget.All, movementController.position);
                        doneDeadZone = true;
                    }
                }
                // *For our player:
                //if (isPlayerOurs)
                //{
                //    HandleInputs();
                //}
                //// *For the bots:
                //else
                //{
                //    if (!gm.isGameOver)
                //    {
                //        // Smooth mouse aim sync for the bot:
                //        mousePos = nMousePos;
                //    }
                //}

                if (GetInput<PlayerInput>(out var playerInput))
                {
                    xInput = playerInput.xInput;
                    yInput = playerInput.yInput;
                }
                // Is moving on ground?:
                moving = rg.velocity.x != 0 && isGrounded && xInput != 0;
                //if (Object.HasStateAuthority || playerInstance.isMine)
                //{

                //}
                //else
                //{
                //    // Smooth mouse aim sync:
                //    mousePos = Vector3.MoveTowards(mousePos, nMousePos, Time.deltaTime * (mousePos - nMousePos).magnitude * 15);
                //}

                // Landing VFX:
                if (isGrounded)
            {
                if (!lastFrameGrounded && (Time.time - lastGroundedTime) > 0.1f)
                {
                    Land();
                }
                lastFrameGrounded = isGrounded;
                lastGroundedTime = Time.time;
            }
            else
            {
                lastFrameGrounded = isGrounded;
            }                
                // Flipping:
                //t.localScale = new Vector3(mousePos.x > t.position.x ? 1 : mousePos.x < t.position.x ? -1 : t.localScale.x, 1, 1);
            }
            allowJump = true;
            isGrounded = false;
            Collider2D[] cols = Physics2D.OverlapCircleAll(groundCheckerOffset + new Vector2(transform.position.x, transform.position.y), groundCheckerRadius);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].CompareTag("JumpPad"))
                {
                    allowJump = false;
                }
                if (cols[i].gameObject != gameObject && !cols[i].isTrigger && !cols[i].CompareTag("Portal"))
                {
                    if (!isGrounded) isGrounded = true;
                }
            }
            m.x = isGrounded ? xInput : xInput != 0 ? xInput : movement.x;
            m.y = /*yInput != 0 ?*/ yInput /*: movement.y*/;
            if (!isGrounded)
            {
                m.x = Mathf.MoveTowards(movement.x, 0, Time.deltaTime);
            }
            Vector2 veloc = rg.velocity;
            if (!stopRegularVelocity)
            {
                veloc.x = movement.x == 0 ? slidePlayer ? veloc.x : movement.x * (moveSpeed / 10) : movement.x * (moveSpeed / 10);
                if (GameManager.instance.userVerticalAxis)
                {
                    veloc.y = movement.y * (moveSpeed / 10);
                }
            }
            else
            {
                var vv = new Vector3(moveSpeed / 1.5f, 0);
                veloc.x = vv.x;
            }
            //veloc.x = movement.x * (moveSpeed / 10);
            //veloc.x = isGrounded ? xInput : 0 * (moveSpeed / 10);
            //rg.MovePosition(rg.position + new Vector2(0, y / 10));
            rg.velocity = veloc;            
            // Animations:
            if (character.animator)
        {
            character.animator.SetBool("Moving", moving);
            character.animator.SetBool("Dead", isDead);
            character.animator.SetBool("Falling", isFalling);

            // Set the animator speed based on the current movement speed (only applies to grounded moving animations such as running):
            character.animator.speed = moving && isGrounded ? Mathf.Abs(xInput) : 1;
        }
            networkPos = rg.position;
            //Debug.LogError("Sending Data rgPos = " + rg.position + "NPos = " + networkPos);
            networkVel = rg.velocity;
            networkScaleX = transform.localScale.x;
            networkMoving = moving;
            networkIsFalling = isFalling;
            networkxInput = xInput;
            networkyInput = yInput;
            networkIsgrounded = isGrounded;
            networkAllowJump = allowJump;
        }
        private void Update()
        {
            if (GameManager.gameMode == GameMode.SinglePlayer)
            {

                //Let the movement controller know how to behave:
                // If we're the local player, let the camera know:
                if (playerInstance.isMine)
                {
                    camController.target = this;
                    //rg.mass = 1;
                }
                else
                {
                    //rg.mass = 1000;
                }

                if (forPreview) return;

                Transform t = transform;

                if (!isDead)
                {                    
                    // Check if we're currently falling:
                    isFalling = rg.velocity.y < 0;

                    // If owned by us (including bots):
                    if (playerInstance.isMine)
                    {
                        // Dead zone interaction:
                        if (gm.deadZone)
                        {
                            if (t.position.y < gm.deadZoneOffset && !doneDeadZone)
                            {
                                TriggerDeadZone(rg.position);
                                doneDeadZone = true;
                            }
                        }
                        // *For our player:
                        if (isPlayerOurs)
                        {
                            HandleInputs();
                            m.x = isGrounded ? xInput : xInput != 0 ? xInput : movement.x;
                            if (!isGrounded)
                            {
                                m.x = Mathf.MoveTowards(movement.x, 0, Time.deltaTime);
                            }
                            Vector2 veloc = rg.velocity;
                            if (!stopRegularVelocity)
                            {
                                veloc.x = movement.x == 0 ? slidePlayer ? veloc.x : movement.x * (moveSpeed / 10) : movement.x * (moveSpeed / 10);                    
                            }
                            else
                            {
                                var vv = new Vector3(moveSpeed / 1.5f, 0);
                                veloc.x = vv.x;
                            }
                            rg.velocity = veloc;
                            moving = rg.velocity.x != 0 && isGrounded && xInput != 0;
                        }                        
                    }
                    else
                    {
                        xInput = 0;
                        yInput = 0;
                        rg.velocity = new Vector2(0, rg.velocity.y);
                        moving = false;
                    }
                    // Landing VFX:
                    if (isGrounded)
                    {
                        if (!lastFrameGrounded && (Time.time - lastGroundedTime) > 0.1f)
                        {
                            Land();
                        }
                        lastFrameGrounded = isGrounded;
                        lastGroundedTime = Time.time;
                    }
                    else
                    {
                        lastFrameGrounded = isGrounded;
                    }
                    // Flipping:
                    if (!stopLookingBackwards)
                    {
                        t.localScale = new Vector3(rg.velocity.x > 0 ? 1 : rg.velocity.x < 0 ? -1 : t.localScale.x, 1, 1);
                    }
                }                
                // Animations:
                if (character.animator)
                {
                    character.animator.SetBool("Moving", moving);
                    character.animator.SetBool("Dead", isDead);
                    character.animator.SetBool("Falling", isFalling);

                    // Set the animator speed based on the current movement speed (only applies to grounded moving animations such as running):
                    character.animator.speed = moving && isGrounded ? Mathf.Abs(xInput) : 1;
                }            
            allowJump = true;
            isGrounded = false;
            Collider2D[] cols = Physics2D.OverlapCircleAll(groundCheckerOffset + new Vector2(transform.position.x, transform.position.y), groundCheckerRadius);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].CompareTag("JumpPad"))
                {
                    allowJump = false;
                }
                if (cols[i].gameObject != gameObject && !cols[i].isTrigger && !cols[i].CompareTag("Portal"))
                {
                    if (!isGrounded) isGrounded = true;
                }
            }
            }
            else
            {
                if (Object.HasStateAuthority)
                {
                    if (!stopLookingBackwards)
                    {
                        if (rg.velocity.x < 0)
                        {
                            transform.localScale = new Vector3(-1, 1, 1);
                        }
                        if (rg.velocity.x > 0)
                        {
                            transform.localScale = new Vector3(1, 1, 1);
                        }
                        transform.localScale = new Vector3(rg.velocity.x > 0 ? 1 : rg.velocity.x < 0 ? -1 : transform.localScale.x, 1, 1);
                    }
                }
            }
        }
        public Vector2 movement { get { return m; } }
        Vector2 m;  // movement
        Vector2 vel = Vector2.zero;
        public bool stopLookingBackwards = false;
        public Collider2D PlayerAtBottomCheckTrigger;
        public ContactFilter2D playerContactFilter2D;
        public Collider2D[] colliders = new Collider2D[1];
        float pingSentTimeElapsed = 3.0f;
        void FixedUpdate()
    {
        if (forPreview) return;

            if (GameManager.instance.isGameOver || GameManager.instance.LevelWinScreen.activeInHierarchy)
            {
                character.animator.SetBool("Moving", false);
                character.animator.speed = 0;
                return;
            }
            PlayerAtBottomCheckTrigger.OverlapCollider(playerContactFilter2D, colliders);
            var moving = rg.velocity.x != 0 && xInput != 0;
            if (GameManager.instance.gameStarted && colliders[0] != null && !moving)
            {
                //if (!photonView.IsMine)
                //{
                //    if (Helper.GetDistance(playerController.networkPos, position) < 2)
                //    {
                //        position += colliders[0].gameObject != null ? new Vector2(colliders[0].GetComponent<MovementController>().position.x - position.x, 0) : Vector2.zero;
                //    }
                //}
                //else
                //{
                rg.position += colliders[0].gameObject != null ? new Vector2(colliders[0].GetComponent<PlayerController>().rg.position.x - rg.position.x, 0) : Vector2.zero;                
                //}
            }
            else
            {
                colliders[0] = null;
            }
            if (GameManager.gameMode == GameMode.Multiplayer)
            {
                if (Object.HasStateAuthority) return;
                if (!Connector.instance.networkRunner.IsServer)
                {
                    if (pingSentTimeElapsed >= 3)
                    {
                        GameManager.instance.clientPing = (int)(Connector.instance.networkRunner.GetPlayerRtt(Connector.instance.networkRunner.LocalPlayer) * 1000f);
                        RPC_SetPingForServer(GameManager.instance.clientPing);
                        pingSentTimeElapsed = 0;
                    }
                    else
                    {
                        pingSentTimeElapsed += Time.deltaTime;
                    }
                }
                rg.position = Vector2.SmoothDamp(rg.position, networkPos, ref vel, 0.3f);
                rg.velocity = networkVel;
                transform.localScale = new Vector3(networkScaleX,1,1);
                moving = networkMoving;
                isFalling = networkIsFalling;
                xInput = networkxInput;
                yInput = networkyInput;
                isGrounded = networkIsgrounded;
                allowJump = networkAllowJump;
            }                       
            if (character.animator)
            {
                character.animator.SetBool("Moving", moving);
                character.animator.SetBool("Dead", isDead);
                character.animator.SetBool("Falling", isFalling);

                // Set the animator speed based on the current movement speed (only applies to grounded moving animations such as running):
                character.animator.speed = moving && isGrounded ? Mathf.Abs(xInput) : 1;
            }
        }
        [Rpc]
        public void RPC_SetPingForServer(int _clientPing)
        {
            if (Connector.instance.networkRunner.IsServer)
            {
                GameManager.instance.clientPing = _clientPing;
                //Debug.Log("Server Client Value = " + GameManager.instance.clientPing + "received Value = " + _clientPing);
            }
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.name != "BoosterBox" && collision.transform.parent != null && collision.transform.parent.name != "Wall")
            {
                stopRegularVelocity = false;
            }
        }
        [Networked] private Vector2 networkPos { get; set; }
        [Networked] private Vector2 networkVel { get; set; }
        [Networked] private float networkScaleX { get; set; }
        [Networked] private bool networkMoving { get; set; }
        [Networked] private bool networkIsFalling { get; set; }
        [Networked] private float networkxInput { get; set; }
        [Networked] private float networkyInput { get; set; }
        [Networked] private bool networkIsgrounded { get; set;}
        [Networked] private bool networkAllowJump { get; set; }
        void HandleInputs()
    {

            //Debug.Log("AA tau gaya");
        // Only allow controls if the menu is not shown (the menu when you press 'ESC' on PC):
        if (!gm.ui.isMenuShown)
        {
                //Debug.Log("Menu bhi shown hai");
                // Example emote keys (this is just a hard-coded example of displaying an emote using alphanumeric keys
                //  so you may have to implement a more robust emote input system depending on your project's needs):
                if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                //photonView.RPC("Emote", RpcTarget.All, 0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                //photonView.RPC("Emote", RpcTarget.All, 1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                //photonView.RPC("Emote", RpcTarget.All, 2);
            }
            // Player controls:
            if (gm.gameStarted && !gm.isGameOver)
            {
                    //Debug.Log("Xinput ko bhi value mil rhi ahi");
                    xInput = gm.controlsManager.mobileControls ? gm.controlsManager.horizontal : gm.controlsManager.horizontalRaw;
                    yInput = gm.controlsManager.mobileControls ? gm.controlsManager.vertical : gm.controlsManager.verticalRaw;
                    //Debug.Log("Xinput ki value = " + xInput);
                    //xInput = gm.useMobileControls ? gm.controlsManager.horizontal : gm.controlsManager.horizontalRaw;
                }
                else
            {
                // Reset movement inputs when game is over:
                xInput = 0;
                yInput = 0;
            }
        }
        else
        {
            xInput = 0;
            yInput = 0;
        }
    }

    /// <Summary> 
    /// Disable unnecessary components for main menu preview.
    /// Should be called before the Start() function.
    ///</Summary>
    public void SetAsPreview()
    {
        if (forPreview)
        {
            forPreview = true;
            Destroy(rg);
            //Destroy(photonView);

            // Get the chosen character (locally):
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i].data == DataCarrier.characters[DataCarrier.chosenCharacter])
                {
                    curCharacterID = i;
                }
            }

            // Enable only the chosen character's graphics:
            for (int i = 0; i < characters.Length; i++)
            {
                characters[i].animator.gameObject.SetActive(i == curCharacterID);
            }
            return;
        }
    }
    public void SubscribeJump()
    {
            // Subscibe to Controls Manager's jump event if player is ours:
            if (GameManager.gameMode == GameMode.Multiplayer)
            {
                if (!Object.HasInputAuthority) return;
                if (Object.HasInputAuthority)
                {
                    //Debug.Log("Pehlaaa Debug");
                    gm.controlsManager.jump += Jump;
                }
            }            
            else
            {
                if (isPlayerOurs)
                {
                    //gm.controlsManager.jump += Jump;
                    gm.controlsManager.jump += Jump;
                }
                else
                {
                    //gm.controlsManager.jump -= Jump;
                    gm.controlsManager.jump -= Jump;
                }
            }        
    }
    public void RestartPlayer()
    {            
            // Get the dedicated player instance for this player:
            if (GameManager.gameMode == GameMode.Multiplayer)
        {
                //Debug.Log("Cheeeeek Karoooo " + GameManager.instance.punPlayersAll.ToList().IndexOf(Object.InputAuthority));
                playerInstance = gm.GetPlayerInstance(GameManager.instance.punPlayersAll.ToList().IndexOf(Object.InputAuthority));
                //networkPlayerInstance = playerInstance;
                //RPC_RestartPlayer(Object.InputAuthority);
            }
        else
        {
            playerInstance = gm.GetPlayerInstance(actorNumber);
        }
        // Get the chosen character of this player (we only need the index of the chosen character in DataCarrier's characters array):
        int chosenCharacter = playerInstance.character;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].data == DataCarrier.characters[chosenCharacter])
            {
                curCharacterID = i;
            }
        }

        // Enable only the chosen character's graphics:
        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].animator.gameObject.SetActive(i == curCharacterID);
        }

    }        

        public void Jump()
    {
        if (!gm.gameStarted || gm.isGameOver) return;

        if (!isOnJumpPad && isGrounded && allowJump)
        {
                // Call jump in character controller:
                if (GameManager.gameMode == GameMode.Multiplayer)
                {
                    if (Object.HasStateAuthority)
                    {
                        DoJump();
                    }
                    else
                    {
                        RPC_DoJump();
                    }
                }
                else
                {
                    DoJump();
                }

                if (character.data.jumpSFX.Length > 0)
            {
                aus.PlayOneShot(character.data.jumpSFX[Random.Range(0, character.data.jumpSFX.Length)]);
            }
        }
    }
    [HideInInspector] public float jumpForce;
    [HideInInspector] public float moveSpeed;
        private void DoJump()
    {
        if (!rg) return;

            if (isGrounded && allowJump)
            {
                Vector2 veloc = rg.velocity;
                veloc.y = 12;
                rg.velocity = veloc;

                // Don't allow jumping right after a jump:
                allowJump = false;
            }        
    }
        [Rpc/*(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)*/]
        private void RPC_DoJump(/*[RpcTarget] PlayerRef player*/)
        {
            //if (!Object.HasStateAuthority)
            //{
            //    return;
            //}
            //if (Object.InputAuthority != player)
            //{
            //    return;
            //}
            if (!rg) return;
            if (isGrounded && allowJump)
            {
                Vector2 veloc = rg.velocity;
                veloc.y = 12;
                rg.velocity = veloc;

                // Don't allow jumping right after a jump:
                allowJump = false;
            }            
        }
        public void Land()
    {
        gm.pooler.Spawn("LandDust", transform.position);
        // Sound:
        if (character.data.landingsSFX.Length > 0) aus.PlayOneShot(character.data.landingsSFX[Random.Range(0, character.data.landingsSFX.Length)]);
    }

    public void OwnerShootCommand()
    {
        //photonView.RPC("Shoot", RpcTarget.All, mousePos, movementController.position, movementController.velocity);
    }
    // Called by the owner from mobile or pc input:
    public void OwnerMeleeAttack()
    {
        //if (curMeleeAttackRate >= 1)
        //{
        //    photonView.RPC("MeleeAttack", RpcTarget.All);
        //    curMeleeAttackRate = 0;
        //}
    }
    public void OwnerThrowGrenade()
    {
        //if (curGrenadeCount > 0)
        //{
        //    curGrenadeCount -= 1;
        //    photonView.RPC("ThrowGrenade", RpcTarget.All);
        //}
    }

    public void Die()
    {
        if (!gm.isGameOver)
        {
                // and then destroy (give a time for the death animation):
                //if (Object.HasInputAuthority)
                //{
                //    Invoke("PhotonDestroy", 1f);
                //}
                if (GameManager.gameMode == GameMode.Multiplayer)
                {
                    Object.Runner.Despawn(gameObject.GetComponent<NetworkObject>());
                }
            if (playerInstance.isMine)
            {
                Destroy(gameObject);
            }
        }

        // Cancel any movement:
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = false;
        }
        // and remove the rigidbody:
        Destroy(rg);
    }

    // Instant death from dead zone:
    public void DeadZoned()
    {
        // VFX:
        if (gm.maps[gm.chosenMap].deadZoneVFX)
        {
            Instantiate(gm.maps[gm.chosenMap].deadZoneVFX, new Vector3(transform.position.x, gm.deadZoneOffset, 0), Quaternion.identity);
            rg.gravityScale = 0;
            rg.mass = 0;
            isDead = true;
            Object.Runner.Despawn(gameObject.GetComponent<NetworkObject>());
            GameManager.instance.isGameOver = true;
            }
        }

    void PhotonDestroy()
    {
        //PhotonNetwork.Destroy(photonView);
    }


    /// <summary>
    /// Deal damage to player.
    /// </summary>
    /// <param name="fromPlayer">Damage dealer player name.</param>
    /// <param name="value">Can be either a weapon id (if a gun was used) or a damage value (if melee attack or grenade).</param>
    /// <param name="gun">If set to <c>true</c>, "value" will be used as weapon id.</param>
    public void ApplyDamage(int fromPlayer, int value, bool gun)
    {
        //photonView.RPC("Hurt", RpcTarget.AllBuffered, fromPlayer, value, gun);
    }
        void TriggerDeadZone(Vector2 position)
        {
            rg.position = position;
            networkPos = position;
            //DeadZoned();
            if (gm.maps[gm.chosenMap].deadZoneVFX)
            {
                Instantiate(gm.maps[gm.chosenMap].deadZoneVFX, new Vector3(transform.position.x, gm.deadZoneOffset, 0), Quaternion.identity);
                rg.gravityScale = 0;
                rg.mass = 0;
                isDead = true;
                GameManager.instance.isGameOver = true;
                Destroy(gameObject);

            }
        }
        [Rpc]
        void RPC_TriggerDeadZone(Vector2 position)
        {
            rg.position = position;
            networkPos = position;
            //DeadZoned();
            if (gm.maps[gm.chosenMap].deadZoneVFX)
            {
                Instantiate(gm.maps[gm.chosenMap].deadZoneVFX, new Vector3(transform.position.x, gm.deadZoneOffset, 0), Quaternion.identity);
                rg.gravityScale = 0;
                rg.mass = 0;
                isDead = true;
                GameManager.instance.isGameOver = true;
                var players = FindObjectsOfType<PlayerController>();
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].gameObject.SetActive(false);
                }
                //Object.Runner.Despawn(gameObject.GetComponent<NetworkObject>());
            
        }
    }
        [Rpc]
        public void RPC_Emote(int emote)
    {
        if (curEmote && curEmote.isReady)
        {
            curEmote.Show(emote);
        }
    }


        void OnDestroy()
    {
        if (!forPreview) gm.playerControllers.RemoveAll(p => p == null);
    }
    }




    //public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable, IInRoomCallbacks
    //{
    //    public bool hasDoorKey;
    //    public GameObject keyObj;
    //    public bool forPreview = false;                                 // used for non in-game such as character customization preview in the main menu

    //    [System.Serializable]
    //    public class Character
    //    {
    //        public CharacterData data;
    //        public Animator animator;

    //        // For cosmetics:
    //        public Transform hatPoint;
    //    }
    //    public Character[] characters;                                  // list of characters for the spawnable characters (modifying this will not change the main menu character selection screen
    //                                                                    // NOTE: please read the manual to learn how to add and remove characters from the character selection screen)

    //    [Space]
    //    [Header("Settings:")]
    //    public int maxShield;
    //    public string grenadePrefab;
    //    public float grenadeThrowForce = 20;

    //    [Header("References:")]
    //    public AIController ai;									        // the AI controller for this player (only gets enabled if this is a bot, disabled when not)
    //    public AudioSource aus;                                         // the AudioSource that will play the player sounds
    //    public MovementController movementController;                   // the one that controls the rigidbody movement
    //    public Transform weaponHandler;                                 // the transform that holds weapon prefabs
    //    public Transform grenadePoint;                                  // where grenades spawn
    //    public MeleeWeaponController meleeWeapon;                       // the melee weapon controller
    //    public CosmeticsManager cosmeticsManager;                       // the component that manages the cosmetic side of things
    //    public GameObject invulnerabilityIndicator;				        // shown when player is invulnerable
    //    public AudioClip[] hurtSFX;                                     // audios that are played randomly when getting damaged
    //    public AudioClip throwGrenadeSFX;               		        // audio that's played when throwing grenades
    //    public GameObject spawnVFX;								        // the effect that's shown on spawn
    //    public EmotePopup emotePopupPrefab;                             // emote prefab to spawn
    //    public int actorNumber;
    //    public GameObject talkingIndicator;

    //    // In-Game:
    //    public PlayerInstance playerInstance;
    //    public PlayerInstance lastDamageDealer { get; protected set; }  // the last player to damage us (used to track who killed us and etc.)
    //    public int curCharacterID { get; protected set; }               // determines which character is used for this player
    //    public int health { get; protected set; }                       // current health amount
    //    public int shield { get; protected set; }                       // current shield amount
    //    public int lastWeaponId { get; protected set; }                 // used when sending damage across the network so everyone knows what weapon were used (negative value means character id)
    //    public bool isDead { get; protected set; }                      // is this player dead?
    //    public Vector3 mousePos { get; protected set; }                 // the mouse position we're working on locally
    //    public Weapon curWeapon { get; protected set; }                 // the current "physical" weapon the player is holding
    //    public Weapon originalWeapon { get; protected set; }            // the current weapon's prefab reference
    //    public EmotePopup curEmote { get; protected set; }              // this player's own emote popup
    //    public int curGrenadeCount { get; protected set; }              // how much grenades left
    //    public int curMultikill { get; protected set; }                 // current multi kills
    //    [HideInInspector] public bool isOnJumpPad;				        // when true, jumping is disabled to not interfere with the jump pad
    //    [HideInInspector] public Vector3 nMousePos;                     // mouse position from network. We're gonna smoothly interpolate the mousePos' value to this one to prevent the jittering effect.
    //    [HideInInspector] public bool shooting;                         // are we shooting?
    //    [HideInInspector] public float xInput;				            // the X input for the movement controls (sent to other clients for animation speed control)
    //    float jumpProgress;                                             // longer press means higher jump
    //    float curInvulnerability;
    //    float curMeleeAttackRate;
    //    float curMultikillDelay = 1;
    //    bool moving;                                                    // are we moving on ground?
    //    bool isFalling;                                                 // are we falling? (can be used for something like a falling animation)
    //    bool lastFrameGrounded;                                         // used for spawning landing vfx
    //    bool doneDeadZone;										        // makes sure that DeadZoned() doesn't called repeatedly
    //    float lastGroundedTime;
    //    Vector3 lastAimPos;                             		        // used for mobile controls
    //    GameManager gm;                                                 // GameManger instance reference for simplicity

    //    // Network:
    //    [HideInInspector] public Vector2 lastPos, networkPos;
    //    float lag;

    //    // Returns the chosen character:
    //    public Character character
    //    {
    //        get
    //        {
    //            return characters[curCharacterID];
    //        }
    //    }
    //    // Returns true if invulnerable:
    //    public bool invulnerable
    //    {
    //        get
    //        {
    //            return curInvulnerability < gm.invulnerabilityDuration;
    //        }
    //    }
    //    // Returns true if this is a bot:
    //    public bool isBot
    //    {
    //        get
    //        {
    //            if (GameManager.gameMode == GameMode.Multiplayer)
    //            {
    //                return photonView.IsSceneView;
    //            }
    //            else
    //            {
    //                return false;
    //            }
    //        }
    //    }
    //    // Check if this player is ours and not owned by a bot or another player:
    //    public bool isPlayerOurs
    //    {
    //        get
    //        {
    //            return !playerInstance.isBot && playerInstance.isMine;
    //        }
    //    }

    //    void Awake()
    //    {
    //        // Find essential references:
    //        gm = GameManager.instance;
    //    }
    //    public override void OnEnable()
    //    {
    //        if (gm)
    //        {
    //            // Add this to the player controllers list:
    //            gm.playerControllers.Add(this);
    //        }
    //    }
    //    public override void OnDisable()
    //    {
    //        if (gm)
    //        {
    //            // Unsubscibe to Controls Manager events (doesn't do anything if player isn't ours):
    //            gm.controlsManager.jump -= Jump;

    //            // Remove from the player controllers list
    //            gm.playerControllers.Remove(this);
    //        }
    //    }
    //    void Start()
    //    {
    //        if (forPreview) return;

    //        // Spawn VFX:
    //        Instantiate(spawnVFX, transform);

    //        // If this is a bot, we need to initialize it and get its bot index from its instantiation data:
    //        if (isBot)
    //        {
    //            ai.InitializeBot((int)photonView.InstantiationData[0]);
    //            ai.enabled = true;
    //        }
    //        else
    //        {
    //            ai.enabled = false;
    //        }

    //        // Reset player stats and stuff:
    //        RestartPlayer();

    //        // Create a floating bar and apply stats from chosen character data to player:
    //        gm.ui.SpawnFloatingBar(this);
    //        movementController.moveSpeed = character.data.moveSpeed;
    //        movementController.jumpForce = character.data.jumpForce;
    //        curGrenadeCount = character.data.grenades;

    //        // Apply the cosmetics:
    //        cosmeticsManager.Refresh(playerInstance.cosmeticItems);

    //        // Spawn our own emote popup:
    //        curEmote = Instantiate(emotePopupPrefab, Vector3.zero, Quaternion.identity);
    //        curEmote.owner = this;

    //        // Equip the starting weapon (if our current character has one):
    //        EquipStartingWeapon();

    //        if (photonView.IsMine)
    //        {
    //            // Setting up send rates:
    //            PhotonNetwork.SendRate = 30;
    //            PhotonNetwork.SerializationRate = 30;
    //        }
    //    }
    //    void Update()
    //    {
    //        // Let the movement controller know how to behave:
    //        if (GameManager.gameMode == GameMode.Multiplayer)
    //        {
    //            movementController.isMine = photonView.IsMine;
    //        }
    //        else
    //        {
    //            movementController.isMine = playerInstance.isMine;
    //        }
    //        // If we're the local player, let the camera know:
    //        //if (playerInstance.isMine)
    //        //{
    //        //    gm.gameCam.target = this;
    //        //}

    //        if (forPreview) return;

    //        Transform t = transform;

    //        if (!isDead)
    //        {
    //            // Manage invulnerability:
    //            // *When invulnerable:
    //            if (curInvulnerability < gm.invulnerabilityDuration)
    //            {
    //                if (gm.gameStarted) curInvulnerability += Time.deltaTime;

    //                // Show the invulnerability indicator:
    //                invulnerabilityIndicator.SetActive(true);
    //            }
    //            // *When not:
    //            else
    //            {
    //                // Hide invulnerability indicator when finally vulnerable:
    //                if (invulnerabilityIndicator.activeSelf) invulnerabilityIndicator.SetActive(false);
    //            }

    //            // Check if we're currently falling:
    //            isFalling = movementController.velocity.y < 0;

    //            // If owned by us (including bots):
    //            if (photonView.IsMine || playerInstance.isMine)
    //            {
    //                // Dead zone interaction:
    //                if (gm.deadZone)
    //                {
    //                    if (t.position.y < gm.deadZoneOffset && !doneDeadZone)
    //                    {
    //                        photonView.RPC("TriggerDeadZone", RpcTarget.All, movementController.position);
    //                        doneDeadZone = true;
    //                    }
    //                }
    //                // *For our player:
    //                if (isPlayerOurs)
    //                {
    //                    HandleInputs();
    //                }
    //                // *For the bots:
    //                else
    //                {
    //                    if (!gm.isGameOver)
    //                    {
    //                        // Smooth mouse aim sync for the bot:
    //                        mousePos = nMousePos;
    //                    }
    //                }

    //                // Melee attack rate:
    //                if (curMeleeAttackRate < 1)
    //                {
    //                    curMeleeAttackRate += Time.deltaTime * meleeWeapon.attackSpeed;
    //                }

    //                // Multikill timer:
    //                if (curMultikillDelay > 0)
    //                {
    //                    curMultikillDelay -= Time.deltaTime;
    //                }
    //                else
    //                {
    //                    curMultikill = 0;
    //                }
    //            }
    //            else
    //            {
    //                // Smooth mouse aim sync:
    //                mousePos = Vector3.MoveTowards(mousePos, nMousePos, Time.deltaTime * (mousePos - nMousePos).magnitude * 15);
    //            }

    //            // Apply movement input to the movement controller:
    //            movementController.InputMovement(xInput);

    //            // Landing VFX:
    //            if (movementController.isGrounded)
    //            {
    //                if (!lastFrameGrounded && (Time.time - lastGroundedTime) > 0.1f)
    //                {
    //                    Land();
    //                }
    //                lastFrameGrounded = movementController.isGrounded;
    //                lastGroundedTime = Time.time;
    //            }
    //            else
    //            {
    //                lastFrameGrounded = movementController.isGrounded;
    //            }

    //            // Hide gun if attacking with melee weapon:
    //            weaponHandler.gameObject.SetActive(!meleeWeapon.isAttacking);

    //            // Flipping:
    //            t.localScale = new Vector3(mousePos.x > t.position.x ? 1 : mousePos.x < t.position.x ? -1 : t.localScale.x, 1, 1);

    //            // Since we're syncing everyone's mouse position across the network, we can just do the aiming locally:
    //            Vector3 diff = mousePos - weaponHandler.position;
    //            diff.Normalize();
    //            float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
    //            weaponHandler.rotation = Quaternion.Euler(0f, 0f, rot_z + (t.localScale.x == -1 ? 180 : 0));
    //        }

    //        // Handling death:
    //        if (health <= 0 && !isDead)
    //        {
    //            isDead = true;

    //            if (!gm.isGameOver)
    //            {
    //                // Remove any weapons:
    //                DisarmItem();
    //            }

    //            // Update the others about our status:
    //            if (photonView.IsMine)
    //            {
    //                photonView.RPC("UpdateOthers", RpcTarget.All, health, shield);

    //                // If this is local player's, let the game manager know this is ours and is now dead:
    //                if (isPlayerOurs && !gm.isGameOver) gm.dead = true;
    //            }
    //            Die();
    //        }

    //        // Animations:
    //        if (character.animator)
    //        {
    //            character.animator.SetBool("Moving", moving);
    //            character.animator.SetBool("Dead", isDead);
    //            character.animator.SetBool("Falling", isFalling);

    //            // Set the animator speed based on the current movement speed (only applies to grounded moving animations such as running):
    //            character.animator.speed = moving && movementController.isGrounded ? Mathf.Abs(xInput) : 1;
    //        }
    //    }
    //    Vector2 vel = Vector2.zero;
    //    void FixedUpdate()
    //    {
    //        if (forPreview) return;

    //        // Simple lag compensation (movement interpolation + movement controller velocity set at OnPhotonSerializeView()):
    //        if (!photonView.IsMine && movementController)
    //        {
    //            //Vector2 diff = networkPos - movementController.position;
    //            //movementController.position += diff; 
    //            //movementController.position = Vector2.MoveTowards(movementController.position, networkPos, Time.fixedDeltaTime * (lag * 100));
    //            movementController.position = Vector2.SmoothDamp(movementController.position, networkPos, ref vel, 0.3f);
    //            //movementController.position = networkPos;
    //        }
    //    }

    //    void HandleInputs()
    //    {
    //        // Is moving on ground?:
    //        moving = movementController.velocity.x != 0 && movementController.isGrounded && xInput != 0;

    //        // Only allow controls if the menu is not shown (the menu when you press 'ESC' on PC):
    //        if (!gm.ui.isMenuShown)
    //        {
    //            // Example emote keys (this is just a hard-coded example of displaying an emote using alphanumeric keys
    //            //  so you may have to implement a more robust emote input system depending on your project's needs):
    //            if (Input.GetKeyDown(KeyCode.Alpha1))
    //            {
    //                photonView.RPC("Emote", RpcTarget.All, 0);
    //            }
    //            if (Input.GetKeyDown(KeyCode.Alpha2))
    //            {
    //                photonView.RPC("Emote", RpcTarget.All, 1);
    //            }
    //            if (Input.GetKeyDown(KeyCode.Alpha3))
    //            {
    //                photonView.RPC("Emote", RpcTarget.All, 2);
    //            }
    //            // Player controls:
    //            if (gm.gameStarted && !gm.isGameOver)
    //            {
    //                // Mouse position on screen or Joystick value if mobile (will be sent across the network):
    //                if (gm.useMobileControls)
    //                {
    //                    // Mobile joystick:
    //                    lastAimPos = new Vector3(gm.controlsManager.aimX, gm.controlsManager.aimY, 0).normalized;
    //                    mousePos = lastAimPos + new Vector3(transform.position.x, weaponHandler.position.y, 0);
    //                }
    //                else
    //                {
    //                    // PC mouse:
    //                    //mousePos = gm.gameCam.theCamera.ScreenToWorldPoint(Input.mousePosition);
    //                }

    //                // Horizontal movement input:
    //                xInput = gm.useMobileControls ? gm.controlsManager.horizontal : gm.controlsManager.horizontalRaw;

    //                // Shooting:
    //                shooting = gm.controlsManager.shoot;

    //                // Melee:
    //                //if (!gm.useMobileControls && Input.GetButtonDown("Fire2"))
    //                //{
    //                //    OwnerMeleeAttack();
    //                //}

    //                // Grenade throw:
    //                //if (!gm.useMobileControls && Input.GetButtonDown("Fire3"))
    //                //{
    //                //    OwnerThrowGrenade();
    //                //}
    //            }
    //            else
    //            {
    //                // Reset movement inputs when game is over:
    //                xInput = 0;
    //            }
    //        }
    //        else
    //        {
    //            xInput = 0;
    //        }
    //    }

    //    /// <Summary> 
    //    /// Disable unnecessary components for main menu preview.
    //    /// Should be called before the Start() function.
    //    ///</Summary>
    //    public void SetAsPreview()
    //    {
    //        if (forPreview)
    //        {
    //            forPreview = true;

    //            invulnerabilityIndicator.SetActive(false);
    //            movementController.DestroyRigidbody();
    //            ai.enabled = false;
    //            meleeWeapon.enabled = false;
    //            Destroy(photonView);

    //            // Get the chosen character (locally):
    //            for (int i = 0; i < characters.Length; i++)
    //            {
    //                if (characters[i].data == DataCarrier.characters[DataCarrier.chosenCharacter])
    //                {
    //                    curCharacterID = i;
    //                }
    //            }

    //            // Enable only the chosen character's graphics:
    //            for (int i = 0; i < characters.Length; i++)
    //            {
    //                characters[i].animator.gameObject.SetActive(i == curCharacterID);
    //            }
    //            return;
    //        }
    //    }

    //    public void EquipStartingWeapon()
    //    {
    //        if (character.data.startingWeapon)
    //        {
    //            // A negative value as a weapon id is invalid, but we can use it to tell everyone that it's a starting weapon since starting weapons don't need id's because 
    //            // there is only one starting weapon for each character anyway.
    //            // Starting weapons might not be set as spawnable in a map so refer to the current character's data instead:
    //            lastWeaponId = -(curCharacterID + 1); // deacreased by 1 because an index of 0 will not do the trick (will be resolve later)

    //            // Spawn the starting weapon:
    //            originalWeapon = character.data.startingWeapon;
    //            curWeapon = Instantiate(originalWeapon, weaponHandler);
    //            curWeapon.owner = this;
    //        }
    //    }
    //    public void SubscribeJump()
    //    {
    //        // Subscibe to Controls Manager's jump event if player is ours:
    //        if (isPlayerOurs)
    //        {
    //            gm.controlsManager.jump += Jump;
    //        }
    //        else
    //        {
    //            gm.controlsManager.jump -= Jump;
    //        }
    //    }
    //    public void RestartPlayer()
    //    {
    //        // Get the dedicated player instance for this player:
    //        if (GameManager.gameMode == GameMode.Multiplayer)
    //        {
    //            playerInstance = gm.GetPlayerInstance(isBot ? ai.botID : photonView.Owner.ActorNumber);
    //        }
    //        else
    //        {
    //            playerInstance = gm.GetPlayerInstance(actorNumber);
    //        }
    //        SubscribeJump();
    //        // Get the chosen character of this player (we only need the index of the chosen character in DataCarrier's characters array):
    //        int chosenCharacter = playerInstance.character;
    //        for (int i = 0; i < characters.Length; i++)
    //        {
    //            if (characters[i].data == DataCarrier.characters[chosenCharacter])
    //            {
    //                curCharacterID = i;
    //            }
    //        }

    //        // Enable only the chosen character's graphics:
    //        for (int i = 0; i < characters.Length; i++)
    //        {
    //            characters[i].animator.gameObject.SetActive(i == curCharacterID);
    //        }

    //        // Get the stat infos from the character data:
    //        health = character.data.maxHealth;

    //        // Remove any weapon:
    //        DisarmItem();
    //    }

    //    public void Jump()
    //    {
    //        if (!gm.gameStarted || gm.isGameOver) return;

    //        if (!isOnJumpPad && movementController.isGrounded && movementController.allowJump)
    //        {
    //            // Call jump in character controller:
    //            movementController.Jump();

    //            if (character.data.jumpSFX.Length > 0)
    //            {
    //                aus.PlayOneShot(character.data.jumpSFX[Random.Range(0, character.data.jumpSFX.Length)]);
    //            }
    //        }
    //    }

    //    public void Land()
    //    {
    //        gm.pooler.Spawn("LandDust", transform.position);
    //        // Sound:
    //        if (character.data.landingsSFX.Length > 0) aus.PlayOneShot(character.data.landingsSFX[Random.Range(0, character.data.landingsSFX.Length)]);
    //    }

    //    public void OwnerShootCommand()
    //    {
    //        //photonView.RPC("Shoot", RpcTarget.All, mousePos, movementController.position, movementController.velocity);
    //    }
    //    // Called by the owner from mobile or pc input:
    //    public void OwnerMeleeAttack()
    //    {
    //        //if (curMeleeAttackRate >= 1)
    //        //{
    //        //    photonView.RPC("MeleeAttack", RpcTarget.All);
    //        //    curMeleeAttackRate = 0;
    //        //}
    //    }
    //    public void OwnerThrowGrenade()
    //    {
    //        //if (curGrenadeCount > 0)
    //        //{
    //        //    curGrenadeCount -= 1;
    //        //    photonView.RPC("ThrowGrenade", RpcTarget.All);
    //        //}
    //    }

    //    public void Die()
    //    {
    //        if (!gm.isGameOver)
    //        {
    //            // Multikill (if we are the killer and we are not the one dying):
    //            PlayerController killerPc = gm.GetPlayerControllerOfPlayer(lastDamageDealer);

    //            // If killer matched:
    //            if (killerPc)
    //            {
    //                // If the killer is ours (bots are also ours if we're the master client):
    //                if (killerPc.playerInstance.playerID != playerInstance.playerID && (killerPc.isPlayerOurs || (PhotonNetwork.IsMasterClient && killerPc.isBot)))
    //                {
    //                    killerPc.curMultikill += 1;
    //                    killerPc.curMultikillDelay = gm.multikillDuration;

    //                    // Add a bonus score to killer for doing a multi kill:
    //                    if (killerPc.curMultikill > 1)
    //                    {
    //                        int scoreToAdd = gm.multiKillMessages[Mathf.Clamp(killerPc.curMultikill - 1, 0, gm.multiKillMessages.Length - 1)].bonusScore;
    //                        gm.AddScore(lastDamageDealer, false, false, scoreToAdd);
    //                    }
    //                }
    //            }

    //            // Let GameManager handle the other death related stuff (scoring, display kill/death message etc...):
    //            if (lastDamageDealer != null)
    //            {
    //                gm.SomeoneDied(playerInstance.playerID, lastDamageDealer.playerID);
    //            }

    //            // and then destroy (give a time for the death animation):
    //            if (photonView.IsMine)
    //            {
    //                Invoke("PhotonDestroy", 1f);
    //            }
    //            else if (playerInstance.isMine)
    //            {
    //                Destroy(gameObject);
    //            }
    //        }

    //        // Cancel any movement:
    //        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
    //        for (int i = 0; i < cols.Length; i++)
    //        {
    //            cols[i].enabled = false;
    //        }
    //        // and remove the rigidbody:
    //        movementController.DestroyRigidbody();
    //        // ...and others:
    //        invulnerabilityIndicator.SetActive(false);
    //    }

    //    public void Teleport(Vector3 newPos)
    //    {
    //        networkPos = newPos;
    //        if (movementController) movementController.transform.position = networkPos;
    //    }

    //    // Instant death from dead zone:
    //    public void DeadZoned()
    //    {
    //        lastDamageDealer = playerInstance;
    //        health = 0;

    //        // VFX:
    //        if (gm.maps[gm.chosenMap].deadZoneVFX)
    //        {
    //            Instantiate(gm.maps[gm.chosenMap].deadZoneVFX, new Vector3(transform.position.x, gm.deadZoneOffset, 0), Quaternion.identity);
    //        }
    //    }

    //    void PhotonDestroy()
    //    {
    //        PhotonNetwork.Destroy(photonView);
    //    }


    //    /// <summary>
    //    /// Deal damage to player.
    //    /// </summary>
    //    /// <param name="fromPlayer">Damage dealer player name.</param>
    //    /// <param name="value">Can be either a weapon id (if a gun was used) or a damage value (if melee attack or grenade).</param>
    //    /// <param name="gun">If set to <c>true</c>, "value" will be used as weapon id.</param>
    //    public void ApplyDamage(int fromPlayer, int value, bool gun)
    //    {
    //        photonView.RPC("Hurt", RpcTarget.AllBuffered, fromPlayer, value, gun);
    //    }
    //    [PunRPC]
    //    void Hurt(int fromPlayer, int value, bool gun)
    //    {
    //        if (!gm.isGameOver)
    //        {
    //            // Only damage if vulnerable:
    //            if (!invulnerable)
    //            {
    //                int finalDamage = 0; // the damage value

    //                // If damage is from a gun:
    //                if (gun)
    //                {
    //                    // Get the weapon used using the "value" parameter as weapon id (or if it's a negative value, then it's a character id):
    //                    Weapon weaponUsed = value >= 0 ? gm.maps[gm.chosenMap].spawnableWeapons[value] : characters[value * -1 - 1].data.startingWeapon;

    //                    // ...then get the weapon's damage value:
    //                    finalDamage = weaponUsed.damage;
    //                }
    //                else
    //                {
    //                    // If not a gun then it could be from a grenade or a melee attack, either way, just assume that the "value" parameter is the damage value:
    //                    finalDamage = value;
    //                }

    //                // Now do the damage application:
    //                // First, calculate the penetrating damage:
    //                int damageToHP = finalDamage - shield;
    //                // ...then apply damage to shield:
    //                shield = shield - finalDamage <= 0 ? 0 : shield - finalDamage;
    //                // Finally, apply the excess damage to HP:
    //                if (damageToHP > 0) health -= damageToHP;

    //                // Let others know our real health value if this player is ours:
    //                if (photonView.IsMine)
    //                {

    //                }
    //                // Do a damage prediction locally while waiting for the server's side if not ours:
    //                else
    //                {

    //                }

    //                // Damage popup:
    //                if (gm.damagePopups)
    //                {
    //                    gm.pooler.Spawn("DamagePopup", weaponHandler.position).GetComponent<DamagePopup>().Set(finalDamage);
    //                }

    //                // Sound:
    //                aus.PlayOneShot(hurtSFX[Random.Range(0, hurtSFX.Length)]);

    //                // Do the "hurt screen" effect:
    //                if (isPlayerOurs)
    //                {
    //                    gm.ui.Hurt();
    //                }
    //                lastDamageDealer = gm.GetPlayerInstance(fromPlayer);
    //            }
    //        }
    //    }
    //    [PunRPC]
    //    void TriggerDeadZone(Vector2 position)
    //    {
    //        movementController.position = position;
    //        networkPos = position;
    //        DeadZoned();
    //    }
    //    // Called by the owner client of this player:
    //    [PunRPC]
    //    public void Shoot(Vector3 curMousePos, Vector2 curPlayerPos, Vector2 curVelocity)
    //    {
    //        // Set updated position and aim directly so everything's synced up on shoot:
    //        mousePos = curMousePos;
    //        nMousePos = curMousePos;
    //        if (movementController) movementController.position = curPlayerPos;
    //        networkPos = curPlayerPos;
    //        movementController.velocity = curVelocity;
    //        // ...then the shooting itself:
    //        if (curWeapon != null)
    //        {
    //            curWeapon.Shoot();
    //        }
    //    }
    //    [PunRPC]
    //    public void ThrowGrenade()
    //    {
    //        // Sound:
    //        //aus.PlayOneShot(throwGrenadeSFX);

    //        //// Grenade spawning:
    //        //if (photonView.IsMine)
    //        //{
    //        //    Vector2 p1 = new Vector2(grenadePoint.position.x, grenadePoint.position.y);
    //        //    Vector2 p2 = new Vector2(weaponHandler.position.x, weaponHandler.position.y);
    //        //    object[] data = new object[] { (p1 - p2) * grenadeThrowForce, playerInstance.playerID }; // the instantiation data of a grenade includes the direction of the throw and the owner's player ID 

    //        //    PhotonNetwork.Instantiate(grenadePrefab, grenadePoint.position, Quaternion.identity, 0, data);
    //        //}
    //    }
    //    [PunRPC]
    //    public void MeleeAttack()
    //    {
    //        //meleeWeapon.Attack(photonView.IsMine, this);
    //    }
    //    [PunRPC]
    //    public void GrabWeapon(int id, int getFromSpawnPoint)
    //    {
    //        // Find the weapon in spawnable weapons of the current map:
    //        Weapon theWeapon = getFromSpawnPoint != -1 ? gm.maps[gm.chosenMap].weaponSpawnPoints[getFromSpawnPoint].onlySpawnThisHere : gm.maps[gm.chosenMap].spawnableWeapons[id];

    //        // Disarm current item first (if we have one):
    //        DisarmItem();

    //        originalWeapon = theWeapon;
    //        // ...then instantiate one based on the new item:
    //        curWeapon = Instantiate(theWeapon, weaponHandler);
    //        curWeapon.owner = this;
    //        // Also, let's save the weapon ID:
    //        lastWeaponId = getFromSpawnPoint != -1 ? System.Array.IndexOf(gm.maps[gm.chosenMap].spawnableWeapons, gm.maps[gm.chosenMap].weaponSpawnPoints[getFromSpawnPoint].onlySpawnThisHere) : id;
    //    }
    //    [PunRPC]
    //    public void ReceivePowerUp(int id, int getFromSpawnPoint)
    //    {
    //        // Find the power-up in spawnable power-ups of the current map:
    //        PowerUp thePowerUp = getFromSpawnPoint != -1 ? gm.maps[gm.chosenMap].powerUpSpawnPoints[getFromSpawnPoint].onlySpawnThisHere : gm.maps[gm.chosenMap].spawnablePowerUps[id];

    //        // ...then do the power-up's effects:
    //        // HEALTH:
    //        if (thePowerUp.fullRefillHealth)
    //        {
    //            health = character.data.maxHealth;
    //        }
    //        else
    //        {
    //            health += thePowerUp.addedHealth;
    //            health = Mathf.Clamp(health, 0, character.data.maxHealth);
    //        }
    //        // SHIELD:
    //        if (thePowerUp.fullRefillShield)
    //        {
    //            shield = maxShield;
    //        }
    //        else
    //        {
    //            shield += thePowerUp.addedShield;
    //            shield = Mathf.Clamp(shield, 0, maxShield);
    //        }

    //        // ADD GRENADE:
    //        curGrenadeCount += thePowerUp.addedGrenade;
    //        // AMMO REFILL:
    //        if (curWeapon && thePowerUp.fullRefillAmmo) curWeapon.curAmmo = curWeapon.ammo;

    //        // Update others about our current vital stats (health and shield):
    //        photonView.RPC("UpdateOthers", RpcTarget.Others, health, shield);
    //    }

    //    [PunRPC]
    //    public void Emote(int emote)
    //    {
    //        if (curEmote && curEmote.isReady)
    //        {
    //            curEmote.Show(emote);
    //        }
    //    }
    //    // *************************************************

    //    [PunRPC]
    //    public void UpdateOthers(int curHealth, int curShield)
    //    {
    //        health = curHealth;
    //        shield = curShield;
    //    }

    //    void DisarmItem()
    //    {
    //        if (curWeapon)
    //        {
    //            //Pickup p = Instantiate (pickupPrefab, transform.up * 2, Quaternion.identity);
    //            //p.itemHandled = originalItem;
    //            Destroy(curWeapon.gameObject);
    //        }
    //    }

    //    void OnDestroy()
    //    {
    //        if (!forPreview) gm.playerControllers.RemoveAll(p => p == null);
    //    }
    //    // *****************************************************

    //    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    //    {
    //        if (forPreview) return;

    //        if (stream.IsWriting && photonView.IsMine)
    //        {
    //            // Send controls over network
    //            stream.SendNext(movementController ? movementController.position : new Vector2());  // position
    //            stream.SendNext(movementController ? movementController.velocity : new Vector2());  // velocity
    //            stream.SendNext((Vector2)mousePos);                                                 // send a Vector2 version of the mouse position because we don't need the Z axis
    //            stream.SendNext(moving);
    //            stream.SendNext(isFalling);
    //            stream.SendNext(xInput);
    //        }
    //        else if (stream.IsReading)
    //        {

    //            // Receive controls
    //            networkPos = (Vector2)(stream.ReceiveNext());
    //            movementController.velocity = (Vector2)(stream.ReceiveNext());
    //            nMousePos = (Vector2)(stream.ReceiveNext());
    //            moving = (bool)(stream.ReceiveNext());
    //            isFalling = (bool)(stream.ReceiveNext());
    //            xInput = (float)(stream.ReceiveNext());

    //            // Calculate lag:
    //            lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
    //            //networkPos += Vector2.one * lag;
    //            // If is still moving, do predict next location based on current velocity and lag:
    //            //if (Helper.GetDistance(lastPos, networkPos) > 0.2f)
    //            //{
    //            //networkPos += (movementController.velocity * lag);
    //            //}
    //            lastPos = networkPos;

    //            // If network position is just too far, force to update local position:
    //            //if (Helper.GetDistance(networkPos, transform.position) > 0.2f){
    //            //    movementController.position = networkPos;
    //            //}
    //        }
    //    }
    //}
}