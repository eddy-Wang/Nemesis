// NetworkPlayingCard.cs
// 这个结构体将用于在网络间传输卡牌信息。
// Mirror 可以直接序列化只包含简单类型的结构体。
[System.Serializable] // 通常Mirror不需要这个，但有时有益
public struct NetworkPlayingCard
{
    public CardSuit suit;
    public CardRank rank;

    // 构造函数
    public NetworkPlayingCard(CardSuit suit, CardRank rank)
    {
        this.suit = suit;
        this.rank = rank;
    }

    // 为了方便在List中查找或比较，可以重写Equals和GetHashCode
    public override bool Equals(object obj)
    {
        return obj is NetworkPlayingCard other &&
               suit == other.suit &&
               rank == other.rank;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(suit, rank);
    }

    public override string ToString()
    {
        return $"{rank} of {suit}";
    }
}