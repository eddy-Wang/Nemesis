using UnityEngine;
using TMPro;
using Mirror;

public class TurnDisplayUI : MonoBehaviour
{
    public static TurnDisplayUI Instance { get; private set; }
    public TMP_Text turnText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (turnText == null) turnText = GetComponent<TMP_Text>();
        turnText.text = "Waiting for game to start...";
        if (GameManager.Instance != null)
        {
            DisplayTurn(GameManager.Instance.currentPlayerNetId);
        }
    }

    public void DisplayTurn(uint currentPlayerNetId_FromServer)
    {
        if (turnText == null) return;

        if (currentPlayerNetId_FromServer == 0)
        {
            turnText.text = "Waiting for players...";
            return;
        }

        // NetworkClient.localPlayer 是本地客户端所拥有的玩家对象的 NetworkIdentity
        if (NetworkClient.localPlayer != null && currentPlayerNetId_FromServer == NetworkClient.localPlayer.netId)
        {
            turnText.text = "Your Turn!";
        }
        else
        {
            // NetworkClient.spawned 包含所有当前客户端知道的网络对象
            if (NetworkClient.spawned.TryGetValue(currentPlayerNetId_FromServer, out NetworkIdentity opponentIdentity))
            {
                // 获取到对手的NetworkIdentity后，可以尝试获取其上的PlayerNetObject脚本
                PlayerNetObject opponentPlayerScript = opponentIdentity.GetComponent<PlayerNetObject>();
                if (opponentPlayerScript != null)
                {
                    // 假设 PlayerNetObject 未来会有一个可显示的玩家标识，如playerNumber或playerName
                    // 例如: turnText.text = $"Player {opponentPlayerScript.playerNumber}'s Turn";
                    // 为了简单起见，目前我们只用 netId
                    turnText.text = $"Opponent's Turn (ID: {opponentPlayerScript.netId})";
                }
                else
                {
                    // 如果没有PlayerNetObject脚本（理论上应该有），就只显示netId
                    turnText.text = $"Opponent's Turn (ID: {currentPlayerNetId_FromServer})";
                }
            }
            else
            {
                turnText.text = "Opponent's Turn (Info N/A)"; // Fallback，如果找不到该netId的对象
            }
        }
    }
}