using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : NetworkBehaviour
{
    [Range(1, 10)]
    public int value;
    public Suits suit;

    [SerializeField] Renderer _front, _back;

    Animator _anim;

    float _lerpTime = 1;

    float _baseTablePosX = 0.75f;
    float _baseTablePosY = 1;

    Vector3 _goTo;
    Quaternion _rotateTo;

    float _timer;

    public bool moving;

    public enum Suits
    {
        Espada,
        Basto,
        Copa,
        Oro
    }

    public enum Visibility
    {
        Visible,
        Hidden,
        Syncing
    }

    Visibility _wasVisible;

    [Networked, OnChangedRender(nameof(TurnCard))]
    public Visibility visibility { get; set; }

    void TurnCard()
    {
        switch (visibility)
        {
            case Visibility.Visible:
                StartCoroutine(TurnUpAnim());
                break;
            case Visibility.Hidden:
                StartCoroutine(TurnDownAnim());
                break;
            case Visibility.Syncing:
                StartCoroutine(SyncVisibility());
                break;
            default:
                break;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcSetVisibility(Visibility visible)
    {
        _wasVisible = visibility;
        visibility = visible;
    }

    private void Awake()
    {
        _goTo = transform.position;
        _rotateTo = transform.rotation;

        _anim = GetComponent<Animator>();
    }

    public override void FixedUpdateNetwork()
    {
        if (Vector3.Distance(transform.position, _goTo) > 0.01f || transform.rotation != _rotateTo)
        {
            _timer += Runner.DeltaTime;

            transform.position = Vector3.Lerp(transform.position, _goTo, _timer / _lerpTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, _rotateTo, _timer / _lerpTime);
        }
        else
        {
            moving = false;
        }
    }

    public void PlaceInDeck()
    {
        GameManager.instance.deck.Push(this);

        var deckPos = GameManager.instance.deckPos;
        Move(deckPos);
    }

    public void PlaceOnTable()
    {
        Vector3 pos = Vector3.zero;

        int cardsOnTable = GameManager.instance.onTable.Count;
        print("la cantidad de cartas en la mesa es: " + cardsOnTable);
        int placement = cardsOnTable % 4;

        switch (placement)
        {
            case 0:
                pos = new Vector3(-_baseTablePosX, _baseTablePosY);
                break;
            case 1:
                pos = new Vector3(-_baseTablePosX, -_baseTablePosY);
                break;
            case 2:
                pos = new Vector3(_baseTablePosX, _baseTablePosY);
                break;
            case 3:
                pos = new Vector3(_baseTablePosX, -_baseTablePosY);
                break;
        }

        pos += new Vector3(pos.x * Mathf.FloorToInt(cardsOnTable * 0.25f) * 2, 0);
        
        Move(pos, Quaternion.identity);

        GameManager.instance.RpcSetOnTable(this);
    }

    public void Deal(Player player)
    {
        Move(player.handPos[player.handSize]);
        player.RpcBeDealt(this);
    }

    public void Select()
    {
        _anim.SetBool("Selected", true);
        print("me seleccionaron");
    }

    public void Deselect()
    {
        _anim.SetBool("Selected", false);
        print("me deseleccionaron");
    }

    public void TurnFaceUp()
    {
        _front.enabled = true;
        _back.enabled = false;
    }

    public void TurnFaceDown()
    {
        _front.enabled = false;
        _back.enabled = true;
    }

    public void Move(Transform endPos)
    {
        _timer = 0;

        _goTo = endPos.position;
        _rotateTo = endPos.rotation;

        moving = true;
    }

    public void Move(Vector3 endPosition, Quaternion endRotation)
    {
        _timer = 0;

        _goTo = endPosition;
        _rotateTo = endRotation;

        moving = true;
    }

    IEnumerator TurnUpAnim()
    {
        _anim.SetTrigger("TurnUp");

        yield return new WaitForSeconds(_anim.GetCurrentAnimatorClipInfo(0).Length * 0.5f);

        TurnFaceUp();
    }

    IEnumerator TurnDownAnim()
    {
        _anim.SetTrigger("TurnDown");

        yield return new WaitForSeconds(_anim.GetCurrentAnimatorClipInfo(0).Length * 0.5f);

        TurnFaceDown();
    }

    IEnumerator SyncVisibility()
    {
        print("start sync");
        yield return new WaitForSeconds(0.4f);
        print("finished sync");
        RpcSetVisibility(Visibility.Hidden);
    }
}
