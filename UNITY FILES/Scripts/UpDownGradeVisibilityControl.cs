using UnityEngine;

public class UpDownGradeVisibilityControl : MonoBehaviour
{
    // Görünürlüğü kontrol edilecek iki GameObject
    public GameObject object1;
    public GameObject object2;

    // Dışarıdan alınacak boolean değeri ile görünürlüğü kontrol edecek kaynak
    public BoolHolder boolSource;

    // Bool değerine göre objelerin aktiflik durumunu uygular
    public void ApplyVisibility()
    {
        if (boolSource.isVisible)
        {
            object1.SetActive(true);  // Bool true ise object1 görünür
            object2.SetActive(false); // object2 gizlenir
        }
        else
        {
            object1.SetActive(false); // Bool false ise object1 gizlenir
            object2.SetActive(true);  // object2 görünür
        }
    }
}
