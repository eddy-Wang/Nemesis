using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    [Header("Game Objects")]
    public GameObject gameManagerPrefab;

    private GameObject gameManagerInstance;


    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<RoomCodeMessage>(OnReceiveRoomCode);

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

        Debug.Log("[Manager] Server stopped. Resetting UI to Main Menu.");

        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ResetLobbyUI();
        }

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

        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ResetLobbyUI();
        }

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
