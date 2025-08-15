using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;

// Bu script, GeminiController'�n olaylar�n� dinleyen bir Observer'd�r.
// Konu�ma ge�mi�ini y�netir ve doktorun genel duygu durumunu analiz eder.
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

    [Header("API Ayarlar�")]
    public string geminiApiKey = "BURAYA_API_KEY";

    // Konu�ma ge�mi�ini tutan liste.
    private List<string> conversationHistory = new List<string>();

    [Header("UI Ba�lant�lar�")]
    [Tooltip("Analiz sonu�lar�n�n g�sterilece�i TextMeshPro nesnesi.")]
    public TMP_Text resultText;

    // Doktorun genel duygu durumunu tutacak de�i�kenler
    [Header("Doktorun Duygu Durumu")]
    public float currentNegativeScore = 0f;
    public float currentNeutralScore = 0f;
    public float currentPositiveScore = 0f;
    public string lastEmotionSummary = "";

    void Start()
    {
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Debug.LogError("Gemini API Anahtar� eksik! L�tfen Inspector'dan girin.");
        }
    }

    // YEN�: GeminiController'dan gelen olay� dinleyen metot.
    // Doktorun ve �ocu�un konu�ma par�alar�n� bu metot al�r.
    public void AddConversation(string text, string speakerRole)
    {
        conversationHistory.Add($"{speakerRole}: {text}");
        Debug.Log($"Konu�ma ge�mi�ine eklendi: '{speakerRole}: {text}'");

        // Her 4 turda bir duygu analizi yap.
        if (conversationHistory.Count % 4 == 0)
        {
            StartCoroutine(AnalyzeEmotions());
        }
    }

    // Konu�ma ge�mi�ini Gemini'ye g�ndererek duygu analizi yap.
    IEnumerator AnalyzeEmotions()
    {
        string prompt = "Analyze the sentiment of the Doctor's communication throughout the conversation. Provide a percentage score for Negative, Neutral, and Positive emotions. Also, provide a brief summary of the overall tone. Respond in JSON format only. Do not include any other text.\n\nConversation:\n" + string.Join("\n", conversationHistory);

        Debug.Log("Duygu analizi i�in Gemini'ye istek g�nderiliyor...");

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
                Debug.LogError($"Gemini Duygu Analiz Hatas�: {geminiRequest.error}");
                yield break;
            }

            string responseJson = geminiRequest.downloadHandler.text;
            Debug.Log($"Gemini'den gelen duygu analizi yan�t�: {responseJson}");

            try
            {
                int start = responseJson.IndexOf("\"text\": \"") + 9;
                int end = responseJson.IndexOf("\"", start);
                string jsonPayload = responseJson.Substring(start, end - start);

                GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(jsonPayload);

                currentNegativeScore = geminiResponse.emotionScores.negative;
                currentNeutralScore = geminiResponse.emotionScores.neutral;
                currentPositiveScore = geminiResponse.emotionScores.positive;

                // YEN�: Puanlara g�re dinamik �zet olu�turma
                if (currentPositiveScore > 50 && currentPositiveScore > currentNegativeScore)
                {
                    lastEmotionSummary = "Seans genel olarak �ok olumlu ve ba�ar�l� ge�ti. Doktorun yakla��m� pozitif alg�land�.";
                }
                else if (currentNegativeScore > 50 && currentNegativeScore > currentPositiveScore)
                {
                    lastEmotionSummary = "Seans, genel olarak olumsuz ge�ti. Doktorun yakla��m� ba�ar�s�z oldu.";
                }
                else
                {
                    lastEmotionSummary = "Seans, daha �ok n�tr bir tonda ilerledi. Belirgin bir olumlu veya olumsuz durum g�zlenmedi.";
                }

                Debug.Log($"Doktorun Ruh Hali Analizi Sonucu:\nNegatif: {currentNegativeScore}%\nN�tr: {currentNeutralScore}%\nPozitif: {currentPositiveScore}%\n�zet: {lastEmotionSummary}");

                // Sonu�lar� UI'a yazd�rma
                if (resultText != null)
                {
                    resultText.text =
                        $"Doktorun Ruh Hali Analizi:\n" +
                        $"Pozitif: %{currentPositiveScore:F0}\n" +
                        $"N�tr: %{currentNeutralScore:F0}\n" +
                        $"Negatif: %{currentNegativeScore:F0}\n\n" +
                        $"�zet: {lastEmotionSummary}";
                }

                // Buraya doktorun ruh haline g�re �ocuk karakterinin davran��lar�n� de�i�tirecek mant��� ekleyebilirsiniz.
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yan�t� parse edilirken hata olu�tu: {e.Message}");
                Debug.Log($"Yan�t: {responseJson}");
            }
        }
    }
}
