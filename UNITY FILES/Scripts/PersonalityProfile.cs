using UnityEngine;


[CreateAssetMenu(fileName = "YeniKisilikProfili", menuName = "Dent-Vr/KisilikProfili")]
public class PersonalityProfile : ScriptableObject
{
    public string personalityName;

    [Header("Duygu Durumu (yavaş değişim)")]
    [Tooltip("Duygunun nötre dönme hızı. Yüksek değer = daha yavaş unutur.")]
    public float yariOmur = 10f;
    [Tooltip("Modun ulaşabileceği maksimum pozitif/negatif değer.")]
    public float sinir = 0.9f;

    [Header("Anlık Etkiler (bir cümlenin katkısı)")]
    [Tooltip("Pozitif bir cümlenin anlık katkısı.")]
    public float pozitifEtki = 0.25f;
    [Tooltip("Negatif bir cümlenin anlık katkısı.")]
    public float negatifEtki = 0.35f;

    [Header("Toparlanma Kapısı (kötüyken + az etki)")]
    [Tooltip("Karakter kötüyken, pozitif cümlelerin etkisinin ne kadar azalacağını belirler.")]
    public AnimationCurve pozitifAzaltmaEgrisi = new AnimationCurve(
        new Keyframe(-1f, 0.10f), new Keyframe(-0.5f, 0.30f),
        new Keyframe(0f, 0.60f), new Keyframe(0.5f, 0.85f),
        new Keyframe(1f, 1.00f)
    );

    [Header("Refrakter (ani sıçramayı engelle)")]
    [Tooltip("Bir cümleden sonra yeni bir cümlenin etki etmesi için gereken bekleme süresi.")]
    public float beklemeSuresi = 2f;

    [Header("Histerezis + Sürdürme (Mod Değişim Eşikleri)")]
    [Tooltip("Kötü moda girmek için inilmesi gereken mod seviyesi.")]
    public float kotuGiris = -0.40f;
    [Tooltip("Kötü moddan çıkıp nötre dönmek için çıkılması gereken mod seviyesi.")]
    public float kotuCikis = -0.20f;
    [Tooltip("İyi moda girmek için çıkılması gereken mod seviyesi.")]
    public float iyiGiris = 0.40f;
    [Tooltip("İyi moddan çıkıp nötre dönmek için inilmesi gereken mod seviyesi.")]
    public float iyiCikis = 0.20f;
    [Tooltip("Bir moda girmek için eşiğin altında/üstünde kalınması gereken süre.")]
    public float girisSuresi = 2.0f;
    [Tooltip("Bir moddan çıkmak için eşiğin üstünde/altında kalınması gereken süre.")]
    public float cikisSuresi = 1.5f;
}