using UnityEngine;

// Butona basıldığında BoolHolder üzerinden görünürlük durumunu güncelleyen 
// ve sahnedeki objelerin aktifliğini kontrol eden sınıf
public class ButtonVisibilityTrigger : MonoBehaviour
{
    [Header("Objeler")]
    // Inspector üzerinden atanacak sahnedeki objeler
    public GameObject object1;
    public GameObject object2;

    [Header("Bool Kaynağı")]
    // Bool değerini dışarıya taşıyan script
    public BoolHolder boolSource;

    [Header("Buton ile Gönderilecek Değer")]
    // Inspector’dan ayarlanacak, butona basıldığında gönderilecek bool değeri
    public bool valueToSend; 

    // Unity butonuna atanacak fonksiyon
    public void Trigger()
    {
        // BoolHolder varsa, bool değerini güncelle
        if (boolSource != null)
        {
            // Bool'u dışarıdan seçilen değere ayarla
            boolSource.isVisible = valueToSend;
        }

        // Anında görünürlük uygula
        if (valueToSend)
        {
            // object1 aktif, object2 pasif
            object1.SetActive(true);
            object2.SetActive(false);
        }
        else
        {
            // object1 pasif, object2 aktif
            object1.SetActive(false);
            object2.SetActive(true);
        }
    }
}
