using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class chairInteractionLog : MonoBehaviour
{

    public void LogHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"[InteractionLogger] HOVER BAÞLADI: '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesinin üzerine geldi.");
    }

    public void LogHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"[InteractionLogger] HOVER BÝTTÝ: '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesinin üzerinden ayrýldý.");
    }

    public void LogSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[InteractionLogger] TUTMA/ÇEKME BAÞLADI (Select): '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesini tuttu.");
    }

    public void LogSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"[InteractionLogger] TUTMA/ÇEKME BÝTTÝ (Select): '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesini býraktý.");
    }



}
