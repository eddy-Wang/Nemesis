using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Mirror;

public class DeckManager : NetworkBehaviour 
{
    public static DeckManager Instance { get; private set; }

    [Header("Card Data Assets")]
    public List<PlayingCardData> masterDeckData = new List<PlayingCardData>(); 

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

    public override void OnStartServer() 
    {
        base.OnStartServer();
        InitializeDeck();
        ShuffleDeck();
        Debug.Log($"[Server] Deck Initialized with {serverDeck.Count} cards.");
    }

    [Server] 
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
        // Fisher-Yates
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
                break;
            }
        }
        return drawnCards;
    }

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