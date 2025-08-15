using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class chairInteractionLog : MonoBehaviour
{

    public void LogHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"[InteractionLogger] HOVER BA�LADI: '{args.interactorObject.transform.name}' isimli kontrolc� '{args.interactableObject.transform.name}' nesnesinin �zerine geldi.");
    }

    public void LogHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"[InteractionLogger] HOVER B�TT�: '{args.interactorObject.transform.name}' isimli kontrolc� '{args.interactableObject.transform.name}' nesnesinin �zerinden ayr�ld�.");
    }

    public void LogSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[InteractionLogger] TUTMA/�EKME BA�LADI (Select): '{args.interactorObject.transform.name}' isimli kontrolc� '{args.interactableObject.transform.name}' nesnesini tuttu.");
    }

    public void LogSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"[InteractionLogger] TUTMA/�EKME B�TT� (Select): '{args.interactorObject.transform.name}' isimli kontrolc� '{args.interactableObject.transform.name}' nesnesini b�rakt�.");
    }



}
