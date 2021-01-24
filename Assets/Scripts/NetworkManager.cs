using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{

    [Header("Login UI Panel")]     //with this we will have a clear view in the inspector when we write codes for other UI panels
    public InputField playerNameInput;
    public GameObject Login_UI_Panel;


    [Header("Connection Status")]
    public Text connectionStatusText;


    [Header("Game Options UI Panel")]
    public GameObject GameOptions_UI_Panel;

    [Header("Create Room UI Panel")]
    public GameObject CreateRoom_UI_Panel;
    public InputField roomNameInputField;

    public InputField maxPlayerInputField;

    [Header("Inside Room UI Panel")]
    public GameObject InsideRoom_UI_Panel;
    public Text roomInfoText;
    public GameObject playerListPrefab;
    public GameObject playerListContent;
    public GameObject startGameButoon;

    [Header("Room List UI Panel")]
    public GameObject RoomList_UI_Panel;
    public GameObject roomListEntryPrefab;
    public GameObject roomListParentGameObject;     //in editor we want to spawn the roomlist prefab under "content" section....so we need to get a reference for that content

    [Header("Join Random Room UI Panel")]
    public GameObject JoinRandomRoom_UI_Panel;

    private Dictionary<string, RoomInfo> cachedRoomList;
    private Dictionary<string, GameObject> roomListGameObjects;
    private Dictionary<int, GameObject> playerListGameobjects;          //we want to identify playerList game objects with their player actor number


    #region Unity Methods


    // Start is called before the first frame update
    void Start()
    {

        ActivatePanel(Login_UI_Panel.name);

        cachedRoomList = new Dictionary<string, RoomInfo>();
        roomListGameObjects = new Dictionary<string, GameObject>();

        PhotonNetwork.AutomaticallySyncScene = true;            //this way all the cleints in the room will load the same scene as the owner

    }

    // Update is called once per frame
    void Update()
    {
        connectionStatusText.text = "Connected status: " + PhotonNetwork.NetworkClientState;     //this NetworkClientState provides naetwork state if we are not offline
    }


    #endregion



    #region UI Callbacks

    public void OnLoginButtonClicked()
    {
        string playerName = playerNameInput.text;
        if (!string.IsNullOrEmpty(playerName))
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();

        }
        else
        {
            Debug.Log("Playername is invalid!");
        }
    }



    public void OnCreateRoomButtonClicked()
    {
        string roomName = roomNameInputField.text;

        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Room " + Random.Range(1000, 10000);

        }

        RoomOptions roomOptions = new RoomOptions();          //here...we can customize rooms
        roomOptions.MaxPlayers = (byte)int.Parse(maxPlayerInputField.text);           //we need to cast string to byte...for that we first changed its type to int then to byte and made a room with maximum possible players






        PhotonNetwork.CreateRoom(roomName, roomOptions);               //when this method is called first we join a lobby and then create a room


    }


    public void OnCancelButtonClicked()
    {
        ActivatePanel(GameOptions_UI_Panel.name);


    }


    public void OnShowRoomListButtonClicked()
    {

        if (!PhotonNetwork.InLobby)               
        {
            PhotonNetwork.JoinLobby();           
        }




        ActivatePanel(RoomList_UI_Panel.name);

    }


    public void OnBackButtonClicked()
    {

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        ActivatePanel(GameOptions_UI_Panel.name);

    }


    public void OnLeaveGameButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }


    public void OnJoinRandomRoomButtonClicked()
    {

        ActivatePanel(JoinRandomRoom_UI_Panel.name);
        PhotonNetwork.JoinRandomRoom();

    }


    public void OnStartGameButtonClicked()
    { 
        if (PhotonNetwork.IsMasterClient)     //we can only click the start game button only if we are the master
        {
            PhotonNetwork.LoadLevel("GameScene");         //loads the gamescene level when button is clicked
        }    
    }



    #endregion


    #region Photon Callbacks

    public override void OnConnected()
    {
        Debug.Log("connected to Internet");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " is connected to Photon");
        ActivatePanel(GameOptions_UI_Panel.name);


    }


    public override void OnCreatedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.Name + " is created.");
    }



    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " joined to " + PhotonNetwork.CurrentRoom.Name);
        ActivatePanel(InsideRoom_UI_Panel.name);

        if (PhotonNetwork.LocalPlayer.IsMasterClient)           //if the player is the one who created the room
        {
            startGameButoon.SetActive(true);
        }
        else
        {
            startGameButoon.SetActive(false);
        }

        roomInfoText.text = "Room name: " + PhotonNetwork.CurrentRoom.Name + " " + "Players/Max.Players: " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;


        if (playerListGameobjects == null)
        {
            playerListGameobjects = new Dictionary<int, GameObject>();

        }
        
        
        
        //Instanitiating player list gameObjects
        foreach (Player player in PhotonNetwork.PlayerList)
        {

            GameObject playerListGameobject = Instantiate(playerListPrefab);
            playerListGameobject.transform.SetParent(playerListContent.transform);
            playerListGameobject.transform.localScale = Vector3.one;


            playerListGameobject.transform.Find("PlayerNameText").GetComponent<Text>().text = player.NickName;

            if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)               //actor number is the identifier of the player in the current room _ changes when the player enters or leaves the room------(-1 if outside)
            {
                playerListGameobject.transform.Find("PlayerIndicator").gameObject.SetActive(true);
            }
            else
            {
                playerListGameobject.transform.Find("PlayerIndicator").gameObject.SetActive(false);
            }

            playerListGameobjects.Add(player.ActorNumber, playerListGameobject);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)                        //called if any update is is done to the existing rooms.....this update can have either adding new rooms or removing old rooms 
    {

        ClearRoomListView();

        foreach (var roomListGameObject in roomListGameObjects.Values)
        {
            Destroy(roomListGameObject);
        }

        roomListGameObjects.Clear();
        
        foreach(RoomInfo room in roomList)
        {
            Debug.Log(room.Name);

            if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)       //if a room is closed or hidden then it is marked as RemovedFromList
            {

                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList.Remove(room.Name);
                }


            }
            else
            {
                //update cachedRoom list
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList[room.Name] = room;                    //in case if room is already present this statement updates the room
                }
                //add the new room to the cached room list
                else
                {
                    cachedRoomList.Add(room.Name, room);                 //in case the room is not present....this adds the new room

                }

            }
            
        }

        foreach (RoomInfo room in cachedRoomList.Values)
        {

            GameObject roomListEntryGameObject = Instantiate(roomListEntryPrefab);
            roomListEntryGameObject.transform.SetParent(roomListParentGameObject.transform);
            roomListEntryGameObject.transform.localScale = Vector3.one;             //prevents scaling issue


            roomListEntryGameObject.transform.Find("RoomNameText").GetComponent<Text>().text = room.Name;
            roomListEntryGameObject.transform.Find("RoomPlayersText").GetComponent<Text>().text = room.PlayerCount + " / " + room.MaxPlayers;
            roomListEntryGameObject.transform.Find("JoinRoomButton").GetComponent<Button>().onClick.AddListener(()=>OnJoinRoomButtonClicked(room.Name));     //this is a way to add listener to a button via script and it takes a parameter

            roomListGameObjects.Add(room.Name, roomListEntryGameObject);



        }


    }

    public override void OnLeftLobby()     //on leaving the lobby we want to clear all the previous prefabs as to when we go to lobby again we get the updated room lists
    {

        ClearRoomListView();
        cachedRoomList.Clear();


    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {

        //update room info text
        roomInfoText.text = "Room name: " + PhotonNetwork.CurrentRoom.Name + " " + "Players/Max.Players: " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;


        GameObject playerListGameobject = Instantiate(playerListPrefab);
        playerListGameobject.transform.SetParent(playerListContent.transform);
        playerListGameobject.transform.localScale = Vector3.one;


        playerListGameobject.transform.Find("PlayerNameText").GetComponent<Text>().text = newPlayer.NickName;

        if (newPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)               //actor number is the identifier of the player in the current room _ changes when the player enters or leaves the room------(-1 if outside)
        {
            playerListGameobject.transform.Find("PlayerIndicator").gameObject.SetActive(true);
        }
        else
        {
            playerListGameobject.transform.Find("PlayerIndicator").gameObject.SetActive(false);
        }

        playerListGameobjects.Add(newPlayer.ActorNumber, playerListGameobject);
    }


    public override void OnPlayerLeftRoom(Player otherPlayer)
    {

        //update room info text
        roomInfoText.text = "Room name: " + PhotonNetwork.CurrentRoom.Name + " " + "Players/Max.Players: " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;

        Destroy(playerListGameobjects[otherPlayer.ActorNumber].gameObject);               //this destroys the player game objects and arrange the actor number when a player leaves the room except us....but what if WE leave the room
        playerListGameobjects.Remove(otherPlayer.ActorNumber);


        if (PhotonNetwork.LocalPlayer.IsMasterClient)         //after the master leaves the room... the player who did join next will be made the owner of the room
        {
            startGameButoon.SetActive(true);
        }



    }


    public override void OnLeftRoom()
    {

        ActivatePanel(GameOptions_UI_Panel.name);

        foreach (GameObject playerListGameObject in playerListGameobjects.Values)
        {
            Destroy(playerListGameObject);
        }

        playerListGameobjects.Clear();
        playerListGameobjects = null;


    }


    public override void OnJoinRandomFailed(short returnCode, string message)               //this will create a random room in case there is no room in the lobby to join randomly
    {
        Debug.Log(message);


        string roomName = "Room " + Random.Range(1000, 10000);

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 20;

        PhotonNetwork.CreateRoom(roomName, roomOptions);

    }

    #endregion


    #region Private Methods
    void OnJoinRoomButtonClicked(string _roomName)
    {

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
           
        }

        PhotonNetwork.JoinRoom(_roomName);

    }


    void ClearRoomListView()
    {

        foreach (var roomListGameObject in roomListGameObjects.Values)
        {
            Destroy(roomListGameObject);
        }

        roomListGameObjects.Clear();
    }



    #endregion



    #region Public Methods

    public void ActivatePanel(string panelToBeActivated)
    {
        Login_UI_Panel.SetActive(panelToBeActivated.Equals(Login_UI_Panel.name));                 //if panelNameToBeActivated is not equal to Login UI Panel name the we will deactivate LoginUIpanel.....with this we activate only the panelname to be activated and other panels will be deactivated.
        GameOptions_UI_Panel.SetActive(panelToBeActivated.Equals(GameOptions_UI_Panel.name));
        CreateRoom_UI_Panel.SetActive(panelToBeActivated.Equals(CreateRoom_UI_Panel.name));
        InsideRoom_UI_Panel.SetActive(panelToBeActivated.Equals(InsideRoom_UI_Panel.name));
        RoomList_UI_Panel.SetActive(panelToBeActivated.Equals(RoomList_UI_Panel.name));
        JoinRandomRoom_UI_Panel.SetActive(panelToBeActivated.Equals(JoinRandomRoom_UI_Panel.name));

    }



    #endregion





}
