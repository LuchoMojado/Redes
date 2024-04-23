using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [Min(1)]
    public int value;
    public Suit suit;

    public enum Suit
    {
        Espada,
        Basto,
        Copa,
        Oro
    }

    public void Deal(Player player)
    {
        // flip only for this player
        transform.position = player.handPos[player.handSize].position;
        transform.rotation = player.handPos[player.handSize].rotation;
        player.BeDealt(this);
    }

}
