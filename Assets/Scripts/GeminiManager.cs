using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using TMPro;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;

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

public class GeminiApiResponseObserverFinal { [JsonProperty("candidates")] public CandidateObserverFinal[] Candidates; }
public class CandidateObserverFinal { [JsonProperty("content")] public GeminiRequestContentObserver Content; }
public class GeminiRequestContentObserver { [JsonProperty("parts")] public GeminiRequestPartObserver[] Parts; }
public class GeminiRequestPartObserver { [JsonProperty("text")] public string Text; }
[System.Serializable] public class EmotionScores { [JsonProperty("negative")] public float Negative; [JsonProperty("neutral")] public float Neutral; [JsonProperty("positive")] public float Positive; }
public class Summary { [JsonProperty("feedback")] public string feedback; }
[System.Serializable] public class GeminiEmotionResponse { [JsonProperty("emotionScores")] public EmotionScores EmotionScores; [JsonProperty("summary")] public Summary Summary; }


[System.Serializable]
public class SpeakerExtremes
{
    [JsonProperty("positive")] public string Positive;
    [JsonProperty("negative")] public string Negative;
}

public struct AnalysisResult
{
    public float PositiveScore;
    public float NeutralScore;
    public float NegativeScore;
    public string feedback;
}

public class GeminiManager : MonoBehaviour
{

    public struct FinalResultData
    {
        public string IknaDurumu;
        public int ToplamKonusma;
        public string EnPozitifCumle;
        public string EnNegatifCumle;
        public string UzunFeedback;
    }

    public static event Action<FinalResultData> onFinalHazir;

    [Header("Duygu Sistemi Bağlantısı")]
    public AffectSystem affectSystem;

    [Header("API Ayarları")]
    [SerializeField, Tooltip("Google Gemini API Anahtarınız")]
    private string geminiApiKey = "YOUR_GEMINI_API_KEY";

    [Header("UI")]
    public TMP_Text sonucText;

    [Header("Oyun Akışı Ayarları")]
    [SerializeField, Tooltip("Seviyenin bitmesi için gereken toplam konuşma sayısı.")]
    private int seviyeBitisKonusmaSayisi = 5;

    // Olaylar
    public static event Action<AIResponse> onAIResponseAlındı;
    public static event Action<string> onCocukTepkisiUretildi;
    public static event Action onKonusmaBitti;
    public static event Action<AnalysisResult> onAnalizTamamlandı;

    // İç durum
    private int konusmaSayaci = 0;
    private List<string> currentConversationHistory;
    private List<string> iknaSonuclari;

    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;
    private string systemPrimer;
    private string analysisPrompt;


    [Header("Ekstrakt Ayarı")]
    [SerializeField] private string hedefKonusmaciEtiketi = "Doktor";


    private string _finalFeedback = "";
    private string _speakerEnPozitif = "";
    private string _speakerEnNegatif = "";


    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";


        string primerPath = "prompts/tripliprompt";
        TextAsset primerAsset = Resources.Load<TextAsset>(primerPath);
        if (primerAsset == null) Debug.LogError($"Prompt dosyası bulunamadı: Assets/Resources/{primerPath}");
        systemPrimer = (primerAsset != null) ? primerAsset.text : "";

        string analysisPath = "prompts/degerlendirme";
        TextAsset analysisAsset = Resources.Load<TextAsset>(analysisPath);
        if (analysisAsset == null) Debug.LogError($"Prompt dosyası bulunamadı: Assets/Resources/{analysisPath}");
        analysisPrompt = (analysisAsset != null) ? analysisAsset.text : "";

