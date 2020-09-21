using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using DG.Tweening;

public class MainMenuPhoton : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform playerSpawnPoint = null;
    [SerializeField] private GameObject playerToSpawn = null, effectPrefab;
    [SerializeField] private GameObject nameInputPanel = null;
    [SerializeField] private GameObject landingPagePanel = null;
    [SerializeField] private PhotonRoomMenu hostRoomPanel = null;
    [SerializeField] private GameObject clientPanel = null;
    [SerializeField] private Button buttonSwitchColor = null;
    [SerializeField] private PlayerColor playerColorScript = null;
    [SerializeField] private TMP_Text waitingStatusTextHost = null;
    [SerializeField] private TMP_Text waitingStatusTextClient = null;
    [SerializeField] private float panelSwitchTime = 1.5f;
    [SerializeField] private Transform player = null;
    [SerializeField] private string generatedRoomName;
    [SerializeField] private string inputRoomName;
    [SerializeField] private Color textColorEmptySlot = Color.gray, textColorFilledSlot = Color.white, boxColorEmptySlot = Color.white, boxColorFilledSlot = Color.black;

    private List<Player> players;   // player that connected into the network

    private bool isConnecting = false;

    private const string gameVersion = "1";
    private const int MaxPlayerPerRoom = 4;

    private GameObject playerPrefab = null;

    private bool hosting = false;

    #region PhotonRelated
    private void Start()
    {
        // offline mode by default
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.AutomaticallySyncScene = true;
        playerPrefab = Resources.Load<GameObject>("Player") as GameObject;
        if (playerPrefab == null)
        {
            Debug.LogError("playerPrefab reference null");
        }
        //PhotonNetwork.Instantiate(playerPrefab.name, playerSpawnPoint.position, playerSpawnPoint.rotation);
        player = Instantiate(playerToSpawn, playerSpawnPoint.position, playerSpawnPoint.rotation).transform;
        player.gameObject.name = "offlinePlayer";
        player.GetComponent<PlayerManagerPhoton>().offlineCharacter = true;
        players = new List<Player>();

        // set up its color
        playerColorScript.SetUpColor();
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master!");
        
        if (isConnecting)
        {
            if (hosting)
            {
                landingPagePanel.SetActive(false);

                waitingStatusTextHost.text = "<color=black>Creating the room...</color>";
                CreateRoom();
            }
            else
            {
                waitingStatusTextClient.text = "<color=black>Searching for the room...</color>";
                JoinRoom();
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        hostRoomPanel.gameObject.SetActive(false);
        landingPagePanel.SetActive(true);

        Debug.Log($"Disconnected due to : {cause}");
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        // ClientSide
        // Called when client try to join a room with a name, but the room isn't found
        Debug.Log("couldn't join the room " + inputRoomName);
        waitingStatusTextClient.text = "<color=red>Couldn't join " + inputRoomName + "!</color>";
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient) return;
        // ClientSide
        Debug.Log("Client successfully joined a room");
        waitingStatusTextClient.text = "<color=green>Joined " + inputRoomName + "!</color>";

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

        // Update the player list by sync it with photon network
        foreach (Player player in PhotonNetwork.PlayerList)
        {
             players.Add(player);
        }

        // MoveCamera
        Camera.main.transform.DOLocalMove(new Vector3(15f, -11.0f, Camera.main.transform.position.z), panelSwitchTime);
        // switch panel
        clientPanel.SetActive(false);
        StartCoroutine(SwitchPanel(hostRoomPanel.gameObject, true, new Vector2(15f, 0f), new Vector2(0f, -10f)));
        // Set Up the room info visual
        hostRoomPanel.RoomInitiate(inputRoomName);
        RearrangePlayerSlot();  // initialize the slots
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // MasterClient side
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        if (playerCount == MaxPlayerPerRoom)
        {
            // close this room if the player count reach its limit (4)
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        
        Debug.Log(playerCount + "th player has joined the room! his name is " + newPlayer.NickName);

        // add player into the list so we have a reference to all the players
        players.Add(newPlayer);

        // Visual
        hostRoomPanel.SetPlayerSlotName(playerCount, newPlayer.NickName);
        hostRoomPanel.SetPlayerSlotColor(playerCount, textColorFilledSlot, boxColorFilledSlot);

        // Reset the player character list in singleton
        StartCoroutine(UpdatePlayerListInDelay(3.5f));
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // MasterClient side
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

        Debug.Log(otherPlayer.NickName + "leave the room. Rearrange the slot");
        // Remove it from the list
        players.Remove(otherPlayer);

        // Rearrange player slot
        RearrangePlayerSlot();
    }

    public void CreatingRoom()
    {
        isConnecting = true;
        PhotonNetwork.OfflineMode = false;
        hosting = true;

        waitingStatusTextHost.text = "Connecting to server...";
        
        ConnectToMaster();
    }

    public void JoiningRoom()
    {
        isConnecting = true;
        PhotonNetwork.OfflineMode = false;
        hosting = false;

        waitingStatusTextClient.text = "<color=black>Connecting to server...</color>";

        ConnectToMaster();
    }

    public void ConnectToMaster()
    {
        if (PhotonNetwork.IsConnected)
        {
            // This player is already connected to the server before (Probably because he quited from game)
            // then he's not necessary to connect again

            JoinRoom();
        }
        else
        {
            // Connect to the photon server
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();

            // After connect sucess the callback OnConnectedToMaster() will be called by PUN
        }
    }

    public void CreateRoom()
    {
        waitingStatusTextHost.text = "<color=green>Room Created!</ color >";
        // Gererate a specific name for this room
        generatedRoomName = PhotonNetwork.CountOfRooms.ToString() + ((char)('A' + Random.Range(0, 26))) + Random.Range(0, 9).ToString() + PhotonNetwork.NickName.Substring(0, 1).ToUpperInvariant() + gameVersion;
        // This player is trying to create a new room
        PhotonNetwork.CreateRoom(generatedRoomName, new RoomOptions { MaxPlayers = MaxPlayerPerRoom });
        // MoveCamera
        Camera.main.transform.DOLocalMove(new Vector3(15f, -11.0f, Camera.main.transform.position.z), panelSwitchTime);
        // activate panel
        StartCoroutine(SwitchPanel(hostRoomPanel.gameObject, true, new Vector2(15f, 0f), new Vector2(0f, -10f)));
        // Set Up the room info visual
        hostRoomPanel.RoomInitiate(generatedRoomName);
        hostRoomPanel.SetPlayerSlotName(1, PhotonNetwork.NickName); // 1 because only one player
        hostRoomPanel.SetPlayerSlotColor(1, textColorFilledSlot, boxColorFilledSlot); // 1 because only one player

        players.Add(PhotonNetwork.LocalPlayer);
    }

    public void JoinRoom()
    {
        // Join Room
        PhotonNetwork.JoinRoom(inputRoomName);
    }
    
    #endregion





    // ------------------------------------------
    // ----------- Non Photon Related -----------
    // ------------------------------------------
    #region NonPhotonRelated

    public void StartPlatformStepped(CollideCallback activate)
    {
        // when the player step on the start game platform
        Instantiate(effectPrefab, player.position, player.rotation);

        // move Camera
        Camera.main.transform.DOLocalMoveX(5.5f, panelSwitchTime);

        // activate panel
        StartCoroutine(SwitchPanel(landingPagePanel, true, activate));
    }

    public void BackPlatformStepped(CollideCallback activate)
    {
        // when the player step on the start game platform
        Instantiate(effectPrefab, player.position, player.rotation);

        // move Camera
        Camera.main.transform.DOLocalMoveX(0.0f, panelSwitchTime);

        // activate panel
        StartCoroutine(SwitchPanel(nameInputPanel, true, activate));
    }

    IEnumerator SwitchPanel(GameObject panel, bool boolean, CollideCallback activate)
    {
        yield return new WaitForSeconds(panelSwitchTime);

        panel.SetActive(boolean);

        activate.SetEnabled(true);
    }

    IEnumerator SwitchPanel(GameObject panel, bool boolean, Vector2 setPlayerPos, Vector2 setVelocity)
    {
        yield return new WaitForSeconds(panelSwitchTime);

        panel.SetActive(boolean);


        // Save Color
        Color color = playerColorScript.GetPlayerColor();
        Destroy(player.gameObject);

        // remake the character, copying its properties to new character created with authority assigned
        Transform newPlayer;
        newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, setPlayerPos, Quaternion.identity).transform;
        newPlayer.GetComponent<Controller>().photonView.RPC("RPCSetCapeColor", RpcTarget.AllBuffered, color.r, color.g, color.b);
        newPlayer.position = setPlayerPos;
        newPlayer.GetComponent<Rigidbody2D>().velocity = setVelocity;
        player = newPlayer; // done replacement
        player.gameObject.name = PhotonNetwork.LocalPlayer.NickName + "[" + PhotonNetwork.LocalPlayer.ActorNumber + "]";
        
        StartCoroutine(UpdatePlayerListInDelay(0.5f)); 
    }

    IEnumerator UpdatePlayerListInDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        Debug.Log("initialize the list after a delay");
        PlayerManager.Instance().Initialize();  // get reference for every player character in the scene and remove unneccesary transform in the list
    }

    public void ClientRoomNameInput(string roomName)
    {
        inputRoomName = roomName;
    }
    
    /// <summary>
    /// get reference
    /// </summary>
    public Transform GetPlayer()
    {
        return player;
    }

    /// <summary>
    /// get reference
    /// </summary>
    public Button GetColorButton()
    {
        return buttonSwitchColor;
    }

    /// <summary>
    /// Mostly called when a player leave the room or join the room.
    /// </summary>
    public void RearrangePlayerSlot()
    {
        int tmp = 0;
        foreach (Player player in players)
        {
            tmp++;
            hostRoomPanel.SetPlayerSlotName(tmp, player.NickName);
            hostRoomPanel.SetPlayerSlotColor(tmp, textColorFilledSlot, boxColorFilledSlot);
        }

        // Reset the rest of the room
        for (int i = tmp + 1; i <= MaxPlayerPerRoom; i++)
        {
            hostRoomPanel.SetPlayerSlotName(i, "Empty");
            hostRoomPanel.SetPlayerSlotColor(i, textColorEmptySlot, boxColorEmptySlot);
        }
    }

    #endregion
}
