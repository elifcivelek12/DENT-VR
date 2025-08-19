using UnityEngine;

// Avatarın yürüme hareketlerini kontrol eden sınıf
public class AvatarMoveControl : MonoBehaviour
{
    [Header("Bağlantılar")]
    [Tooltip("Animasyonları kontrol edecek avatarın Animator bileşeni.")]
    // Avatarın animasyonlarını yöneten Unity Animator bileşeni
    public Animator animator;
    
    void Start()
    {
        // GameManager üzerinden "hasta girdi" olayı tetiklendiğinde
        // BaslatYurume metodunu çalıştır
        GameManager.onPatientEntered += BaslatYurume;
    }

    void Update()
    {
        // Eğer o an oynatılan animasyon "walk" ise, konsola bilgi yazdır
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
        {
            Debug.Log("Yürüme animasyonu oynatılıyor");
        }
    }

    // Yürüme animasyonunu başlatır
    public void BaslatYurume()
    {
        Debug.Log("StartWalk başladı");
        // Animator’da "startwalk" tetikleyicisini çalıştır
        animator.SetTrigger("startwalk");

        // 10 saniye sonra yürüme animasyonunu durdur
        Invoke("DurdurYurume", 10f);
    }

    // Yürüme animasyonunu durdurur
    public void DurdurYurume()
    {
        Debug.Log("Stopwalk başladı");
        // Animator’da "stopwalk" tetikleyicisini çalıştır
        animator.SetTrigger("stopwalk");
    }
}
