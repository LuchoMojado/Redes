using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class UIText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _cardsOnTableText;
    bool _textIsActive = true;

    private void Start()
    {
        SetCardTableText(GameManager.instance.onTable);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _cardsOnTableText.gameObject.SetActive(_textIsActive);

             _textIsActive = !_textIsActive;
        }
        
        if (!_textIsActive)
        {
            SetCardTableText(GameManager.instance.onTable);
        }
    }

    //IA Where, Select, OrderByDescending, TakeWhile, First
    public void SetCardTableText(IEnumerable<Card> cardsOnTable)
    {
        if (cardsOnTable.Count() <= 0) _cardsOnTableText.SetText("No cards on table");

        int highestGoldOnTable = cardsOnTable.Where(x => x.suit == Card.Suits.Oro).Select(x => x.value).OrderByDescending(x => x).First();

        int highestCardOnTable = cardsOnTable.Select(x => x.value).OrderBy(x => x).Last();

        //ARREGLAR
        int amountOfHighestCards = cardsOnTable/*.OfType<Card>()*/.Select(x => x.value).OrderByDescending(x => x).TakeWhile(x => x == cardsOnTable.Select(x => x.value).First()).Count();

        _cardsOnTableText.SetText("Carta mas alta de oro: " + highestGoldOnTable + "  Carta mas alta de la mesa: " + highestCardOnTable + ", y hay " + amountOfHighestCards + " con ese valor");
    }
}
