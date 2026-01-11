using System;
using UnityEngine;

public enum Suit
{
    Green,
    Purple
}

[Serializable]
public struct CardId
{
    public Suit suit;
    public int value; // 2-10
}