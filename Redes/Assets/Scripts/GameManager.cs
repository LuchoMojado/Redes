using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

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

    bool _wait = false;

    int _dealerIndex = 0;

    //[Networked, OnChangedRender(nameof(PlayerJoined)), Capacity(4)]
    //public NetworkArray<Player> joinedPlayers { get; set; }
    //
    //void PlayerJoined()
    //{
    //    players.Add(joinedPlayers[Runner.ActivePlayers.Count() - 1]);
    //}

    void Awake()
    {
        if (instance != null) Destroy(gameObject);
        instance = this;

        //PreGame();
    }

    public void SyncCards()
    {
        foreach (var item in _allCards)
        {
            item.RpcSetVisibility(Card.Visibility.Syncing);
        }
    }

    public void PreGame()
    {
        if (!HasStateAuthority) return;

        deckPos = _preGameDeckPos;

        foreach (var item in _allCards)
        {
            item.RpcSetVisibility(Card.Visibility.Hidden);
            item.PlaceInDeck();
        }

        startGameButton.SetActive(false);

        deck = deck.Shuffle().ToStack();
    }

    public void Start()
    {
        if (!HasStateAuthority) return;
        StartCoroutine(StartGame());
        //startGameButton.SetActive(false);
    }

    public IEnumerator StartGame()
    {
        if (!HasStateAuthority) yield break;

        _dealerIndex = Random.Range(0, players.Count);
        deckPos = players[_dealerIndex].deckPos;

        foreach (var item in deck)
        {
            item.Move(deckPos);
        }

        while (deck.Peek().moving) yield return null;

        StartCoroutine(StartRound());
    }

    IEnumerator StartRound()
    {
        if (!HasStateAuthority) yield break;

        StartCoroutine(StartHand());

        while (_wait) yield return null;

        for (int i = 0; i < 4; i++)
        {
            var card = deck.Pop();
            card.RpcSetVisibility(Card.Visibility.Visible);
            card.PlaceOnTable();
            onTable.Add(card);

            while (card.moving) yield return null;
        }
    }

    IEnumerator StartHand()
    {
        if (!HasStateAuthority) yield break;

        _wait = true;

        var k = _dealerIndex + 1;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < players.Count; j++)
            {
                if (k >= players.Count)
                {
                    k = 0;
                }

                var card = deck.Pop();
                
                card.Deal(players[k]);

                while (card.moving) yield return null;

                k++;
            }
        }

        _wait = false;
    }
}
