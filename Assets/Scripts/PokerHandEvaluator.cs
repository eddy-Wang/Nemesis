using System.Collections.Generic;
using System.Linq;

public static class PokerHandEvaluator
{
    public static PokerHandType EvaluateHand(List<NetworkPlayingCard> hand)
    {
        // Initial defensive programming: handle empty hand or invalid input
        if (hand == null || hand.Count == 0) return PokerHandType.HighCard;

        // For easier evaluation, first sort by rank in ascending order
        var sortedHand = hand.OrderBy(card => card.rank).ToList();

        // Pre-calculate whether it's a flush and a straight.
        // The refactored helper methods now include strict card count checks.
        bool isFlush = IsFlush(sortedHand);
        bool isStraight = IsStraight(sortedHand);

        // 1. Straight Flush (Highest priority)
        // A hand is a Straight Flush only if it is both a straight and a flush.
        if (isStraight && isFlush)
        {
            return PokerHandType.StraightFlush;
        }

        // 2. Four of a Kind
        // Use LINQ's GroupBy to group by rank, then check if any group has a count of 4.
        var rankGroups = sortedHand.GroupBy(card => card.rank)
                                   .ToDictionary(group => group.Key, group => group.Count());
        if (rankGroups.ContainsValue(4))
        {
            return PokerHandType.FourOfAKind;
        }

        // 3. Full House
        // A Full House is formed when the hand contains both a Three of a Kind and a Pair.
        if (rankGroups.ContainsValue(3) && rankGroups.ContainsValue(2))
        {
            return PokerHandType.FullHouse;
        }

        // 4. Flush
        // The isFlush variable has already undergone a strict 5-card check.
        if (isFlush)
        {
            return PokerHandType.Flush;
        }

        // 5. Straight
        // The isStraight variable has also undergone strict 5-card and consecutiveness checks.
        if (isStraight)
        {
            return PokerHandType.Straight;
        }

        // 6. Three of a Kind
        if (rankGroups.ContainsValue(3))
        {
            return PokerHandType.ThreeOfAKind;
        }

        // 7. Two Pair
        // Check if there are two groups with a count of 2 among the rank groups.
        if (rankGroups.Count(kvp => kvp.Value == 2) == 2)
        {
            return PokerHandType.TwoPair;
        }

        // 8. Pair
        if (rankGroups.ContainsValue(2))
        {
            return PokerHandType.Pair;
        }

        // 9. High Card
        // If none of the above hand types are met, it's a High Card.
        return PokerHandType.HighCard;
    }

    /// <summary>
    /// [Fixed] Checks if a hand is a flush.
    /// </summary>
    private static bool IsFlush(List<NetworkPlayingCard> hand)
    {
        // Core fix: A flush must consist of exactly 5 cards. This fixes the "single card flush" bug.
        if (hand.Count != 5) return false;

        // Get the suit of the first card
        var firstSuit = hand[0].suit;
        // Use LINQ's All method to check if all cards have the same suit as the first one.
        return hand.All(card => card.suit == firstSuit);
    }

    /// <summary>
    /// [Refactored] Checks if a hand is a straight, with clearer and more robust logic.
    /// </summary>
    private static bool IsStraight(List<NetworkPlayingCard> hand)
    {
        // Core fix: A straight must consist of exactly 5 cards. This prevents "fake straights" formed by 3 or 4 cards.
        if (hand.Count != 5) return false;

        // Check for standard straight (e.g., 5, 6, 7, 8, 9)
        // The hand is already sorted, so we just need to check if adjacent cards have a rank difference of 1.
        bool isStandardStraight = true;
        for (int i = 0; i < hand.Count - 1; i++)
        {
            if (hand[i + 1].rank != hand[i].rank + 1)
            {
                isStandardStraight = false;
                break;
            }
        }
        if (isStandardStraight) return true;

        // If not a standard straight, then we need to specifically handle A-2-3-4-5 (Ace as 1).
        // Since the hand is sorted, Ace will be at the end. So we check if the ranks precisely match.
        bool isAceLowStraight = hand[0].rank == CardRank.Two &&
                                 hand[1].rank == CardRank.Three &&
                                 hand[2].rank == CardRank.Four &&
                                 hand[3].rank == CardRank.Five &&
                                 hand[4].rank == CardRank.Ace;

        return isAceLowStraight;
    }
}