// ConsoleTester.cs
using System;
using UnityEngine;

public class ConsoleTester : MonoBehaviour
{
    // OBSERVER: Bu kanal artık basit bir string yerine tam bir "AIResponse" nesnesi yayınlıyor.
    public static event Action<AIResponse> OnAIResponseReceived;

    [Header("Test Cümleleri")]
    [SerializeField] private string sadResponse = "Dişçilerden korkuyorum... duygu:uzgun tepki:aglama";
    [SerializeField] private string happyResponse = "Harika bir gün! duygu:mutlu tepki:gulme";
    [SerializeField] private string neutralResponse = "Bugün hava normal. duygu:notr tepki:yok";

    [ContextMenu("TEST -> Üzgün Yanıt Gönder (Ağlama)")]
    void SendSadResponse()
    {
        ParseAndSendResponse(sadResponse);
    }

    [ContextMenu("TEST -> Mutlu Yanıt Gönder (Gülme)")]
    void SendHappyResponse()
    {
        ParseAndSendResponse(happyResponse);
    }
    
    [ContextMenu("TEST -> Nötr Yanıt Gönder (Tepki Yok)")]
    void SendNeutralResponse()
    {
        ParseAndSendResponse(neutralResponse);
    }

    // Gelen ham metni ayrıştırıp anonsu yapan merkezi fonksiyon.
    private void ParseAndSendResponse(string rawText)
    {
        Debug.Log($"[YAYINCI] Ham metin alındı: '{rawText}'. Ayrıştırılıp anons edilecek...");

        // Basit bir ayrıştırma yapıyoruz. Gerçek projede daha sağlam bir yapı (regex vb.) kullanılabilir.
        string[] parts = rawText.Split(new string[] { "duygu:", "tepki:" }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            Debug.LogError($"[YAYINCI] Metin formatı hatalı! 'duygu:' ve 'tepki:' etiketleri bulunamadı. Metin: '{rawText}'");
            return;
        }

        AIResponse response = new AIResponse
        {
            OriginalSentence = parts[0].Trim(),
            Duygu = parts[1].Trim(),
            Tepki = parts[2].Trim()
        };

        // Hazırlanan yapılandırılmış veriyi tüm dinleyicilere anons et.
        OnAIResponseReceived?.Invoke(response);
    }
}