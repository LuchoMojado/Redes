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
        StartCoroutine(MoveAndRotate(deckPos));
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

        StartCoroutine(MoveAndRotate(pos, Quaternion.identity));
    }

    public void Deal(Player player)
    {
        // flip only for this player
        StartCoroutine(MoveAndRotate(player.handPos[player.handSize]));
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

    public void Move(Transform endPos)
    {
        StartCoroutine(MoveAndRotate(endPos));
    }

    IEnumerator MoveAndRotate(Transform endPos)
    {
        float startDist = Vector3.Distance(transform.position, endPos.position);

        while (transform.position != endPos.position)
        {
            float currentDist = Vector3.Distance(transform.position, endPos.position);

            float delta = 1 - Mathf.Pow(currentDist / startDist, 5.0f / 9.0f);

            transform.position = Vector3.Lerp(transform.position, endPos.position, delta / startDist);
            transform.rotation = Quaternion.Slerp(transform.rotation, endPos.rotation, delta / startDist);

            yield return null;
        }
    }

    public IEnumerator MoveAndRotate(Vector3 goalPosition, Quaternion goalRotation)
    {
        float startDist = Vector3.Distance(transform.position, goalPosition);

        while (transform.position != goalPosition)
        {
            float currentDist = Vector3.Distance(transform.position, goalPosition);

            float delta = 1 - Mathf.Pow(currentDist / startDist, 5.0f / 9.0f);

            transform.position = Vector3.MoveTowards(transform.position, goalPosition, delta / startDist);
            transform.rotation = Quaternion.Slerp(transform.rotation, goalRotation, delta / startDist);

            yield return null;
        }
    }
}
