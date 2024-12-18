using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using System;

public class Player : NetworkBehaviour, IAfterSpawned
{
    [Networked]
    public PlayerRef playerRef { get; set; }

    [SerializeField] Sprite _readyImage, _cancelImage;

    public Transform[] handPos;

    List<Card> _hand = new List<Card>();
    List<Card> selectedCards = new List<Card>();

    [Networked]
    public int handSize { get; set; } = 0;

    [Networked]
    public int playerNumber { get; set; }

    [Networked]
    public bool ready { get; set; } = false;

    public Transform deckPos;
    public Transform earnedCardsPos;

    [SerializeField] LayerMask _cardLayer;

    [Networked]
    public bool myTurn { get; set; } = false;

    bool _clicked = false, _showScore = false;
    Ray _ray;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcReset()
    {
        ready = false;
        myTurn = false;
        handSize = 0;
        _hand.Clear();
        selectedCards.Clear();

        GameManager.instance.throwButton.SetActive(false);
        GameManager.instance.pickUpButton.SetActive(false);
        GameManager.instance.ready.image.sprite = _readyImage;
        GameManager.instance.RpcReadyCheck();
    }

    public override void Spawned()
    {
        if (GameManager.instance.gameStarted) return;

        Debug.Log("Spawned");
        StartCoroutine(SpawnedWait());
    }

