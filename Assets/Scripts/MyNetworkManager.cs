using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    // The reference to NetworkDiscovery has been completely removed from this script.
    // Its responsibility is now solely to manage connections and players.
    [Header("Game Objects")]
    public GameObject gameManagerPrefab;

    private GameObject gameManagerInstance;


    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<RoomCodeMessage>(OnReceiveRoomCode);

        // 2. 当服务器启动时，实例化并生成一个全新的GameManager
        if (gameManagerPrefab != null)
        {
            Debug.Log("[Server] Spawning GameManager...");
            gameManagerInstance = Instantiate(gameManagerPrefab);
            NetworkServer.Spawn(gameManagerInstance);
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        // 当服务器停止时（也就是Host返回主菜单时），
        // 我们也用同样的方式重置UI。
        Debug.Log("[Manager] Server stopped. Resetting UI to Main Menu.");

        // 1. 重置主菜单的逻辑状态和UI
        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ResetLobbyUI();
        }

        // 2. 确保主菜单面板被显示
        if (GameHUDController.Instance != null)
        {
            GameHUDController.Instance.ShowMainMenuPanel();
        }
    }


    public override void OnClientConnect()
    {
        base.OnClientConnect();
        RoomCodeMessage codeMessage = new RoomCodeMessage
        {
            roomCode = MainMenuController.EnteredCode
        };
        NetworkClient.Send(codeMessage);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        // 当客户端停止时（无论是主动退出还是被踢出），
        // 我们需要确保UI被重置到初始状态。
        // 这是一个比在按钮点击时处理更可靠的地方。

        // 1. 重置主菜单的逻辑状态和UI
        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ResetLobbyUI();
        }

        // 2. 确保主菜单面板被显示
        if (GameHUDController.Instance != null)
        {
            GameHUDController.Instance.ShowMainMenuPanel();
        }
    }

    // The core logic for room code verification on the server
    private void OnReceiveRoomCode(NetworkConnectionToClient conn, RoomCodeMessage message)
    {
        if (conn == NetworkServer.localConnection) return;

        if (message.roomCode == MainMenuController.HostRoomCode)
        {
            Debug.Log($"[Server] Remote ConnId {conn.connectionId} passed room code check.");
            if (conn.identity != null)
            {
                GameManager.Instance?.RegisterPlayer(conn.identity.GetComponent<PlayerNetObject>(), conn);
            }
        }
        else
        {
            Debug.LogWarning($"[Server] Remote ConnId {conn.connectionId} failed room code check ({message.roomCode}). Kicking.");
            conn.Disconnect();
        }
    }

    // This method is called when a player object is created on the server
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        if (conn == NetworkServer.localConnection)
        {
            Debug.Log($"[Server] Host (ConnId {conn.connectionId}) has connected. Adding to game immediately.");
            if (conn.identity != null)
            {
                GameManager.Instance?.RegisterPlayer(conn.identity.GetComponent<PlayerNetObject>(), conn);
            }
        }
        else
        {
            Debug.Log($"[Server] Remote client (ConnId {conn.connectionId}) connected. Waiting for room code verification...");
        }
    }
     
    // Your existing OnServerDisconnect logic remains unchanged
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        if (GameManager.Instance != null && conn.identity != null)
        {
            PlayerNetObject playerNetScript = conn.identity.GetComponent<PlayerNetObject>();
            if (playerNetScript != null)
            {
                GameManager.Instance.UnregisterPlayer(playerNetScript);
            }
        }
    }
    
}
