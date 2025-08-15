using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.Events;
using System.Text.RegularExpressions;
using System;

// --- API Sýnýflarý (Deðiþiklik Yok) ---
public class GeminiApiResponse { [JsonProperty("candidates")] public Candidate[] Candidates; }
public class Candidate { [JsonProperty("content")] public GeminiContent Content; }
public class GeminiContent { [JsonProperty("parts")] public GeminiPart[] Parts; }
public class GeminiPart { [JsonProperty("text")] public string Text; }

[System.Serializable]
public class CocukTepki
{
    [JsonProperty("kategori")]
    public string Kategori;

    [JsonProperty("tepki")]
    public string Tepki;

    [JsonProperty("animasyon")]
    public string Animasyon; // Yeni eklendi

    [JsonProperty("duygu")]
    public string Duygu; // Yeni eklendi
}

public class GeminiController : MonoBehaviour
{


    [Header("API Ayarlarý")]
    // Deðiþkeni private yapýp [SerializeField] eklemek, onu Inspector'da gösterir
    // ama diðer script'lerden doðrudan eriþilmesini engeller. Bu daha iyi bir pratiktir.
    [SerializeField, Tooltip("Google Gemini API Anahtarýnýz")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    [Header("UI Baðlantýlarý")]
    [Tooltip("Sonucun gösterileceði TextMeshPro nesnesi")]
    public TMP_Text sonucText;

    [Header("Olaylar (Events)")]
    public UnityEvent<string, string> onKonusmaGecmisiEklendi;
    public UnityEvent<string> onCocukTepkisiUretildi;
    public static event Action<string> onDuyguBelirlendi;

    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;

    private const string SYSTEM_PRIMER = @"
Sen bir VR diþ hekimi simülasyonunda 8 yaþýnda bir kýz çocuðusun.
Görev:
1) Doktorun cümlesini 'pozitif', 'negatif' veya 'nötr' olarak sýnýflandýr.
2) Kýsa, doðal ve çocukça bir tepki üret.
3) Tepkiye uygun animasyon seç.
4) Duyguyu belirt.

JSON formatýnda dön:
{
    ""kategori"": ""pozitif|negatif|nötr"",
    ""tepki"": ""<çocuðun kýsa cevabý>"",
    ""animasyon"": ""<oynatýlacak animasyon>"",
    ""duygu"": ""olumlu|kötü""
}
JSON dýþýnda metin yazma.

Örnekler:
- Pozitif: ""Çok cesursun, aferin sana."", animasyon: ""gülümseme"", duygu: ""mutlu""
- Negatif: ""Hareket edersen acýyabilir."", animasyon: ""korku"", duygu: ""endiþeli""
- Nötr: ""Lütfen koltuða otur."", animasyon: ""bekleme"", duygu: ""nötr"" ";

    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";
    }

    public void MetniAlVeGeminiyeGonder(string gelenMetin)
    {
        Debug.Log($"GeminiController, þu metni iþlemek üzere aldý: '{gelenMetin}'");

        if (string.IsNullOrWhiteSpace(gelenMetin))
        {
            if (sonucText != null) sonucText.text = "Gelen metin boþ, iþlem yapýlamadý.";
            Debug.LogWarning("Ýþlenecek metin boþ olduðu için iþlem iptal edildi.");
            return;
        }

        StartCoroutine(ClassifyAndRespond(gelenMetin));
    }

    private IEnumerator ClassifyAndRespond(string doktorCumlesi)
    {
        if (sonucText != null) sonucText.text = "Düþünüyor...";

        string prompt = $"{SYSTEM_PRIMER}\n\nDoktorun cümlesi: \"{doktorCumlesi}\"";
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
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
                Debug.LogError($"Gemini API Hatasý: {request.error}\nYanýt: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = $"Bir API hatasý oluþtu: {request.error}";
            }
            else
            {
                string hamCikti = request.downloadHandler.text;
                try
                {

                    GeminiApiResponse apiResponse = JsonConvert.DeserializeObject<GeminiApiResponse>(hamCikti);
                    string modelinUrettigiText = apiResponse.Candidates[0].Content.Parts[0].Text;


                    Match match = Regex.Match(modelinUrettigiText, @"\{.*\}", RegexOptions.Singleline);

                    string temizlenmisJson = modelinUrettigiText; // Varsayýlan deðer
                    if (match.Success)
                    {
                        temizlenmisJson = match.Value;
                    }


                    sonuc = JsonConvert.DeserializeObject<CocukTepki>(temizlenmisJson);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"JSON parse hatasý: {e.Message}\nHam Çýktý: {hamCikti}");
                    if (sonucText != null) sonucText.text = "Yanýtta geçerli bir format bulunamadý.";
                }
            }

            if (sonuc != null)
            {
                if (sonucText != null)
                {
                    sonucText.text = $"Kategori: {sonuc.Kategori}\n" +
                                     $"Tepki: {sonuc.Tepki}\n" +
                                     $"Animasyon: {sonuc.Animasyon}\n" +
                                     $"Duygu: {sonuc.Duygu}";
                }

                Debug.Log($"Baþarýlý! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}, Animasyon: {sonuc.Animasyon}, Duygu: {sonuc.Duygu}");

                if (onCocukTepkisiUretildi != null)
                {
                    onCocukTepkisiUretildi.Invoke(sonuc.Tepki);

                }
                if (onDuyguBelirlendi != null)
                {
                    onDuyguBelirlendi.Invoke(sonuc.Duygu);
                }
                if (onKonusmaGecmisiEklendi != null)
                {
                    onKonusmaGecmisiEklendi.Invoke(doktorCumlesi, "Doktor");
                    onKonusmaGecmisiEklendi.Invoke(sonuc.Tepki, "Çocuk");
                }


            }
        }
    }
}