using UnityEngine;

public class AvatarMoodController : MonoBehaviour
{
    [Header("Fabrika Bağlantısı")]
    public PersonalityFactory factory;

    [Header("Yapılandırma")]
    public PersonalityType personality;

    [Header("Bağlantılar")]
    [Tooltip("Animasyonları kontrol edilecek avatarın Animator bileşeni.")]
    public Animator avatarAnimator;

    private IMoodStrategy currentStrategy;
    private IMoodAnimationFactory currentAnimationFactory; // Mevcut ruh hali animasyon fabrikamız

    void Start()
    {
        if (avatarAnimator == null)
        {
            Debug.LogError("Avatar Animator referansı atanmamış! Lütfen Inspector'dan atayın.", this.gameObject);
        }

        PersonalityProfile profile = factory.GetProfile(personality);
        if (profile == null)
        {
            Debug.LogError($"'{personality}' için profil bulunamadı! Lütfen fabrikayı kontrol edin.");
            return;
        }

        // Başlangıçta Nötr ruh hali fabrikasını ayarla. Bu, null hatası almayı önler.
        currentAnimationFactory = new NotrMoodAnimationFactory();
        Debug.Log("[AvatarMoodController] Başlangıç animasyon fabrikası 'NotrMoodAnimationFactory' olarak ayarlandı.");

        currentStrategy = new StreakBasedStrategy();
        currentStrategy.SetProfile(profile);
        currentStrategy.OnMoodShouldChange += HandleMoodChange; // Ruh hali değişince fabrikayı da değiştir
    }

    void OnEnable()
    {
        GeminiController.onAIResponseAlındı += HandleAIResponse;
    }

    void OnDisable()
    {
        GeminiController.onAIResponseAlındı -= HandleAIResponse;
        if (currentStrategy != null)
        {
            currentStrategy.OnMoodShouldChange -= HandleMoodChange;
        }
    }

    void HandleAIResponse(AIResponse response)
    {
        Debug.Log($"[GÖZLEMCİ] Anons alındı -> Duygu: '{response.Duygu}', Tepki: '{response.Animasyon}'.");

        // Gelen genel animasyon talebini, mevcut ruh haline göre fabrikadan alıp tetikle
        TriggerInstantReaction(response.Animasyon);

        string category = TranslateDuyguToCategory(response.Duygu);
        if (!string.IsNullOrEmpty(category))
        {
            currentStrategy?.ProcessCategory(category);
        }
    }

    // Bu metot artık doğrudan trigger isimlerini bilmiyor, fabrikadan istiyor.
    void TriggerInstantReaction(string animasyonTipi)
    {
        if (avatarAnimator == null || animasyonTipi == "yok" || string.IsNullOrEmpty(animasyonTipi))
        {
            return;
        }

        IAnimationBehaviour animationBehaviour = null;

        switch (animasyonTipi.ToLower())
        {
            case "aglama":
                animationBehaviour = currentAnimationFactory.CreateAglamaAnimation();
                break;

            case "gulme":
                animationBehaviour = currentAnimationFactory.CreateGulmeAnimation();
                break;

            case "korkma":
                animationBehaviour = currentAnimationFactory.CreateKorkmaAnimation();
                break;

            default:
                Debug.LogWarning($"[AvatarMoodController] Tanımlanmamış animasyon tipi geldi: '{animasyonTipi}'.");
                break;
        }

        // Fabrikadan gelen animasyon davranışını oynat (null değilse).
        animationBehaviour?.Play(avatarAnimator);
    }

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

    // RUH HALİ DEĞİŞTİĞİNDE DOĞRU FABRİKAYI SEÇEN EN ÖNEMLİ METOT
    void HandleMoodChange(string newMood)
    {
        Debug.LogWarning($"!!!!!!!! AVATAR RUH HALİ DEĞİŞTİ -> {newMood} !!!!!!!");

        switch (newMood.ToUpper())
        {
            case "İYİ":
                currentAnimationFactory = new IyiMoodAnimationFactory();
                Debug.LogWarning("[AvatarMoodController] Animasyon fabrikası 'IyiMoodAnimationFactory' olarak değiştirildi.");
                break;
            case "KÖTÜ":
                currentAnimationFactory = new KotuMoodAnimationFactory();
                Debug.LogWarning("[AvatarMoodController] Animasyon fabrikası 'KotuMoodAnimationFactory' olarak değiştirildi.");
                break;
            default: // Nötr veya tanımsız bir durum için güvenli varsayılan.
                currentAnimationFactory = new NotrMoodAnimationFactory();
                Debug.LogWarning("[AvatarMoodController] Animasyon fabrikası 'NotrMoodAnimationFactory' olarak değiştirildi.");
                break;
        }
    }
}
