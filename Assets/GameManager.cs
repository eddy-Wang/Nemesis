using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private List<PlayerNetObject> gamePlayers = new List<PlayerNetObject>();
    [SyncVar(hook = nameof(OnCurrentPlayerNetIdChanged))]
    public uint currentPlayerNetId;
    private int currentPlayerIndex = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 如果GameManager需要在场景切换时保留，则取消注释
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Server]
    public void RegisterPlayer(PlayerNetObject playerNetObj, NetworkConnectionToClient conn) // MyNetworkManager 会调用这个
    {
        if (!gamePlayers.Any(p => p.netId == playerNetObj.netId)) // 确保不重复添加
        {
            gamePlayers.Add(playerNetObj);
            // 可选：如果还需要基于connectionId的快速查找，可以保留之前的connectedPlayers列表或用字典
            // playerNetObj.playerNumber = gamePlayers.Count; // 简单分配一个玩家编号

            Debug.Log($"Player object (netId: {playerNetObj.netId}, connId: {conn.connectionId}) registered. Total players: {gamePlayers.Count}");

            if (gamePlayers.Count == 2 && currentPlayerNetId == 0) // 假设是2人游戏且游戏尚未开始
            {
                StartGame();
            }
        }
    }


    [Server]
    public void UnregisterPlayer(PlayerNetObject playerNetObj)
    {
        if (playerNetObj == null) return;

        PlayerNetObject foundPlayer = gamePlayers.FirstOrDefault(p => p.netId == playerNetObj.netId);
        if (foundPlayer != null)
        {
            if (playerNetObj.netId == currentPlayerNetId)
            {
                Debug.LogWarning($"Current turn player (netId: {playerNetObj.netId}) disconnected.");
                // 需要处理当前回合玩家掉线的情况，例如轮到下一个玩家或结束游戏
            }
            gamePlayers.Remove(foundPlayer);
            Debug.Log($"Player object (netId: {playerNetObj.netId}) unregistered. Total players: {gamePlayers.Count}");

            if (gamePlayers.Count < 2 && NetworkServer.active)
            {
                 Debug.Log("Not enough players to continue.");
                 // 可能需要停止游戏或等待
            }
        }
    }
    [Server]
    void StartGame()
    {
        Debug.Log("Game Starting! Determining first player.");
        if (gamePlayers.Count > 0)
        {
            currentPlayerIndex = 0;
            currentPlayerNetId = gamePlayers[currentPlayerIndex].netId; // 获取PlayerNetObject的netId
            Debug.Log($"Player object with netId {currentPlayerNetId} starts the game.");
        }
    }
     [Server]
    public void EndTurn()
    {
        if (gamePlayers.Count == 0) return;

        currentPlayerIndex = (currentPlayerIndex + 1) % gamePlayers.Count;
        currentPlayerNetId = gamePlayers[currentPlayerIndex].netId;
        Debug.Log($"Turn ended. Next player object is netId {currentPlayerNetId}");
    }

    // Hook 方法，当 currentPlayerNetId 在客户端变化时调用
    void OnCurrentPlayerNetIdChanged(uint oldPlayerNetId, uint newPlayerNetId)
    {
        Debug.Log($"UI Hook: Current player netId changed from {oldPlayerNetId} to {newPlayerNetId} on this client.");
        UpdateTurnDisplay();
    }

    public void UpdateTurnDisplay()
    {
        if (TurnDisplayUI.Instance != null)
        {
            TurnDisplayUI.Instance.DisplayTurn(currentPlayerNetId);
        }
    }

    // OnGUI 测试按钮保持不变
    void OnGUI()
    {
        if (isServer)
        {
            if (GUI.Button(new Rect(10, 150, 120, 30), "Server: End Turn"))
            {
                EndTurn();
            }
        }
    }
}
