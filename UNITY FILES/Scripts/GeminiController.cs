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
    public string Role { get; set; } 

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
    [Header("Duygu Sistemi Bağlantısı")]
    public AffectSystem affectSystem;

    [Header("API Ayarları")]
    [SerializeField, Tooltip("Google Gemini API Anahtarınız")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    [Header("UI Ba�lant�lar�")]
    [Tooltip("Sonucun g�sterilece�i TextMeshPro nesnesi")]
    public TMP_Text sonucText;

    public static event Action<AIResponse> onAIResponseAlındı;
    public static event Action<string, string> onKonusmaGecmisiEklendi;
    public static event Action<string> onCocukTepkisiUretildi; 
    public static event Action onKonusmaBitti;    

    [Header("Oyun Ak��� Ayarlar�")]
    [SerializeField, Tooltip("Seviyenin bitmesi i�in gereken toplam konu�ma say�s�.")]
    private int seviyeBitisKonusmaSayisi = 5;
    private int konusmaSayaci = 0;

    private List<string> currentConversationHistory;
    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;
    private List<string> iknaSonuclari = new List<string>();
    private string SYSTEM_PRIMER;

    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";
        string promptPath = "prompts/tripliprompt";

        TextAsset promptAsset = Resources.Load<TextAsset>(promptPath);

        if (promptAsset == null)
        {
            Debug.LogError($"Prompt dosyası bulunamadı: Assets/Resources/{promptPath}");
            return;
        }
        else
        {
            Debug.Log("Prompt dosyası bulundu ve prompt asset alındı");
        }

        SYSTEM_PRIMER = promptAsset.text;

        Debug.Log("Prompt başarıyla yüklendi.");
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
        currentConversationHistory.Clear(); 
        iknaSonuclari.Clear();

        Debug.Log("GeminiController: Konuşma sayacı sıfırlandı.");
    }

    public void GecmisiAlVeGeminiyeGonder(List<string> updatedHistory)
    {
        Debug.Log("Yeni konuşma geçmişi alındı! Toplam satır: " + updatedHistory.Count);
        this.currentConversationHistory = updatedHistory;
    }
    public void MetniAlVeGeminiyeGonder(string gelenMetin)
    {
        Debug.Log($"GeminiController, su metni islemek uzere aldı: '{gelenMetin}' ");
        
        if (string.IsNullOrWhiteSpace(gelenMetin))
        {
            if (sonucText != null) sonucText.text = "Gelen metin bos, islem yapılamadı.";
            Debug.LogWarning("Islenecek metin bos oldugu icin islem iptal edildi.");
            return;
        }

        StartCoroutine(ClassifyAndRespond(gelenMetin));
        Debug.Log("Metin Gemini'ye yollandı");
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

                    // Konuşmacıya göre rolü belirliyoruz. API "user" ve "model" rollerini anlar.
                    string role = (speaker.ToLower() == "doktor") ? "user" : "model";

                    // Konuşmanın bu parçasını listeye ekliyoruz.
                    requestBody.Contents.Add(new ContentEntry
                    {
                        Role = role,
                        Parts = new[] { new RequestPart { Text = text } }
                    });
                }
            }
        }

        // Bu, modele "tüm bu geçmişe ve bu kurallara bakarak ŞİMDİ BU CÜMLEYE cevap ver" demektir.
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

                    Match match = Regex.Match(modelinUrettigiText, @"\{.*\}", RegexOptions.Singleline);

                    string temizlenmisJson = modelinUrettigiText;
                    if (match.Success)
                    {
                        temizlenmisJson = match.Value;
                    }

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
                if (sonucText != null)
                {
                    sonucText.text = $"Kategori: {sonuc.Kategori}\n" +
                                        $"Tepki: {sonuc.Tepki}\n" +
                                        $"Animasyon: {sonuc.Animasyon}\n" +
                                        $"Duygu: {sonuc.Duygu}\n" +
                                        $"İkna: {sonuc.Ikna}";

                    AIResponse response = new AIResponse
                    {
                        Kategori = sonuc.Kategori,
                        Tepki = sonuc.Tepki,
                        Animasyon = sonuc.Animasyon,
                        Duygu = sonuc.Duygu,
                        Ikna =sonuc.Ikna,
                    };
                    onAIResponseAlındı?.Invoke(response);

                    if (affectSystem != null)
                    {
                        Deger gelenDeger = Deger.Notr;
                        switch (sonuc.Kategori.ToLower())
                        {
                            case "pozitif": gelenDeger = Deger.Pozitif; break;
                            case "negatif": gelenDeger = Deger.Negatif; break;
                        }
                        affectSystem.CumleKaydet(gelenDeger);
                        Debug.Log($"<color=cyan>[GeminiController] AffectSystem'e bildirildi: {gelenDeger}</color>");
                    }
                }

                Debug.Log($"Başarılı! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}, Animasyon: {sonuc.Animasyon}, Duygu: {sonuc.Duygu}");

                onCocukTepkisiUretildi?.Invoke(sonuc.Tepki);
                onKonusmaGecmisiEklendi?.Invoke(doktorCumlesi, "Doktor");
                onKonusmaGecmisiEklendi?.Invoke(sonuc.Tepki, "Çocuk");
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