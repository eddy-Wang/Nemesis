using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用于OrderBy
using Mirror; // 如果需要[Server]等属性

public class DeckManager : NetworkBehaviour // 继承NetworkBehaviour以使用[Server]等
{
    public static DeckManager Instance { get; private set; }

    [Header("Card Data Assets")]
    public List<PlayingCardData> masterDeckData = new List<PlayingCardData>(); // 在Inspector中拖入所有52张PlayingCardData资源

    // 服务器端的当前牌库 (不需要同步给客户端)
    private List<PlayingCardData> serverDeck = new List<PlayingCardData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer() // 只在服务器启动时执行
    {
        base.OnStartServer();
        InitializeDeck();
        ShuffleDeck();
        Debug.Log($"[Server] Deck Initialized with {serverDeck.Count} cards.");
    }

    [Server] // 确保这些操作只在服务器上执行
    public void InitializeDeck()
    {
        serverDeck.Clear();
        foreach (PlayingCardData cardData in masterDeckData)
        {
            serverDeck.Add(cardData);
        }
    }

    [Server]
    public void ShuffleDeck()
    {
        // 简单的Fisher-Yates洗牌算法
        System.Random rng = new System.Random();
        int n = serverDeck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            PlayingCardData value = serverDeck[k];
            serverDeck[k] = serverDeck[n];
            serverDeck[n] = value;
        }
        Debug.Log("[Server] Deck shuffled.");
    }

    [Server]
    public PlayingCardData DrawCard()
    {
        if (serverDeck.Count == 0)
        {
            Debug.LogWarning("[Server] Deck is empty! Cannot draw card. Consider reshuffling discard pile or ending game phase.");
            // 在这里可以加入重新洗牌弃牌堆的逻辑，或者发布牌库耗尽事件
            // EventManager.Instance.Publish(new DeckExhaustedEvent());
            return null;
        }

        PlayingCardData drawnCard = serverDeck[0];
        serverDeck.RemoveAt(0);
        return drawnCard;
    }

    [Server]
    public List<PlayingCardData> DrawMultipleCards(int amount)
    {
        List<PlayingCardData> drawnCards = new List<PlayingCardData>();
        for (int i = 0; i < amount; i++)
        {
            PlayingCardData card = DrawCard();
            if (card != null)
            {
                drawnCards.Add(card);
            }
            else
            {
                break; // 牌库没牌了
            }
        }
        return drawnCards;
    }

    // (可选) 服务器重置并重洗牌库的方法
    [Server]
    public void ResetAndShuffleDeck()
    {
        InitializeDeck();
        ShuffleDeck();
        Debug.Log($"[Server] Deck has been reset and reshuffled. Cards: {serverDeck.Count}");
    }
    
    [Server]
    public bool IsDeckEmpty()
    {
        return serverDeck.Count == 0;
    }
}