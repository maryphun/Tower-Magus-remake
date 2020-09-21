using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    private List<Transform> playerCharacters = new List<Transform>();
    private static Transform playerHolder; 

    public Transform PlayerHolder
    {
        get
        {
            if (playerHolder != null) { return playerHolder; }
            return playerHolder = GameObject.Find("PlayerHolder").transform;
        }
    }

    public void RegisterNewPlayer(Transform player)
    {
        ///playerCharacters.Add(player);
        //player.GetComponent<PlayerIdentity>().AssignPlayerIndex(index);
        player.SetParent(PlayerHolder);
        //Initialize();
    }

    /// <summary>
    /// Need to take care of bugs after networking
    /// </summary>
    /// <param name="playerNum"></param>
    public void UnregisterPlayer(int index)
    {
        
    }

    public Transform GetPlayerTransform(int playerNum)
    {
        //Debug.Log("looking for player " + playerNum.ToString());
        /////LOWEFFECIENT
        //foreach (Transform player in playerCharacters)
        //{
        //    if (player.GetComponent<Controller>().playerNumber == playerNum)
        //    {
        //        Debug.Log("found for player " + playerNum.ToString());
        //        return player;
        //    }
        //}

        //Debug.Log("player " + playerNum.ToString() + "not found");
        //return null;

        return playerCharacters[playerNum - 1];
    }
    
    /// <summary>
    /// return the player number of collided player, return -1 if not collided
    /// The value of point send in this function should be mathematically predicted for the intrapolation for its position X before pass in if it's not the owner
    /// </summary>
    /// <param name="point"></param>
    /// <param name="ignorePlayer"></param>
    /// <returns></returns>
    public Transform CollisionCheckBetweenPlayers(Vector2 point, Transform ignorePlayer)
    {
        int rtn = 0;
        
        foreach (Transform player in playerCharacters)
        {
            if (player != null)
            {
                // check if this player should be ignored for collision check
                if (player != ignorePlayer)
                {
                    Vector2 targetPlayerPos = new Vector2(PredictRealPositionX(player.GetComponent<PlayerManagerPhoton>()), player.position.y);
                    Vector2 targetPlayerSize = player.GetComponent<Collider2D>().bounds.extents;

                    // Collided in the predicted space
                    Rect rect = new Rect(targetPlayerPos.x - targetPlayerSize.x, targetPlayerPos.y - targetPlayerSize.y, targetPlayerSize.x * 2, targetPlayerSize.y * 2);
                    Debug.DrawLine(targetPlayerPos, player.position, Color.red, 0.15f);
                    if (rect.Contains(point))
                    {
                        Debug.Log("collide detection");
                        return player;
                    }

                    //if (IsPointInBox(point, targetPlayerPos.x - targetPlayerBoundsSize.x, targetPlayerPos.x + targetPlayerBoundsSize.x,
                    //    targetPlayerPos.y - targetPlayerBoundsSize.y, targetPlayerPos.y + targetPlayerBoundsSize.y))
                    //{
                    //    // collided
                    //    Debug.Log("collide detection");
                    //    return player;
                    //}
                }
            }
        }

        return null;
    }

    public void Initialize()
    {
        playerCharacters.Clear();
        //Debug.Log("initialize playercharacters: (" + playerCharacters.Count + ")");
        //Debug.Log("-------------------------------------------------------------------------------------------------------");
        foreach (Transform child in playerHolder)
        {
            if (child.gameObject.tag == "Player" && child != null)
            {
                playerCharacters.Add(child);
                //Debug.Log(child.gameObject.name + " - index: " + child.GetComponent<PlayerIdentity>().playerIndex);
            }
        }
        //Debug.Log("-------------------------------------------------------------------------------------------------------");
        Debug.Log("count: (" + playerCharacters.Count + ")");
    }

    public float PredictRealPositionX(PlayerManagerPhoton player)
    {
        float result = 0f;

        if (player.IsOwnedByThisPC())
        {
            result = player.transform.position.x;   //doesn't need to predict anything because if this is the player, it's the real position.
        }
        else
        {
            result = player.GetNewPosX() + (player.GetNewPosX() - player.transform.position.x);
        }

        return result;
    }

    public bool IsPointInBox(Vector2 point, float boxleft, float boxRight, float boxBottom, float boxTop)
    {
        if (point.x >= boxleft &&
            point.x <= boxRight &&
            point.y <= boxBottom &&
            point.y >= boxTop)
        {
            return true;
        }
        return false;
    }
}