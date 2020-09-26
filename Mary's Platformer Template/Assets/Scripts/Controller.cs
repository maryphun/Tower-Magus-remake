using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D)),
    RequireComponent(typeof(PlayerRenderer)), RequireComponent(typeof(PlayerControl)), RequireComponent(typeof(PhotonView))]
public class Controller : MonoBehaviourPun
{
    [Header("Configiuration")]
    [SerializeField] private float moveSpeed = 2f, jumpInitiateVelocity = 2f, tailSpawnInterval = 0.05f, tailSpawnVelocity = 5.0f, velocityLimit = 30f, shootInterval = 0.5f;
    [SerializeField] private float jumpPressedRememberTime = 0.2f, groundedRememberTime = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Color capeColor;
    [SerializeField] private PlayerManagerPhoton playerManagerPhoton = null;
    [SerializeField] private GameObject fireballPrefab = null;

    [Header("Debug")]
    [SerializeField] private bool ControlOn = true;

    private PlayerControl playerCtrl = null;
    private Rigidbody2D rigidbody;
    private Collider2D collider;
    private PlayerRenderer playerRenderer;
    private PlayerIdentity playerIdentity;  // replacement of singleton playermanager
    private float jumpPressedRemember, groundedRemember, tailTimeCount, shootIntervalCount;
    private Vector2 newPos;
    private bool StillJumping; // remain true if the jump key still not released
    private bool isShooting;

    private float previousInput;
    private bool previousGrounded;  // check if this character was grounded last frame


    private PlayerControl PlayerCtrl
    {
        get
        {
            if (playerCtrl != null) { return playerCtrl; }
            return playerCtrl = new PlayerControl();
        }
    }

    public void Awake()
    {
        // only enable this control when it get authority
        enabled = true;

        // Initialization
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        playerRenderer = GetComponentInChildren<PlayerRenderer>();
        playerIdentity = GetComponent<PlayerIdentity>();
        playerManagerPhoton = GetComponent<PlayerManagerPhoton>();
        jumpPressedRemember = 0f;
        StillJumping = false;

        // configure character color
        playerRenderer.SetColor(capeColor);

        // Only register the control to owner
        if (photonView.IsMine && PhotonNetwork.IsConnected == true)
        {
            // register Jump
            PlayerCtrl.Default.Jump.performed += _ => JumpPress();
            PlayerCtrl.Default.Jump.canceled += _ => JumpRelease();

            // register movement
            PlayerCtrl.Default.Move.performed += ctx => SetMovement(ctx.ReadValue<float>());
            PlayerCtrl.Default.Move.canceled += ctx => ResetMovement();
            
            // register Shoot
            PlayerCtrl.Default.Shoot.performed += _ => Shoot(true);
            PlayerCtrl.Default.Shoot.canceled += _ => Shoot(false);

            if (!ControlOn)
            {
                PlayerCtrl.Disable();
            }
        }
        else
        {
            // other player can't control this as they don't own this character
            PlayerCtrl.Disable();
        }
    }

    private void OnEnable()
    {
        // ControlOn is used for debugging. photonView.IsMine use to determine if this character belong to the player
        if (ControlOn && photonView.IsMine && PhotonNetwork.IsConnected == true)
        {
            PlayerCtrl.Enable();
        }
    }

    private void OnDisable()
    {
        PlayerCtrl.Disable();
    }

    #region Color 
    public Color GetCapeColor()
    {
        return capeColor;
    }

    public void SetCapeColor(Color newColor)
    {
        capeColor = newColor;
        playerRenderer.SetColor(capeColor);
    }

    [PunRPC]
    public void RPCSetCapeColor(float newColorR, float newColorG, float newColorB)
    {
        var colorTmp = new Color(newColorR, newColorG, newColorB, 1.0f);
        capeColor = colorTmp;
        playerRenderer.SetColor(capeColor);
    }
    #endregion

    private void SetMovement(float movement) => previousInput = movement;
    private void ResetMovement() => previousInput = 0f;

