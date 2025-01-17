using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Visyde;

public class FusionPlayerController : NetworkBehaviour
{
    //public bool hasDoorKey;
    //public GameObject keyObj;
    //public bool forPreview = false;                                 // used for non in-game such as character customization preview in the main menu

    //[System.Serializable]
    //public class Character
    //{
    //    public CharacterData data;
    //    public Animator animator;

    //    // For cosmetics:
    //    public Transform hatPoint;
    //}
    //public Character[] characters;                                  // list of characters for the spawnable characters (modifying this will not change the main menu character selection screen

    //[Header("References:")]                                    // the AI controller for this player (only gets enabled if this is a bot, disabled when not)
    //public AudioSource aus;                                         // the AudioSource that will play the player sounds
    //public GameObject spawnVFX;                                     // the effect that's shown on spawn
    //public EmotePopup emotePopupPrefab;                             // emote prefab to spawn
    //public int actorNumber;
    //public GameObject talkingIndicator;

    //// In-Game:
    //public PlayerInstance playerInstance;
    //public int curCharacterID { get; protected set; }               // determines which character is used for this player
    //public bool isDead { get; protected set; }                      // is this player dead?
    //public Vector3 mousePos { get; protected set; }                 // the mouse position we're working on locally
    //public EmotePopup curEmote { get; protected set; }              // this player's own emote popup
    //[HideInInspector] public bool isOnJumpPad;                      // when true, jumping is disabled to not interfere with the jump pad
    //[HideInInspector] public Vector3 nMousePos;                     // mouse position from network. We're gonna smoothly interpolate the mousePos' value to this one to prevent the jittering effect.
    //[HideInInspector] public float xInput;                          // the X input for the movement controls (sent to other clients for animation speed control)
    //float jumpProgress;                                             // longer press means higher jump
    //bool moving;                                                    // are we moving on ground?
    //bool isFalling;                                                 // are we falling? (can be used for something like a falling animation)
    //bool lastFrameGrounded;                                         // used for spawning landing vfx
    //bool doneDeadZone;                                              // makes sure that DeadZoned() doesn't called repeatedly
    //float lastGroundedTime;
    //Vector3 lastAimPos;                                             // used for mobile controls
    //GameManager gm;                                                 // GameManger instance reference for simplicity
    //Rigidbody2D rg;

    //// Network:
    //[HideInInspector] public Vector2 lastPos, networkPos;
    //float lag;

    //// Returns the chosen character:
    //public Character character
    //{
    //    get
    //    {
    //        return characters[curCharacterID];
    //    }
    //}
    //// Check if this player is ours and not owned by a bot or another player:
    //public bool isPlayerOurs
    //{
    //    get
    //    {
    //        return !playerInstance.isBot && playerInstance.isMine;
    //    }
    //}
    //void Awake()
    //{
    //    // Find essential references:
    //    gm = GameManager.instance;
    //}
    //public void OnEnable()
    //{
    //    if (gm)
    //    {
    //        // Add this to the player controllers list:
    //        gm.playerControllers.Add(this);
    //    }
    //}
    //public void OnDisable()
    //{
    //    if (gm)
    //    {
    //        // Unsubscibe to Controls Manager events (doesn't do anything if player isn't ours):
    //        gm.controlsManager.jump -= Jump;

    //        // Remove from the player controllers list
    //        gm.playerControllers.Remove(this);
    //    }
    //}
    //void Start()
    //{
    //    if (forPreview) return;

    //    // Spawn VFX:
    //    Instantiate(spawnVFX, transform);

    //    // Reset player stats and stuff:
    //    RestartPlayer();

    //    // Spawn our own emote popup:
    //    curEmote = Instantiate(emotePopupPrefab, Vector3.zero, Quaternion.identity);
    //    curEmote.owner = this;

    //    jumpForce = character.data.jumpForce;
    //}
    //[Header("Settings:")]
    //public float groundCheckerRadius;
    //public Vector2 groundCheckerOffset;
    //public bool allowJump { get; set; }
    //bool isGrounded = false;
    //public override void FixedUpdateNetwork()
    //{
    //    if (forPreview) return;

    //    Transform t = transform;

    //    if (!isDead)
    //    {
    //        // Check if we're currently falling:
    //        isFalling = rg.velocity.y < 0;

