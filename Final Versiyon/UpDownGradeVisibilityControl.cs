using UnityEngine;

public class UpDownGradeVisibilityControl : MonoBehaviour
{
    public GameObject object1;
    public GameObject object2;
    public BoolHolder boolSource;

    public void ApplyVisibility()
    {
        if (boolSource.isVisible)
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

