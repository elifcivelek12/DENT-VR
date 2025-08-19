// ---- Gerekli Kütüphaneler ----
using System.Collections; // Coroutine'ler gibi Unity'nin temel işlevleri için.
using System.Text; // Metin ve byte dizileri arasında dönüşüm yapmak için (JSON gönderimi).
using UnityEngine; // Unity motorunun temel sınıfları ve işlevleri için.
using UnityEngine.Networking; // Web istekleri göndermek ve almak için (API iletişimi).
using TMPro; // Gelişmiş metin renderlama için TextMeshPro kütüphanesi.
using Newtonsoft.Json; // C# nesnelerini JSON formatına dönüştürmek ve tersini yapmak için popüler bir kütüphane.
using UnityEngine.Events; // Unity içinde event (olay) tabanlı sistemler oluşturmak için.
using System.Text.RegularExpressions; // Metin içinde belirli desenleri (JSON gibi) aramak için.
using System; // Temel .NET olayları ve veri türleri için.
using System.Collections.Generic; // Listeler gibi koleksiyon yapılarını kullanmak için.


// ---- Yapay Zeka Yanıt Veri Yapısı ----
// Bu sınıf, yapay zekadan gelen JSON yanıtını temsil eden temel veri modelidir.
[System.Serializable] // Unity Inspector'da görünebilmesi için bu attribute eklenir.
public class AIResponse
{
    public string Kategori; // Doktorun cümlesinin kategorisi (pozitif, negatif, nötr).
    public string Tepki; // Çocuğun ürettiği metinsel cevap.
    public string Animasyon; // Çocuğun tepkisine uygun animasyon adı.
    public string Duygu; // Çocuğun o anki duygusal durumu.
    public string Ikna; // Çocuğun ikna olup olmadığı (evet, hayır, kararsız).
}

// ---- Gemini API Model Yanıt Tipleri ----
// Bu sınıflar, Gemini API'sinden dönen ham JSON yanıtını doğrudan C# nesnelerine dönüştürmek (deserialize) için kullanılır.
// Yanıtın iç içe geçmiş yapısını (candidates -> content -> parts -> text) yansıtırlar.
public class GeminiApiResponse { [JsonProperty("candidates")] public Candidate[] Candidates; }
public class Candidate { [JsonProperty("content")] public GeminiContent Content; }
public class GeminiContent { [JsonProperty("parts")] public GeminiPart[] Parts; }
public class GeminiPart { [JsonProperty("text")] public string Text; }

// ---- Gemini API'ye Çoklu Tur Konuşma İsteği Yapısı ----
// Bu sınıflar, API'ye konuşma geçmişini de içeren bir istek göndermek için kullanılır.
// C# nesneleri olarak oluşturulur ve ardından JSON formatına dönüştürülür (serialize).
public class GeminiMultiTurnRequest
{
    // Konuşma geçmişini ve yeni istemi içeren liste.
    [JsonProperty("contents")]
    public List<ContentEntry> Contents { get; set; }
}

public class ContentEntry
{
    // Konuşan kişinin rolü: "user" (kullanıcı/doktor) veya "model" (yapay zeka/çocuk).
    [JsonProperty("role")]
    public string Role { get; set; } 

    // O konuşma turundaki metin içeriği.
    [JsonProperty("parts")]
    public RequestPart[] Parts { get; set; }
}

public class RequestPart
{
    // Gönderilecek asıl metin.
    [JsonProperty("text")]
    public string Text { get; set; }
}

// ---- Çocuk Tepkisi Veri Yapısı ----
// API'den gelen metin içindeki JSON bloğunu deserialize etmek için kullanılır. AIResponse ile benzerdir.
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

