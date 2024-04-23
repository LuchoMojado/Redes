using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Transform[] playerSpawns;

    public Player activePlayer = null;
    public List<Player> players = new List<Player>();

    public List<Card> onTable = new List<Card>();
    public Stack<Card> deck = new Stack<Card>();

    void Awake()
    {
        instance = this;
    }

    void StartGame()
    {
        StartRound();

        for (int i = 0; i < 4; i++)
        {
            // (prender?) mover a los lugares de la mesa
            onTable.Add(deck.Pop());
        }
    }

    void StartRound()
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (var item in players)
            {
                deck.Pop().Deal(item);
            }
        }
    }
}
