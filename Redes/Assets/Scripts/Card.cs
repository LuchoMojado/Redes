using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [Range(1, 10)]
    public int value;
    public Suits suit;

    [SerializeField] Renderer _front, _back;

    float _baseTablePosX = 0.75f;
    float _baseTablePosY = 1;

    public enum Suits
    {
        Espada,
        Basto,
        Copa,
        Oro
    }

    public void PlaceInDeck()
    {
        TurnFaceDown();
        GameManager.instance.deck.Push(this);

        var deckPos = GameManager.instance.deckPos;
        transform.position = deckPos.position;
        transform.rotation = deckPos.rotation;
        //hacer corrutina de movimiento
    }

    public void PlaceOnTable()
    {
        Vector3 pos = Vector3.zero;

        int cardsOnTable = GameManager.instance.onTable.Count;

        int placement = cardsOnTable % 4;

        switch (placement)
        {
            case 0:
                pos = new Vector3(-_baseTablePosX, _baseTablePosY);
                break;
            case 1:
                pos = new Vector3(_baseTablePosX, _baseTablePosY);
                break;
            case 2:
                pos = new Vector3(-_baseTablePosX, -_baseTablePosY);
                break;
            case 3:
                pos = new Vector3(_baseTablePosX, -_baseTablePosY);
                break;
        }

        pos += pos * Mathf.FloorToInt(cardsOnTable * 0.25f);

        transform.position = pos;
        transform.rotation = Quaternion.identity;
    }

    public void Deal(Player player)
    {
        // flip only for this player
        transform.position = player.handPos[player.handSize].position;
        transform.rotation = player.handPos[player.handSize].rotation;
        player.BeDealt(this);
    }

    public void TurnFaceUp()
    {
        //animacion
        _front.enabled = true;
        _back.enabled = false;
    }

    public void TurnFaceDown()
    {
        //animacion
        _front.enabled = false;
        _back.enabled = true;
    }
}
