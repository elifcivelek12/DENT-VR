// AvatarMoodController.cs
using UnityEngine;

public class AvatarMoodController : MonoBehaviour
{
    [Header("Fabrika Bağlantısı")]
    public PersonalityFactory factory;

    [Header("Yapılandırma")]
    public PersonalityType personality;
    
    // YENİ: Animator referansını tutacak değişken.
    [Header("Bağlantılar")]
    [Tooltip("Animasyonları kontrol edilecek avatarın Animator bileşeni.")]
    public Animator avatarAnimator; // Editörden sürükleyip bırakacağız.

    private IMoodStrategy currentStrategy;

    void Start()
    {
        // Animator'ın atanıp atanmadığını kontrol edelim.
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
        ConsoleTester.OnAIResponseReceived += HandleAIResponse;
    }

    void OnDisable()
    {
        ConsoleTester.OnAIResponseReceived -= HandleAIResponse;
        if (currentStrategy != null)
        {
            currentStrategy.OnMoodShouldChange -= HandleMoodChange;
        }
    }

    void HandleAIResponse(AIResponse response)
    {
        Debug.Log($"[GÖZLEMCİ] Anons alındı -> Duygu: '{response.Duygu}', Tepki: '{response.Tepki}'.");
        
        TriggerInstantReaction(response.Tepki);

        string category = TranslateDuyguToCategory(response.Duygu);
        if (!string.IsNullOrEmpty(category))
        {
            currentStrategy?.ProcessCategory(category);
        }
    }

    // GÜNCELLENMİŞ FONKSİYON: Artık Debug.Log yerine animasyonları tetikliyor.
    void TriggerInstantReaction(string tepki)
    {
        // Animator referansımız yoksa hiçbir şey yapma.
        if (avatarAnimator == null || tepki == "yok" || string.IsNullOrEmpty(tepki))
        {
            return;
        }

        // Gelen 'tepki' string'ine göre doğru trigger'ı ateşle.
        // Buradaki string'ler ("aglama", "gulme") Animator'deki parametre isimleriyle
        // eşleşmek zorunda DEĞİL, ama tutarlılık için aynı yapmak mantıklıdır.
        switch (tepki.ToLower())
        {
            case "aglama":
                Debug.Log("<color=cyan>!!!!!! ANİMASYON TETİKLENDİ -> aglamaTrigger !!!!!!!</color>");
                avatarAnimator.SetTrigger("aglamaTrigger"); // Animator'deki parametrenin adını kullanıyoruz.
                break;

            case "gulme":
                Debug.Log("<color=cyan>!!!!!! ANİMASYON TETİKLENDİ -> gulmeTrigger !!!!!!!</color>");
                avatarAnimator.SetTrigger("gulmeTrigger"); // Animator'deki parametrenin adını kullanıyoruz.
                break;
            
            // Gelecekte eklemek isteyebileceğin başka tepkiler...
            // case "korkma":
            //     avatarAnimator.SetTrigger("korkmaTrigger");
            //     break;

            default:
                Debug.LogWarning($"[AvatarMoodController] Tanımlanmamış tepki geldi: '{tepki}'. Herhangi bir animasyon tetiklenmedi.");
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