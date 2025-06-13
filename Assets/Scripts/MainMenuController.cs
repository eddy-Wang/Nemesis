using Mirror;
using Mirror.Discovery;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("UI References")]
    public GameObject MainMenuPanel;
    public Button HostButton;
    public Button JoinButton;
    public TMP_InputField RoomCodeInput;
    public TMP_Text InfoText;
    public TMP_Text NameText;

    private NetworkDiscovery networkDiscovery;

    public static string HostRoomCode { get; private set; }
    public static string EnteredCode { get; private set; }

    private readonly Dictionary<long, ServerResponse> foundServers = new Dictionary<long, ServerResponse>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        networkDiscovery = GetComponent<NetworkDiscovery>();
    }

    void Start()
    {
        MainMenuPanel.SetActive(true);
        ResetLobbyUI();

        HostButton.onClick.AddListener(OnHostButtonClicked);
        JoinButton.onClick.AddListener(OnJoinButtonClicked);
        networkDiscovery.OnServerFound.AddListener(OnServerDiscovered);
    }

    private void OnDestroy()
    {
        HostButton.onClick.RemoveListener(OnHostButtonClicked);
        JoinButton.onClick.RemoveListener(OnJoinButtonClicked);
        networkDiscovery.OnServerFound.RemoveListener(OnServerDiscovered);
    }

    private void OnHostButtonClicked()
    {
        networkDiscovery.StopDiscovery();
        foundServers.Clear();
        
        NetworkManager.singleton.StartHost();

        HostRoomCode = Random.Range(1000, 9999).ToString();
        networkDiscovery.AdvertiseServer();

        InfoText.text = $"Your Room Code is: {HostRoomCode}\nWaiting for other players to join...";
        SetButtonsInteractable(false);
    }

    private void OnJoinButtonClicked()
    {
        EnteredCode = RoomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(EnteredCode))
        {
            InfoText.text = "Please enter a valid room code!";
            return;
        }


        if (foundServers.Count > 0)
        {

            networkDiscovery.StopDiscovery();
            
            ServerResponse serverToJoin = foundServers.Values.First();
            NetworkManager.singleton.StartClient(serverToJoin.uri);

            InfoText.text = $"Attempting to join room...";
            SetButtonsInteractable(false);
        }
        else
        {

            InfoText.text = "No game room found yet. Still searching...";
        }
    }
    

    private void OnServerDiscovered(ServerResponse response)
    {

        foundServers[response.serverId] = response;
        networkDiscovery.StopDiscovery(); 

        InfoText.text = "Game room found! Please enter the room code to join.";
        Debug.Log($"[Discovery] Found server at {response.uri}");
    }
    
    public void ResetLobbyUI()
    {
        InfoText.text = "Welcome to the Strategy Card Game";
        SetButtonsInteractable(true);
        RoomCodeInput.text = "";
        StartDiscovery();
    }
    

    private void StartDiscovery()
    {
        foundServers.Clear();
        networkDiscovery.StartDiscovery();
        InfoText.text = "Searching for rooms in the local area network...";
    }
    
    private void SetButtonsInteractable(bool interactable)
    {
        HostButton.interactable = interactable;
        JoinButton.interactable = interactable;
        RoomCodeInput.interactable = interactable;
    }
}