    public void AfterSpawned()
    {
        //GameManager.instance.ready.onClick.AddListener(ToggleReady);
        //GameManager.instance.disconnect.onClick.AddListener(Disconnect);
        //GameManager.instance.SyncCards();
        //GameManager.instance.PreGame();
        //GameManager.instance.players.Add((this, playerRef));
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (playerNumber == 0 && !HasStateAuthority)
        {
            GameManager.instance.StartingPlayerLeft();
        }

        GameManager.instance.players.RemoveAt(playerNumber);
        GameManager.instance.ArrangePlayers();

        base.Despawned(runner, hasState);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcUpdateNumber(int newNumber)
    {
        playerNumber = newNumber;

        transform.position = GameManager.instance.playerSpawns[newNumber].position;
        transform.rotation = GameManager.instance.playerSpawns[newNumber].rotation;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcSetPlayerRef(PlayerRef pRef)
    {
        playerRef = pRef;
    }

    IEnumerator SpawnedWait()
    {
        yield return new WaitForSeconds(2);

        Debug.Log("SpawnedWaitDone");
        GameManager.instance.ready.onClick.AddListener(ToggleReady);
        GameManager.instance.disconnect.onClick.AddListener(Disconnect);
        GameManager.instance.SyncCards();
        GameManager.instance.PreGame();
        GameManager.instance.players.Add((this, playerRef));
        //RpcAsignPlayer(this, playerRef);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcAssignPlayer(Player player, PlayerRef pRef)
    {
        Debug.Log("Assigned");
        
        GameManager.instance.players.Add((player, pRef));
    }

    private void Update()
    {
        if (!myTurn)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            _ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            _clicked = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_clicked)
        {
            RaycastHit2D hit = Runner.GetPhysicsScene2D().Raycast(_ray.origin, _ray.direction, Mathf.Infinity, _cardLayer);

            if (hit.collider != null)
            {
                SelectCard(hit.transform.GetComponent<Card>());
            }

            _clicked = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcBeDealt(Card card)
    {
        _hand.Add(card);
        card.TurnFaceUp();
        handSize++;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcStartTurn()
    {
        myTurn = true;
    }

    void SelectCard(Card card)
    {
        bool inHand = _hand.Contains(card);

        if (!GameManager.instance.onTable.Contains(card) && !inHand) return;

        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
            card.RpcDeselect();
        }
        else
        {
            selectedCards.Add(card);
            card.RpcSelect();
        }

        if (inHand)
        {
            var selectedInHand = _hand.Intersect(selectedCards);

            if (selectedInHand.Count() > 1)
            {
                foreach (var item in selectedInHand)
                {
                    if (item != card)
                    {
                        selectedCards.Remove(item);
                        item.RpcDeselect();
                    }
                }
            }
        }

        if (selectedCards.Count == 1 && _hand.Intersect(selectedCards).Any())
        {
            GameManager.instance.throwButton.SetActive(true);
            GameManager.instance.pickUpButton.SetActive(false);
        }
        else if (CanPickUp(selectedCards, _hand))
        {
            GameManager.instance.throwButton.SetActive(false);
            GameManager.instance.pickUpButton.SetActive(true);
        }
        else
        {
            GameManager.instance.throwButton.SetActive(false);
            GameManager.instance.pickUpButton.SetActive(false);
        }
    }

    public IEnumerator PlayCard()
    {
        GameManager.instance.throwButton.SetActive(false);
        var card = selectedCards.First();
        selectedCards.Clear();
        RemoveFromHand(card);
        card.RpcPlaceOnTable();
        card.RpcSetVisibility(Card.Visibility.Visible);
        card.RpcDeselect();

        while (card.moving) yield return null;

        myTurn = false;
        GameManager.instance.RpcNextPlayerTurn(false);
    }

    // IA Select, Any
    bool CanPickUp(IEnumerable<Card> selected, IEnumerable<Card> inHand)
    {
        return selected.Select(x => x.value).Sum() == 15 && inHand.Intersect(selected).Any();
    }

    public IEnumerator PickUp()
    {
        GameManager.instance.pickUpButton.SetActive(false);

        GameManager.instance.RpcPlayerEarnsCards(playerNumber, selectedCards.ToArray());

        var handCards = _hand.Intersect(selectedCards);
        var handCard = handCards.First();
        handCard.RpcSetVisibility(Card.Visibility.Visible);
        RemoveFromHand(handCard);

        yield return new WaitForSeconds(1);

        foreach (var item in selectedCards.Except(handCards))
        {
            GameManager.instance.RpcRemoveFromTable(item);
        }

        if (!GameManager.instance.onTable.Any())
        {
            GameManager.instance.RpcPlayerGetsBroom(playerNumber, handCard);
            handCard.RpcDeselect();

            Vector3 earnedEulerRotation = earnedCardsPos.rotation.eulerAngles;
            Quaternion endRotation = Quaternion.Euler(earnedEulerRotation.x, earnedEulerRotation.y, earnedEulerRotation.z + 90);

            handCard.RpcMove(earnedCardsPos.position, endRotation);

            selectedCards.Remove(handCard);

            while (handCard.moving) yield return null;
        }

        foreach (var item in selectedCards)
        {
            item.RpcSetVisibility(Card.Visibility.Hidden);
            item.RpcDeselect();
            item.RpcMove(earnedCardsPos.position, earnedCardsPos.rotation);
        }

        while (selectedCards.First().moving) yield return null;
        
        selectedCards.Clear();

        myTurn = false;
        GameManager.instance.RpcNextPlayerTurn(true);
    }

    void RemoveFromHand(Card card)
    {
        _hand.Remove(card);

        handSize--;
    }

    public void ToggleScore()
    {
        _showScore = !_showScore;

        GameManager.instance.ToggleScore(_showScore);
    }

    public void ToggleReady()
    {
        if (!HasStateAuthority) return;

        ready = !ready;

        if (ready) GameManager.instance.ready.image.sprite = _cancelImage;
        else GameManager.instance.ready.image.sprite = _readyImage;

        GameManager.instance.RpcReadyCheck();
    }

    public void Disconnect()
    {
        Destroy(Runner.gameObject);
        ScenesManager.instance.ChangeScene("MainMenu");
        //StartCoroutine(WaitToMenu(Runner.Shutdown()));
        //GameManager.instance.RpcDisconnectPlayer(playerNumber);
    }

    IEnumerator WaitToMenu(System.Threading.Tasks.Task shutdown)
    {
        yield return new WaitWhile(() => shutdown.Status == System.Threading.Tasks.TaskStatus.Running);

        Destroy(Runner.gameObject);
        
        Debug.Log("completed");
        ScenesManager.instance.ChangeScene("MainMenu");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcClaimGamemanager()
    {
        GameManager.instance.Object.RequestStateAuthority();
    }

    /*[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcGetCardsLeftOnTable()
    {
        GameManager.instance.RpcPlayerEarnsCards(playerNumber, GameManager.instance.onTable.ToArray());

        while (GameManager.instance.onTable.Count > 0)
        {
            var card = GameManager.instance.onTable.First();
            GameManager.instance.RpcRemoveFromTable(card);
            card.RpcSetVisibility(Card.Visibility.Hidden);
            card.RpcMove(earnedCardsPos.position, earnedCardsPos.rotation);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcBrooms()
    {
        int counter = 0;

        foreach (var item in brooms)
        {
            counter++;
            item.RpcMove(earnedCardsPos.position + earnedCardsPos.right * (1.5f * counter), transform.rotation);
            StartCoroutine(ReturnToEarnedStack(item, 5));
        }

        broomCount = counter;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcTotalEarnedCards()
    {
        cardCount = earnedCards.Count;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcGoldSeven()
    {
        foreach (var item in earnedCards)
        {
            if (item.value == 7 && item.suit == Card.Suits.Oro)
            {
                item.RpcSetVisibility(Card.Visibility.Visible);
                item.RpcMove(handPos[1].position, transform.rotation);
                StartCoroutine(ReturnToEarnedStack(item, 5));
                hasGold7 = true;

                return;
            }
        }

        hasGold7 = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcSeventy()
    {
        //int total = 0;
        var sevenOrLower = earnedCards.Where(x => x.value <= 7).OrderBy(x => x.suit).ThenByDescending(x => x.value);

        var seventies = sevenOrLower.Aggregate(Tuple.Create(-1, 0, new List<Card>()), (acum, current) =>
        {
            var result = acum.Item3;
            if ((int)current.suit > acum.Item1)
            {
                result.Add(current);
                acum = Tuple.Create((int)current.suit, acum.Item2 + current.value, result);
            }
            return acum;
        });

        foreach (var item in seventies.Item3)
        {
            item.RpcSetVisibility(Card.Visibility.Visible);
            item.RpcMove(handPos[1].position - handPos[1].right * 2.25f + handPos[1].right * (1.5f * (int)item.suit), transform.rotation);
            StartCoroutine(ReturnToEarnedStack(item, 8));
        }

        var espada = sevenOrLower.First();
        if (espada.suit == Card.Suits.Espada)
        {
            total += espada.value;
            espada.RpcSetVisibility(Card.Visibility.Visible);
            espada.RpcMove(handPos[1].position - handPos[1].right * 2.25f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(espada, 8));
        }

        var basto = sevenOrLower.SkipWhile(x => x.suit < Card.Suits.Basto).First();
        if (basto.suit == Card.Suits.Basto)
        {
            total += basto.value;
            basto.RpcSetVisibility(Card.Visibility.Visible);
            basto.RpcMove(handPos[1].position - handPos[1].right * 0.75f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(basto, 8));
        }

        var copa = sevenOrLower.SkipWhile(x => x.suit < Card.Suits.Copa).First();
        if (copa.suit == Card.Suits.Copa)
        {
            total += copa.value;
            copa.RpcSetVisibility(Card.Visibility.Visible);
            copa.RpcMove(handPos[1].position + handPos[1].right * 0.75f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(copa, 8));
        }

        var oro = sevenOrLower.SkipWhile(x => x.suit < Card.Suits.Oro).First();
        if (oro.suit == Card.Suits.Oro)
        {
            total += oro.value;
            oro.RpcSetVisibility(Card.Visibility.Visible);
            oro.RpcMove(handPos[1].position + handPos[1].right * 2.25f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(oro, 8));
        }

        seventy = seventies.Item2;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcGolds()
    {
        int counter = 0;

        foreach (var item in earnedCards)
        {
            if (item.suit == Card.Suits.Oro)
            {
                item.RpcSetVisibility(Card.Visibility.Visible);
                item.RpcMove(earnedCardsPos.position + earnedCardsPos.right * (1.5f + 0.75f * counter), transform.rotation);
                StartCoroutine(ReturnToEarnedStack(item, 6));
                counter++;
            }
        }

        golds = counter;
    }

    public IEnumerator ReturnToEarnedStack(Card card, float delay = 0)
    {
        yield return new WaitForSeconds(delay);

        card.RpcMove(earnedCardsPos.position, earnedCardsPos.rotation);
        card.RpcSetVisibility(Card.Visibility.Hidden);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcClearLists()
    {
        earnedCards.Clear();
        brooms.Clear();
    }*/


}