    // Update is called once per frame
    void FixedUpdate()
    {
        // Player visual that doesn't care if player own this character or not.
        Visuals();

        // Remember time. Even client side need to know if this player is grounded or not.
        jumpPressedRemember = Mathf.Clamp(jumpPressedRemember - Time.deltaTime, 0.0f, jumpPressedRememberTime);
        groundedRemember = Mathf.Clamp(groundedRemember - Time.deltaTime, 0.0f, groundedRememberTime);
        shootIntervalCount = Mathf.Clamp(shootIntervalCount - Time.deltaTime, 0.0f, shootInterval);

        // Check if player is grounded
        GroundedRemember();

        // Shoot Bullet
        if (isShooting) ShootBullet(playerRenderer.FlipSide());

        rigidbody.velocity = new Vector2(rigidbody.velocity.x, Mathf.Clamp(rigidbody.velocity.y, -velocityLimit, velocityLimit));

        if (!photonView.IsMine && PhotonNetwork.IsConnected == true) { return; } // only called for who belong to it

        // Read the input value
        float movementInput = previousInput;
        // calculate new position
        newPos = transform.position;
        newPos.x += movementInput * moveSpeed * Time.deltaTime;
        // Apply new position
        Move(newPos, movementInput);

        // Key Remembered
        if (jumpPressedRemember > 0.0f)
        {
            if (IsGrounded())
            {
                if (!PhotonNetwork.OfflineMode)
                {
                    photonView.RPC("Jump", RpcTarget.All);
                }
                else
                {
                    Jump();
                }
            }
        }
    }
    
    private void JumpPress()
    {
        jumpPressedRemember = jumpPressedRememberTime;
    }

    private void JumpRelease()
    {
        if (!PhotonNetwork.OfflineMode)
        {
            photonView.RPC("JumpReleaseRPC", RpcTarget.All);
        }
        else
        {
            StillJumping = false;
            if (rigidbody.velocity.y > 0.0f)
            {
                rigidbody.velocity = new Vector2(rigidbody.velocity.x, rigidbody.velocity.y / 2);
            }
        }
    }

