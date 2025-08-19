using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.Events;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;


[System.Serializable]
public class AIResponse
{
    public string Kategori;
    public string Tepki;
    public string Animasyon;
    public string Duygu;
    public string Ikna;
}

// ---- Gemini API model yanıt tipleri ----
public class GeminiApiResponse { [JsonProperty("candidates")] public Candidate[] Candidates; }
public class Candidate { [JsonProperty("content")] public GeminiContent Content; }
public class GeminiContent { [JsonProperty("parts")] public GeminiPart[] Parts; }
public class GeminiPart { [JsonProperty("text")] public string Text; }

public class GeminiMultiTurnRequest
{
    [JsonProperty("contents")]
    public List<ContentEntry> Contents { get; set; }
}

public class ContentEntry
{
    [JsonProperty("role")]
    public string Role { get; set; } // "user" ya da "model"

    [JsonProperty("parts")]
    public RequestPart[] Parts { get; set; }
}

public class RequestPart
{
    [JsonProperty("text")]
    public string Text { get; set; }
}

[System.Serializable]
public class CocukTepki
{
    [JsonProperty("kategori")]
    public string Kategori;

    [JsonProperty("tepki")]
    public string Tepki;

    [JsonProperty("animasyon")]
    public string Animasyon;

    [JsonProperty("duygu")]
    public string Duygu;

    [JsonProperty("ikna")]
    public string Ikna;
}

public class GeminiController : MonoBehaviour
{
    [Header("API Ayarları")]
    [SerializeField, Tooltip("Google Gemini API Anahtarınız")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    [Header("UI Bağlantıları")]
    [Tooltip("Sonucun gösterileceği TextMeshPro nesnesi")]
    public TMP_Text sonucText;

    public static event Action<AIResponse> onAIResponseAlındı;
    public static event Action<string, string> onKonusmaGecmisiEklendi;
    public static event Action<string> onCocukTepkisiUretildi;
    public static event Action<string> onDuyguBelirlendi;
    public static event Action onKonusmaBitti;
    public static event Action<string> onCocukAnimSecildi;

    [Header("Oyun Akışı Ayarları")]
    [SerializeField, Tooltip("Seviyenin bitmesi için gereken toplam konuşma sayısı.")]
    private int seviyeBitisKonusmaSayisi = 10;
    private int konusmaSayaci = 0;

    private List<string> currentConversationHistory;
    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;
    private List<string> iknaSonuclari = new List<string>();

    [SerializeField] private AffectSystem affect; // Inspector’dan çocuk avatar (AffectSystem) sürükle-bırak

    private const string SYSTEM_PRIMER = @"
Sen bir VR diş hekimi simülasyonunda 8 yaşında bir kız çocuğusun.
Görev:
1) Doktorun cümlesini 'pozitif', 'negatif' veya 'nötr' olarak sınıflandır.
2) Kısa, doğal ve çocukça bir tepki üret. Unutma: çocuklar dişçiye gitmeyi sevmez; tedavi, diş çekimi, dolgu ve benzeri istekleri genelde reddedersin. Biraz inatçı ol.
3) Tepkiye uygun animasyon seç.
4) Duyguyu belirt.
5) (İsteğe bağlı) ikna durumunu 'evet' | 'hayır' | 'kararsız' olarak ver.

