using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using UnityEngine.UI;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public GameObject startGameButton, playButton, pickUpButton, scoreButton;

    public List<Player> players = new List<Player>();

    public List<Card> onTable = new List<Card>();
    public Stack<Card> deck = new Stack<Card>();

    [HideInInspector] public Transform deckPos;

    [SerializeField] Card[] _allCards;
    [SerializeField] Transform _preGameDeckPos;
    [SerializeField] Text _displayText;

    public bool gameStarted = false;

    bool _wait = false;

    int _dealerIndex = 0, _activeIndex = 0, _remainingTurns = 0, _lastToPickUpIndex = 0;

    [Networked, Capacity(4)]
    public NetworkArray<int> scores { get; } = MakeInitializer(new int[] { 0, 0, 0, 0 });

    List<Card>[] earnedCards = { new List<Card>(), new List<Card>(), new List<Card>(), new List<Card>() };
    List<Card>[] brooms = { new List<Card>(), new List<Card>(), new List<Card>(), new List<Card>() };

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSetOnTable(Card card)
    {
        onTable.Add(card);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcRemoveFromTable(Card card)
    {
        onTable.Remove(card);

        if (!HasStateAuthority) return;

        for (int i = 0; i < onTable.Count; i++)
        {
            card = onTable[i];
            card.RpcMove(card.GetTablePos(i), Quaternion.identity);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcPlayerEarnsCards(int playerIndex, Card[] cardsToEarn)
    {
        earnedCards[playerIndex] = earnedCards[playerIndex].Concat(cardsToEarn).ToList();
        Debug.Log($"El jugador {playerIndex + 1} tiene {earnedCards[playerIndex].Count} cartas");
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcPlayerGetsBroom(int playerIndex, Card broom)
    {
        brooms[playerIndex].Add(broom);
        Debug.Log($"El jugador {playerIndex + 1} tiene {brooms[playerIndex].Count} escobas");
    }

    void GiveCardsOnTable(int playerIndex)
    {
        RpcPlayerEarnsCards(playerIndex, onTable.ToArray());

        while (onTable.Count > 0)
        {
            var card = onTable.First();
            RpcRemoveFromTable(card);
            var pos = players[playerIndex].earnedCardsPos;
            card.RpcSetVisibility(Card.Visibility.Hidden);
            card.RpcMove(pos.position, pos.rotation);
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
        players = players.OrderBy(x => x.playerNumber).ToList();
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

        _dealerIndex = UnityEngine.Random.Range(0, players.Count);

        StartCoroutine(StartRound());
    }

    IEnumerator StartRound()
    {
        if (!HasStateAuthority) yield break;

        RpcToggleScoreButton(true);

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
                    if (onTable.Count > 0) GiveCardsOnTable(_lastToPickUpIndex);

                    RpcToggleScoreButton(false);
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
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
            var copia = i;
            //players[i].RpcBrooms();
            var broomCount = brooms[copia].Count;

            for (int j = 0; j < broomCount; j++)
            {
                var otraCopia = j;
                var pos = players[copia].earnedCardsPos;
                brooms[copia][otraCopia].RpcSetVisibility(Card.Visibility.Visible);
                brooms[copia][otraCopia].RpcMoveAndReturn(pos.position + pos.right * (1.5f + 1.5f * otraCopia), pos.rotation, 7 - copia);
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {i + 1}: {broomCount}", false);
            RpcUpdateScore(i, broomCount);

            yield return new WaitForSeconds(1);
        }

        yield return new WaitForSeconds(4);

        RpcUpdateText("Cartas", true);

        yield return new WaitForSeconds(2);

        for (int i = 0; i < players.Count; i++)
        {
            var copia = i;
            var cards = earnedCards[copia].Count;

            if (cards > currentHighest)
            {
                currentHighest = cards;
                winnerIndex = copia;
                tied = false;
            }
            else if (cards == currentHighest)
            {
                tied = true;
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {copia + 1}: {cards}", false);

            yield return new WaitForSeconds(1);
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hubo un empate, nadie gana el punto", false);
        }
        else
        {
            RpcUpdateScore(winnerIndex, 1);
            RpcUpdateText($"{Environment.NewLine}El jugador {winnerIndex + 1} gana el punto", false);
        }

        yield return new WaitForSeconds(4);

        RpcUpdateText("7 de Oro", true);

        yield return new WaitForSeconds(2);

        for (int i = 0; i < players.Count; i++)
        {
            var copia = i;
            var card = CheckForGold7(earnedCards[copia], out bool gotIt);

            yield return new WaitForSeconds(1);

            if (gotIt)
            {
                RpcUpdateText($"{Environment.NewLine}El jugador {copia + 1} lo tiene", false);
                RpcUpdateScore(copia, 1);
                var pos = players[copia].earnedCardsPos;
                card.RpcSetVisibility(Card.Visibility.Visible);
                card.RpcMoveAndReturn(pos.position + pos.right * 1.5f, pos.rotation, 4);

                break;
            }
        }

        yield return new WaitForSeconds(4);

        currentHighest = 0;

        RpcUpdateText("Setenta", true);

        yield return new WaitForSeconds(2);

        Tuple<int, List<Card>> seventy;

        for (int i = 0; i < players.Count; i++)
        {
            var copia = i;
            seventy = Seventy(earnedCards[copia]);
            Debug.Log(copia);

            int total = seventy.Item1;
            Debug.Log(total);
            Debug.Log(seventy.Item2.Count);
            if (total > currentHighest)
            {
                currentHighest = total;
                winnerIndex = copia;
                tied = false;
            }
            else if (total == currentHighest)
            {
                tied = true;
            }

            foreach (var item in seventy.Item2)
            {
                var pos = players[copia].earnedCardsPos;
                item.RpcSetVisibility(Card.Visibility.Visible);
                item.RpcMoveAndReturn(pos.position + pos.right * (1.5f + 1.5f * (int)item.suit), pos.rotation, 10 - copia);
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {copia + 1}: sumó {total}", false);

            yield return new WaitForSeconds(1);
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hubo un empate, nadie gana el punto", false);
        }
        else
        {
            RpcUpdateScore(winnerIndex, 1);
            RpcUpdateText($"{Environment.NewLine}El jugador {winnerIndex + 1} gana el punto", false);
        }

        yield return new WaitForSeconds(7);

        currentHighest = 0;

        RpcUpdateText("Oro", true);

        yield return new WaitForSeconds(2);

        for (int i = 0; i < players.Count; i++)
        {
            var copia = i;
            var golds = Golds(earnedCards[copia]);

            int goldCount = golds.Count();

            if (goldCount > currentHighest)
            {
                currentHighest = goldCount;
                winnerIndex = copia;
                tied = false;
            }
            else if (goldCount == currentHighest)
            {
                tied = true;
            }

            for (int j = 0; j < goldCount; j++)
            {
                var otraCopia = j;
                var pos = players[copia].earnedCardsPos;
                golds[otraCopia].RpcSetVisibility(Card.Visibility.Visible);
                golds[otraCopia].RpcMoveAndReturn(pos.position + pos.right * (1.5f + 0.75f * otraCopia), pos.rotation, 8 - copia);
            }

            RpcUpdateText($"{Environment.NewLine}Jugador {i + 1}: {goldCount}", false);

            yield return new WaitForSeconds(1);
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hubo un empate, nadie gana el punto", false);
        }
        else
        {
            RpcUpdateScore(winnerIndex, 1);
            RpcUpdateText($"{Environment.NewLine}El jugador {winnerIndex + 1} gana el punto", false);
        }

        yield return new WaitForSeconds(5);

        RpcUpdateText("Puntaje", true);

        bool winner = false;
        tied = false;
        currentHighest = 0;

        for (int i = 0; i < players.Count; i++)
        {
            ResetLists();

            int score = scores.Get(i);

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
        }

        if (tied)
        {
            RpcUpdateText($"{Environment.NewLine}Hay empate, comienza ronda de desempate", false);
        }
        else if (winner)
        {
            RpcUpdateText($"{Environment.NewLine}Fin del juego, ganó el jugador {winnerIndex + 1}", false);

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

    void ResetLists()
    {
        for (int i = 0; i < players.Count; i++)
        {
            earnedCards[i] = new List<Card>();
            brooms[i] = new List<Card>();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdateText(string text, bool overwrite)
    {
        if (overwrite) _displayText.text = "";

        _displayText.text += text;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RpcUpdateScore(int index, int addedValue)
    {
        scores.Set(index, scores.Get(index) + addedValue);
    }

    public void ToggleScore(bool show)
    {
        if (show)
        {
            _displayText.text = "Puntaje:";

            for (int i = 0; i < players.Count; i++)
            {
                _displayText.text += $"{Environment.NewLine}Jugador {i + 1}: {scores.Get(i)}";
            }
        }
        else
        {
            _displayText.text = "";
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcToggleScoreButton(bool on)
    {
        scoreButton.SetActive(on);
    }

    Card CheckForGold7(IEnumerable<Card> cards, out bool gotIt)
    {
        var list = cards.Where(x => x.suit == Card.Suits.Oro && x.value == 7);

        if (list.Any())
        {
            gotIt = true;
            return list.First();
        }
        else
        {
            gotIt = false;
            return default;
        }
    }

    Tuple<int,List<Card>> Seventy(IEnumerable<Card> cards)
    {
        var seventies = cards.Where(x => x.value <= 7).OrderBy(x => x.suit).ThenByDescending(x => x.value)
            .Aggregate(Tuple.Create(-1, 0, new List<Card>()), (acum, current) =>
            {
                var result = acum.Item3;
                if ((int)current.suit > acum.Item1)
                {
                    result.Add(current);
                    acum = Tuple.Create((int)current.suit, acum.Item2 + current.value, result);
                }
                return acum;
            });

        return Tuple.Create(seventies.Item2, seventies.Item3);
    }

    List<Card> Golds(IEnumerable<Card> cards)
    {
        return cards.Where(x => x.suit == Card.Suits.Oro).ToList();
    }
}
