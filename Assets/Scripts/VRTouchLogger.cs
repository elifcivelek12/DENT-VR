using UnityEngine;
// XR Interaction Toolkit k�t�phanelerini ekliyoruz
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Bu script'in bir XRRayInteractor bile�eni olan objeye eklenmesini zorunlu k�lar.
// Bu genellikle sizin "RightHand Controller" veya "LeftHand Controller" objenizdir.
[RequireComponent(typeof(XRRayInteractor))]
public class VRTouchLogger : MonoBehaviour
{
    private XRRayInteractor rayInteractor;

    void Awake()
    {
        // Script'in ba�l� oldu�u objedeki XRRayInteractor bile�enini al�yoruz.
        rayInteractor = GetComponent<XRRayInteractor>();
    }

    private void OnEnable()
    {
        // Ray Interactor'�n 'selectEntered' olay�na abone oluyoruz.
        // Bu olay, kullan�c� bir objeye bakarken tetik tu�una bast���nda tetiklenir.
        rayInteractor.selectEntered.AddListener(LogSelectedObject);
    }

    private void OnDisable()
    {
        // Haf�za s�z�nt�lar�n� �nlemek i�in olay aboneli�ini sonland�r�yoruz.
        rayInteractor.selectEntered.RemoveListener(LogSelectedObject);
    }

    /// <summary>
    /// selectEntered olay� tetiklendi�inde �a�r�lan metot.
    /// </summary>
    /// <param name="args">Se�im olay�yla ilgili bilgileri i�erir.</param>
    private void LogSelectedObject(SelectEnterEventArgs args)
    {
        // Etkile�ime girilen (se�ilen) objenin ad�n� al�yoruz.
        // args.interactableObject bize se�ilen objenin kendisini verir.
        string objectName = args.interactableObject.transform.name;

        // Konsola hangi objenin se�ildi�ini yazd�r�yoruz.
        Debug.Log($"VR Kontrolc�s� ile dokunulan obje: {objectName}");
    }
}