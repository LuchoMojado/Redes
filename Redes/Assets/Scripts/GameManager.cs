using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEngine.Rendering.Universal;
using Unity.Collections.LowLevel.Unsafe;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public GameObject startGameButton, playButton, pickUpButton;

    public List<Player> players = new List<Player>();

    public List<Card> onTable = new List<Card>();
    public Stack<Card> deck = new Stack<Card>();

    [HideInInspector] public Transform deckPos;

    [SerializeField] Card[] _allCards;
    [SerializeField] Transform _preGameDeckPos;
    [SerializeField] Text _resultsText, _scoreText;

    /*[Networked, OnChangedRender(nameof(UpdateText))]
    public string displayText { get; set; }

    void UpdateText()
    {
        _text.text = displayText;
    }*/

    public bool gameStarted = false;

    bool _wait = false;

    int _dealerIndex = 0, _activeIndex = 0, _remainingTurns = 0, _lastToPickUpIndex = 0;

    [Networked, Capacity(4)]
    public NetworkArray<int> scores { get; set; } = MakeInitializer(new int[] { 0, 0, 0, 0 });

    //[Networked, OnChangedRender(nameof(PlayerJoined)), Capacity(4)]
    //public NetworkArray<Player> joinedPlayers { get; set; }
    //
    //void PlayerJoined()
    //{
    //    players.Add(joinedPlayers[Runner.ActivePlayers.Count() - 1]);
    //}

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSetOnTable(Card card)
    {
        onTable.Add(card);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcRemoveFromTable(Card card)
    {
        onTable.Remove(card);

        if (HasStateAuthority)
        {
            for (int i = 0; i < onTable.Count; i++)
            {
                card = onTable[i];
                card.RpcMove(card.GetTablePos(i), Quaternion.identity);
            }
        }
    }

    void Awake()
    {
        if (instance != null) Destroy(gameObject);
        instance = this;
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
            item.RpcMove(deckPos.position, deckPos.rotation);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcStartGame()
    {
        startGameButton.SetActive(false);
        gameStarted = true;
        StartCoroutine(StartGame());
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcTurnOnStartGameButton()
    {
        startGameButton.SetActive(true);
        gameStarted = false;
    }

    public IEnumerator StartGame()
    {
        if (!HasStateAuthority) yield break;

        players.OrderBy(x => x.playerNumber);

        _dealerIndex = UnityEngine.Random.Range(0, players.Count);

        StartCoroutine(StartRound());
    }

    IEnumerator StartRound()
    {
        if (!HasStateAuthority) yield break;

        foreach (var item in _allCards)
        {
            item.RpcSetVisibility(Card.Visibility.Hidden);
            item.PlaceInDeck();
        }

        deck = deck.Shuffle().ToStack();

        _dealerIndex++;
        if (_dealerIndex >= players.Count) _dealerIndex = 0;

        deckPos = players[_dealerIndex].deckPos;

        foreach (var item in deck)
        {
            item.RpcMove(deckPos.position, deckPos.rotation);
        }

        while (deck.Peek().moving) yield return null;

        StartCoroutine(StartHand());

        while (_wait) yield return null;

        for (int i = 0; i < 4; i++)
        {
            var card = deck.Pop();
            card.RpcSetVisibility(Card.Visibility.Visible);
            card.RpcPlaceOnTable();

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
                if (k >= players.Count) k = 0;

                var card = deck.Pop();
                
                card.Deal(players[k]);

                while (card.moving) yield return null;

                k++;
            }
        }

        if (k >= players.Count) k = 0;
        players[k].RpcStartTurn();
        _activeIndex = k;

        _remainingTurns = 3;

        _wait = false;
    }

    public void ActivePlaysCard()
    {
        foreach (var item in players)
        {
            if (item.myTurn)
            {
                StartCoroutine(item.PlayCard());
                break;
            }
        }
    }

    public void ActivePicksUp()
    {
        foreach (var item in players)
        {
            if (item.myTurn)
            {
                StartCoroutine(item.PickUp());
                break;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcNextPlayerTurn(bool pickedUp)
    {
        if (pickedUp) _lastToPickUpIndex = _activeIndex;

        if (_activeIndex == _dealerIndex)
        {
            _remainingTurns--;

            if (_remainingTurns <= 0)
            {
                if (deck.Count <= 0)
                {
                    if (onTable.Count > 0) players[_lastToPickUpIndex].RpcGetCardsLeftOnTable();

                    RpcCountPoints();

                    return;
                }

                StartCoroutine(StartHand());
                return;
            }
        }

        _activeIndex++;
        if (_activeIndex >= players.Count) _activeIndex = 0;
        players[_activeIndex].RpcStartTurn();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcCountPoints()
    {
        StartCoroutine(CountingPoints());
    }

    IEnumerator CountingPoints()
    {
        int currentHighest = 0, winnerIndex = 0;
        bool tied = false;

        yield return new WaitForSeconds(2);

        RpcUpdateText("Escobas", true);

        yield return new WaitForSeconds(2);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcBrooms();
            var brooms = players[i].broomCount;

            yield return new WaitForSeconds(2);

            RpcUpdateText($"{Environment.NewLine}Jugador {i + 1}: {brooms}", false);
            //_text.text += $"{Environment.NewLine}Jugador {i + 1}: {brooms}";
            scores.Set(i, scores.Get(i) + brooms);
        }

        yield return new WaitForSeconds(4);

        //_text.text = "Cartas";
        RpcUpdateText("Cartas", true);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcTotalEarnedCards();
            var cards = players[i].cardCount;

            yield return new WaitForSeconds(2);

            if (cards > currentHighest)
            {
                currentHighest = cards;
                winnerIndex = i;
                tied = false;
            }
            else if (cards == currentHighest)
            {
                tied = true;
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {i + 1}: {cards}", false);
            //_text.text += $"{Environment.NewLine}Jugador {i + 1}: {cards}";
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hubo un empate, nadie gana el punto", false);
            //_text.text += $"{Environment.NewLine}Hubo un empate, nadie gana el punto";
        }
        else
        {
            scores.Set(winnerIndex, scores.Get(winnerIndex) + 1);
            RpcUpdateText($"{Environment.NewLine}El jugador {winnerIndex + 1} gana el punto", false);
            //_text.text += $"{Environment.NewLine}El jugador {winnerIndex + 1} gana el punto";
        }

        yield return new WaitForSeconds(4);

        RpcUpdateText("7 de Oro", true);
        //_text.text = "7 de Oro";

        yield return new WaitForSeconds(2);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcGoldSeven();

            yield return new WaitForSeconds(2);

            if (players[i].hasGold7)
            {
                RpcUpdateText($"{Environment.NewLine}El jugador {i + 1} lo tiene", false);
                scores.Set(i, scores.Get(i) + 1);

                break;
            }
        }

        yield return new WaitForSeconds(4);

        currentHighest = 0;

        RpcUpdateText("Setenta", true);
        //_text.text = "Setenta";

        yield return new WaitForSeconds(2);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcSeventy();

            yield return new WaitForSeconds(2);

            int total = players[i].seventy;

            if (total > currentHighest)
            {
                currentHighest = total;
                winnerIndex = i;
                tied = false;
            }
            else if (total == currentHighest)
            {
                tied = true;
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {i + 1}: sumó {total}", false);
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hubo un empate, nadie gana el punto", false);
            //_text.text += $"{Environment.NewLine}Hubo un empate, nadie gana el punto";
        }
        else
        {
            scores.Set(winnerIndex, scores.Get(winnerIndex) + 1);
            RpcUpdateText($"{Environment.NewLine}El jugador {winnerIndex + 1} gana el punto", false);
        }

        yield return new WaitForSeconds(7);

        currentHighest = 0;

        RpcUpdateText("Oro", true);
        //_text.text = "Oro";

        yield return new WaitForSeconds(2);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcGolds();

            yield return new WaitForSeconds(2);

            int golds = players[i].golds;

            if (golds > currentHighest)
            {
                currentHighest = golds;
                winnerIndex = i;
                tied = false;
            }
            else if (golds == currentHighest)
            {
                tied = true;
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {i + 1}: {golds}", false);
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hubo un empate, nadie gana el punto", false);
            //_text.text += $"{Environment.NewLine}Hubo un empate, nadie gana el punto";
        }
        else
        {
            scores.Set(winnerIndex, scores.Get(winnerIndex) + 1);
            RpcUpdateText($"{Environment.NewLine}El jugador {winnerIndex + 1} gana el punto", false);
        }

        yield return new WaitForSeconds(5);

        RpcUpdateText("Puntaje", true);
        //_text.text = "Puntaje";

        bool winner = false;
        tied = false;
        currentHighest = 0;

        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcClearLists();
            int score = scores[i];

            if (score >= 15)
            {
                winner = true;
                winnerIndex = i;
                tied = false;

                if (currentHighest == score)
                {
                    tied = true;
                }

                currentHighest = score;
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {i + 1}: {score}", false);
            //_text.text += $"{Environment.NewLine}Jugador {i + 1}: {score}";
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hay empate, comienza ronda de desempate", false);
            //_text.text += $"{Environment.NewLine}Hay empate, comienza ronda de desempate";
        }
        else if (winner)
        {
            RpcUpdateText($"{Environment.NewLine}Fin del juego, ganó el jugador {winnerIndex + 1}", false);
            //_text.text += $"{Environment.NewLine}Fin del juego, ganó el jugador {winnerIndex + 1}";

            yield return new WaitForSeconds(5);

            RpcUpdateText("", true);

            PreGame();
            RpcTurnOnStartGameButton();

            yield break;
        }

        yield return new WaitForSeconds(5);

        RpcUpdateText("", true);

        StartCoroutine(StartRound());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdateText(string text, bool overwrite)
    {
        if (overwrite) _resultsText.text = "";

        _resultsText.text += text;
    }
}
