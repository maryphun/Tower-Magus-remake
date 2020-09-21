using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Controller))]
public class PlayerManagerPhoton : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Private Fields
    private Controller ctrl;  // the script reference where we should get our Input data from.
    
    [SerializeField] float smoothnessFactor;
    private float newX, oldX, newY, oldY;
    private float lerpY;
    private bool isGrounded;
    #endregion

    public bool offlineCharacter = false;

    #region IPunObservable implementation

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(transform.position.x);
            stream.SendNext(ctrl.GetPlayerRenderer().IsGroundedAnimParameter());
        }
        else
        {
            // Network player, receive data
            newX = (float)stream.ReceiveNext();
            isGrounded = (bool)stream.ReceiveNext();
        }
    }

    #endregion
    private void Awake()
    {
        ctrl = GetComponent<Controller>();
    }

    private void FixedUpdate()
    {
        // These should run globally

        //Sync the visual flipX
        ctrl.GetPlayerRenderer().FlipSide(transform.position.x - oldX);

        // determine the player animation base on the position X diffferent
        float differentX = Mathf.Abs(transform.position.x - oldX);
        bool isMoving = differentX >= 0.0025f;  // fomula to determine if the player is moving.
        ctrl.GetPlayerRenderer().MoveAnimation(isMoving);
        ctrl.GetPlayerRenderer().WalkDustEnable(isMoving && ctrl.IsGrounded());

        // store the old position
        oldX = transform.position.x;

        if (photonView.IsMine) { ProcessInputs(); }
        else
        {
            if (offlineCharacter) { return; }

            /// This Character doesn't own by this player
            // lerp from old posX to new posX with the updated position
            transform.position = new Vector2(Mathf.Lerp(transform.position.x, newX, smoothnessFactor * Time.deltaTime), transform.position.y);

            // synchronize the jumping animation, the owner doesn't need this because it will be done more precisely wihtin the playercontroller script
            ctrl.GetPlayerRenderer().IsGroundedAnimParameter(isGrounded);

            if (lerpY < 1.0f)
            {
                lerpY = Mathf.Clamp(lerpY + smoothnessFactor * Time.deltaTime, 0.0f, 1.0f);
                // lerp from old posY to new posY if it has conflict
                transform.position = new Vector2(transform.position.x, Mathf.Lerp(oldY, newY, lerpY));
            }
        }
    }

    #region Custom

    /// <summary>
    /// Processes the inputs
    /// </summary>
    void ProcessInputs()
    {
        // these only run for the owner of this character
        
        // player character animation should base on the input instead of different on position X.
        ctrl.GetPlayerRenderer().MoveAnimation(ctrl.GetInputData() != 0);
    }

    public void SetNewY(float value)
    {
        oldY = transform.position.y;
        newY = value;
        lerpY = 0.0f;
    }
    
    public bool IsOwnedByThisPC()
    {
        return photonView.IsMine;
    }

    public float GetOldPosX()
    {
        return oldX;
    }

    public float GetNewPosX()
    {
        return newX;
    }

    #endregion
}
