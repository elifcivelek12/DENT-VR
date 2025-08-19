using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.Rendering;

// Gemini API’ye gönderilecek request için gözlemci sınıflar
public class GeminiRequestPartObserver { [JsonProperty("text")] public string Text; }
public class GeminiRequestContentObserver { [JsonProperty("parts")] public GeminiRequestPartObserver[] Parts; }
public class GeminiRequestBodyObserver { [JsonProperty("contents")] public GeminiRequestContentObserver[] Contents; }
public class GeminiApiResponseObserverFinal { [JsonProperty("candidates")] public CandidateObserverFinal[] Candidates; }
public class CandidateObserverFinal { [JsonProperty("content")] public GeminiRequestContentObserver Content; }

// Duygu analizini yapan sınıf
public class EmotionObserver : MonoBehaviour
{
    // Analiz sonuçlarını tutacak struct
    public struct AnalysisResult
    {
        public float PositiveScore;  // Olumlu duygu skoru
        public float NeutralScore;   // Nötr duygu skoru
        public float NegativeScore;  // Olumsuz duygu skoru
        public string feedback;      // Gemini API’den gelen özet veya geri bildirim
    }

    // Konuşma geçmişi güncellendiğinde tetiklenecek event
    public static event Action<List<string>> onKonusmaGecmisiGuncellendi;
    // Analiz tamamlandığında tetiklenecek event
    public static event Action<AnalysisResult> onAnalizTamamlandı;

    [System.Serializable]
    public class EmotionScores { [JsonProperty("negative")] public float Negative; [JsonProperty("neutral")] public float Neutral; [JsonProperty("positive")] public float Positive; }
    public class Summary { [JsonProperty("feedback")] public string feedback; }
    [System.Serializable]
    public class GeminiEmotionResponse { [JsonProperty("emotionScores")] public EmotionScores EmotionScores; [JsonProperty("summary")] public Summary Summary; }

    [SerializeField, Tooltip("Google Gemini API Anahtarınız")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0"; // API anahtarı
    private List<string> conversationHistory = new List<string>(); // Konuşma geçmişini tutan liste

    void Start()
    {
        // API anahtarı eksikse hata mesajı ver
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Debug.LogError("Gemini API Anahtarı eksik!");
        }
    }

    void OnEnable()
    {
        // Konuşma geçmişi eklendiğinde ve konuşma bittiğinde eventlere abone ol
        GeminiController.onKonusmaGecmisiEklendi += AddConversation;
        GeminiController.onKonusmaBitti += HandleConversationEnded;
    }

    void OnDisable()
    {
        // Script devre dışı bırakıldığında event aboneliklerini kaldır
        GeminiController.onKonusmaGecmisiEklendi -= AddConversation;
        GeminiController.onKonusmaBitti -= HandleConversationEnded;
    }

    // Yeni bir konuşma eklenince çağrılır
    public void AddConversation(string text, string speakerRole)
    {
        // Konuşmayı "rol: metin" formatında listeye ekle
        conversationHistory.Add($"{speakerRole}: {text}");
        Debug.Log($"Konuşma geçmişine eklendi: '{speakerRole}: {text}'");

        // Abonelere güncellenmiş konuşma geçmişini bildir
        onKonusmaGecmisiGuncellendi?.Invoke(conversationHistory);
    }

    // Konuşma sona erdiğinde çağrılır
    private void HandleConversationEnded()
    {
        Debug.LogWarning("[EmotionObserver] KONUŞMA BİTTİ! Final analiz yapılıyor...");

        // Konuşma geçmişi varsa analiz coroutine’ini başlat
        if (conversationHistory.Count > 0)
        {
            StartCoroutine(AnalyzeEmotions());
        }
    }

