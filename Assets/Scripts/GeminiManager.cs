using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using UnityEngine.Events;

// JSON ve dahili veri yap�lar�
[System.Serializable]
public class GeminiAIResponse
{
    [JsonProperty("kategori")] public string Kategori;
    [JsonProperty("tepki")] public string Tepki;
    [JsonProperty("animasyon")] public string Animasyon;
    [JsonProperty("duygu")] public string Duygu;
    [JsonProperty("ikna")] public string Ikna;
}

public class GeminiApiResponse { [JsonProperty("candidates")] public Candidate[] Candidates; }
public class Candidate { [JsonProperty("content")] public GeminiContent Content; }
public class GeminiContent { [JsonProperty("parts")] public GeminiPart[] Parts; }
public class GeminiPart { [JsonProperty("text")] public string Text; }

public class GeminiMultiTurnRequest
{
    [JsonProperty("contents")] public List<ContentEntry> Contents { get; set; }
}

public class ContentEntry
{
    [JsonProperty("role")] public string Role { get; set; }
    [JsonProperty("parts")] public RequestPart[] Parts { get; set; }
}

public class RequestPart { [JsonProperty("text")] public string Text { get; set; } }

// Duygu analizi i�in �zel JSON yap�lar�
public class GeminiApiResponseObserverFinal { [JsonProperty("candidates")] public CandidateObserverFinal[] Candidates; }
public class CandidateObserverFinal { [JsonProperty("content")] public GeminiRequestContentObserver Content; }
public class GeminiRequestContentObserver { [JsonProperty("parts")] public GeminiRequestPartObserver[] Parts; }
public class GeminiRequestPartObserver { [JsonProperty("text")] public string Text; }
[System.Serializable] public class EmotionScores { [JsonProperty("negative")] public float Negative; [JsonProperty("neutral")] public float Neutral; [JsonProperty("positive")] public float Positive; }
public class Summary { [JsonProperty("feedback")] public string feedback; }
[System.Serializable] public class GeminiEmotionResponse { [JsonProperty("emotionScores")] public EmotionScores EmotionScores; [JsonProperty("summary")] public Summary Summary; }

public struct AnalysisResult
{
    public float PositiveScore;
    public float NeutralScore;
    public float NegativeScore;
    public string feedback;
}

public class GeminiManager : MonoBehaviour
{
    [Header("Duygu Sistemi Ba�lant�s�")]
    public AffectSystem affectSystem;

    [Header("API Ayarlar�")]
    [SerializeField, Tooltip("Google Gemini API Anahtar�n�z")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    
    public TMP_Text sonucText;

    [Header("Oyun Ak��� Ayarlar�")]
    [SerializeField, Tooltip("Seviyenin bitmesi i�in gereken toplam konu�ma say�s�.")]
    private int seviyeBitisKonusmaSayisi = 5;

    // �zel Olaylar (Di�er script'ler bu olaylara abone olabilir)
    public static event Action<AIResponse> onAIResponseAl�nd�;
    public static event Action<string> onCocukTepkisiUretildi;
    public static event Action onKonusmaBitti;
    public static event Action<AnalysisResult> onAnalizTamamland�;

    // �zel de�i�kenler
    private int konusmaSayaci = 0;
    private List<string> currentConversationHistory;
    private List<string> iknaSonuclari;
    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;
    private string systemPrimer;
    private string analysisPrompt;

    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";

        // Prompt dosyalar�n� y�kleme
        string primerPath = "prompts/tripliprompt";
        TextAsset primerAsset = Resources.Load<TextAsset>(primerPath);
        if (primerAsset == null) Debug.LogError($"Prompt dosyas� bulunamad�: Assets/Resources/{primerPath}");
        systemPrimer = (primerAsset != null) ? primerAsset.text : "";

        string analysisPath = "prompts/degerlendirme";
        TextAsset analysisAsset = Resources.Load<TextAsset>(analysisPath);
        if (analysisAsset == null) Debug.LogError($"Prompt dosyas� bulunamad�: Assets/Resources/{analysisPath}");
        analysisPrompt = (analysisAsset != null) ? analysisAsset.text : "";

        Debug.Log("Promptlar ba�ar�yla y�klendi.");
    }

