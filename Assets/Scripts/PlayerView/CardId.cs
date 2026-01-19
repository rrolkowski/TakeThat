using System;
using UnityEngine;

public enum Suit
{
    None,
    Green,
    Purple,
    Blue,
    Red
}

public enum CardType
{
    Number,
    Skip,
    Reverse,
    Draw2,
    Draw3
}

[Serializable]
public struct CardId
{
    public CardType type;
    public Suit suit;
    public int value;

    public bool IsNumber => type == CardType.Number;

    public override string ToString()
    {
        return IsNumber ? $"{suit} {value}" : $"{type}";
    }
}