using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public GameObject startGameButton;

    [HideInInspector] public Player activePlayer = null;
    public List<Player> players = new List<Player>();

    [HideInInspector] public List<Card> onTable = new List<Card>();
    public Stack<Card> deck = new Stack<Card>();

    [HideInInspector] public Transform deckPos;

    [SerializeField] Card[] _allCards;
    [SerializeField] Transform _preGameDeckPos;

    int _dealerIndex = 0;

    void Awake()
    {
        if (instance != null) Destroy(gameObject);
        instance = this;
    }

    public void PreGame()
    {
        //if (!HasStateAuthority) return;

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
            card.SetVisibility(true);
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
