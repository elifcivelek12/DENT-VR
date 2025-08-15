using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;
using Newtonsoft.Json;

public class GeminiRequestPartObserver { [JsonProperty("text")] public string Text; }
public class GeminiRequestContentObserver { [JsonProperty("parts")] public GeminiRequestPartObserver[] Parts; }
public class GeminiRequestBodyObserver { [JsonProperty("contents")] public GeminiRequestContentObserver[] Contents; }
public class GeminiApiResponseObserverFinal { [JsonProperty("candidates")] public CandidateObserverFinal[] Candidates; }
public class CandidateObserverFinal { [JsonProperty("content")] public GeminiRequestContentObserver Content; }

public class EmotionObserver : MonoBehaviour
{
    public struct AnalysisResult
    {
        public float PositiveScore;
        public float NeutralScore;
        public float NegativeScore;
        public string Summary;
    }

    public static event Action<List<string>> onKonusmaGecmisiGuncellendi;
    public static event Action<AnalysisResult> onAnalizTamamlandı;

    [System.Serializable]
    public class EmotionScores { [JsonProperty("negative")] public float Negative; [JsonProperty("neutral")] public float Neutral; [JsonProperty("positive")] public float Positive; }
    [System.Serializable]
    public class GeminiEmotionResponse { [JsonProperty("emotionScores")] public EmotionScores EmotionScores; }

    [SerializeField, Tooltip("Google Gemini API Anahtarınız")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";
    private List<string> conversationHistory = new List<string>();


    void Start()
    {
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Debug.LogError("Gemini API Anahtarı eksik! Lütfen Inspector'dan girin.");
        }
    }

    void OnEnable()
    {
        GeminiController.onKonusmaGecmisiEklendi += AddConversation;
        GeminiController.onKonusmaBitti += HandleConversationEnded;
    }

    void OnDisable()
    {
        GeminiController.onKonusmaGecmisiEklendi -= AddConversation;
        GeminiController.onKonusmaBitti -= HandleConversationEnded;
    }

    public void AddConversation(string text, string speakerRole)
    {
        conversationHistory.Add($"{speakerRole}: {text}");
        Debug.Log($"Konuşma geçmişine eklendi: '{speakerRole}: {text}'");
        onKonusmaGecmisiGuncellendi?.Invoke(conversationHistory);

    }

    private void HandleConversationEnded()
    {
        Debug.LogWarning("[EmotionObserver] KONUŞMA BİTTİ! Final analiz yapılıyor...");
        if (conversationHistory.Count > 0)
        {
            StartCoroutine(AnalyzeEmotions());
        }
    }

    IEnumerator AnalyzeEmotions()
    {
        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";

        // 1. Tıpkı GeminiController'da olduğu gibi yapılandırılmış bir istek oluşturuyoruz.
        var requestBody = new GeminiMultiTurnRequest
        {
            Contents = new List<ContentEntry>()
        };

        // 2. Biriktirdiğimiz konuşma geçmişini rollere ayırarak listeye ekliyoruz.
        foreach (var line in conversationHistory)
        {
            string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string speaker = parts[0];
                string text = parts[1];
                string role = (speaker.ToLower() == "doktor") ? "user" : "model";

                requestBody.Contents.Add(new ContentEntry
                {
                    Role = role,
                    Parts = new[] { new RequestPart { Text = text } }
                });
            }
        }

        // 3. Analiz talimatını, tüm konuşma geçmişinden sonra, son bir "user" mesajı olarak ekliyoruz.
        // Bu, modele "Yukarıdaki konuşmayı oku ve ŞİMDİ sana vereceğim talimatı uygula" demektir.
        string analysisPrompt = @"
    Lütfen yukarıdaki konuşma geçmişini analiz et. Sadece doktorun cümlelerine odaklan. 
    Doktorun genel yaklaşımının pozitif, negatif ve nötr duygu skorlarını yüzde olarak belirle.
    Cevabını SADECE aşağıdaki JSON formatında ver. Başka hiçbir metin ekleme.

    {
      ""emotionScores"": {
        ""positive"": <pozitif yüzde>,
        ""neutral"": <nötr yüzde>,
        ""negative"": <negatif yüzde>
      }
    }
    ";

        requestBody.Contents.Add(new ContentEntry
        {
            Role = "user",
            Parts = new[] { new RequestPart { Text = analysisPrompt } }
        });


        // 4. Yapılandırılmış nesneyi JSON'a çeviriyoruz.
        string jsonData = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Duygu Analizi için gönderilen JSON:\n" + jsonData); // Hata ayıklama için önemli!

        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            // ... metodun geri kalanı aynı kalabilir ...
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            geminiRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            geminiRequest.downloadHandler = new DownloadHandlerBuffer();
            geminiRequest.SetRequestHeader("Content-Type", "application/json");

            yield return geminiRequest.SendWebRequest();

            if (geminiRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini Duygu Analiz Hatası: {geminiRequest.error}\nYanıt: {geminiRequest.downloadHandler.text}");
                yield break;
            }

            string responseJson = geminiRequest.downloadHandler.text;

            try
            {
                // Cevap formatımız değişmediği için burası aynı kalabilir.
                var apiResponse = JsonConvert.DeserializeObject<GeminiApiResponseObserverFinal>(responseJson);
                string modelTextOutput = apiResponse.Candidates[0].Content.Parts[0].Text.Trim().Replace("```json", "").Replace("```", "").Trim();
                var emotionResponse = JsonConvert.DeserializeObject<GeminiEmotionResponse>(modelTextOutput);

                float negativeScore = emotionResponse.EmotionScores.Negative;
                float neutralScore = emotionResponse.EmotionScores.Neutral;
                float positiveScore = emotionResponse.EmotionScores.Positive;
                string summary;

                if (positiveScore > 50 && positiveScore > negativeScore) { summary = "Seans genel olarak çok olumlu... (Özet metniniz)"; }
                else if (negativeScore > 50 && negativeScore > positiveScore) { summary = "Seans, genel olarak olumsuz geçti... (Özet metniniz)"; }
                else { summary = "Seans, daha çok nötr bir tonda ilerledi... (Özet metniniz)"; }

                AnalysisResult resultData = new AnalysisResult
                {
                    PositiveScore = positiveScore,
                    NeutralScore = neutralScore,
                    NegativeScore = negativeScore,
                    Summary = summary
                };

                onAnalizTamamlandı?.Invoke(resultData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yanıtı parse edilirken hata oluştu: {e.Message}\nYanıt: {responseJson}");
            }
        }
    
    }
}