    void Start()
    {
        if (string.IsNullOrEmpty(geminiApiKey)) Debug.LogError("Gemini API Anahtar� eksik!");
        konusmaSayaci = 0;
        currentConversationHistory = new List<string>();
        iknaSonuclari = new List<string>();
    }

    void OnEnable()
    {
        ElevenLabsVoiceDemo.onTextTranscribed += HandleVoiceInput;
        GameManager.onLevelStart += KonusmaSayaciniSifirla;
    }

    void OnDisable()
    {
        ElevenLabsVoiceDemo.onTextTranscribed -= HandleVoiceInput;
        GameManager.onLevelStart -= KonusmaSayaciniSifirla;
    }

    public void KonusmaSayaciniSifirla()
    {
        konusmaSayaci = 0;
        if (currentConversationHistory == null) currentConversationHistory = new List<string>();
        if (iknaSonuclari == null) iknaSonuclari = new List<string>();

        currentConversationHistory.Clear();
        iknaSonuclari.Clear();
        Debug.Log("Gemini Manager: Konu�ma sayac� s�f�rland�.");
    }

    public void HandleVoiceInput(string gelenMetin)
    {
        if (string.IsNullOrWhiteSpace(gelenMetin))
        {
            if (sonucText != null) sonucText.text = "Gelen metin bo�, i�lem yap�lamad�.";
            Debug.LogWarning("��lenecek metin bo� oldu�u i�in i�lem iptal edildi.");
            return;
        }

        StartCoroutine(ProcessConversationTurn(gelenMetin));
    }

    private IEnumerator ProcessConversationTurn(string doktorCumlesi)
    {
        AddConversation(doktorCumlesi, "Doktor");
        yield return StartCoroutine(ClassifyAndRespond(doktorCumlesi));

        konusmaSayaci++;
        Debug.Log($"Konu�ma tamamland�. Toplam konu�ma say�s�: {konusmaSayaci}/{seviyeBitisKonusmaSayisi}");

        if (konusmaSayaci >= seviyeBitisKonusmaSayisi)
        {
            Debug.LogWarning("[GeminiManager] KONU�MA B�TT�! Final analiz yap�l�yor...");
            if (currentConversationHistory.Count > 0)
            {
                yield return StartCoroutine(AnalyzeEmotions());
            }
            onKonusmaBitti?.Invoke();
        }
    }

    private IEnumerator ClassifyAndRespond(string doktorCumlesi)
    {
        if (sonucText != null) sonucText.text = "Dinleniyor...";

        var requestBody = new GeminiMultiTurnRequest { Contents = new List<ContentEntry>() };

        foreach (var line in currentConversationHistory)
        {
            string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string speaker = parts[0];
                string text = parts[1];
                string role = (speaker.ToLower() == "doktor") ? "user" : "model";
                requestBody.Contents.Add(new ContentEntry { Role = role, Parts = new[] { new RequestPart { Text = text } } });
            }
        }

