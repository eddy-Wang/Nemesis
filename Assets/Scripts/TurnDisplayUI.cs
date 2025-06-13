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
            turnText = GetComponent<TMP_Text>();
            if (turnText == null) return; 
        }
        turnText.text = "Waiting for game to start...";
        Debug.Log("TurnDisplayUI Start: Initial text set.");

        if (GameManager.Instance != null && NetworkClient.isConnected)
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
                turnText.text = $"Opponent's Turn (NetID: {currentPlayerNetId_FromServer})";
            }
            else
            {
                turnText.text = "Opponent's Turn (Info N/A)";
            }
        }
    }
}