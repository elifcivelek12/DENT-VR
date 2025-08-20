using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AffectSystem))]
public class AvatarMoodController : MonoBehaviour
{
    [Header("Kişilik Yapılandırması")]
    [Tooltip("Kişilik profillerini barındıran fabrika.")]
    public PersonalityFactory factory;
    [Tooltip("Bu avatar için kullanılacak kişilik tipi.")]
    public PersonalityType personality;

    [Header("Bağlantılar")]
    [Tooltip("Animasyonları kontrol edilecek avatarın Animator bileşeni.")]
    public Animator avatarAnimator;

    [Tooltip("Avatarın duygu durumunu yöneten sistem.")]
    public AffectSystem affectSystem;

    void Awake()
    {
        // Gerekli bileşenleri otomatik olarak almayı dene.
        if (avatarAnimator == null) avatarAnimator = GetComponent<Animator>();
        if (affectSystem == null) affectSystem = GetComponent<AffectSystem>();

        // Kişilik Profili Yüklemesi
        ConfigureAffectSystem();
    }

    /// <summary>
    /// PersonalityFactory'den doğru profili alır ve ayarlarını AffectSystem'e uygular.
    /// </summary>
    void ConfigureAffectSystem()
    {
        if (factory == null)
        {
            Debug.LogError("PersonalityFactory atanmamış! Kişilik yüklenemedi.", this.gameObject);
            return;
        }

        PersonalityProfile profile = factory.GetProfile(personality);
        if (profile == null)
        {
            Debug.LogError($"'{personality}' için profil bulunamadı! Varsayılan ayarlar kullanılacak.", this.gameObject);
            return;
        }

        Debug.Log($"<color=yellow>Kişilik profili yükleniyor: {profile.name}</color>");

        // Profilden okunan değerleri AffectSystem'e ata
        affectSystem.yariOmur = profile.yariOmur;
        affectSystem.sinir = profile.sinir;
        affectSystem.pozitifEtki = profile.pozitifEtki;
        affectSystem.negatifEtki = profile.negatifEtki;
        affectSystem.pozitifAzaltmaEgrisi = profile.pozitifAzaltmaEgrisi;
        affectSystem.beklemeSuresi = profile.beklemeSuresi;
        affectSystem.kotuGiris = profile.kotuGiris;
        affectSystem.kotuCikis = profile.kotuCikis;
        affectSystem.iyiGiris = profile.iyiGiris;
        affectSystem.iyiCikis = profile.iyiCikis;
        affectSystem.girisSuresi = profile.girisSuresi;
        affectSystem.cikisSuresi = profile.cikisSuresi;
    }

    void OnEnable()
    {
        GeminiManager.onAIResponseAlındı += HandleAIResponse;
    }

    void OnDisable()
    {
        GeminiManager.onAIResponseAlındı -= HandleAIResponse;
    }

    void HandleAIResponse(AIResponse response)
    {
        TriggerInstantReaction(response.Animasyon);
    }

    void TriggerInstantReaction(string animasyonTipi)
    {
        if (affectSystem == null || avatarAnimator == null || animasyonTipi == "yok" || string.IsNullOrEmpty(animasyonTipi))
        {
            return;
        }

        IMoodAnimationFactory currentAnimationFactory;
        DuyguDurumu mevcutDurum = affectSystem.Durum;

        switch (mevcutDurum)
        {
            case DuyguDurumu.Iyi:
                currentAnimationFactory = new IyiMoodAnimationFactory();
                break;
            case DuyguDurumu.Kotu:
                currentAnimationFactory = new KotuMoodAnimationFactory();
                break;
            default: // DuyguDurumu.Notr
                currentAnimationFactory = new NotrMoodAnimationFactory();
                break;
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
                Debug.LogWarning($"[AvatarMoodController] Tanımlanmamış animasyon tipi: '{animasyonTipi}'.");
                break;
        }

        animationBehaviour?.Play(avatarAnimator);
    }
}