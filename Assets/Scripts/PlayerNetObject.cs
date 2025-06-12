using Mirror;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerNetObject : NetworkBehaviour
{
    // --- 同步变量 ---
    [SyncVar(hook = nameof(OnPlayerNumberChanged))]
    public int playerNumber = 0;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score = 0;

    [SyncVar(hook = nameof(OnScoreTierChanged))]
    public int scoreTier = 0;

    // --- 服务器端权威数据 ---
    [System.NonSerialized]
    public List<NetworkPlayingCard> Server_AuthoritativeHand;

    // --- 客户端本地数据 ---
    public readonly List<PlayingCardData> Client_LocalHand = new List<PlayingCardData>();
    public event System.Action Client_OnHandUpdated;

    #region Mirror生命周期函数
    public override void OnStartServer()
    {
        base.OnStartServer();
        Server_AuthoritativeHand = new List<NetworkPlayingCard>();
        score = 0;
        scoreTier = 0;
        Debug.Log($"[Server] PlayerNetObject {netId} OnStartServer: 数据已初始化。");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Client_LocalHand.Clear();
        Debug.Log($"[Client] PlayerNetObject {netId} OnStartClient: 本地手牌已清空。");
        // --- 新增代码在这里 ---
        // OnStartClient 是初始化客户端UI的最佳时机，
        // 因为 SyncVar 的 hook 在对象初次生成时可能不会触发。
        // 我们在这里用 score 的初始值主动更新一次UI。
        if (isLocalPlayer)
        {
            GameHUDController.Instance?.UpdateMyScoreDisplay(score);
            // 如果你还有自己的进度条UI，也在这里初始化
        }
        else // 这是对手的玩家对象
        {
            GameHUDController.Instance?.UpdateOpponentScoreTierDisplay(scoreTier);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        gameObject.name = $"LocalPlayer_{netId}";
        Debug.Log($"我是本地玩家! 我的 netId 是: {netId}. 我的玩家编号是: {playerNumber}");
    }
    #endregion

    #region SyncVar Hooks
    void OnScoreChanged(int oldScore, int newScore)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"[My Client] 我的分数从 {oldScore} 变成了 {newScore}");
            GameHUDController.Instance?.UpdateMyScoreDisplay(newScore);
        }
    }

    void OnScoreTierChanged(int oldTier, int newTier)
    {
        if (!isLocalPlayer)
        {
            Debug.Log($"[My Client] 对手的进度条从 {oldTier} 变成了 {newTier}");
            GameHUDController.Instance?.UpdateOpponentScoreTierDisplay(newTier);
        }
    }

    void OnPlayerNumberChanged(int oldNum, int newNum)
    {
        if (isLocalPlayer)
        {
            gameObject.name = $"LocalPlayer_{netId} (Player {newNum})";
        }
        else
        {
            gameObject.name = $"OpponentPlayer_{netId} (Player {newNum})";
        }
    }
    #endregion

    #region 服务器端指令与客户端RPC

    /// <summary>
    /// 【已添加回来】这个方法由GameManager在服务器上调用，用于分发初始手牌。
    /// </summary>
    [Server]
    public void Server_DealInitialHand(List<NetworkPlayingCard> initialCards)
    {
        Server_AuthoritativeHand.Clear();
        Server_AuthoritativeHand.AddRange(initialCards);
        Debug.Log($"[Server] 玩家 {netId} 的权威手牌已设置，数量: {initialCards.Count}。");

        // 调用TargetRpc，将手牌信息只发送给这个玩家所属的客户端
        Target_ReceiveInitialHand(initialCards);
    }

    [TargetRpc]
    public void Target_ReceiveInitialHand(List<NetworkPlayingCard> initialNetCards)
    {
        Debug.Log($"[Client {netId}] RPC: 收到初始手牌，数量: {initialNetCards?.Count ?? 0}。");
        Client_LocalHand.Clear();
        if (initialNetCards != null)
        {
            foreach (var netCard in initialNetCards)
            {
                PlayingCardData cardData = CardLibrary.Instance?.GetCardData(netCard);
                if (cardData != null)
                {
                    Client_LocalHand.Add(cardData);
                }
            }
        }
        Client_OnHandUpdated?.Invoke();
    }

    [TargetRpc]
    public void Target_AddCardsToClientHand(List<NetworkPlayingCard> netCards)
    {
        if (netCards == null || netCards.Count == 0) return;
        Debug.Log($"[Client {netId}] RPC: 收到补牌，数量: {netCards.Count}。");
        foreach (var netCard in netCards)
        {
            PlayingCardData cardData = CardLibrary.Instance?.GetCardData(netCard);
            if (cardData != null)
            {
                Client_LocalHand.Add(cardData);
            }
        }
        Client_OnHandUpdated?.Invoke();
    }

    [TargetRpc]
    public void Target_AcknowledgePlayAndRemoveCards(List<NetworkPlayingCard> playedNetCards)
    {
        if (playedNetCards == null || playedNetCards.Count == 0) return;
        Debug.Log($"[Client {netId}] RPC: 确认出牌，移除 {playedNetCards.Count} 张牌。");
        bool handChanged = false;
        foreach (var netCardToRemove in playedNetCards)
        {
            PlayingCardData cardToRemove = Client_LocalHand.FirstOrDefault(card => card.suit == netCardToRemove.suit && card.rank == netCardToRemove.rank);
            if (cardToRemove != null)
            {
                Client_LocalHand.Remove(cardToRemove);
                handChanged = true;
            }
        }
        if (handChanged)
        {
            Client_OnHandUpdated?.Invoke();
        }
    }
    #endregion

    #region 客户端出牌指令
    [Command]
    public void CmdPlayCards(List<NetworkPlayingCard> playedNetCards)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentPlayerNetId != this.netId)
        {
            Debug.LogWarning($"[Server] 玩家 {netId} 试图在非其回合出牌。");
            return;
        }

        if (playedNetCards == null || playedNetCards.Count == 0) return;

        foreach (var playedCard in playedNetCards)
        {
            if (!Server_AuthoritativeHand.Contains(playedCard))
            {
                Debug.LogError($"[Server] 玩家 {netId} 试图打出不存在的牌: {playedCard}。可能是作弊或状态不同步。");
                Target_ReceiveInitialHand(new List<NetworkPlayingCard>(Server_AuthoritativeHand));
                return;
            }
        }

        GameManager.Instance.ProcessPlayerPlay(this, playedNetCards);

        foreach (var playedCard in playedNetCards)
        {
            Server_AuthoritativeHand.Remove(playedCard);
        }

        Target_AcknowledgePlayAndRemoveCards(playedNetCards);

        int cardsToDraw = 5 - Server_AuthoritativeHand.Count;
        if (cardsToDraw > 0 && DeckManager.Instance != null)
        {
            List<PlayingCardData> drawnCardsData = DeckManager.Instance.DrawMultipleCards(cardsToDraw);
            List<NetworkPlayingCard> netCardsToDeal = new List<NetworkPlayingCard>();
            foreach (var cardData in drawnCardsData)
            {
                netCardsToDeal.Add(new NetworkPlayingCard(cardData.suit, cardData.rank));
            }
            Server_AuthoritativeHand.AddRange(netCardsToDeal);
            Target_AddCardsToClientHand(netCardsToDeal);
        }
    }
    #endregion
    
    void OnDestroy()
    {
        Debug.Log($"[LIFECYCLE] PlayerNetObject with netId {netId} and playerNumber {playerNumber} is being DESTROYED.");
    }

}
