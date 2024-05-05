using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class Player : NetworkBehaviour
{
    public Transform[] handPos;

    Card[] hand = new Card[3];
    List<Card> selectedCards = new List<Card>();
    List<Card> earnedCards = new List<Card>();

    [Networked]
    public int handSize { get; set; } = 0;

    public Transform deckPos;
    public Transform earnedCardsPos;

    [SerializeField] LayerMask _cardLayer;

    [Networked]
    public bool _myTurn { get; set; } = false;

    bool _clicked = false;
    Ray _ray;

    public override void Spawned()
    {
        GameManager.instance.players.Add(this);
        GameManager.instance.SyncCards();
    }

    private void Update()
    {
        if (!_myTurn)
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
        hand[handSize] = card;
        card.TurnFaceUp();
        handSize++;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcStartTurn()
    {
        _myTurn = true;
    }

    void SelectCard(Card card)
    {
        print("entramos a select");

        bool inHand = hand.Contains(card);

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
            var selectedInHand = hand.Intersect(selectedCards);

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

        if (selectedCards.Count == 1 && inHand)
        {
            //permitir jugar
        }
        else if (CanPickUp())
        {
            //permitir levantar
        }
        else
        {
            //no permitir jugar
        }
    }

    void PlayCard()
    {
        hand.ToList().Remove(selectedCards.First());
    }

    bool CanPickUp()
    {
        return selectedCards.Select(x => x.value).Sum() == 15 && hand.Intersect(selectedCards).Any();
    }

    public void PickUp()
    {
        var handCard = hand.Intersect(selectedCards);
        hand.ToList().Remove(handCard.First());
        foreach (var item in selectedCards.Except(handCard))
        {
            GameManager.instance.onTable.Remove(item);
        }

        if (!GameManager.instance.onTable.Any())
        {
            //escoba
        }
        //mover selected a la pila de earned (y apagar?)
        earnedCards.Concat(selectedCards);
        
        selectedCards.Clear();
    }
}
