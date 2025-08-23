using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NPCGazeController : MonoBehaviour
{
    [Header("1. Temel Hedefler")]
    [Tooltip("Karakterin varsayýlan olarak bakacaðý hedef (Oyuncunun VR Kamerasý). BU ALAN ASLA BOÞ OLMAMALI!")]
    public Transform playerHead;

    [Header("2. Bakýþ Ayarlarý")]
    [Tooltip("Bakýþýn hedefe ne kadar odaklanacaðýný belirler (0: hiç bakmaz, 1: tamamen kilitlenir).")]
    [Range(0, 1)]
    public float lookAtWeight = 1.0f;

    // --- YENÝ EKLENEN AYAR ---
    [Tooltip("Nesne býrakýldýktan sonra karakterin o nesneyi kaç saniye izleyeceðini belirler.")]
    public float firlatmaIzlemeSuresi = 2.5f;

    // --- Kodun iç deðiþkenleri ---
    private Animator animator;
    private Transform currentTarget;
    private Coroutine temporaryLookCoroutine; // Zamanlayýcý görevini saklamak için

    void Start()
    {
        animator = GetComponent<Animator>();
        currentTarget = playerHead; // Oyun baþlangýcýnda oyuncuya bak

        // Baþlangýç kontrolleri
        if (playerHead == null)
        {
            Debug.LogError("HATA: NPCGazeController'daki 'Player Head' alaný boþ!", this.gameObject);
        }
        if (animator.GetBoneTransform(HumanBodyBones.Head) == null)
        {
            Debug.LogError("HATA: Karakterin kafa kemiði bulunamadý! Lütfen rig tipinin 'Humanoid' olduðunu kontrol edin.", this.gameObject);
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (currentTarget == null) return;

        // SetLookAtWeight'e dinamik bir deðer vererek bakýþlarý daha yumuþak açýp kapatabiliriz.
        // Þimdilik sabit býrakýyoruz.
        animator.SetLookAtWeight(lookAtWeight);
        animator.SetLookAtPosition(currentTarget.position);
    }

    /// <summary>
    /// Karakterin bakacaðý ana hedefi deðiþtirir.
    /// </summary>
    /// <param name="newTarget">Yeni hedef. Eðer null ise varsayýlan hedefe döner.</param>
    public void SetGazeTarget(Transform newTarget)
    {
        // Eðer devam eden bir "geçici bakýþ" görevi varsa, onu iptal et.
        // Bu, oyuncu nesneyi fýrlattýktan hemen sonra yeni bir nesne alýrsa önemlidir.
        if (temporaryLookCoroutine != null)
        {
            StopCoroutine(temporaryLookCoroutine);
            temporaryLookCoroutine = null;
        }

        if (newTarget != null)
        {
            // Yeni bir hedef varsa (örn: oyuncu küpü tuttu), ona bak.
            currentTarget = newTarget;
        }
        else
        {
            // Eðer yeni hedef boþsa (örn: oyuncu küpü býraktý), varsayýlan hedefe (oyuncuya) geri dön.
            currentTarget = playerHead;
        }
    }

    /// <summary>
    /// Nesne býrakýldýðýnda çaðrýlacak YENÝ fonksiyon.
    /// Nesneyi bir süre izler, sonra oyuncuya döner.
    /// </summary>
    public void StartTemporaryGaze(Transform temporaryTarget)
    {
        // Eðer çalýþan bir zamanlayýcý varsa durdur.
        if (temporaryLookCoroutine != null)
        {
            StopCoroutine(temporaryLookCoroutine);
        }
        // Yeni zamanlayýcýyý baþlat.
        temporaryLookCoroutine = StartCoroutine(LookAtForDuration(temporaryTarget));
    }

    /// <summary>
    /// Belirli bir süre bir hedefe bakýp sonra varsayýlan hedefe dönen zamanlayýcý.
    /// </summary>
    private IEnumerator LookAtForDuration(Transform tempTarget)
    {
        // 1. Adým: Geçici hedefe (fýrlatýlan küpe) bak.
        currentTarget = tempTarget;

        // 2. Adým: Inspector'da belirlediðimiz süre kadar bekle.
        yield return new WaitForSeconds(firlatmaIzlemeSuresi);

        // 3. Adým: Süre doldu, artýk varsayýlan hedefe (oyuncuya) geri dön.
        currentTarget = playerHead;

        // 4. Adým: Zamanlayýcý görevini temizle.
        temporaryLookCoroutine = null;
    }
}