using UnityEngine;

public enum Deger { Negatif = -1, Notr = 0, Pozitif = 1 }
public enum DuyguDurumu { Kotu, Notr, Iyi }

[RequireComponent(typeof(Animator))]
public class AffectSystem : MonoBehaviour
{
    [Header("Animator (isteğe bağlı)")]
    public Animator animator;
    float animatorMod, animatorHizi;

    [Header("Duygu Durumu (yavaş değişim)")]
    [Range(-1f, 1f)] public float mod = 0f;     
    public float yariOmur = 10f;                
    public float sinir = 0.9f;                  

    [Header("Anlık Etkiler (bir cümlenin katkısı)")]
    public float pozitifEtki = 0.25f;
    public float negatifEtki = 0.35f;
    public float notrEtki    = 0.00f;

    [Header("Toparlanma kapısı (kötüyken + az etki)")]
    public AnimationCurve pozitifAzaltmaEgrisi = new AnimationCurve(
        new Keyframe(-1f, 0.10f), new Keyframe(-0.5f, 0.30f),
        new Keyframe( 0f, 0.60f), new Keyframe( 0.5f, 0.85f),
        new Keyframe( 1f, 1.00f)
    );

    [Header("Refrakter (ani sıçramayı engelle)")]
    public float beklemeSuresi = 2f; 
    float tekrarBaslamaZamani = 0f;

    [Header("Histerezis + Sürdürme")]
    public float kotuGiris  = -0.40f;
    public float kotuCikis  = -0.20f;
    public float iyiGiris   =  0.40f;
    public float iyiCikis   =  0.20f;
    public float girisSuresi = 2.0f;
    public float cikisSuresi = 1.5f;

    public DuyguDurumu Durum { get; private set; } = DuyguDurumu.Notr;
    float yukariSayac = 0f, asagiSayac = 0f;

    void Awake() { if (!animator) animator = GetComponent<Animator>(); }

    void Update()
    {
        
        float tau = yariOmur / Mathf.Log(2f);
        float k = 1f - Mathf.Exp(-Time.deltaTime / tau);
        mod = Mathf.Lerp(mod, 0f, k);
        mod = Mathf.Clamp(mod, -sinir, sinir);

        DurumuGuncelle();

        // Animator’a aktar
        animatorMod = Mathf.SmoothDamp(animatorMod, mod, ref animatorHizi, 0.5f);
        if (animator) animator.SetFloat("mood", animatorMod);
    }

    public void CumleKaydet(Deger deger, float siddet = 1f)
    {
        if (Time.time < tekrarBaslamaZamani) return;

        float etki = 0f;
        switch (deger)
        {
            case Deger.Pozitif:
                float carpan = Mathf.Clamp01(pozitifAzaltmaEgrisi.Evaluate(mod));
                etki = pozitifEtki * carpan;
                break;
            case Deger.Negatif:
                etki = -negatifEtki;
                break;
            default:
                etki = notrEtki;
                break;
        }

        etki *= Mathf.Clamp01(siddet);
        mod = Mathf.Clamp(mod + etki, -1f, 1f);
        tekrarBaslamaZamani = Time.time + beklemeSuresi;
    }

    void DurumuGuncelle()
    {
        switch (Durum)
        {
            case DuyguDurumu.Notr:
                if (mod <= kotuGiris)
                {
                    asagiSayac += Time.deltaTime;
                    if (asagiSayac >= girisSuresi)
                    { Durum = DuyguDurumu.Kotu; asagiSayac = yukariSayac = 0f; }
                }
                else if (mod >= iyiGiris)
                {
                    yukariSayac += Time.deltaTime;
                    if (yukariSayac >= girisSuresi)
                    { Durum = DuyguDurumu.Iyi; asagiSayac = yukariSayac = 0f; }
                }
                else { asagiSayac = yukariSayac = 0f; }
                break;

            case DuyguDurumu.Kotu:
                if (mod >= kotuCikis)
                {
                    yukariSayac += Time.deltaTime;
                    if (yukariSayac >= cikisSuresi)
                    { Durum = DuyguDurumu.Notr; asagiSayac = yukariSayac = 0f; }
                }
                else yukariSayac = 0f;
                break;

            case DuyguDurumu.Iyi:
                if (mod <= iyiCikis)
                {
                    asagiSayac += Time.deltaTime;
                    if (asagiSayac >= cikisSuresi)
                    { Durum = DuyguDurumu.Notr; asagiSayac = yukariSayac = 0f; }
                }
                else asagiSayac = 0f;
                break;
        }
    }
}
