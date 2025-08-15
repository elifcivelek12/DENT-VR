using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;

// Bu script, GeminiController'ýn olaylarýný dinleyen bir Observer'dýr.
// Konuþma geçmiþini yönetir ve doktorun genel duygu durumunu analiz eder.
public class EmotionObserver : MonoBehaviour
{
    [System.Serializable]
    public class EmotionScores
    {
        public float negative;
        public float neutral;
        public float positive;
        public string summary;
    }

    [System.Serializable]
    public class GeminiResponse
    {
        public EmotionScores emotionScores;
    }

    [Header("API Ayarlarý")]
    public string geminiApiKey = "BURAYA_API_KEY";

    // Konuþma geçmiþini tutan liste.
    private List<string> conversationHistory = new List<string>();

    [Header("UI Baðlantýlarý")]
    [Tooltip("Analiz sonuçlarýnýn gösterileceði TextMeshPro nesnesi.")]
    public TMP_Text resultText;

    // Doktorun genel duygu durumunu tutacak deðiþkenler
    [Header("Doktorun Duygu Durumu")]
    public float currentNegativeScore = 0f;
    public float currentNeutralScore = 0f;
    public float currentPositiveScore = 0f;
    public string lastEmotionSummary = "";

    void Start()
    {
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Debug.LogError("Gemini API Anahtarý eksik! Lütfen Inspector'dan girin.");
        }
    }

    // YENÝ: GeminiController'dan gelen olayý dinleyen metot.
    // Doktorun ve çocuðun konuþma parçalarýný bu metot alýr.
    public void AddConversation(string text, string speakerRole)
    {
        conversationHistory.Add($"{speakerRole}: {text}");
        Debug.Log($"Konuþma geçmiþine eklendi: '{speakerRole}: {text}'");

        // Her 4 turda bir duygu analizi yap.
        if (conversationHistory.Count % 4 == 0)
        {
            StartCoroutine(AnalyzeEmotions());
        }
    }

    // Konuþma geçmiþini Gemini'ye göndererek duygu analizi yap.
    IEnumerator AnalyzeEmotions()
    {
        string prompt = "Analyze the sentiment of the Doctor's communication throughout the conversation. Provide a percentage score for Negative, Neutral, and Positive emotions. Also, provide a brief summary of the overall tone. Respond in JSON format only. Do not include any other text.\n\nConversation:\n" + string.Join("\n", conversationHistory);

        Debug.Log("Duygu analizi için Gemini'ye istek gönderiliyor...");

        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20:generateContent?key={geminiApiKey}";

        var requestData = new
        {
            contents = new[]
            {
                new {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            geminiRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            geminiRequest.downloadHandler = new DownloadHandlerBuffer();
            geminiRequest.SetRequestHeader("Content-Type", "application/json");

            yield return geminiRequest.SendWebRequest();

            if (geminiRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini Duygu Analiz Hatasý: {geminiRequest.error}");
                yield break;
            }

            string responseJson = geminiRequest.downloadHandler.text;
            Debug.Log($"Gemini'den gelen duygu analizi yanýtý: {responseJson}");

            try
            {
                int start = responseJson.IndexOf("\"text\": \"") + 9;
                int end = responseJson.IndexOf("\"", start);
                string jsonPayload = responseJson.Substring(start, end - start);

                GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(jsonPayload);

                currentNegativeScore = geminiResponse.emotionScores.negative;
                currentNeutralScore = geminiResponse.emotionScores.neutral;
                currentPositiveScore = geminiResponse.emotionScores.positive;

                // YENÝ: Puanlara göre dinamik özet oluþturma
                if (currentPositiveScore > 50 && currentPositiveScore > currentNegativeScore)
                {
                    lastEmotionSummary = "Seans genel olarak çok olumlu ve baþarýlý geçti. Doktorun yaklaþýmý pozitif algýlandý.";
                }
                else if (currentNegativeScore > 50 && currentNegativeScore > currentPositiveScore)
                {
                    lastEmotionSummary = "Seans, genel olarak olumsuz geçti. Doktorun yaklaþýmý baþarýsýz oldu.";
                }
                else
                {
                    lastEmotionSummary = "Seans, daha çok nötr bir tonda ilerledi. Belirgin bir olumlu veya olumsuz durum gözlenmedi.";
                }

                Debug.Log($"Doktorun Ruh Hali Analizi Sonucu:\nNegatif: {currentNegativeScore}%\nNötr: {currentNeutralScore}%\nPozitif: {currentPositiveScore}%\nÖzet: {lastEmotionSummary}");

                // Sonuçlarý UI'a yazdýrma
                if (resultText != null)
                {
                    resultText.text =
                        $"Doktorun Ruh Hali Analizi:\n" +
                        $"Pozitif: %{currentPositiveScore:F0}\n" +
                        $"Nötr: %{currentNeutralScore:F0}\n" +
                        $"Negatif: %{currentNegativeScore:F0}\n\n" +
                        $"Özet: {lastEmotionSummary}";
                }

                // Buraya doktorun ruh haline göre çocuk karakterinin davranýþlarýný deðiþtirecek mantýðý ekleyebilirsiniz.
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yanýtý parse edilirken hata oluþtu: {e.Message}");
                Debug.Log($"Yanýt: {responseJson}");
            }
        }
    }
}
