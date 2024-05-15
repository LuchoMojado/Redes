using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class UIText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _cardsOnTableText;
    bool _textIsActive = true;


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

    //IA OfType,Where, Select, OrderByDescending, TakeWhile, First
    public void SetCardTableText(IEnumerable<Card> cardsOnTable)
    {
        if (cardsOnTable.Count() <= 0) _cardsOnTableText.SetText("No cards on table");

        cardsOnTable.OfType<Card>().Select(x => x.value).OrderByDescending(x => x).TakeWhile(x => x == cardsOnTable.Select(x => x.value).First()).ToList();


        foreach (var item in cardsOnTable)
        {
            _cardsOnTableText.SetText("Highest Card on table: " + item);
        }
    }
}
