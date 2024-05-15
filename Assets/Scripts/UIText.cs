using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class UIText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _goldCardsOnTableText;
    [SerializeField] TextMeshProUGUI _highestCardsOnTableText;
    bool _textIsActive;

    private void Start()
    {
        SetCardTableText(GameManager.instance.onTable);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _goldCardsOnTableText.gameObject.SetActive(_textIsActive);
            _highestCardsOnTableText.gameObject.SetActive(_textIsActive);

             _textIsActive = !_textIsActive;
        }
        
        if (!_textIsActive)
        {
            SetCardTableText(GameManager.instance.onTable);
        }
    }

    //IA Where, Select, OrderByDescending, OrderBy, TakeWhile, First, Last
    public void SetCardTableText(IEnumerable<Card> cardsOnTable)
    {
        if (cardsOnTable.Count() <= 0)
        {
            _goldCardsOnTableText.SetText("No hay cartas en la mesa");
            _highestCardsOnTableText.SetText("");
            return;
        }

        int highestGoldOnTable=111;
        
        foreach (var item in cardsOnTable)
        {
            if (item.suit != Card.Suits.Oro) continue;
            
            highestGoldOnTable = cardsOnTable.Where(x => x.suit == Card.Suits.Oro).Select(x => x.value).OrderByDescending(x => x).First();
        }

        if (highestGoldOnTable == 111) highestGoldOnTable = 0;


        int highestCardOnTable = cardsOnTable.Select(x => x.value).OrderBy(x => x).Last();

        int amountOfHighestCards = cardsOnTable/*.OfType<Card>()*/.Select(x => x.value).OrderByDescending(x => x).TakeWhile(x => x == highestCardOnTable).Count();

        _goldCardsOnTableText.SetText("Carta mas alta de oro: " + highestGoldOnTable);
        _highestCardsOnTableText.SetText("Carta mas alta de la mesa: " + highestCardOnTable + ". Hay " + amountOfHighestCards + " con ese valor");
    }
}
