using UnityEngine;

// Avatarın ruh halini (mood) yapay zekâdan gelen verilere göre yöneten kontrol sınıfı
public class AvatarMoodController : MonoBehaviour
{
    [Header("Fabrika Bağlantısı")]
    // Kişilik profillerini üreten fabrika nesnesi
    public PersonalityFactory factory;

    [Header("Yapılandırma")]
    // Avatarın sahip olacağı kişilik tipi
    public PersonalityType personality;
    
    [Header("Bağlantılar")]
    [Tooltip("Animasyonları kontrol edilecek avatarın Animator bileşeni.")]
    // Avatarın animasyonlarını oynatmaya yarayan Unity Animator bileşeni
    public Animator avatarAnimator; 

    // O an kullanılan ruh hali stratejisi (ör: StreakBasedStrategy)
    private IMoodStrategy currentStrategy;

    void Start()
    {
        // Avatar Animator atanmazsa hata mesajı ver
        if (avatarAnimator == null)
        {
            Debug.LogError("Avatar Animator referansı atanmamış! Lütfen Inspector'dan atayın.", this.gameObject);
        }

        // Fabrikadan kişilik profili al
        PersonalityProfile profile = factory.GetProfile(personality);
        if (profile == null)
        {
            Debug.LogError($"'{personality}' için profil bulunamadı! Lütfen fabrikayı kontrol edin.");
            return;
        }

        // Stratejiyi başlat ve kişilik profilini uygula
        currentStrategy = new StreakBasedStrategy();
        currentStrategy.SetProfile(profile);

        // Strateji ruh hali değişikliği gerektiğinde HandleMoodChange metodunu çağırır
        currentStrategy.OnMoodShouldChange += HandleMoodChange;
    }

    void OnEnable()
    {
        // GeminiController'dan gelen yapay zekâ yanıtlarını dinlemeye başla
        GeminiController.onAIResponseAlındı += HandleAIResponse;
    }

    void OnDisable()
    {
        // Olay aboneliklerini kaldır (bellek kaçağını önlemek için)
        GeminiController.onAIResponseAlındı -= HandleAIResponse;
        if (currentStrategy != null)
        {
            currentStrategy.OnMoodShouldChange -= HandleMoodChange;
        }
    }

    // Yapay zekâdan gelen tepkiyi işle
    void HandleAIResponse(AIResponse response)
    {
        Debug.Log($"[GÖZLEMCİ] Anons alındı -> Duygu: '{response.Duygu}', Tepki: '{response.Animasyon}'.");

        // Eğer animasyon varsa anında tetikle
        TriggerInstantReaction(response.Animasyon);

        // Duyguyu kategoriye çevir ve stratejiye gönder
        string category = TranslateDuyguToCategory(response.Duygu);
        if (!string.IsNullOrEmpty(category))
        {
            currentStrategy?.ProcessCategory(category);
        }
    }

    // Gelen animasyona göre Animator tetikleyici çalıştır
    void TriggerInstantReaction(string animasyon)
    {
        if (avatarAnimator == null || animasyon == "yok" || string.IsNullOrEmpty(animasyon))
        {
            return;
        }

        switch (animasyon.ToLower())
        {
            case "aglama":
                Debug.Log("<color=cyan>!!!!!! ANİMASYON TETİKLENDİ -> aglamaTrigger !!!!!!!</color>");
                avatarAnimator.SetTrigger("aglamaTrigger"); 
                break;

            case "gulme":
                Debug.Log("<color=cyan>!!!!!! ANİMASYON TETİKLENDİ -> gulmeTrigger !!!!!!!</color>");
                avatarAnimator.SetTrigger("gulmeTrigger"); 
                break;

            case "korkma":
                Debug.Log("<color=cyan>!!!!!! ANİMASYON TETİKLENDİ -> korkmaTrigger !!!!!!!</color>");
                avatarAnimator.SetTrigger("korkmaTrigger");
                break;

            default:
                Debug.LogWarning($"[AvatarMoodController] Tanımlanmamış tepki geldi: '{animasyon}'. Herhangi bir animasyon tetiklenmedi.");
                break;
        }
    }

    // Duygu durumunu kategoriye dönüştür (örn: mutlu → olumlu)
    private string TranslateDuyguToCategory(string duygu)
    {
        switch (duygu.ToLower())
        {
            case "mutlu":
            case "sevinçli":
                return "olumlu";
            case "uzgun":
            case "kızgın":
                return "kotu";
            default:
                return null;
        }
    }

    // Ruh hali değiştiğinde tetiklenen metod
    void HandleMoodChange(string newMood)
    {
        Debug.LogWarning($"!!!!!!!! AVATAR RUH HALİ DEĞİŞTİ -> {newMood} !!!!!!!");
    }
}
