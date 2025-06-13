[System.Serializable]
public struct NetworkPlayingCard
{
    public CardSuit suit;
    public CardRank rank;

    public NetworkPlayingCard(CardSuit suit, CardRank rank)
    {
        this.suit = suit;
        this.rank = rank;
    }

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