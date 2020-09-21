using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
///  Code in this class will be seperately running by each client doesn't matter it has the authority or not. 
///  That mean this class have nothing to do with the networking.
///  
///  Every line of code written here should be careful of conflict between clients
/// </summary>
public class PlayerIdentity : MonoBehaviourPun
{
    // Number that assigned to this character
    [SerializeField] public int playerIndex;

    private PhotonView pView;
    
    void Start()
    {
        pView = GetComponent<PhotonView>();
        if (pView == null)
        {
            Debug.LogError("photon view reference not found! in player identity");
        }

        PlayerManager.Instance().RegisterNewPlayer(transform);

        playerIndex = PhotonNetwork.LocalPlayer.ActorNumber;
        //pView.RPC("SyncId", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    //[PunRPC]
    //public void SyncId(int id)
    //{
    //    if (!pView.IsMine)
    //    {
    //        playerIndex = id;
    //    }
    //}
    
    //public void AssignPlayerIndex(int index)
    //{
    //    playerIndex = index;
    //}
}