    //        if (Object.HasInputAuthority || playerInstance.isMine)
    //        {
    //            // Dead zone interaction:
    //            if (gm.deadZone)
    //            {
    //                if (t.position.y < gm.deadZoneOffset && !doneDeadZone)
    //                {
    //                    //photonView.RPC("TriggerDeadZone", RpcTarget.All, movementController.position);
    //                    doneDeadZone = true;
    //                }
    //            }
    //            // *For our player:
    //            if (isPlayerOurs)
    //            {
    //                HandleInputs();
    //            }
    //            // *For the bots:
    //            else
    //            {
    //                if (!gm.isGameOver)
    //                {
    //                    // Smooth mouse aim sync for the bot:
    //                    mousePos = nMousePos;
    //                }
    //            }
    //        }
    //        else
    //        {
    //            // Smooth mouse aim sync:
    //            mousePos = Vector3.MoveTowards(mousePos, nMousePos, Time.deltaTime * (mousePos - nMousePos).magnitude * 15);
    //        }

    //        // Landing VFX:
    //        if (isGrounded)
    //        {
    //            if (!lastFrameGrounded && (Time.time - lastGroundedTime) > 0.1f)
    //            {
    //                Land();
    //            }
    //            lastFrameGrounded = isGrounded;
    //            lastGroundedTime = Time.time;
    //        }
    //        else
    //        {
    //            lastFrameGrounded = isGrounded;
    //        }

    //        // Flipping:
    //        t.localScale = new Vector3(mousePos.x > t.position.x ? 1 : mousePos.x < t.position.x ? -1 : t.localScale.x, 1, 1);
    //    }
    //    allowJump = true;
    //    isGrounded = false;
    //    Collider2D[] cols = Physics2D.OverlapCircleAll(groundCheckerOffset + new Vector2(transform.position.x, transform.position.y), groundCheckerRadius);
    //    for (int i = 0; i < cols.Length; i++)
    //    {
    //        if (cols[i].CompareTag("JumpPad"))
    //        {
    //            allowJump = false;
    //        }
    //        if (cols[i].gameObject != gameObject && !cols[i].isTrigger && !cols[i].CompareTag("Portal"))
    //        {
    //            if (!isGrounded) isGrounded = true;
    //        }
    //    }

    //    // Animations:
    //    if (character.animator)
    //    {
    //        character.animator.SetBool("Moving", moving);
    //        character.animator.SetBool("Dead", isDead);
    //        character.animator.SetBool("Falling", isFalling);

    //        // Set the animator speed based on the current movement speed (only applies to grounded moving animations such as running):
    //        character.animator.speed = moving && isGrounded ? Mathf.Abs(xInput) : 1;
    //    }
    //}
    //Vector2 vel = Vector2.zero;
    //void FixedUpdate()
    //{
    //    if (forPreview) return;

    //    // Simple lag compensation (movement interpolation + movement controller velocity set at OnPhotonSerializeView()):
    //    if (!Object.HasInputAuthority /*&& movementController*/)
    //    {
    //        rg.position = Vector2.SmoothDamp(rg.position, networkPos, ref vel, 0.3f);
    //    }
    //}

    ////public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    ////{
    ////    if (forPreview) return;

    ////    if (stream.IsWriting && photonView.IsMine)
    ////    {
    ////        // Send controls over network
    ////        stream.SendNext(movementController ? movementController.position : new Vector2());  // position
    ////        stream.SendNext(movementController ? movementController.velocity : new Vector2());  // velocity
    ////        stream.SendNext((Vector2)mousePos);                                                 // send a Vector2 version of the mouse position because we don't need the Z axis
    ////        stream.SendNext(moving);
    ////        stream.SendNext(isFalling);
    ////        stream.SendNext(xInput);
    ////    }
    ////    else if (stream.IsReading)
    ////    {

    ////        // Receive controls
    ////        networkPos = (Vector2)(stream.ReceiveNext());
    ////        movementController.velocity = (Vector2)(stream.ReceiveNext());
    ////        nMousePos = (Vector2)(stream.ReceiveNext());
    ////        moving = (bool)(stream.ReceiveNext());
    ////        isFalling = (bool)(stream.ReceiveNext());
    ////        xInput = (float)(stream.ReceiveNext());