Sadece JSON dön:
{
  ""kategori"": ""pozitif|negatif|nötr"",
  ""tepki"": ""<çocuğun kısa cevabı>"",
  ""animasyon"": ""aglama|gulme|bekleme|korkma|gulumseme"",
  ""duygu"": ""mutlu|endişeli|nötr|kötü"",
  ""ikna"": ""evet|hayır|kararsız""
}
";

    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";
    }

    void OnEnable()
    {
        EmotionObserver.onKonusmaGecmisiGuncellendi += GecmisiAlVeGeminiyeGonder;
        ElevenLabsVoiceDemo.onTextTranscribed += MetniAlVeGeminiyeGonder;
        GameManager.onLevelStart += KonusmaSayaciniSifirla;
    }

    void OnDisable()
    {
        EmotionObserver.onKonusmaGecmisiGuncellendi -= GecmisiAlVeGeminiyeGonder;
        ElevenLabsVoiceDemo.onTextTranscribed -= MetniAlVeGeminiyeGonder;
        GameManager.onLevelStart -= KonusmaSayaciniSifirla;
    }

    public void KonusmaSayaciniSifirla()
    {
        konusmaSayaci = 0;
        Debug.Log("GeminiController: Konuşma sayacı sıfırlandı.");
    }

    public void GecmisiAlVeGeminiyeGonder(List<string> updatedHistory)
    {
        Debug.Log("Yeni konuşma geçmişi alındı! Toplam satır: " + updatedHistory.Count);
        this.currentConversationHistory = updatedHistory;
    }

    public void MetniAlVeGeminiyeGonder(string gelenMetin)
    {
        Debug.Log($"GeminiController, şu metni işlemek üzere aldı: '{gelenMetin}'");

        if (string.IsNullOrWhiteSpace(gelenMetin))
        {
            if (sonucText != null) sonucText.text = "Gelen metin boş, işlem yapılamadı.";
            Debug.LogWarning("İşlenecek metin boş olduğu için işlem iptal edildi.");
            return;
        }

        StartCoroutine(ClassifyAndRespond(gelenMetin));
    }

    private string HesaplaIknaDurumu()
    {
        int evet = iknaSonuclari.FindAll(x => x == "evet").Count;
        int hayir = iknaSonuclari.FindAll(x => x == "hayır").Count;
        int kararsiz = iknaSonuclari.FindAll(x => x == "kararsız").Count;

        if (evet > hayir && evet > kararsiz) return "ikna oldu (evet)";
        if (hayir > evet && hayir > kararsiz) return "ikna olmadı (hayır)";
        return "kararsız";
    }

    // ---- Yardımcılar ----
    private static string NormalizeTr(string s)
    {
        if (s == null) return "";
        s = s.ToLowerInvariant();
        s = s.Replace('ı', 'i').Replace('İ', 'i')
             .Replace('ö', 'o').Replace('Ö', 'o')
             .Replace('ü', 'u').Replace('Ü', 'u')
             .Replace('ş', 's').Replace('Ş', 's')
             .Replace('ğ', 'g').Replace('Ğ', 'g')
             .Replace('ç', 'c').Replace('Ç', 'c');
        return s;
    }

    private static Deger DegerHaritasi(string kategori)
    {
        if (string.IsNullOrEmpty(kategori)) return Deger.Notr;
        string k = NormalizeTr(kategori);
        if (k.Contains("pozitif") || k.Contains("olumlu") || k.Contains("iyi")) return Deger.Pozitif;
        if (k.Contains("negatif") || k.Contains("olumsuz") || k.Contains("kotu")) return Deger.Negatif;
        if (k.Contains("notr") || k.Contains("nötr")) return Deger.Notr;
        return Deger.Notr;
    }

    private static string GuardedVersion(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return "Biraz endişeli hissediyorum... Yavaş gidebilir miyiz?";
        return $"Biraz endişeli hissediyorum... {original}";
    }

    // ---- Ana coroutine ----
    private IEnumerator ClassifyAndRespond(string doktorCumlesi)
    {
        if (sonucText != null) sonucText.text = "Dinleniyor...";

        var requestBody = new GeminiMultiTurnRequest
        {
            Contents = new List<ContentEntry>()
        };

        if (currentConversationHistory != null && currentConversationHistory.Count > 0)
        {
            foreach (var line in currentConversationHistory)
            {
                string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string speaker = parts[0];
                    string text = parts[1];

                    // Konuşmacıya göre rol
                    string role = (speaker.ToLower() == "doktor") ? "user" : "model";

                    requestBody.Contents.Add(new ContentEntry
                    {
                        Role = role,
                        Parts = new[] { new RequestPart { Text = text } }
                    });
                }
            }
        }

        // Şimdi bu cümleye yanıt ver
        string finalUserPrompt = $"{SYSTEM_PRIMER}\n\n---\n\nDoktorun YENİ cümlesi: \"{doktorCumlesi}\"";

        requestBody.Contents.Add(new ContentEntry
        {
            Role = "user",
            Parts = new[] { new RequestPart { Text = finalUserPrompt } }
        });

        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Gemini'ye gönderilen yapılandırılmış JSON:\n" + jsonRequestBody);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest request = new UnityWebRequest(_apiURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            CocukTepki sonuc = null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini API Hatası: {request.error}\nYanıt: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = $"Bir API hatası oluştu: {request.error}";
            }
            else
            {
                string hamCikti = request.downloadHandler.text;
                try
                {
                    GeminiApiResponse apiResponse = JsonConvert.DeserializeObject<GeminiApiResponse>(hamCikti);
                    string modelinUrettigiText = apiResponse.Candidates[0].Content.Parts[0].Text;

                    // JSON'u güvenle çek
                    Match match = Regex.Match(modelinUrettigiText, @"\{.*\}", RegexOptions.Singleline);
                    string temizlenmisJson = match.Success ? match.Value : modelinUrettigiText;

                    sonuc = JsonConvert.DeserializeObject<CocukTepki>(temizlenmisJson);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"JSON parse hatası: {e.Message}\nHam Çıktı: {hamCikti}");
                    if (sonucText != null) sonucText.text = "Yanıtta geçerli bir format bulunamadı.";
                }
            }

            if (sonuc != null)
            {
                // ========== [DUYGU ATÂLETİ] ==========
                if (affect != null)
                {
                    var deger = DegerHaritasi(sonuc.Kategori);
                    affect.CumleKaydet(deger);
                }

                // Kötü moddaysa, pozitif cevabı bir anda şenlendirmeyelim → temkinli varyant
                if (affect != null && affect.Durum == DuyguDurumu.Kotu)
                {
                    string k = NormalizeTr(sonuc.Kategori);
                    if (k.Contains("pozitif") || k.Contains("olumlu") || k.Contains("iyi"))
                    {
                        sonuc.Tepki = GuardedVersion(sonuc.Tepki);
                        sonuc.Animasyon = "bekleme"; // ani sevinç yerine sakin
                        sonuc.Duygu = "tedirgin";    // “mutlu”ya sıçrama yok
                    }
                }
                // =====================================

                // UI yaz
                if (sonucText != null)
                {
                    sonucText.text = $"Kategori: {sonuc.Kategori}\n" +
                                     $"Tepki: {sonuc.Tepki}\n" +
                                     $"Animasyon: {sonuc.Animasyon}\n" +
                                     $"Duygu: {sonuc.Duygu}\n" +
                                     $"İkna: {sonuc.Ikna}";
                }

                // Event nesnesi
                AIResponse response = new AIResponse
                {
                    Kategori = sonuc.Kategori,
                    Tepki = sonuc.Tepki,
                    Animasyon = sonuc.Animasyon,
                    Duygu = sonuc.Duygu,
                    Ikna = sonuc.Ikna
                };
                onAIResponseAlındı?.Invoke(response);

                Debug.Log($"Başarılı! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}, Animasyon: {sonuc.Animasyon}, Duygu: {sonuc.Duygu}");

                onCocukTepkisiUretildi?.Invoke(sonuc.Tepki);
                onDuyguBelirlendi?.Invoke(sonuc.Duygu);
                onKonusmaGecmisiEklendi?.Invoke(doktorCumlesi, "Doktor");
                onKonusmaGecmisiEklendi?.Invoke(sonuc.Tepki, "Çocuk");
                onCocukAnimSecildi?.Invoke(sonuc.Animasyon);
                iknaSonuclari.Add(sonuc.Ikna);

                konusmaSayaci++;
                Debug.Log($"Konuşma tamamlandı. Toplam konuşma sayısı: {konusmaSayaci}/{seviyeBitisKonusmaSayisi}");

                if (konusmaSayaci >= seviyeBitisKonusmaSayisi)
                {
                    string finalIknaDurumu = HesaplaIknaDurumu();
                    sonucText.text += $"\n\n---\nFinal Karar: Çocuk {finalIknaDurumu}";
                    Debug.Log($"10 diyalog tamamlandı. Final Sonuç: {finalIknaDurumu}");
                    onKonusmaBitti?.Invoke();
                }
            }
        }
    }
}
