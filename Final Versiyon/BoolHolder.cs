using UnityEngine;

public class BoolHolder : MonoBehaviour
{
    public bool isVisible;

    // Bu metodu ba�ka bir scriptten �a��r�rs�n
    public void TriggerVisibility(bool value)
    {
        isVisible = value;
    }
}
