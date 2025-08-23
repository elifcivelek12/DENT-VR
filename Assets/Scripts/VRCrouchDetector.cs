// Gerekli using satırını ekliyoruz
using UnityEngine.InputSystem;
using UnityEngine;
using System;

public class VRCrouchDetector : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform head;             // XR Origin içindeki Main Camera (VR baş)

    [Header("Ayarlar")]
    [Range(0.5f, 0.95f)]
    public float crouchRatio = 0.75f;  // Ayaktaki yüksekliğin yüzde kaçı altına inerse "eğildi" saysın

    [Header("Input Actions")]
    [Tooltip("Yeniden kalibrasyon yapmak için kullanılacak Input Action.")]
    public InputActionReference recalibrateAction; 

    // STATIC EVENTS
    public static event Action OnCrouchStartStatic;
    public static event Action OnCrouchEndStatic;

    private bool isCrouching;
    private float standingHeight;      // Oyun başında otomatik ölçülür

    private void OnEnable()
    {
        // Input aksiyonunu dinlemeye başla
        if (recalibrateAction != null)
        {
            recalibrateAction.action.Enable();
            recalibrateAction.action.performed += Recalibrate;
        }
    }

    private void OnDisable()
    {
        // Komponent kapatıldığında veya yok edildiğinde dinlemeyi bırak
        if (recalibrateAction != null)
        {
            recalibrateAction.action.performed -= Recalibrate;
            recalibrateAction.action.Disable();
        }
    }

    void Start()
    {
        if (head == null)
        {
            Debug.LogError("[VRCrouchDetector] Head referansı atanmamış! XR Origin > Camera Offset > Main Camera objesini sürükleyip bırakın.");
            enabled = false;
            return;
        }

        // Oyun başında bulunduğun pozisyonu "ayakta" kabul et
        CalibrateStandingHeight();
    }

    void Update()
    {
        float currentYPosition = head.localPosition.y;
        float crouchThreshold = standingHeight * crouchRatio;

        // Eğilme durumunu kontrol et
        if (!isCrouching && currentYPosition < crouchThreshold)
        {
            isCrouching = true;
            OnCrouchStartStatic?.Invoke(); // Eğilme başlangıcı event'ini tetikle
        }
        else if (isCrouching && currentYPosition >= crouchThreshold)
        {
            isCrouching = false;
            OnCrouchEndStatic?.Invoke(); // Eğilme bitişi event'ini tetikle
        }
    }

    /// <summary>
    /// Input Action tetiklendiğinde çağrılan metot.
    /// </summary>
    private void Recalibrate(InputAction.CallbackContext context)
    {
        CalibrateStandingHeight();
    }

    /// <summary>
    /// O anki kafa pozisyonunu "ayakta durma yüksekliği" olarak ayarlar.
    /// </summary>
    private void CalibrateStandingHeight()
    {
        standingHeight = head.localPosition.y;
        Debug.Log($"[Yeniden Kalibrasyon] Yeni ayakta durma yüksekliği: {standingHeight:F2}");
    }
}