using UnityEngine;

public class ButtonVisibilityTrigger : MonoBehaviour
{
    [Header("Objeler")]
    public GameObject object1;
    public GameObject object2;

    [Header("Bool Kayna��")]
    public BoolHolder boolSource;

    [Header("Buton ile G�nderilecek De�er")]
    public bool valueToSend; // Inspector'dan ayarlanacak

    // Butona atanacak fonksiyon
    public void Trigger()
    {
        if (boolSource != null)
        {
            // Bool'u d��ar�dan se�ilen de�ere ayarla
            boolSource.isVisible = valueToSend;
        }

        // An�nda g�r�n�rl�k uygula
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

