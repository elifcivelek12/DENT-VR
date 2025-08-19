using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class chairInteractionLog : MonoBehaviour
{
    // Kullanıcı kontrolcüsü bir nesnenin üzerine geldiğinde tetiklenir
    public void LogHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"[InteractionLogger] HOVER BAŞLADI: '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesinin üzerine geldi.");
    }

    // Kullanıcı kontrolcüsü nesneden ayrıldığında tetiklenir
    public void LogHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"[InteractionLogger] HOVER BİTTİ: '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesinin üzerinden ayrıldı.");
    }

    // Kullanıcı nesneyi tutmaya veya çekmeye başladığında tetiklenir
    public void LogSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[InteractionLogger] TUTMA/ÇEKME BAŞLADI (Select): '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesini tuttu.");
    }

    // Kullanıcı nesneyi bıraktığında tetiklenir
    public void LogSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"[InteractionLogger] TUTMA/ÇEKME BİTTİ (Select): '{args.interactorObject.transform.name}' isimli kontrolcü '{args.interactableObject.transform.name}' nesnesini bıraktı.");
    }
}
