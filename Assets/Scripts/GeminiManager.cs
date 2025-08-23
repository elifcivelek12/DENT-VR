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

[System.Serializable] 
public class EmotionScores { [JsonProperty("negative")] public float Negative; [JsonProperty("neutral")] public float Neutral; [JsonProperty("positive")] public float Positive; }
public class Summary { [JsonProperty("feedback")] public string feedback; }

[System.Serializable]
public class Kategoriler
{
    [JsonProperty("basit_dil")] public int BasitDil;
    [JsonProperty("negatiften_kacinma")] public int NegatiftenKacinma;
    [JsonProperty("ses_yuksekligi_denge")] public int SesYuksekligiDenge;
    [JsonProperty("dogru_bilgi")] public int DogruBilgi;
    [JsonProperty("karar")] public string Karar;
    [JsonProperty("notlar")] public string Notlar;
}

[System.Serializable]
public class DetayliDegerlendirme
{
    [JsonProperty("toplam_puan")] public int ToplamPuan;
    [JsonProperty("kategoriler")] public Kategoriler Kategoriler;
}


[System.Serializable]
public class GeminiEmotionResponse
{
    [JsonProperty("emotionScores")] public EmotionScores EmotionScores;
    [JsonProperty("summary")] public Summary Summary;
    [JsonProperty("detaylidegerlendirme")] public DetayliDegerlendirme DetayliDegerlendirme;
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
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    public TMP_Text sonucText;

    [Header("Oyun Akışı Ayarları")]
    [SerializeField, Tooltip("Seviyenin bitmesi için gereken toplam konuşma sayısı.")]
    private int seviyeBitisKonusmaSayisi;

    public static event Action<AIResponse> onAIResponseAlındı;
    public static event Action<string> onCocukTepkisiUretildi;
    public static event Action onKonusmaBitti;
    public static event Action<AnalysisResult> onAnalizTamamlandı;

    private int konusmaSayaci = 0;
    private List<string> currentConversationHistory;
    private List<string> iknaSonuclari;
    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;
    private string systemPrimer;
    private string analysisPrompt;
    private readonly List<(string cumle, string kategori)> cocukCumleleri = new();
    private string _finalFeedback = "";

    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";

        string primerPath = "prompts/tripliprompt";
        TextAsset primerAsset = Resources.Load<TextAsset>(primerPath);
        if (primerAsset == null) Debug.LogError($"Prompt dosyası bulunamadı: Assets/Resources/{primerPath}");
        else systemPrimer = primerAsset.text;

        string analysisPath = "prompts/degerlendirme";
        TextAsset analysisAsset = Resources.Load<TextAsset>(analysisPath);
        if (analysisAsset == null) Debug.LogError($"Prompt dosyası bulunamadı: Assets/Resources/{analysisPath}");
        else analysisPrompt = analysisAsset.text;

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
        currentConversationHistory.Clear();
        iknaSonuclari.Clear();
        cocukCumleleri.Clear();
        _finalFeedback = "";
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

        bool konusmaLimitiDoldu = konusmaSayaci >= seviyeBitisKonusmaSayisi;
        bool iknaEdildi = IknaDurumuKontrolEt();

        if (konusmaLimitiDoldu || iknaEdildi)
        {
            if (iknaEdildi)
            {
                Debug.LogWarning("[GeminiManager] HEDEFE ULAŞILDI! Çocuk ikna edildi. Seviye bitiriliyor...");
            }
            else
            {
                Debug.LogWarning("[GeminiManager] KONUŞMA LİMİTİ DOLDU! Final analiz yapılıyor...");
            }

            if (currentConversationHistory.Count > 0)
            {
                yield return StartCoroutine(AnalyzeEmotions());
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
                string role = (speaker.ToLower() == "doktor") ? "user" : "model";
                requestBody.Contents.Add(new ContentEntry { Role = role, Parts = new[] { new RequestPart { Text = text } } });
            }
        }

