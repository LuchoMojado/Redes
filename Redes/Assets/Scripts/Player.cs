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

    public override void Spawned()
    {
        GameManager.instance.players.Add(this);
        GameManager.instance.SyncCards();
    }

    private void Update()
    {
        if (GameManager.instance.activePlayer != this) return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, _cardLayer);

            if (hit.collider != null)
            {
                SelectCard(hit.transform.GetComponent<Card>());
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcBeDealt(Card card)
    {
        hand[handSize] = card;
        card.TurnFaceUp();
        handSize++;
    }

    void SelectCard(Card card)
    {
        bool inHand = hand.Contains(card);

        if (!GameManager.instance.onTable.Contains(card) || !inHand) return;

        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
            card.Deselect();
        }
        else
        {
            selectedCards.Add(card);
            card.Select();
        }

        if (hand.Contains(card))
        {
            inHand = true;

            var selectedInHand = hand.Intersect(selectedCards);

            if (selectedInHand.Count() > 1)
            {
                foreach (var item in selectedInHand)
                {
                    if (item != card) selectedCards.Remove(item);
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