    [PunRPC]
    private void JumpReleaseRPC()
    {
        StillJumping = false;
        if (rigidbody.velocity.y > 0.0f)
        {
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, rigidbody.velocity.y / 2);
        }
    }

    [PunRPC]
    private void Jump()
    {
        rigidbody.velocity = new Vector2(rigidbody.velocity.x, 0.0f);
        rigidbody.AddForce(new Vector2(0, jumpInitiateVelocity), ForceMode2D.Impulse);
        jumpPressedRemember = 0.0f;
        groundedRemember = 0.0f;
        StillJumping = true;
        playerRenderer.EnableTrail(true);
        playerRenderer.JumpDustPlay();
    }

    /// <summary>
    /// called when velocity > 0
    /// </summary>
    private void JumpUpward()
    {
        // ----- Check Collision of head to wall -----
        Vector2 topLeftPoint = new Vector2(PlayerManager.Instance().PredictRealPositionX(playerManagerPhoton) - collider.bounds.extents.x, transform.position.y + collider.bounds.extents.y);
        Vector2 topRightPoint = new Vector2(PlayerManager.Instance().PredictRealPositionX(playerManagerPhoton) + collider.bounds.extents.x, transform.position.y + collider.bounds.extents.y);

        if (CollisionCheck(topLeftPoint, groundMask) || CollisionCheck(topRightPoint, groundMask))
        {
            // head collided while jumping
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, (rigidbody.velocity.y * -1f) * 0.55f);
        }

        // ----- Check Collision of head to player -----
        Transform[] collide = { null, null };

        collide[0] = PlayerManager.Instance().CollisionCheckBetweenPlayers(topLeftPoint, transform);
        collide[1] = PlayerManager.Instance().CollisionCheckBetweenPlayers(topRightPoint, transform);

        if ((collide[0] != null)
            || (collide[1] != null))   // null mean not collided or it will return the collided player
        {
            // get the correct player
            Transform collidedPlayer;
            collidedPlayer = collide[0] != null ? collide[0] : collide[1];

            //calculate new velocity
            float newVelocity = (rigidbody.velocity.y * -1f);// * 0.55f;
            if (collidedPlayer != null)
            {
                collidedPlayer.GetComponent<Controller>().photonView.RPC("Pushed", RpcTarget.All, rigidbody.velocity.y * 2f, transform.position.y + collider.bounds.extents.y + collidedPlayer.GetComponent<Collider2D>().bounds.extents.y + 0.01f);
            }
            else
            {
                Debug.LogError("Null Warning, collided player not found in the character list.");
            }

            // player collision
            StillJumping = false;
            //photonView.RPC("Stepped", RpcTarget.All, newVelocity);
            Stepped(newVelocity);
        }
    }

    /// <summary>
    /// called when velocity < 0
    /// </summary>
    private void JumpDownward()
    {
        // Check Collision of Leg if it colliding with another player
        Vector2 bottomLeftPoint = new Vector2(PlayerManager.Instance().PredictRealPositionX(playerManagerPhoton) - collider.bounds.extents.x, transform.position.y - collider.bounds.extents.y);
        Vector2 bottomRightPoint = new Vector2(PlayerManager.Instance().PredictRealPositionX(playerManagerPhoton) + collider.bounds.extents.x, transform.position.y - collider.bounds.extents.y);

        Transform[] collide = { null, null };

        collide[0] = PlayerManager.Instance().CollisionCheckBetweenPlayers(bottomLeftPoint, transform);
        collide[1] = PlayerManager.Instance().CollisionCheckBetweenPlayers(bottomRightPoint, transform);

        if ((collide[0] != null)
            || (collide[1] != null))   // null mean not collided or it will return the collided player
        {
            // get the correct player
            Transform collidedPlayer;
            collidedPlayer = collide[0] != null ? collide[0] : collide[1];

            //calculate new velocity
            float newVelocity = jumpInitiateVelocity * 0.35f;
            if (collidedPlayer != null)
            {
                // take collided's player velocity into calculation
                Rigidbody2D targetRigid = collidedPlayer.GetComponent<Rigidbody2D>();
                newVelocity += targetRigid.velocity.y * 2.0f;

                // step on it
                collidedPlayer.GetComponent<Controller>().photonView.RPC("Stepped", RpcTarget.All, rigidbody.velocity.y);

                // pull this player upward so it wont be colliding anymore, and assign new velocity on it
                //photonView.RPC("Pushed", RpcTarget.All, newVelocity, collidedPlayer.position.y + collidedPlayer.GetComponent<Collider2D>().bounds.extents.y + collider.bounds.extents.y + 0.01f);
                Pushed(newVelocity, collidedPlayer.position.y + collidedPlayer.GetComponent<Collider2D>().bounds.extents.y + collider.bounds.extents.y + 0.01f);
            }
            else
            {
                Debug.LogError("Null Warning, collided player not found in the character list. ");
            }
            
            // this player can't jump higher
            StillJumping = false;
        }
    }
    

    /// <summary>
    /// determine if the player is on ground. have some delay time default 0.2f
    /// </summary>
    /// <returns></returns>
    public bool IsGrounded()
    {
        return (groundedRemember > 0.0f);
    }

    private void GroundedRemember()
    {
        // Code Inside here only run locally
        bool groundedtmp = (rigidbody.velocity.y == 0.0f);

        playerRenderer.IsGroundedAnimParameter(groundedtmp);

        if (groundedtmp)
        {
            if (groundedtmp != previousGrounded)    // the inside of this if statement is the exact frame where player reach the ground.
            {
                // Reach the ground, call RPC here to synchronize
                if (!PhotonNetwork.OfflineMode)
                {
                    photonView.RPC("ReachGroundRPC", RpcTarget.All, transform.position.y);
                }
                else
                {
                    StillJumping = false;
                    playerRenderer.EnableTrail(false);
                }
            }
            groundedRemember = groundedRememberTime;
            playerRenderer.IsGroundedAnimParameter(true);
        }
        else
        {
            // On The Air
            if (rigidbody.velocity.y > 0.0f)
            {
                // Upward
                JumpUpward();
            }
            else
            {
                // Downward
                JumpDownward();
            }
        }

        previousGrounded = groundedtmp;
    }

    [PunRPC]    //should be called when player reach the ground. Called on RPC to make sure they fall into the same ground.
    private void ReachGroundRPC(float newY)
    {
        StillJumping = false;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
        playerRenderer.EnableTrail(false);

        // Synchronize position
        playerManagerPhoton.SetNewY(newY);
    }

    private void Move(Vector2 newPosition, float direction)
    {
        // change flip side (commented out because it's already done in the GameManagerPhoton script)
        //playerRenderer.FlipSide(direction); 

        // collision check before actually move the player
        Vector2 destinationTop = new Vector2(newPosition.x + (direction * collider.bounds.extents.x), transform.position.y + collider.bounds.extents.y - 0.05f);
        Vector2 destinationMid = new Vector2(newPosition.x + (direction * collider.bounds.extents.x), transform.position.y);
        Vector2 destinationBottom = new Vector2(newPosition.x + (direction * collider.bounds.extents.x), transform.position.y - collider.bounds.extents.y + 0.05f);

        // assign new position to the character 
        if (!CollisionCheck(destinationTop, groundMask)     // collision with wall
            && !CollisionCheck(destinationBottom, groundMask)
            && !CollisionCheck(destinationMid, groundMask)                      
            &&
            (PlayerManager.Instance().CollisionCheckBetweenPlayers(destinationTop, transform) == null &&    //collision with players
             PlayerManager.Instance().CollisionCheckBetweenPlayers(destinationBottom, transform) == null &&
             PlayerManager.Instance().CollisionCheckBetweenPlayers(destinationMid, transform) == null))
        {
            transform.position = newPosition;
        }
    }

    private void Shoot(bool boolean)
    {
        playerRenderer.ShootAnimation(boolean);
        isShooting = boolean;
    }

    /// <summary>
    /// Add RPC in here
    /// </summary>
    /// <param name="flip"></param>
    private void ShootBullet(bool flip)
    {
        float direction = flip ? -1f : 1f;
        BulletInitialize(direction, playerRenderer.GetColor());
    }

    private void BulletInitialize(float dir, Color col)
    {
        if (shootIntervalCount <= 0.0f)
        {
            Vector2 spawnPos = new Vector2(transform.position.x + (dir * 0.8f), transform.position.y + 0.1f);

            var tmp = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
            tmp.GetComponent<FireballScript>().Initialize(new Vector2(dir, 0f), col);
            shootIntervalCount = shootInterval;
        }
    }

    private bool CollisionCheck(Vector2 point, LayerMask layerMask)
    {
        return (Physics2D.OverlapPoint(point, layerMask));
    }

    /// <summary>
    /// call this when this player got stepped on its head
    /// </summary>
    [PunRPC]
    public void Stepped(float forceVelocity)
    {
        //make sure it's negativve
        float calVelocity = Mathf.Abs(forceVelocity) * -1f;

        //only change its velocity if player is on the air
        if (rigidbody.velocity.y != 0.0f)
        {
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, calVelocity * 1.5f);
        }
    }

    /// <summary>
    /// call this to push player upward
    /// </summary>
    [PunRPC]
    public void Pushed(float forceVelocity, float newY)
    {
        //assign new position to this character so they won't be colliding anymore


        //make sure it's positive
        float calVelocity = Mathf.Abs(forceVelocity);

        rigidbody.velocity = new Vector2(rigidbody.velocity.x, calVelocity);
    }
    
    #region PlayerManagerPhoton call
    // Calling from PlayerManagerPhotonScript to get input data and synchronize the result with other player 
    public float GetInputData()
    {
        return previousInput;
    }

    // Calling from PlayerManagerPhotonScript to get reference of player renderer to do visual synchronization
    public PlayerRenderer GetPlayerRenderer()
    {
        return playerRenderer;
    }
    #endregion

    #region Visuals
    // Code in here isn't synchronized between players. That mean no important logic that would affect the gameplay should be here.
    private void Visuals()
    {
        // Counter
        tailTimeCount = Mathf.Clamp(tailTimeCount - Time.deltaTime, 0.0f, tailSpawnInterval);
        
        // Draw aferimage
        if (tailTimeCount == 0.0f && rigidbody.velocity.y > tailSpawnVelocity)
        {
            tailTimeCount = tailSpawnInterval; //reset timer
            playerRenderer.CreateAfterImage();
        }
    }
    #endregion
}
