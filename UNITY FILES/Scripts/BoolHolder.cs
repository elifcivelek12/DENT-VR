using UnityEngine;

// Basit bir boolean (true/false) değerini tutan sınıf
// Diğer scriptler bu değer üzerinden kontrol yapabilir.
public class BoolHolder : MonoBehaviour
{
    // Dışarıdan Inspector üzerinden ya da scriptlerle erişilebilecek görünürlük durumu
    public bool isVisible;

    // Bu metodu başka bir script çağırarak isVisible değerini değiştirebilir.
    // Örn: TriggerVisibility(true) → görünür, TriggerVisibility(false) → gizli
    public void TriggerVisibility(bool value)
    {
        isVisible = value;
    }
}