    ////        // Calculate lag:
    ////        lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
    ////        //networkPos += Vector2.one * lag;
    ////        // If is still moving, do predict next location based on current velocity and lag:
    ////        //if (Helper.GetDistance(lastPos, networkPos) > 0.2f)
    ////        //{
    ////        //networkPos += (movementController.velocity * lag);
    ////        //}
    ////        lastPos = networkPos;

    ////        // If network position is just too far, force to update local position:
    ////        //if (Helper.GetDistance(networkPos, transform.position) > 0.2f){
    ////        //    movementController.position = networkPos;
    ////        //}
    ////    }
    ////}


    //void HandleInputs()
    //{
    //    // Is moving on ground?:
    //    moving = rg.velocity.x != 0 && isGrounded && xInput != 0;

    //    // Only allow controls if the menu is not shown (the menu when you press 'ESC' on PC):
    //    if (!gm.ui.isMenuShown)
    //    {
    //        // Example emote keys (this is just a hard-coded example of displaying an emote using alphanumeric keys
    //        //  so you may have to implement a more robust emote input system depending on your project's needs):
    //        if (Input.GetKeyDown(KeyCode.Alpha1))
    //        {
    //            //photonView.RPC("Emote", RpcTarget.All, 0);
    //        }
    //        if (Input.GetKeyDown(KeyCode.Alpha2))
    //        {
    //            //photonView.RPC("Emote", RpcTarget.All, 1);
    //        }
    //        if (Input.GetKeyDown(KeyCode.Alpha3))
    //        {
    //            //photonView.RPC("Emote", RpcTarget.All, 2);
    //        }
    //        // Player controls:
    //        if (gm.gameStarted && !gm.isGameOver)
    //        {
    //            xInput = gm.useMobileControls ? gm.controlsManager.horizontal : gm.controlsManager.horizontalRaw;
    //        }
    //        else
    //        {
    //            // Reset movement inputs when game is over:
    //            xInput = 0;
    //        }
    //    }
    //    else
    //    {
    //        xInput = 0;
    //    }
    //}

    ///// <Summary> 
    ///// Disable unnecessary components for main menu preview.
    ///// Should be called before the Start() function.
    /////</Summary>
    //public void SetAsPreview()
    //{
    //    if (forPreview)
    //    {
    //        forPreview = true;
    //        Destroy(rg);
    //        //Destroy(photonView);

    //        // Get the chosen character (locally):
    //        for (int i = 0; i < characters.Length; i++)
    //        {
    //            if (characters[i].data == DataCarrier.characters[DataCarrier.chosenCharacter])
    //            {
    //                curCharacterID = i;
    //            }
    //        }

    //        // Enable only the chosen character's graphics:
    //        for (int i = 0; i < characters.Length; i++)
    //        {
    //            characters[i].animator.gameObject.SetActive(i == curCharacterID);
    //        }
    //        return;
    //    }
    //}
    //public void SubscribeJump()
    //{
    //    // Subscibe to Controls Manager's jump event if player is ours:
    //    if (isPlayerOurs)
    //    {
    //        gm.controlsManager.jump += Jump;
    //    }
    //    else
    //    {
    //        gm.controlsManager.jump -= Jump;
    //    }
    //}
    //public void RestartPlayer()
    //{
    //    // Get the dedicated player instance for this player:
    //    if (GameManager.gameMode == Visyde.GameMode.Multiplayer)
    //    {
    //        playerInstance = gm.GetPlayerInstance(Object.Runner.LocalPlayer.PlayerId);
    //    }
    //    else
    //    {
    //        playerInstance = gm.GetPlayerInstance(actorNumber);
    //    }
    //    SubscribeJump();
    //    // Get the chosen character of this player (we only need the index of the chosen character in DataCarrier's characters array):
    //    int chosenCharacter = playerInstance.character;
    //    for (int i = 0; i < characters.Length; i++)
    //    {
    //        if (characters[i].data == DataCarrier.characters[chosenCharacter])
    //        {
    //            curCharacterID = i;
    //        }
    //    }

    //    // Enable only the chosen character's graphics:
    //    for (int i = 0; i < characters.Length; i++)
    //    {
    //        characters[i].animator.gameObject.SetActive(i == curCharacterID);
    //    }

