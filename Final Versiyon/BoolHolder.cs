using UnityEngine;

public class BoolHolder : MonoBehaviour
{
    public bool isVisible;

    // Bu metodu baþka bir scriptten çaðýrýrsýn
    public void TriggerVisibility(bool value)
    {
        isVisible = value;
    }
}