        string finalUserPrompt = $"{systemPrimer}\n\n---\n\nDoktorun YEN� c�mlesi: \"{doktorCumlesi}\"";
        requestBody.Contents.Add(new ContentEntry { Role = "user", Parts = new[] { new RequestPart { Text = finalUserPrompt } } });

        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Gemini'ye g�nderilen yap�land�r�lm�� JSON:\n" + jsonRequestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest request = new UnityWebRequest(_apiURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini API Hatas�: {request.error}\nYan�t: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = $"Bir API hatas� olu�tu: {request.error}";
                yield break;
            }

            try
            {
                string hamCikti = request.downloadHandler.text;
                GeminiApiResponse apiResponse = JsonConvert.DeserializeObject<GeminiApiResponse>(hamCikti);
                string modelinUrettigiText = apiResponse.Candidates[0].Content.Parts[0].Text;
                Match match = Regex.Match(modelinUrettigiText, @"\{.*\}", RegexOptions.Singleline);
                string temizlenmisJson = match.Success ? match.Value : modelinUrettigiText;
                GeminiAIResponse sonuc = JsonConvert.DeserializeObject<GeminiAIResponse>(temizlenmisJson);

                if (sonuc != null)
                {
                    onCocukTepkisiUretildi?.Invoke(sonuc.Tepki);
                    AddConversation(sonuc.Tepki, "�ocuk");
                    iknaSonuclari.Add(sonuc.Ikna);

                    if (sonucText != null)
                    {
                        sonucText.text = $"Kategori: {sonuc.Kategori}\nTepki: {sonuc.Tepki}\nAnimasyon: {sonuc.Animasyon}\nDuygu: {sonuc.Duygu}\n�kna: {sonuc.Ikna}";
                        onAIResponseAl�nd�?.Invoke(new AIResponse { Kategori = sonuc.Kategori, Tepki = sonuc.Tepki, Animasyon = sonuc.Animasyon, Duygu = sonuc.Duygu, Ikna = sonuc.Ikna });
                    }

                    if (affectSystem != null)
                    {
                        Deger gelenDeger = Deger.Notr;
                        switch (sonuc.Kategori.ToLower())
                        {
                            case "pozitif": gelenDeger = Deger.Pozitif; break;
                            case "negatif": gelenDeger = Deger.Negatif; break;
                        }
                        affectSystem.CumleKaydet(gelenDeger);
                        Debug.Log($"<color=cyan>[GeminiManager] AffectSystem'e bildirildi: {gelenDeger}</color>");
                    }
                    Debug.Log($"Ba�ar�l�! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}, Animasyon: {sonuc.Animasyon}, Duygu: {sonuc.Duygu}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"JSON parse hatas�: {e.Message}\nHam ��kt�: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = "Yan�tta ge�erli bir format bulunamad�.";
            }
        }
    }

    private IEnumerator AnalyzeEmotions()
    {
        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";
        var requestBody = new GeminiMultiTurnRequest { Contents = new List<ContentEntry>() };

        foreach (var line in currentConversationHistory)
        {
            string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string speaker = parts[0];
                string text = parts[1];
                string role = (speaker.ToLower() == "doktor") ? "user" : "model";
                requestBody.Contents.Add(new ContentEntry { Role = role, Parts = new[] { new RequestPart { Text = text } } });
            }
        }

        requestBody.Contents.Add(new ContentEntry { Role = "user", Parts = new[] { new RequestPart { Text = analysisPrompt } } });
        string jsonData = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Duygu Analizi i�in g�nderilen JSON:\n" + jsonData);

        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            geminiRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            geminiRequest.downloadHandler = new DownloadHandlerBuffer();
            geminiRequest.SetRequestHeader("Content-Type", "application/json");
            yield return geminiRequest.SendWebRequest();

            if (geminiRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini Duygu Analiz Hatas�: {geminiRequest.error}\nYan�t: {geminiRequest.downloadHandler.text}");
                yield break;
            }

            // De�i�keni try-catch blo�u d���na ta��d�k.
            string responseJson = geminiRequest.downloadHandler.text;

            try
            {
                var apiResponse = JsonConvert.DeserializeObject<GeminiApiResponseObserverFinal>(responseJson);
                string modelTextOutput = apiResponse.Candidates[0].Content.Parts[0].Text.Trim().Replace("```json", "").Replace("```", "").Trim();
                var emotionResponse = JsonConvert.DeserializeObject<GeminiEmotionResponse>(modelTextOutput);

                AnalysisResult resultData = new AnalysisResult
                {
                    PositiveScore = emotionResponse.EmotionScores.Positive,
                    NeutralScore = emotionResponse.EmotionScores.Neutral,
                    NegativeScore = emotionResponse.EmotionScores.Negative,
                    feedback = emotionResponse.Summary.feedback,
                };
                onAnalizTamamland�?.Invoke(resultData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yan�t� parse edilirken hata olu�tu: {e.Message}\nYan�t: {responseJson}");
            }
        }
    }

    private void AddConversation(string text, string speakerRole)
    {
        currentConversationHistory.Add($"{speakerRole}: {text}");
        Debug.Log($"Konu�ma ge�mi�ine eklendi: '{speakerRole}: {text}'");
    }

    private string HesaplaIknaDurumu()
    {
        int evet = iknaSonuclari.FindAll(x => x == "evet").Count;
        int hayir = iknaSonuclari.FindAll(x => x == "hay�r").Count;
        int kararsiz = iknaSonuclari.FindAll(x => x == "karars�z").Count;

        if (evet > hayir && evet > kararsiz) return "ikna oldu (evet)";
        if (hayir > evet && hayir > kararsiz) return "ikna olmad� (hay�r)";
        return "karars�z";
    }
}