// ---- Ana Kontrolcü Sınıfı ----
// Gemini API ile tüm iletişimi yönetir, gelen metinleri işler ve oyunun geri kalanına olaylar (events) aracılığıyla bilgi verir.
public class GeminiController : MonoBehaviour
{
    [Header("API Ayarları")]
    [SerializeField, Tooltip("Google Gemini API Anahtarınız")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0"; // API anahtarı.

    [Header("UI Bağlantıları")]
    [Tooltip("Sonucun gösterileceği TextMeshPro nesnesi")]
    public TMP_Text sonucText; // Debug veya test amaçlı sonuçların gösterileceği UI metin alanı.

    // ---- Olaylar (Events) ----
    // Bu olaylar, GeminiController'ın diğer sistemlerle (UI, animasyon, ses vb.) gevşek bağlı (loosely coupled) bir şekilde iletişim kurmasını sağlar.
    public static event Action<AIResponse> onAIResponseAlındı; // Tam bir yapay zeka yanıtı alındığında tetiklenir.
    public static event Action<string, string> onKonusmaGecmisiEklendi; // Konuşma geçmişine yeni bir satır eklendiğinde tetiklenir.
    public static event Action<string> onCocukTepkisiUretildi; // Çocuğun konuşacağı metin üretildiğinde tetiklenir.
    public static event Action<string> onDuyguBelirlendi; // Çocuğun duygusu belirlendiğinde tetiklenir.
    public static event Action onKonusmaBitti; // Seviye için belirlenen konuşma sayısı tamamlandığında tetiklenir.
    public static event Action<string> onCocukAnimSecildi; // Çocuk için bir animasyon seçildiğinde tetiklenir.

    [Header("Oyun Akışı Ayarları")]
    [SerializeField, Tooltip("Seviyenin bitmesi için gereken toplam konuşma sayısı.")]
    private int seviyeBitisKonusmaSayisi = 10; // Bir seviyenin tamamlanması için gereken diyalog turu sayısı.
    private int konusmaSayaci = 0; // Mevcut diyalog turu sayacını tutar.

    private List<string> currentConversationHistory; // Mevcut konuşma geçmişini tutan liste.
    private const string MODEL_NAME = "gemini-1.5-flash"; // Kullanılacak Gemini modelinin adı.
    private string _apiURL; // API isteğinin gönderileceği tam URL.
    private List<string> iknaSonuclari = new List<string>(); // Konuşma boyunca "ikna" durumlarını biriktirir.

    [SerializeField] private AffectSystem affect; // Çocuğun genel duygu durumunu yöneten sisteme referans. Inspector'dan atanır.

    // ---- Sistem Talimatı (System Primer) ----
    // Yapay zekanın kim olacağını, görevlerini ve nasıl bir formatta yanıt vermesi gerektiğini belirten ana talimat metni.
    // Bu, modelin rolünü (8 yaşında kız çocuğu) ve JSON formatını zorunlu kılar.
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

    // Script ilk yüklendiğinde bir kere çalışır.
    void Awake()
    {
        // API URL'sini model adı ve anahtarla birlikte oluşturur.
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";
    }

    // Nesne aktif olduğunda çalışır. Olaylara (events) abone olmak için idealdir.
    void OnEnable()
    {
        EmotionObserver.onKonusmaGecmisiGuncellendi += GecmisiAlVeGeminiyeGonder;
        ElevenLabsVoiceDemo.onTextTranscribed += MetniAlVeGeminiyeGonder;
        GameManager.onLevelStart += KonusmaSayaciniSifirla;
    }

    // Nesne pasif olduğunda çalışır. Bellek sızıntılarını önlemek için olay aboneliklerini iptal etmek önemlidir.
    void OnDisable()
    {
        EmotionObserver.onKonusmaGecmisiGuncellendi -= GecmisiAlVeGeminiyeGonder;
        ElevenLabsVoiceDemo.onTextTranscribed -= MetniAlVeGeminiyeGonder;
        GameManager.onLevelStart -= KonusmaSayaciniSifirla;
    }

    // Konuşma sayacını sıfırlar. Genellikle yeni bir seviye başladığında çağrılır.
    public void KonusmaSayaciniSifirla()
    {
        konusmaSayaci = 0;
        Debug.Log("GeminiController: Konuşma sayacı sıfırlandı.");
    }

    // Başka bir script'ten güncellenmiş konuşma geçmişini alır ve yerel kopyayı günceller.
    public void GecmisiAlVeGeminiyeGonder(List<string> updatedHistory)
    {
        Debug.Log("Yeni konuşma geçmişi alındı! Toplam satır: " + updatedHistory.Count);
        this.currentConversationHistory = updatedHistory;
    }

    // Ses tanıma sisteminden (ElevenLabsVoiceDemo) gelen metni alıp API'ye gönderme sürecini başlatır.
    public void MetniAlVeGeminiyeGonder(string gelenMetin)
    {
        Debug.Log($"GeminiController, şu metni işlemek üzere aldı: '{gelenMetin}'");

        // Gelen metnin boş olup olmadığını kontrol eder.
        if (string.IsNullOrWhiteSpace(gelenMetin))
        {
            if (sonucText != null) sonucText.text = "Gelen metin boş, işlem yapılamadı.";
            Debug.LogWarning("İşlenecek metin boş olduğu için işlem iptal edildi.");
            return;
        }

        // API isteğini asenkron olarak (oyunu dondurmadan) yapmak için bir Coroutine başlatır.
        StartCoroutine(ClassifyAndRespond(gelenMetin));
    }

    // Konuşma sonunda genel ikna durumunu hesaplar.
    private string HesaplaIknaDurumu()
    {
        int evet = iknaSonuclari.FindAll(x => x == "evet").Count;
        int hayir = iknaSonuclari.FindAll(x => x == "hayır").Count;
        int kararsiz = iknaSonuclari.FindAll(x => x == "kararsız").Count;

        if (evet > hayir && evet > kararsiz) return "ikna oldu (evet)";
        if (hayir > evet && hayir > kararsiz) return "ikna olmadı (hayır)";
        return "kararsız";
    }

    // ---- Yardımcı Metotlar ----
    // Türkçe karakterleri İngilizce karşılıklarına dönüştürerek metin karşılaştırmalarını kolaylaştırır.
    private static string NormalizeTr(string s)
    {
        if (s == null) return "";
        s = s.ToLowerInvariant(); // Tüm harfleri küçük harfe çevirir.
        s = s.Replace('ı', 'i').Replace('İ', 'i')
             .Replace('ö', 'o').Replace('Ö', 'o')
             .Replace('ü', 'u').Replace('Ü', 'u')
             .Replace('ş', 's').Replace('Ş', 's')
             .Replace('ğ', 'g').Replace('Ğ', 'g')
             .Replace('ç', 'c').Replace('Ç', 'c');
        return s;
    }

    // Gelen kategori metnini (string) bir enum değerine dönüştürür.
    private static Deger DegerHaritasi(string kategori)
    {
        if (string.IsNullOrEmpty(kategori)) return Deger.Notr;
        string k = NormalizeTr(kategori);
        if (k.Contains("pozitif") || k.Contains("olumlu") || k.Contains("iyi")) return Deger.Pozitif;
        if (k.Contains("negatif") || k.Contains("olumsuz") || k.Contains("kotu")) return Deger.Negatif;
        if (k.Contains("notr") || k.Contains("nötr")) return Deger.Notr;
        return Deger.Notr; // Eşleşme bulunamazsa varsayılan olarak nötr döner.
    }

    // Çocuğun modu kötüyken gelen pozitif bir cevabı daha temkinli bir versiyona çevirir.
    private static string GuardedVersion(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return "Biraz endişeli hissediyorum... Yavaş gidebilir miyiz?";
        return $"Biraz endişeli hissediyorum... {original}";
    }

    // ---- Ana Coroutine: API İsteği ve Yanıt İşleme ----
    // API'ye isteği gönderir, yanıtı bekler ve gelen veriyi işler.
    private IEnumerator ClassifyAndRespond(string doktorCumlesi)
    {
        if (sonucText != null) sonucText.text = "Dinleniyor..."; // UI'da kullanıcıya geri bildirim verir.

        // API'ye gönderilecek isteğin gövdesini (body) oluşturur.
        var requestBody = new GeminiMultiTurnRequest
        {
            Contents = new List<ContentEntry>()
        };

        // Eğer mevcut bir konuşma geçmişi varsa, bunu isteğe ekler.
        if (currentConversationHistory != null && currentConversationHistory.Count > 0)
        {
            foreach (var line in currentConversationHistory)
            {
                // Satırı "Konuşmacı: Metin" formatından ayırır.
                string[] parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string speaker = parts[0];
                    string text = parts[1];

                    // Konuşmacıya göre rolü ("user" veya "model") belirler.
                    string role = (speaker.ToLower() == "doktor") ? "user" : "model";

                    // Konuşma geçmişi girişini isteğe ekler.
                    requestBody.Contents.Add(new ContentEntry
                    {
                        Role = role,
                        Parts = new[] { new RequestPart { Text = text } }
                    });
                }
            }
        }

        // Doktorun son cümlesini ve ana sistem talimatını birleştirerek son kullanıcı istemini (prompt) oluşturur.
        string finalUserPrompt = $"{SYSTEM_PRIMER}\n\n---\n\nDoktorun YENİ cümlesi: \"{doktorCumlesi}\"";

        // Bu son istemi de isteğe ekler.
        requestBody.Contents.Add(new ContentEntry
        {
            Role = "user",
            Parts = new[] { new RequestPart { Text = finalUserPrompt } }
        });

        // C# nesnesini JSON formatında bir metne dönüştürür.
        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        Debug.Log("Gemini'ye gönderilen yapılandırılmış JSON:\n" + jsonRequestBody);

        // JSON metnini byte dizisine çevirir.
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

        // UnityWebRequest kullanarak bir POST isteği oluşturur. 'using' bloğu, işlem bitince kaynakların otomatik serbest bırakılmasını sağlar.
        using (UnityWebRequest request = new UnityWebRequest(_apiURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw); // Gövdeyi ayarlar.
            request.downloadHandler = new DownloadHandlerBuffer(); // Yanıtı alacak olan handler.
            request.SetRequestHeader("Content-Type", "application/json"); // İçerik tipini belirtir.

            // İsteği gönderir ve yanıt gelene kadar bekler.
            yield return request.SendWebRequest();

            CocukTepki sonuc = null;

            // İstek başarısız olursa...
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini API Hatası: {request.error}\nYanıt: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = $"Bir API hatası oluştu: {request.error}";
            }
            else // İstek başarılı olursa...
            {
                string hamCikti = request.downloadHandler.text; // API'den gelen ham metin yanıtı.
                try
                {
                    // Önce genel API yanıt yapısını deserialize eder.
                    GeminiApiResponse apiResponse = JsonConvert.DeserializeObject<GeminiApiResponse>(hamCikti);
                    // Modelin ürettiği asıl metni alır.
                    string modelinUrettigiText = apiResponse.Candidates[0].Content.Parts[0].Text;

                    // Modelin ürettiği metin içindeki JSON bloğunu Regex ile güvenli bir şekilde ayıklar.
                    Match match = Regex.Match(modelinUrettigiText, @"\{.*\}", RegexOptions.Singleline);
                    string temizlenmisJson = match.Success ? match.Value : modelinUrettigiText;

                    // Ayıklanan JSON'u CocukTepki nesnesine dönüştürür.
                    sonuc = JsonConvert.DeserializeObject<CocukTepki>(temizlenmisJson);
                }
                catch (System.Exception e)
                {
                    // JSON parse etme sırasında bir hata olursa loglar.
                    Debug.LogError($"JSON parse hatası: {e.Message}\nHam Çıktı: {hamCikti}");
                    if (sonucText != null) sonucText.text = "Yanıtta geçerli bir format bulunamadı.";
                }
            }

            // Eğer 'sonuc' başarılı bir şekilde oluşturulduysa...
            if (sonuc != null)
            {
                // ========== [DUYGU ATÂLETİ (EMOTIONAL INERTIA)] ==========
                // Bu bölüm, çocuğun duygusal durumunun aniden değişmesini önler.
                if (affect != null)
                {
                    var deger = DegerHaritasi(sonuc.Kategori); // "pozitif" -> Deger.Pozitif
                    affect.CumleKaydet(deger); // AffectSystem'e cümlenin duygusal değerini bildirir.
                }

                // Eğer çocuğun genel duygu durumu "Kötü" ise ve API pozitif bir tepki ürettiyse...
                if (affect != null && affect.Durum == DuyguDurumu.Kotu)
                {
                    string k = NormalizeTr(sonuc.Kategori);
                    if (k.Contains("pozitif") || k.Contains("olumlu") || k.Contains("iyi"))
                    {
                        // ...tepkiyi daha temkinli bir versiyonla değiştirir.
                        sonuc.Tepki = GuardedVersion(sonuc.Tepki);
                        sonuc.Animasyon = "bekleme"; // Ani sevinç yerine daha sakin bir animasyon seçer.
                        sonuc.Duygu = "tedirgin";    // "mutlu"ya hemen zıplamak yerine daha ara bir duygu belirler.
                    }
                }
                // ========================================================

                // UI metnini sonuçlarla günceller.
                if (sonucText != null)
                {
                    sonucText.text = $"Kategori: {sonuc.Kategori}\n" +
                                     $"Tepki: {sonuc.Tepki}\n" +
                                     $"Animasyon: {sonuc.Animasyon}\n" +
                                     $"Duygu: {sonuc.Duygu}\n" +
                                     $"İkna: {sonuc.Ikna}";
                }

                // Diğer sistemleri bilgilendirmek için bir olay (event) nesnesi oluşturur.
                AIResponse response = new AIResponse
                {
                    Kategori = sonuc.Kategori,
                    Tepki = sonuc.Tepki,
                    Animasyon = sonuc.Animasyon,
                    Duygu = sonuc.Duygu,
                    Ikna = sonuc.Ikna
                };
                onAIResponseAlındı?.Invoke(response); // Olayı tetikler.

                Debug.Log($"Başarılı! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}, Animasyon: {sonuc.Animasyon}, Duygu: {sonuc.Duygu}");

                // İlgili diğer tüm olayları tetikler.
                onCocukTepkisiUretildi?.Invoke(sonuc.Tepki);
                onDuyguBelirlendi?.Invoke(sonuc.Duygu);
                onKonusmaGecmisiEklendi?.Invoke(doktorCumlesi, "Doktor");
                onKonusmaGecmisiEklendi?.Invoke(sonuc.Tepki, "Çocuk");
                onCocukAnimSecildi?.Invoke(sonuc.Animasyon);
                iknaSonuclari.Add(sonuc.Ikna); // İkna sonucunu listeye ekler.

                konusmaSayaci++; // Konuşma sayacını bir artırır.
                Debug.Log($"Konuşma tamamlandı. Toplam konuşma sayısı: {konusmaSayaci}/{seviyeBitisKonusmaSayisi}");

                // Eğer konuşma sayısı seviye bitiş sınırına ulaştıysa...
                if (konusmaSayaci >= seviyeBitisKonusmaSayisi)
                {
                    string finalIknaDurumu = HesaplaIknaDurumu(); // Final ikna durumunu hesaplar.
                    sonucText.text += $"\n\n---\nFinal Karar: Çocuk {finalIknaDurumu}";
                    Debug.Log($"10 diyalog tamamlandı. Final Sonuç: {finalIknaDurumu}");
                    onKonusmaBitti?.Invoke(); // Konuşmanın bittiğini bildiren olayı tetikler.
                }
            }
        }
    }
}
