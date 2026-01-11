using System;
using UnityEngine;

public enum Suit { Red, Black }

[Serializable]
public struct CardId
{
    public Suit suit;
    public int value; // 2-10
}