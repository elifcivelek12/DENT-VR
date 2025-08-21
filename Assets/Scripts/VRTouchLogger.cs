using UnityEngine;
// XR Interaction Toolkit kütüphanelerini ekliyoruz
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Bu script'in bir XRRayInteractor bileþeni olan objeye eklenmesini zorunlu kýlar.
// Bu genellikle sizin "RightHand Controller" veya "LeftHand Controller" objenizdir.
[RequireComponent(typeof(XRRayInteractor))]
public class VRTouchLogger : MonoBehaviour
{
    private XRRayInteractor rayInteractor;

    void Awake()
    {
        // Script'in baðlý olduðu objedeki XRRayInteractor bileþenini alýyoruz.
        rayInteractor = GetComponent<XRRayInteractor>();
    }

    private void OnEnable()
    {
        // Ray Interactor'ýn 'selectEntered' olayýna abone oluyoruz.
        // Bu olay, kullanýcý bir objeye bakarken tetik tuþuna bastýðýnda tetiklenir.
        rayInteractor.selectEntered.AddListener(LogSelectedObject);
    }

    private void OnDisable()
    {
        // Hafýza sýzýntýlarýný önlemek için olay aboneliðini sonlandýrýyoruz.
        rayInteractor.selectEntered.RemoveListener(LogSelectedObject);
    }

    /// <summary>
    /// selectEntered olayý tetiklendiðinde çaðrýlan metot.
    /// </summary>
    /// <param name="args">Seçim olayýyla ilgili bilgileri içerir.</param>
    private void LogSelectedObject(SelectEnterEventArgs args)
    {
        // Etkileþime girilen (seçilen) objenin adýný alýyoruz.
        // args.interactableObject bize seçilen objenin kendisini verir.
        string objectName = args.interactableObject.transform.name;

        // Konsola hangi objenin seçildiðini yazdýrýyoruz.
        Debug.Log($"VR Kontrolcüsü ile dokunulan obje: {objectName}");
    }
}