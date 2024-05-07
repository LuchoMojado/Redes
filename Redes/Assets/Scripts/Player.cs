using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class Player : NetworkBehaviour
{
    public Transform[] handPos;

    List<Card> _hand = new List<Card>();
    List<Card> selectedCards = new List<Card>();
    List<Card> earnedCards = new List<Card>();
    List<Card> brooms = new List<Card>();

    [Networked]
    public int handSize { get; set; } = 0;

    [Networked]
    public int playerNumber { get; set; }

    public Transform deckPos;
    public Transform earnedCardsPos;

    [SerializeField] LayerMask _cardLayer;

    [Networked]
    public bool myTurn { get; set; } = false;

    [Networked]
    public int broomCount { get; set; } = 0;

    [Networked]
    public int cardCount { get; set; } = 0;

    [Networked]
    public bool hasGold7 { get; set; } = false;

    [Networked]
    public int seventy { get; set; } = 0;

    [Networked]
    public int golds { get; set; } = 0;

    bool _clicked = false;
    Ray _ray;

    public override void Spawned()
    {
        GameManager.instance.players.Add(this);
        GameManager.instance.SyncCards();
    }

    private void Update()
    {
        if (!myTurn)
        {
            print("no soy el player activo");
            return;
        }

        print("soy el player activo");

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            _ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            _clicked = true;
            print("click en update");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_clicked)
        {
            print("click en network update");

            RaycastHit2D hit = Runner.GetPhysicsScene2D().Raycast(_ray.origin, _ray.direction, Mathf.Infinity, _cardLayer);

            if (hit.collider != null)
            {
                print("hit");
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
        print("entramos a select");

        bool inHand = _hand.Contains(card);

        print("esta en mi mano");

        if (!GameManager.instance.onTable.Contains(card) && !inHand) return;

        print("la carta es valida");

        if (selectedCards.Contains(card))
        {
            print("la carta ya estaba seleccionada, la deselecciono");
            selectedCards.Remove(card);
            card.Deselect();
        }
        else
        {
            print("selecciono la carta");
            selectedCards.Add(card);
            card.Select();
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
                        print("otra carta en la mano estaba seleccionada, deselecciono");
                        selectedCards.Remove(item);
                        item.Deselect();
                    }
                }
            }
        }

        if (selectedCards.Count == 1 && _hand.Intersect(selectedCards).Any())
        {
            GameManager.instance.playButton.SetActive(true);
            GameManager.instance.pickUpButton.SetActive(false);
        }
        else if (CanPickUp())
        {
            GameManager.instance.playButton.SetActive(false);
            GameManager.instance.pickUpButton.SetActive(true);
        }
        else
        {
            GameManager.instance.playButton.SetActive(false);
            GameManager.instance.pickUpButton.SetActive(false);
        }
    }

    public IEnumerator PlayCard()
    {
        GameManager.instance.playButton.SetActive(false);
        var card = selectedCards.First();
        selectedCards.Clear();
        RemoveFromHand(card);
        card.RpcPlaceOnTable();
        card.RpcSetVisibility(Card.Visibility.Visible);
        card.Deselect();

        while (card.moving) yield return null;

        myTurn = false;
        GameManager.instance.RpcNextPlayerTurn(false);
    }

    bool CanPickUp()
    {
        return selectedCards.Select(x => x.value).Sum() == 15 && _hand.Intersect(selectedCards).Any();
    }

    public IEnumerator PickUp()
    {
        GameManager.instance.pickUpButton.SetActive(false);

        earnedCards = earnedCards.Concat(selectedCards).ToList();

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
            brooms.Add(handCard);
            handCard.Deselect();

            Vector3 earnedEulerRotation = earnedCardsPos.rotation.eulerAngles;
            Quaternion endRotation = Quaternion.Euler(earnedEulerRotation.x, earnedEulerRotation.y, earnedEulerRotation.z + 90);

            handCard.RpcMove(earnedCardsPos.position, endRotation);

            selectedCards.Remove(handCard);

            while (handCard.moving) yield return null;
        }

        foreach (var item in selectedCards)
        {
            item.RpcSetVisibility(Card.Visibility.Hidden);
            item.Deselect();
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcGetCardsLeftOnTable()
    {
        earnedCards = earnedCards.Concat(GameManager.instance.onTable).ToList();

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
            StartCoroutine(ReturnToEarnedStack(item, 4));
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
                StartCoroutine(ReturnToEarnedStack(item, 4));
                hasGold7 = true;

                return;
            }
        }

        hasGold7 = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcSeventy()
    {
        int total = 0;
        var sevenOrLower = earnedCards.Where(x => x.value <= 7).OrderBy(x => x.suit).ThenByDescending(x => x.value);

        var espada = sevenOrLower.First();
        if (espada.suit == Card.Suits.Espada)
        {
            total += espada.value;
            espada.RpcSetVisibility(Card.Visibility.Visible);
            espada.RpcMove(handPos[1].position - handPos[1].right * 2.25f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(espada, 7));
        }

        var basto = sevenOrLower.SkipWhile(x => x.suit < Card.Suits.Basto).First();
        if (basto.suit == Card.Suits.Basto)
        {
            total += basto.value;
            basto.RpcSetVisibility(Card.Visibility.Visible);
            basto.RpcMove(handPos[1].position - handPos[1].right * 0.75f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(basto, 7));
        }

        var copa = sevenOrLower.SkipWhile(x => x.suit < Card.Suits.Copa).First();
        if (copa.suit == Card.Suits.Copa)
        {
            total += copa.value;
            copa.RpcSetVisibility(Card.Visibility.Visible);
            copa.RpcMove(handPos[1].position + handPos[1].right * 0.75f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(copa, 7));
        }

        var oro = sevenOrLower.SkipWhile(x => x.suit < Card.Suits.Oro).First();
        if (oro.suit == Card.Suits.Oro)
        {
            total += oro.value;
            oro.RpcSetVisibility(Card.Visibility.Visible);
            oro.RpcMove(handPos[1].position + handPos[1].right * 2.25f, transform.rotation);
            StartCoroutine(ReturnToEarnedStack(oro, 7));
        }

        seventy = total;
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
                StartCoroutine(ReturnToEarnedStack(item, 5));
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
    }
}
