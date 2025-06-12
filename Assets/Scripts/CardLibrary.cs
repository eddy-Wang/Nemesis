// CardLibrary.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CardLibrary : MonoBehaviour
{
    public static CardLibrary Instance { get; private set; }

    public List<PlayingCardData> allCardDatas; // 在Inspector中拖入所有52个PlayingCardData资源

    private Dictionary<System.Tuple<CardSuit, CardRank>, PlayingCardData> cardLookup;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeLookup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeLookup()
    {
        cardLookup = new Dictionary<System.Tuple<CardSuit, CardRank>, PlayingCardData>();
        if (allCardDatas == null)
        {
            Debug.LogError("CardLibrary: allCardDatas list is not assigned in Inspector!");
            return;
        }
        foreach (PlayingCardData cardData in allCardDatas)
        {
            if (cardData != null)
            {
                cardLookup[System.Tuple.Create(cardData.suit, cardData.rank)] = cardData;
            }
        }
        Debug.Log($"CardLibrary initialized with {cardLookup.Count} cards in lookup.");
    }

    public PlayingCardData GetCardData(NetworkPlayingCard networkCard)
    {
        return GetCardData(networkCard.suit, networkCard.rank);
    }

    public PlayingCardData GetCardData(CardSuit suit, CardRank rank)
    {
        PlayingCardData cardData;
        if (cardLookup.TryGetValue(System.Tuple.Create(suit, rank), out cardData))
        {
            return cardData;
        }
        Debug.LogWarning($"CardLibrary: Could not find CardData for {suit} {rank}");
        return null;
    }
}