using UnityEngine;

public class ButtonVisibilityTrigger : MonoBehaviour
{
    [Header("Objeler")]
    public GameObject object1;
    public GameObject object2;

    [Header("Bool Kaynaðý")]
    public BoolHolder boolSource;

    [Header("Buton ile Gönderilecek Deðer")]
    public bool valueToSend; // Inspector'dan ayarlanacak

    // Butona atanacak fonksiyon
    public void Trigger()
    {
        if (boolSource != null)
        {
            // Bool'u dýþarýdan seçilen deðere ayarla
            boolSource.isVisible = valueToSend;
        }

        // Anýnda görünürlük uygula
        if (valueToSend)
        {
            object1.SetActive(true);
            object2.SetActive(false);
        }
        else
        {
            object1.SetActive(false);
            object2.SetActive(true);
        }
    }
}

