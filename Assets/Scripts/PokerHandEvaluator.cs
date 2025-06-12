using System.Collections.Generic;
using System.Linq;

public static class PokerHandEvaluator
{
    /// <summary>
    /// 评估一手牌的最终牌型。
    /// 它的判断顺序遵循从高到低的原则，以确保返回最高等级的可能牌型。
    /// </summary>
    /// <param name="hand">需要被评估的手牌列表</param>
    /// <returns>这手牌对应的牌型</returns>
    public static PokerHandType EvaluateHand(List<NetworkPlayingCard> hand)
    {
        // 初始防御性编程：处理空手牌或无效输入
        if (hand == null || hand.Count == 0) return PokerHandType.HighCard;

        // 为了方便评估，先按点数从小到大排序
        var sortedHand = hand.OrderBy(card => card.rank).ToList();

        // 预先计算好是否为同花和顺子。
        // 重构后的辅助方法现在包含了严格的牌数检查。
        bool isFlush = IsFlush(sortedHand);
        bool isStraight = IsStraight(sortedHand);

        // 1. 同花顺 (最高优先级)
        // 只有当一手牌同时是同花和顺子时，它才是同花顺。
        if (isStraight && isFlush)
        {
            return PokerHandType.StraightFlush;
        }

        // 2. 四条 (金刚)
        // 使用LINQ的GroupBy来按点数分组，然后检查是否有任意一组的数量为4。
        var rankGroups = sortedHand.GroupBy(card => card.rank)
                                     .ToDictionary(group => group.Key, group => group.Count());
        if (rankGroups.ContainsValue(4))
        {
            return PokerHandType.FourOfAKind;
        }

        // 3. 葫芦 (满堂彩)
        // 当手牌中同时存在一个三条和一个对子时，构成葫芦。
        if (rankGroups.ContainsValue(3) && rankGroups.ContainsValue(2))
        {
            return PokerHandType.FullHouse;
        }

        // 4. 同花
        // isFlush变量已经经过了严格的5张牌检查。
        if (isFlush)
        {
            return PokerHandType.Flush;
        }

        // 5. 顺子
        // isStraight变量也经过了严格的5张牌和连续性检查。
        if (isStraight)
        {
            return PokerHandType.Straight;
        }

        // 6. 三条
        if (rankGroups.ContainsValue(3))
        {
            return PokerHandType.ThreeOfAKind;
        }

        // 7. 两对
        // 检查点数分组中，数量为2的组是否有两个。
        if (rankGroups.Count(kvp => kvp.Value == 2) == 2)
        {
            return PokerHandType.TwoPair;
        }

        // 8. 一对
        if (rankGroups.ContainsValue(2))
        {
            return PokerHandType.Pair;
        }

        // 9. 高牌
        // 如果以上所有牌型都不满足，则为高牌。
        return PokerHandType.HighCard;
    }

    /// <summary>
    /// 【已修复】检查一手牌是否为同花。
    /// </summary>
    private static bool IsFlush(List<NetworkPlayingCard> hand)
    {
        // 核心修复：同花必须且只能由5张牌组成。这个检查修复了“单张同花”的bug。
        if (hand.Count != 5) return false;

        // 获取第一张牌的花色
        var firstSuit = hand[0].suit;
        // 使用LINQ的All方法，检查是否所有牌的花色都与第一张相同。
        return hand.All(card => card.suit == firstSuit);
    }

    /// <summary>
    /// 【已重构】检查一手牌是否为顺子，逻辑更清晰健壮。
    /// </summary>
    private static bool IsStraight(List<NetworkPlayingCard> hand)
    {
        // 核心修复：顺子必须且只能由5张牌组成。这防止了由3张或4张牌组成的“假顺子”。
        if (hand.Count != 5) return false;

        // 检查标准顺子 (例如：5, 6, 7, 8, 9)
        // hand 已经被排序，所以我们只需检查相邻牌的点数是否相差1。
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

        // 如果不是标准顺子，则需要特殊处理 A-2-3-4-5 (A作为1) 的情况。
        // 因为手牌已排序，A会排在最后。所以我们直接检查牌的点数是否精确匹配。
        bool isAceLowStraight = hand[0].rank == CardRank.Two &&
                                 hand[1].rank == CardRank.Three &&
                                 hand[2].rank == CardRank.Four &&
                                 hand[3].rank == CardRank.Five &&
                                 hand[4].rank == CardRank.Ace;

        return isAceLowStraight;
    }
}
