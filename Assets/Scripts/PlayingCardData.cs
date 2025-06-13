// PlayingCardData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New PlayingCard", menuName = "Cards/Playing Card Data")]
public class PlayingCardData : ScriptableObject
{
    public CardSuit suit;
    public CardRank rank;
    public Sprite cardImage;
    public string cardName;

    public int GetRankValue()
    {
        if (rank >= CardRank.Two && rank <= CardRank.Ten)
        {
            return (int)rank;
        }
        else if (rank >= CardRank.Jack && rank <= CardRank.King)
        {
            return 10; 
        }
        else if (rank == CardRank.Ace)
        {
            return 11;
        }
        return 0; 
    }

    void OnValidate() 
    {
        if (cardImage != null && string.IsNullOrEmpty(cardName))
        {
            cardName = cardImage.name;
        }
    }
}