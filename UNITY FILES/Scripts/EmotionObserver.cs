using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;
using Newtonsoft.Json;

// --- API Sınıfları (Bunlar aynı kalıyor) ---
public class GeminiRequestPartObserver { [JsonProperty("text")] public string Text; }
public class GeminiRequestContentObserver { [JsonProperty("parts")] public GeminiRequestPartObserver[] Parts; }
public class GeminiRequestBodyObserver { [JsonProperty("contents")] public GeminiRequestContentObserver[] Contents; }
public class GeminiApiResponseObserverFinal { [JsonProperty("candidates")] public CandidateObserverFinal[] Candidates; }
public class CandidateObserverFinal { [JsonProperty("content")] public GeminiRequestContentObserver Content; }

public class EmotionObserver : MonoBehaviour
{
    // YENİ: Analiz sonucunu paketleyip göndermek için bir veri yapısı
    public struct AnalysisResult
    {
        public float PositiveScore;
        public float NeutralScore;
        public float NegativeScore;
        public string Summary;
    }

    // YENİ: Analiz tamamlandığında tetiklenecek genel anons (event)
    public static event Action<AnalysisResult> onAnalysisComplete;

    // JSON parse sınıfları (Bunlar aynı kalıyor)
    [System.Serializable]
    public class EmotionScores { [JsonProperty("negative")] public float Negative; [JsonProperty("neutral")] public float Neutral; [JsonProperty("positive")] public float Positive; }
    [System.Serializable]
    public class GeminiEmotionResponse { [JsonProperty("emotionScores")] public EmotionScores EmotionScores; }

    [SerializeField, Tooltip("Google Gemini API Anahtarınız")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";
    private List<string> conversationHistory = new List<string>();

    // DÜZELTME: Bu script artık UI'ı doğrudan kontrol etmiyor, bu yüzden UI referansını sildik.
    // public TMP_Text resultText; 

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
        string prompt = "Analyze the sentiment of the Doctor's communication..."; 
        
           var requestBody = new GeminiRequestBodyObserver
            {
            Contents = new[]
                {
                    new GeminiRequestContentObserver { Parts = new[] { new GeminiRequestPartObserver { Text = prompt } } }
                }
            };

        string jsonData = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            yield return geminiRequest.SendWebRequest();

            if (geminiRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini Duygu Analiz Hatası: {geminiRequest.error}\nYanıt: {geminiRequest.downloadHandler.text}");
                yield break;
            }

            string responseJson = geminiRequest.downloadHandler.text;

            try
            {
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

                // DÜZELTME: Sonuçları doğrudan UI'a yazmak yerine, paketleyip anons ediyoruz.
                AnalysisResult resultData = new AnalysisResult
                {
                    PositiveScore = positiveScore,
                    NeutralScore = neutralScore,
                    NegativeScore = negativeScore,
                    Summary = summary
                };

                // YENİ: Analizin tamamlandığını ve sonuçları herkese duyur.
                onAnalysisComplete?.Invoke(resultData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yanıtı parse edilirken hata oluştu: {e.Message}\nYanıt: {responseJson}");
            }
        }
    }
}