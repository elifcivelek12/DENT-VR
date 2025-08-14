// AvatarMoodController.cs
using UnityEngine;

public class AvatarMoodController : MonoBehaviour
{
    [Header("Fabrika Bağlantısı")]
    public PersonalityFactory factory;

    [Header("Yapılandırma")]
    public PersonalityType personality;
    // Gelecekte başka stratejiler eklersek diye bunu da enum yapabiliriz. Şimdilik tek.

    private IMoodStrategy currentStrategy;

    void Start()
    {
        // 1. FACTORY KULLANIMI: Fabrikadan seçili kişiliğe uygun profili iste.
        PersonalityProfile profile = factory.GetProfile(personality);
        if (profile == null)
        {
            Debug.LogError($"'{personality}' için profil bulunamadı! Lütfen fabrikayı kontrol edin.");
            return;
        }

        // 2. STRATEGY OLUŞTURMA: Yeni bir ruh hali hesaplama stratejisi oluştur.
        currentStrategy = new StreakBasedStrategy();
        currentStrategy.SetProfile(profile); // Stratejiyi fabrikadan gelen kurallarla ayarla.

        // Stratejinin "ruh hali değişmeli" anonsunu dinlemeye başla.
        currentStrategy.OnMoodShouldChange += HandleMoodChange;
    }

    // 3. OBSERVER ABONELİĞİ: Yayıncıyı dinlemeye başla.
    void OnEnable()
    {
        ConsoleTester.OnCategoryReceived += HandleCategoryReceived;
    }

    // Abonelikten çıkmayı unutma!
    void OnDisable()
    {
        ConsoleTester.OnCategoryReceived -= HandleCategoryReceived;
        if(currentStrategy != null)
        {
            currentStrategy.OnMoodShouldChange -= HandleMoodChange;
        }
    }

    // Anons geldiğinde bu fonksiyon çalışır.
    void HandleCategoryReceived(string category)
    {
        Debug.Log($"[GÖZLEMCİ] Anons alındı: '{category}'. İş stratejiye devrediliyor.");
        // İşi doğrudan stratejiye pasla.
        currentStrategy?.ProcessCategory(category);
    }

    // Strateji "ruh hali değişti" dediğinde bu fonksiyon çalışır.
    void HandleMoodChange(string newMood)
    {
        // Bu bizim nihai çıktımız!
        Debug.LogWarning($"!!!!!!!! AVATAR RUH HALİ DEĞİŞTİ -> {newMood} !!!!!!!");
    }
}