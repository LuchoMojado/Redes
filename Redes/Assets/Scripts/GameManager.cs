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

    public Transform deckPos;

    [SerializeField] Card[] _allCards;
    [SerializeField] Transform _preGameDeckPos;

    void Awake()
    {
        instance = this;

        PreGame();
    }

    void PreGame()
    {
        deckPos = _preGameDeckPos;

        foreach (var item in _allCards)
        {
            item.PlaceInDeck();
        }

        deck = deck.Shuffle().ToStack();
    }

    void StartGame()
    {
        StartRound();
    }

    void StartRound()
    {
        StartHand();

        for (int i = 0; i < 4; i++)
        {
            var card = deck.Pop();
            card.TurnFaceUp();
            card.PlaceOnTable();
            onTable.Add(card);
        }
    }

    void StartHand()
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
