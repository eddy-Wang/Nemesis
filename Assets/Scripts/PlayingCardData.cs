// PlayingCardData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New PlayingCard", menuName = "Cards/Playing Card Data")]
public class PlayingCardData : ScriptableObject
{
    public CardSuit suit;
    public CardRank rank;
    public Sprite cardImage; // 用于链接您的PNG图片
    public string cardName; // 例如 "Ace of Spades", "红桃K"

    // 可选：用于牌型评估的数值，A可以特殊处理
    public int GetRankValue()
    {
        if (rank >= CardRank.Two && rank <= CardRank.Ten)
        {
            return (int)rank;
        }
        else if (rank >= CardRank.Jack && rank <= CardRank.King)
        {
            return 10; // 或者 Jack=11, Queen=12, King=13, Ace=14/1 用于比较
        }
        else if (rank == CardRank.Ace)
        {
            return 11; // 或者14，根据您的牌型比较规则
        }
        // else if (rank == CardRank.Joker_Small || rank == CardRank.Joker_Big) ...
        return 0; // 默认或大小王
    }

    void OnValidate() // 当在Inspector中修改值时自动更新cardName
    {
        if (cardImage != null && string.IsNullOrEmpty(cardName))
        {
            cardName = cardImage.name; // 尝试用图片文件名作为默认卡牌名
        }
        // 你也可以根据suit和rank自动生成更规范的名称
        // cardName = $"{rank} of {suit}";
    }
}