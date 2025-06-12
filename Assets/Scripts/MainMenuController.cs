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

    // NetworkDiscovery 会在 Awake 中自动获取
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

    /// <summary>
    /// 当玩家决定自己创建房间时调用
    /// </summary>
    private void OnHostButtonClicked()
    {
        // 停止作为客户端的搜索，准备作为主机
        networkDiscovery.StopDiscovery();
        foundServers.Clear();
        
        NetworkManager.singleton.StartHost();

        HostRoomCode = Random.Range(1000, 9999).ToString();
        networkDiscovery.AdvertiseServer(); // 开始广播自己的服务器

        InfoText.text = $"Your Room Code is: {HostRoomCode}\nWaiting for other players to join...";
        SetButtonsInteractable(false);
    }

    /// <summary>
    /// 当玩家尝试加入一个房间时调用
    /// </summary>
    private void OnJoinButtonClicked()
    {
        EnteredCode = RoomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(EnteredCode))
        {
            InfoText.text = "Please enter a valid room code!";
            return;
        }

        // 只有在已经发现服务器的情况下才尝试加入
        if (foundServers.Count > 0)
        {
            // 停止搜索，因为我们已经决定要加入一个了
            networkDiscovery.StopDiscovery();
            
            ServerResponse serverToJoin = foundServers.Values.First();
            NetworkManager.singleton.StartClient(serverToJoin.uri);

            InfoText.text = $"Attempting to join room...";
            SetButtonsInteractable(false);
        }
        else
        {
            // 如果还没找到服务器，就友好地提示用户
            InfoText.text = "No game room found yet. Still searching...";
        }
    }
    
    /// <summary>
    /// 当NetworkDiscovery发现一个服务器时自动调用
    /// </summary>
    private void OnServerDiscovered(ServerResponse response)
    {
        // 找到了一个服务器，就把它存起来，并停止进一步的搜索
        foundServers[response.serverId] = response;
        networkDiscovery.StopDiscovery(); 

        InfoText.text = "Game room found! Please enter the room code to join.";
        Debug.Log($"[Discovery] Found server at {response.uri}");
    }
    
    /// <summary>
    /// 重置UI到初始状态，并开始搜索
    /// </summary>
    public void ResetLobbyUI()
    {
        InfoText.text = "Welcome to the Strategy Card Game";
        SetButtonsInteractable(true);
        RoomCodeInput.text = "";
        StartDiscovery();
    }
    
    /// <summary>
    /// 开始持续地搜索服务器
    /// </summary>
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