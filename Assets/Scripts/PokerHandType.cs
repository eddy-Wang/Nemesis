using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PokerHandType
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    // RoyalFlush 可以在判断StraightFlush时特殊处理
}