        Debug.Log("Promptlar başarıyla yüklendi.");
    }

    void Start()
    {
        if (string.IsNullOrEmpty(geminiApiKey)) Debug.LogError("Gemini API Anahtarı eksik!");
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

        _finalFeedback = "";
        _speakerEnPozitif = "";
        _speakerEnNegatif = "";

        Debug.Log("Gemini Manager: Konuşma sayacı sıfırlandı.");
    }

    public void HandleVoiceInput(string gelenMetin)
    {
        if (string.IsNullOrWhiteSpace(gelenMetin))
        {
            if (sonucText != null) sonucText.text = "Gelen metin boş, işlem yapılamadı.";
            Debug.LogWarning("İşlenecek metin boş olduğu için işlem iptal edildi.");
            return;
        }

        StartCoroutine(ProcessConversationTurn(gelenMetin));
    }

    private IEnumerator ProcessConversationTurn(string doktorCumlesi)
    {
        AddConversation(doktorCumlesi, "Doktor");
        yield return StartCoroutine(ClassifyAndRespond(doktorCumlesi));

        konusmaSayaci++;
        Debug.Log($"Konuşma tamamlandı. Toplam konuşma sayısı: {konusmaSayaci}/{seviyeBitisKonusmaSayisi}");

        if (konusmaSayaci >= seviyeBitisKonusmaSayisi)
        {
            Debug.LogWarning("[GeminiManager] KONUŞMA BİTTİ! Final analiz yapılıyor...");
            if (currentConversationHistory.Count > 0)
            {

                yield return StartCoroutine(AnalyzeEmotions());

                yield return StartCoroutine(ExtractExtremesForSpeaker(hedefKonusmaciEtiketi));
            }


            PublishFinal();

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
                string role = (speaker.Equals("Doktor", StringComparison.OrdinalIgnoreCase)) ? "user" : "model";
                requestBody.Contents.Add(new ContentEntry { Role = role, Parts = new[] { new RequestPart { Text = text } } });
            }
        }

        string finalUserPrompt = $"{systemPrimer}\n\n---\n\nDoktorun YENİ cümlesi: \"{doktorCumlesi}\"";
        requestBody.Contents.Add(new ContentEntry { Role = "user", Parts = new[] { new RequestPart { Text = finalUserPrompt } } });

        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest request = new UnityWebRequest(_apiURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini API Hatası: {request.error}\nYanıt: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = $"Bir API hatası oluştu: {request.error}";
                yield break;
            }

            try
            {
                string hamCikti = request.downloadHandler.text;
                var apiResponse = JsonConvert.DeserializeObject<GeminiApiResponse>(hamCikti);

                if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0 ||
                    apiResponse.Candidates[0]?.Content?.Parts == null || apiResponse.Candidates[0].Content.Parts.Length == 0 ||
                    string.IsNullOrWhiteSpace(apiResponse.Candidates[0].Content.Parts[0].Text))
                {
                    Debug.LogError("Gemini yanıtı beklenen formatta değil.");
                    sonucText?.SetText("Modelden geçerli bir çıktı alınamadı.");
                    yield break;
                }

                string modelinUrettigiText = apiResponse.Candidates[0].Content.Parts[0].Text;
                Match match = Regex.Match(modelinUrettigiText, @"\{[\s\S]*?\}", RegexOptions.Singleline); // lazy
                string temizlenmisJson = match.Success ? match.Value : modelinUrettigiText;

                GeminiAIResponse sonuc = JsonConvert.DeserializeObject<GeminiAIResponse>(temizlenmisJson);

                if (sonuc != null)
                {
                    onCocukTepkisiUretildi?.Invoke(sonuc.Tepki);
                    AddConversation(sonuc.Tepki, "Çocuk");
                    iknaSonuclari.Add(sonuc.Ikna);

                    if (sonucText != null)
                    {
                        sonucText.text =
                            $"Kategori: {sonuc.Kategori}\n" +
                            $"Tepki: {sonuc.Tepki}\n" +
                            $"Animasyon: {sonuc.Animasyon}\n" +
                            $"Duygu: {sonuc.Duygu}\n" +
                            $"İkna: {sonuc.Ikna}";
                    }

                    onAIResponseAlındı?.Invoke(new AIResponse
                    {
                        Kategori = sonuc.Kategori,
                        Tepki = sonuc.Tepki,
                        Animasyon = sonuc.Animasyon,
                        Duygu = sonuc.Duygu,
                        Ikna = sonuc.Ikna
                    });

                    if (affectSystem != null)
                    {
                        Deger gelenDeger = Deger.Notr;
                        switch ((sonuc.Kategori ?? "").ToLowerInvariant())
                        {
                            case "pozitif": gelenDeger = Deger.Pozitif; break;
                            case "negatif": gelenDeger = Deger.Negatif; break;
                        }
                        affectSystem.CumleKaydet(gelenDeger);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON parse hatası: {e.Message}\nHam Çıktı: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = "Yanıtta geçerli bir format bulunamadı.";
            }
        }
    }

    private IEnumerator AnalyzeEmotions()
    {

        var transcriptBuilder = new StringBuilder();
        foreach (var line in currentConversationHistory)
        {
            transcriptBuilder.AppendLine(line);
        }

        string fullPrompt = $"{analysisPrompt}\n\n---\n\n{transcriptBuilder.ToString()}";

        var requestBody = new GeminiMultiTurnRequest
        {
            Contents = new List<ContentEntry>
        {
            new ContentEntry
            {
                Role = "user",
                Parts = new[] { new RequestPart { Text = fullPrompt } }
            }
        }
        };


        //var requestBody = new GeminiMultiTurnRequest { Contents = new List<ContentEntry>() };

        //foreach (var line in currentConversationHistory)
        //{
        //    string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
        //    if (parts.Length == 2)
        //    {
        //        string speaker = parts[0];
        //        string text = parts[1];
        //        string role = (speaker.Equals("Doktor", StringComparison.OrdinalIgnoreCase)) ? "user" : "model";
        //        requestBody.Contents.Add(new ContentEntry { Role = role, Parts = new[] { new RequestPart { Text = text } } });
        //    }
        //}

        //requestBody.Contents.Add(new ContentEntry { Role = "user", Parts = new[] { new RequestPart { Text = analysisPrompt } } });

        string jsonData = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest geminiRequest = new UnityWebRequest(_apiURL, "POST"))
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

                if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0 ||
                    apiResponse.Candidates[0]?.Content?.Parts == null || apiResponse.Candidates[0].Content.Parts.Length == 0 ||
                    string.IsNullOrWhiteSpace(apiResponse.Candidates[0].Content.Parts[0].Text))
                {
                    Debug.LogError("Duygu analizi yanıtı beklenen formatta değil.");
                    yield break;
                }

                string rawText = apiResponse.Candidates[0].Content.Parts[0].Text;
                Match match = Regex.Match(rawText, @"\{[\s\S]*\}", RegexOptions.Singleline);
                string modelTextOutput = match.Success ? match.Value : rawText;

                var emotionResponse = JsonConvert.DeserializeObject<GeminiEmotionResponse>(modelTextOutput);

                AnalysisResult resultData = new AnalysisResult
                {
                    PositiveScore = emotionResponse.EmotionScores.Positive,
                    NeutralScore = emotionResponse.EmotionScores.Neutral,
                    NegativeScore = emotionResponse.EmotionScores.Negative,
                    feedback = emotionResponse.Summary.feedback,
                };
                _finalFeedback = emotionResponse?.Summary?.feedback ?? "";
                onAnalizTamamlandı?.Invoke(resultData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yanıtı parse edilirken hata oluştu: {e.Message}\nYanıt: {responseJson}");
            }
        }
    }

    // Hedef konuşmacı için en pozitif/en negatif cümleyi çıkarır
    private IEnumerator ExtractExtremesForSpeaker(string speakerLabel)
    {
        var requestBody = new GeminiMultiTurnRequest { Contents = new List<ContentEntry>() };

        foreach (var line in currentConversationHistory)
        {
            string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string speaker = parts[0];
                string text = parts[1];
                string role = (speaker.Equals("Doktor", StringComparison.OrdinalIgnoreCase)) ? "user" : "model";
                requestBody.Contents.Add(new ContentEntry
                {
                    Role = role,
                    Parts = new[] { new RequestPart { Text = text } }
                });
            }
        }

        string prompt =
            $"Aşağıdaki diyalogda iki konuşmacı var: \"Doktor\" ve \"Çocuk\".\n" +
            $"SADECE \"{speakerLabel}\" tarafından söylenen cümleleri değerlendir ve EN POZİTİF ve EN NEGATİF olan TEK cümleyi seç.\n" +
            $"ÇIKTIYI SADECE JSON OLARAK ver:\n" +
            $"{{\"positive\":\"...\",\"negative\":\"...\"}}\n" +
            $"Cümleleri orijinal metinden aynen kopyala. JSON dışında hiçbir şey yazma.";

        requestBody.Contents.Add(new ContentEntry
        {
            Role = "user",
            Parts = new[] { new RequestPart { Text = prompt } }
        });

        string jsonData = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest req = new UnityWebRequest(_apiURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ExtractExtremesForSpeaker] API Hatası: {req.error}\nYanıt: {req.downloadHandler.text}");
                yield break;
            }

            try
            {
                var apiResponse = JsonConvert.DeserializeObject<GeminiApiResponse>(req.downloadHandler.text);

                if (apiResponse?.Candidates == null || apiResponse.Candidates.Length == 0 ||
                    apiResponse.Candidates[0]?.Content?.Parts == null || apiResponse.Candidates[0].Content.Parts.Length == 0 ||
                    string.IsNullOrWhiteSpace(apiResponse.Candidates[0].Content.Parts[0].Text))
                {
                    Debug.LogError("[ExtractExtremesForSpeaker] Beklenen model metni yok.");
                    yield break;
                }

                // NEW, CORRECTED CODE
                string rawText = apiResponse.Candidates[0].Content.Parts[0].Text;
                Match match = Regex.Match(rawText, @"\{[\s\S]*\}", RegexOptions.Singleline);
                string raw = match.Success ? match.Value : rawText;

                var extremes = JsonConvert.DeserializeObject<SpeakerExtremes>(raw);
                _speakerEnPozitif = extremes?.Positive ?? "";
                _speakerEnNegatif = extremes?.Negative ?? "";
                Debug.Log($"[ExtractExtremesForSpeaker] {speakerLabel} → Pozitif: {_speakerEnPozitif} | Negatif: {_speakerEnNegatif}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExtractExtremesForSpeaker] Parse Hatası: {e.Message}\nHam: {req.downloadHandler.text}");
            }
        }
    }

    private void AddConversation(string text, string speakerRole)
    {
        currentConversationHistory.Add($"{speakerRole}: {text}");
        Debug.Log($"Konuşma geçmişine eklendi: '{speakerRole}: {text}'");
    }

    private void PublishFinal()
    {
        var paket = new FinalResultData
        {
            IknaDurumu = HesaplaIknaDurumu(),
            ToplamKonusma = konusmaSayaci,
            EnPozitifCumle = string.IsNullOrWhiteSpace(_speakerEnPozitif) ? "—" : _speakerEnPozitif,
            EnNegatifCumle = string.IsNullOrWhiteSpace(_speakerEnNegatif) ? "—" : _speakerEnNegatif,
            UzunFeedback = string.IsNullOrWhiteSpace(_finalFeedback) ? "Analiz metni üretilemedi." : _finalFeedback
        };

        onFinalHazir?.Invoke(paket);
    }

    private string Normalize(string s) => (s ?? "").Trim().ToLowerInvariant();

    private string HesaplaIknaDurumu()
    {
        int evet = iknaSonuclari.Count(x => Normalize(x) == "evet");
        int hayir = iknaSonuclari.Count(x => Normalize(x) == "hayır" || Normalize(x) == "hayir");
        int kararsiz = iknaSonuclari.Count(x => Normalize(x) == "kararsız" || Normalize(x) == "kararsiz");

        if (evet > hayir && evet > kararsiz) return "ikna oldu (evet)";
        if (hayir > evet && hayir > kararsiz) return "ikna olmadı (hayır)";
        return "kararsız";
    }
}