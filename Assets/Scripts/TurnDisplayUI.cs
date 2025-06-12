using UnityEngine;
using TMPro;
using Mirror;

public class TurnDisplayUI : MonoBehaviour
{
    public static TurnDisplayUI Instance { get; private set; }
    public TMP_Text turnText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("TurnDisplayUI Awake: Instance set.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (turnText == null)
        {
            Debug.LogError("TurnDisplayUI Start: turnText (TMP_Text) is not assigned!");
            turnText = GetComponent<TMP_Text>(); // 尝试获取
            if (turnText == null) return; // 如果还是没有，则无法工作
        }
        turnText.text = "Waiting for game to start...";
        Debug.Log("TurnDisplayUI Start: Initial text set.");

        // 尝试在Start时就从GameManager获取一次当前状态并更新，以防hook稍后才触发或初始值已设定
        if (GameManager.Instance != null && NetworkClient.isConnected) // 只在客户端尝试更新
        {
            Debug.Log($"TurnDisplayUI Start: Attempting initial display update with GameManager.currentPlayerNetId = {GameManager.Instance.currentPlayerNetId}");
            DisplayTurn(GameManager.Instance.currentPlayerNetId);
        }
    }

    public void DisplayTurn(uint currentPlayerNetId_FromServer)
    {
        if (turnText == null)
        {
            Debug.LogError("TurnDisplayUI DisplayTurn: turnText is null!");
            return;
        }

        Debug.Log($"TurnDisplayUI DisplayTurn called with: {currentPlayerNetId_FromServer}. My localPlayer netId is: {(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId.ToString() : "N/A")}");

        if (currentPlayerNetId_FromServer == 0)
        {
            turnText.text = "Waiting for players...";
            return;
        }

        if (NetworkClient.localPlayer != null && currentPlayerNetId_FromServer == NetworkClient.localPlayer.netId)
        {
            turnText.text = "Your Turn!";
        }
        else
        {
            if (NetworkClient.spawned.TryGetValue(currentPlayerNetId_FromServer, out NetworkIdentity opponentIdentity))
            {
                PlayerNetObject opponentPlayerScript = opponentIdentity.GetComponent<PlayerNetObject>();
                // 你可以根据 playerNumber 或其他自定义属性来显示更友好的名称
                // if (opponentPlayerScript != null && opponentPlayerScript.playerNumber != 0)
                // {
                //    turnText.text = $"Player {opponentPlayerScript.playerNumber}'s Turn";
                // } else
                // {
                turnText.text = $"Opponent's Turn (NetID: {currentPlayerNetId_FromServer})";
                // }
            }
            else
            {
                turnText.text = "Opponent's Turn (Info N/A)";
            }
        }
    }
}