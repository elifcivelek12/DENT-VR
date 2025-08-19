using UnityEngine;

public class RagdollController : MonoBehaviour
{
    // Karakterin tüm Rigidbody bileşenleri (ragdoll parçaları)
    private Rigidbody[] ragdollRigidbodies;
    // Karakterin tüm Collider bileşenleri
    private Collider[] ragdollColliders;
    // Animator bileşeni
    private Animator animator;

    void Start()
    {
        // Tüm Rigidbody ve Collider bileşenlerini çocuklardan al
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        animator = GetComponent<Animator>();

        // Başlangıçta ragdoll devre dışı
        DeactivateRagdoll();
    }

    // Ragdoll'u aktif hale getir (animasyon durur, fizik devreye girer)
    public void ActivateRagdoll()
    {
        animator.enabled = false;
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false; // Fizik etkilerini aç
        }
    }

    // Ragdoll'u devre dışı bırak (animasyon aktif, fizik kapalı)
    public void DeactivateRagdoll()
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true; // Fizik etkilerini kapat
        }
        animator.enabled = true; // Animasyonu aç
    }

    // Çarpışma kontrolü
    void OnCollisionEnter(Collision collision)
    {
        // Eğer çarpışan obje "PlayerHand" tag'ine sahipse ragdoll'u aktif et
        if (collision.gameObject.CompareTag("PlayerHand")) // VR el objeleri bu tag ile işaretlenmeli
        {
            ActivateRagdoll();
        }
    }
}