    // Konuşma geçmişini Gemini API’ye gönderip duygu analizi yapan coroutine
    IEnumerator AnalyzeEmotions()
    {
        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";

        // Gönderilecek request body oluşturuluyor
        var requestBody = new GeminiMultiTurnRequest
        {
            Contents = new List<ContentEntry>()
        };

        // Konuşma geçmişini satır satır request formatına dönüştür
        foreach (var line in conversationHistory)
        {
            string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string speaker = parts[0];
                string text = parts[1];
                // Doktor rolü "user", çocuk rolü "model" olarak ayarlanıyor
                string role = (speaker.ToLower() == "doktor") ? "user" : "model";

                requestBody.Contents.Add(new ContentEntry
                {
                    Role = role,
                    Parts = new[] { new RequestPart { Text = text } }
                });
            }
        }

        // Analiz prompt’u oluşturuluyor, JSON formatında yanıt bekleniyor
        string analysisPrompt = @"
    Lütfen yukarıdaki konuşma geçmişini analiz et. Hem doktorun hem de cocuğun cumlelerine odaklan. 
    *Sadece doktorun* genel yaklaşımının pozitif, negatif ve nötr duygu skorlarını yüzde olarak belirle.
    Tüm konuşma boyunca sohbetin ilerleyişini, doktorun kullandığı kelimelerin cocuk ile iletişime uygunluğunu iki cümlede yorumla, string olarak gönder.
    Cevabını SADECE aşağıdaki JSON formatında ver. Başka metin ekleme.

    {
      ""emotionScores"": {
        ""positive"": <pozitif yüzde>,
        ""neutral"": <nötr yüzde>,
        ""negative"": <negatif yüzde>,
        }

      ""summary"": {
        ""feedback"": <sohbetin geneli hakkında geribildirim, 10 üzerinden başarı puanı>
        }      
    }
    ";

        // Analiz prompt’unu request’e ekle
        requestBody.Contents.Add(new ContentEntry
        {
            Role = "user",
            Parts = new[] { new RequestPart { Text = analysisPrompt } }
        });

        // JSON’a dönüştür
        string jsonData = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Duygu Analizi için gönderilen JSON:\n" + jsonData);

        // UnityWebRequest ile POST isteği gönder
        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            geminiRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            geminiRequest.downloadHandler = new DownloadHandlerBuffer();
            geminiRequest.SetRequestHeader("Content-Type", "application/json");

            // İstek gönderiliyor
            yield return geminiRequest.SendWebRequest();

            // Hata kontrolü
            if (geminiRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini Duygu Analiz Hatası: {geminiRequest.error}\nYanıt: {geminiRequest.downloadHandler.text}");
                yield break;
            }

            // Yanıt alındı
            string responseJson = geminiRequest.downloadHandler.text;

            try
            {
                // JSON parse edilip analiz sonuçları elde ediliyor
                var apiResponse = JsonConvert.DeserializeObject<GeminiApiResponseObserverFinal>(responseJson);
                string modelTextOutput = apiResponse.Candidates[0].Content.Parts[0].Text.Trim().Replace("```json", "").Replace("```", "").Trim();
                var emotionResponse = JsonConvert.DeserializeObject<GeminiEmotionResponse>(modelTextOutput);

                float negativeScore = emotionResponse.EmotionScores.Negative;
                float neutralScore = emotionResponse.EmotionScores.Neutral;
                float positiveScore = emotionResponse.EmotionScores.Positive;
                string talkfeedback = emotionResponse.Summary.feedback;

                // AnalysisResult struct’ını oluştur
                AnalysisResult resultData = new AnalysisResult
                {
                    PositiveScore = positiveScore,
                    NeutralScore = neutralScore,
                    NegativeScore = negativeScore,
                    feedback = talkfeedback,
                };

                // Eğer geri bildirim boşsa debug log
                if (resultData.feedback == null)
                {
                    Debug.Log("FEEDBACK IS NULL");
                }

                // Analiz tamamlandı event’i tetikleniyor
                onAnalizTamamlandı?.Invoke(resultData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yanıtı parse edilirken hata oluştu: {e.Message}\nYanıt: {responseJson}");
            }
        }

    }
}
