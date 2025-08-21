using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabLogger : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log($"{gameObject.name} tutuldu!");
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        Debug.Log($"{gameObject.name} býrakýldý!");
    }
}