using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameObject startGameButton;

    public Transform[] playerSpawns;

    [HideInInspector] public Player activePlayer = null;
    [HideInInspector] public List<Player> players = new List<Player>();

    [HideInInspector] public List<Card> onTable = new List<Card>();
    public Stack<Card> deck = new Stack<Card>();

    [HideInInspector] public Transform deckPos;

    [SerializeField] Card[] _allCards;
    [SerializeField] Transform _preGameDeckPos;

    int _dealerIndex = 0;

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

    public void StartGame()
    {
        _dealerIndex = Random.Range(0, players.Count);

        foreach (var item in deck)
        {
            item.Move(players[_dealerIndex].deckPos);
        }

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
        var k = _dealerIndex + 1;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < players.Count; j++)
            {
                if (k >= players.Count)
                {
                    k = 0;
                }

                deck.Pop().Deal(players[k]);

                k++;
            }
        }
    }
}