        string finalUserPrompt = $"{systemPrimer}\n\n---\n\nDoktorun YENİ cümlesi: \"{doktorCumlesi}\"";
        requestBody.Contents.Add(new ContentEntry { Role = "user", Parts = new[] { new RequestPart { Text = finalUserPrompt } } });

        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest request = new UnityWebRequest(_apiURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonRequestBody));
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
                GeminiApiResponse apiResponse = JsonConvert.DeserializeObject<GeminiApiResponse>(hamCikti);
                string modelinUrettigiText = apiResponse.Candidates[0].Content.Parts[0].Text;
                Match match = Regex.Match(modelinUrettigiText, @"\{.*\}", RegexOptions.Singleline);
                string temizlenmisJson = match.Success ? match.Value : modelinUrettigiText;
                GeminiAIResponse sonuc = JsonConvert.DeserializeObject<GeminiAIResponse>(temizlenmisJson);

                if (sonuc != null)
                {
                    onCocukTepkisiUretildi?.Invoke(sonuc.Tepki);
                    AddConversation(sonuc.Tepki, "Çocuk");
                    iknaSonuclari.Add(sonuc.Ikna);
                    cocukCumleleri.Add((sonuc.Tepki, sonuc.Kategori?.ToLowerInvariant() ?? ""));

                    if (sonucText != null)
                    {
                        sonucText.text = $"Kategori: {sonuc.Kategori}\nTepki: {sonuc.Tepki}\nAnimasyon: {sonuc.Animasyon}\nDuygu: {sonuc.Duygu}\nİkna: {sonuc.Ikna}";
                    }

                    onAIResponseAlındı?.Invoke(new AIResponse { Kategori = sonuc.Kategori, Tepki = sonuc.Tepki, Animasyon = sonuc.Animasyon, Duygu = sonuc.Duygu, Ikna = sonuc.Ikna });

                    if (affectSystem != null)
                    {
                        Deger gelenDeger = Deger.Notr;
                        switch (sonuc.Kategori.ToLower())
                        {
                            case "pozitif": gelenDeger = Deger.Pozitif; break;
                            case "negatif": gelenDeger = Deger.Negatif; break;
                        }
                        affectSystem.CumleKaydet(gelenDeger);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"JSON parse hatası: {e.Message}\nHam Çıktı: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = "Yanıtta geçerli bir format bulunamadı.";
                yield break;
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
                string role = (speaker.ToLowerInvariant() == "doktor") ? "user" : "model";

                requestBody.Contents.Add(new ContentEntry
                {
                    Role = role,
                    Parts = new[] { new RequestPart { Text = text } }
                });
            }
        }

        requestBody.Contents.Add(new ContentEntry
        {
            Role = "user",
            Parts = new[] { new RequestPart { Text = analysisPrompt } }
        });

        string jsonData = JsonConvert.SerializeObject(requestBody);
        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            
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

            GeminiApiResponseObserverFinal apiResponse;
            try
            {
                apiResponse = JsonConvert.DeserializeObject<GeminiApiResponseObserverFinal>(responseJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi yanıtı JSON'a çevrilemedi: {e.Message}\nYanıt: {responseJson}");
                yield break;
            }

            var firstCandidate = apiResponse?.Candidates?.FirstOrDefault();
            var firstPartText = firstCandidate?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(firstPartText))
            {
                Debug.LogError("Gelen yanıtta geçerli bir 'text' alanı bulunamadı (Candidates[0].Content.Parts[0].Text).");
                yield break;
            }


            GeminiEmotionResponse emotionResponse;
            try
            {
                Match match = Regex.Match(firstPartText, @"\{[\s\S]*\}");
                string temizlenmisJson = match.Success ? match.Value : firstPartText;

                emotionResponse = JsonConvert.DeserializeObject<GeminiEmotionResponse>(temizlenmisJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duygu analizi JSON'u parse edilirken hata: {e.Message}\nJSON: {firstPartText}");
                yield break;
            }

            if (emotionResponse?.EmotionScores == null || emotionResponse.Summary == null || emotionResponse.DetayliDegerlendirme == null)
            {
                Debug.LogError($"JSON parse başarılı oldu ancak beklenen alanlardan biri (emotionScores, summary, detaylıdegerlendırme) boş geldi.\nJSON: {firstPartText}");
                yield break;
            }

            var resultData = new AnalysisResult
            {
                PositiveScore = emotionResponse.EmotionScores.Positive,
                NeutralScore = emotionResponse.EmotionScores.Neutral,
                NegativeScore = emotionResponse.EmotionScores.Negative,
                feedback = emotionResponse.Summary.feedback,
                
            };

            Debug.Log($"Detaylı Değerlendirme Puanı: {emotionResponse.DetayliDegerlendirme.ToplamPuan}, Karar: {emotionResponse.DetayliDegerlendirme.Kategoriler.Karar}, Notlar: {emotionResponse.DetayliDegerlendirme.Kategoriler.Notlar}");

            _finalFeedback = resultData.feedback;
            onAnalizTamamlandı?.Invoke(resultData);

            try
            {
                _ = FirebaseManager.Instance?.SaveSession(
                        new List<string>(currentConversationHistory),
                        resultData
                    );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Firebase kaydetme sırasında uyarı: {e.Message}");
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
        string enPozitif = cocukCumleleri.FirstOrDefault(x => x.kategori == "pozitif").cumle ?? "";
        string enNegatif = cocukCumleleri.FirstOrDefault(x => x.kategori == "negatif").cumle ?? "";

        var paket = new FinalResultData
        {
            IknaDurumu = HesaplaIknaDurumu(),
            ToplamKonusma = konusmaSayaci,
            EnPozitifCumle = string.IsNullOrWhiteSpace(enPozitif) ? "Pozitif cümle kullanılmadı" : enPozitif,
            EnNegatifCumle = string.IsNullOrWhiteSpace(enNegatif) ? "Negatif cümle kullanılmadı" : enNegatif,
            UzunFeedback = string.IsNullOrWhiteSpace(_finalFeedback) ? "Analiz metni üretilemedi." : _finalFeedback,
            
        };

        onFinalHazir?.Invoke(paket);
    }

    private string HesaplaIknaDurumu()
    {
        int evet = iknaSonuclari.Count(x => x == "evet");
        int hayir = iknaSonuclari.Count(x => x == "hayır");
        int kararsiz = iknaSonuclari.Count(x => x == "kararsız");

        if (evet > hayir && evet > kararsiz) return "ikna oldu (evet)";
        if (hayir > evet && hayir > kararsiz) return "ikna olmadı (hayır)";
        return "kararsız";
    }

    private bool IknaDurumuKontrolEt()
    {
        if (iknaSonuclari == null || iknaSonuclari.Count == 0)
        {
            return false;
        }
        return iknaSonuclari.LastOrDefault()?.ToLowerInvariant() == "evet";
    }
}