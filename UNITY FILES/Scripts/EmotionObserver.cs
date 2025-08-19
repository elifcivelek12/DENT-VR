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

    // API Key direkt gömülü
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    // Txt dosyasından okunacak prompt
    private string analysisPrompt;
    private List<string> conversationHistory = new List<string>(); // Konuşma geçmişini tutan liste

    void Start()
    {
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Debug.LogError("Gemini API Anahtarı eksik!");
        }

        // Resources/degerlendirme.txt dosyasını yükle
        TextAsset promptFile = Resources.Load<TextAsset>("degerlendirme");
        if (promptFile != null)
        {
            analysisPrompt = promptFile.text;
            Debug.Log("[EmotionObserver] Prompt dosyası yüklendi: degerlendirme.txt.txt");
        }
        else
        {
            Debug.LogError("[EmotionObserver] Prompt dosyası bulunamadı! 'Assets/Resources/degerlendirme.txt.txt' oluşturun.");
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

        var requestBody = new GeminiMultiTurnRequest
        {
            Contents = new List<ContentEntry>()
        };

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

        string transcript = string.Join("\n", conversationHistory);
        string promptToSend = (analysisPrompt ?? "").Replace("{TRANSKRIPT}", transcript);

        requestBody.Contents.Add(new ContentEntry
        {
            Role = "user",
            Parts = new[] { new RequestPart { Text = promptToSend } }
        });

        string jsonData = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Duygu Analizi için gönderilen JSON:\n" + jsonData);

        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
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
                var apiResponse = JsonConvert.DeserializeObject<GeminiApiResponseObserverFinal>(responseJson);
                string modelTextOutput = apiResponse.Candidates[0].Content.Parts[0].Text.Trim().Replace("```json", "").Replace("```", "").Trim();
                var emotionResponse = JsonConvert.DeserializeObject<GeminiEmotionResponse>(modelTextOutput);

                float negativeScore = emotionResponse.EmotionScores.Negative;
                float neutralScore = emotionResponse.EmotionScores.Neutral;
                float positiveScore = emotionResponse.EmotionScores.Positive;
                string talkfeedback = emotionResponse.Summary.feedback;

                AnalysisResult resultData = new AnalysisResult
                {
                    PositiveScore = positiveScore,
                    NeutralScore = neutralScore,
                    NegativeScore = negativeScore,
                    feedback = talkfeedback,
                };

                if (resultData.feedback == null)
                {
                    Debug.Log("FEEDBACK IS NULL");
                }

                onAnalizTamamlandı?.Invoke(resultData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yanıtı parse edilirken hata oluştu: {e.Message}\nYanıt: {responseJson}");
            }
        }
    }
}
