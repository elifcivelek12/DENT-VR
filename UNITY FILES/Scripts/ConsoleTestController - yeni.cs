// ConsoleTester.cs
using System;
using UnityEngine;

// Konsoldan test cümleleri ile yapay zekâ yanıtlarını simüle eden sınıf
public class ConsoleTester : MonoBehaviour
{
    // Yapay zekâ yanıtı alındığında tetiklenen event
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

    // Gelen ham metni ayrıştırıp AIResponse oluşturan merkezi fonksiyon
    private void ParseAndSendResponse(string rawText)
    {
        Debug.Log($"[YAYINCI] Ham metin alındı: '{rawText}'. Ayrıştırılıp anons edilecek...");

        // Ham metni "duygu:" ve "tepki:" etiketlerine göre parçala
        string[] parts = rawText.Split(new string[] { "duygu:", "tepki:" }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            Debug.LogError($"[YAYINCI] Metin formatı hatalı! 'duygu:' ve 'tepki:' etiketleri bulunamadı. Metin: '{rawText}'");
            return;
        }

        // parts[0] → serbest metin (ör: "Dişçilerden korkuyorum...")
        // parts[1] → duygu (ör: "uzgun")
        // parts[2] → tepki (ör: "aglama")
        AIResponse response = new AIResponse
        {
            Kategori = "test", // Test amaçlı sabit kategori veriyoruz
            Tepki = parts[2].Trim(),
            Animasyon = parts[2].Trim(),
            Duygu = parts[1].Trim()
        };

        Debug.Log($"[YAYINCI] AIResponse üretildi -> Duygu: {response.Duygu}, Tepki: {response.Tepki}");

        // Event'i tetikle → diğer scriptler bu yanıtı dinleyebilir
        OnAIResponseReceived?.Invoke(response);
    }
}
