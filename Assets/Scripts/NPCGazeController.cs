using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NPCGazeController : MonoBehaviour
{
    [Header("1. Temel Hedefler")]
    [Tooltip("Karakterin varsay�lan olarak bakaca�� hedef (Oyuncunun VR Kameras�). BU ALAN ASLA BO� OLMAMALI!")]
    public Transform playerHead;

    [Header("2. Bak�� Ayarlar�")]
    [Tooltip("Bak���n hedefe ne kadar odaklanaca��n� belirler (0: hi� bakmaz, 1: tamamen kilitlenir).")]
    [Range(0, 1)]
    public float lookAtWeight = 1.0f;

    // --- YEN� EKLENEN AYAR ---
    [Tooltip("Nesne b�rak�ld�ktan sonra karakterin o nesneyi ka� saniye izleyece�ini belirler.")]
    public float firlatmaIzlemeSuresi = 2.5f;

    // --- Kodun i� de�i�kenleri ---
    private Animator animator;
    private Transform currentTarget;
    private Coroutine temporaryLookCoroutine; // Zamanlay�c� g�revini saklamak i�in

    void Start()
    {
        animator = GetComponent<Animator>();
        currentTarget = playerHead; // Oyun ba�lang�c�nda oyuncuya bak

        // Ba�lang�� kontrolleri
        if (playerHead == null)
        {
            Debug.LogError("HATA: NPCGazeController'daki 'Player Head' alan� bo�!", this.gameObject);
        }
        if (animator.GetBoneTransform(HumanBodyBones.Head) == null)
        {
            Debug.LogError("HATA: Karakterin kafa kemi�i bulunamad�! L�tfen rig tipinin 'Humanoid' oldu�unu kontrol edin.", this.gameObject);
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (currentTarget == null) return;

        // SetLookAtWeight'e dinamik bir de�er vererek bak��lar� daha yumu�ak a��p kapatabiliriz.
        // �imdilik sabit b�rak�yoruz.
        animator.SetLookAtWeight(lookAtWeight);
        animator.SetLookAtPosition(currentTarget.position);
    }

    /// <summary>
    /// Karakterin bakaca�� ana hedefi de�i�tirir.
    /// </summary>
    /// <param name="newTarget">Yeni hedef. E�er null ise varsay�lan hedefe d�ner.</param>
    public void SetGazeTarget(Transform newTarget)
    {
        // E�er devam eden bir "ge�ici bak��" g�revi varsa, onu iptal et.
        // Bu, oyuncu nesneyi f�rlatt�ktan hemen sonra yeni bir nesne al�rsa �nemlidir.
        if (temporaryLookCoroutine != null)
        {
            StopCoroutine(temporaryLookCoroutine);
            temporaryLookCoroutine = null;
        }

        if (newTarget != null)
        {
            // Yeni bir hedef varsa (�rn: oyuncu k�p� tuttu), ona bak.
            currentTarget = newTarget;
        }
        else
        {
            // E�er yeni hedef bo�sa (�rn: oyuncu k�p� b�rakt�), varsay�lan hedefe (oyuncuya) geri d�n.
            currentTarget = playerHead;
        }
    }

    /// <summary>
    /// Nesne b�rak�ld���nda �a�r�lacak YEN� fonksiyon.
    /// Nesneyi bir s�re izler, sonra oyuncuya d�ner.
    /// </summary>
    public void StartTemporaryGaze(Transform temporaryTarget)
    {
        // E�er �al��an bir zamanlay�c� varsa durdur.
        if (temporaryLookCoroutine != null)
        {
            StopCoroutine(temporaryLookCoroutine);
        }
        // Yeni zamanlay�c�y� ba�lat.
        temporaryLookCoroutine = StartCoroutine(LookAtForDuration(temporaryTarget));
    }

    /// <summary>
    /// Belirli bir s�re bir hedefe bak�p sonra varsay�lan hedefe d�nen zamanlay�c�.
    /// </summary>
    private IEnumerator LookAtForDuration(Transform tempTarget)
    {
        // 1. Ad�m: Ge�ici hedefe (f�rlat�lan k�pe) bak.
        currentTarget = tempTarget;

        // 2. Ad�m: Inspector'da belirledi�imiz s�re kadar bekle.
        yield return new WaitForSeconds(firlatmaIzlemeSuresi);

        // 3. Ad�m: S�re doldu, art�k varsay�lan hedefe (oyuncuya) geri d�n.
        currentTarget = playerHead;

        // 4. Ad�m: Zamanlay�c� g�revini temizle.
        temporaryLookCoroutine = null;
    }
}