    //}

    //public void Jump()
    //{
    //    if (!gm.gameStarted || gm.isGameOver) return;

    //    if (!isOnJumpPad && isGrounded && allowJump)
    //    {
    //        // Call jump in character controller:
    //        DoJump();

    //        if (character.data.jumpSFX.Length > 0)
    //        {
    //            aus.PlayOneShot(character.data.jumpSFX[Random.Range(0, character.data.jumpSFX.Length)]);
    //        }
    //    }
    //}
    //[HideInInspector] public float jumpForce;
    //private void DoJump()
    //{
    //    if (!rg) return;

    //    Vector2 veloc = rg.velocity;
    //    veloc.y = jumpForce;
    //    rg.velocity = veloc;

    //    // Don't allow jumping right after a jump:
    //    allowJump = false;
    //}

    //public void Land()
    //{
    //    gm.pooler.Spawn("LandDust", transform.position);
    //    // Sound:
    //    if (character.data.landingsSFX.Length > 0) aus.PlayOneShot(character.data.landingsSFX[Random.Range(0, character.data.landingsSFX.Length)]);
    //}

    //public void OwnerShootCommand()
    //{
    //    //photonView.RPC("Shoot", RpcTarget.All, mousePos, movementController.position, movementController.velocity);
    //}
    //// Called by the owner from mobile or pc input:
    //public void OwnerMeleeAttack()
    //{
    //    //if (curMeleeAttackRate >= 1)
    //    //{
    //    //    photonView.RPC("MeleeAttack", RpcTarget.All);
    //    //    curMeleeAttackRate = 0;
    //    //}
    //}
    //public void OwnerThrowGrenade()
    //{
    //    //if (curGrenadeCount > 0)
    //    //{
    //    //    curGrenadeCount -= 1;
    //    //    photonView.RPC("ThrowGrenade", RpcTarget.All);
    //    //}
    //}

    //public void Die()
    //{
    //    if (!gm.isGameOver)
    //    {
    //        // and then destroy (give a time for the death animation):
    //        if (Object.HasInputAuthority)
    //        {
    //            Invoke("PhotonDestroy", 1f);
    //        }
    //        else if (playerInstance.isMine)
    //        {
    //            Destroy(gameObject);
    //        }
    //    }

    //    // Cancel any movement:
    //    Collider2D[] cols = GetComponentsInChildren<Collider2D>();
    //    for (int i = 0; i < cols.Length; i++)
    //    {
    //        cols[i].enabled = false;
    //    }
    //    // and remove the rigidbody:
    //    Destroy(rg);
    //}

    //// Instant death from dead zone:
    //public void DeadZoned()
    //{
    //    // VFX:
    //    if (gm.maps[gm.chosenMap].deadZoneVFX)
    //    {
    //        Instantiate(gm.maps[gm.chosenMap].deadZoneVFX, new Vector3(transform.position.x, gm.deadZoneOffset, 0), Quaternion.identity);
    //    }
    //}

    //void PhotonDestroy()
    //{
    //    //PhotonNetwork.Destroy(photonView);
    //}


    ///// <summary>
    ///// Deal damage to player.
    ///// </summary>
    ///// <param name="fromPlayer">Damage dealer player name.</param>
    ///// <param name="value">Can be either a weapon id (if a gun was used) or a damage value (if melee attack or grenade).</param>
    ///// <param name="gun">If set to <c>true</c>, "value" will be used as weapon id.</param>
    //public void ApplyDamage(int fromPlayer, int value, bool gun)
    //{
    //    //photonView.RPC("Hurt", RpcTarget.AllBuffered, fromPlayer, value, gun);
    //}
    ////[PunRPC]
    //void TriggerDeadZone(Vector2 position)
    //{
    //    //movementController.position = position;
    //    //networkPos = position;
    //    //DeadZoned();
    //}
    ////[PunRPC]
    //public void Emote(int emote)
    //{
    //    if (curEmote && curEmote.isReady)
    //    {
    //        curEmote.Show(emote);
    //    }
    //}

    //void OnDestroy()
    //{
    //    if (!forPreview) gm.playerControllers.RemoveAll(p => p == null);
    //}
}
