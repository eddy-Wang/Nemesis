using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 调用基类方法来生成Player Prefab并与连接关联
        // 这会确保 playerControllerId 被正确设置，并且该对象对于该客户端是 isLocalPlayer
        base.OnServerAddPlayer(conn);

        // conn.identity 是 Mirror 为这个连接生成的玩家对象的 NetworkIdentity
        if (conn.identity != null)
        {
            PlayerNetObject playerNetScript = conn.identity.GetComponent<PlayerNetObject>();
            if (playerNetScript != null && GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayer(playerNetScript, conn);
            }
            else
            {
                Debug.LogError("PlayerNetObject script on player prefab or GameManager instance not found!");
            }
        }
        else
        {
            Debug.LogError("Player object not spawned or identity not set for connection: " + conn.connectionId);
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (GameManager.Instance != null && conn.identity != null)
        {
            PlayerNetObject playerNetScript = conn.identity.GetComponent<PlayerNetObject>();
            if (playerNetScript != null)
            {
                GameManager.Instance.UnregisterPlayer(playerNetScript);
            }
        }
        base.OnServerDisconnect(conn);
    }
}