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

        currentStrategy = new StreakBasedStrategy();
        currentStrategy.SetProfile(profile);
        currentStrategy.OnMoodShouldChange += HandleMoodChange;
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
        
        TriggerInstantReaction(response.Animasyon);

        string category = TranslateDuyguToCategory(response.Duygu);
        if (!string.IsNullOrEmpty(category))
        {
            currentStrategy?.ProcessCategory(category);
        }
    }

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

    void HandleMoodChange(string newMood)
    {
        Debug.LogWarning($"!!!!!!!! AVATAR RUH HALİ DEĞİŞTİ -> {newMood} !!!!!!!");
    }
}