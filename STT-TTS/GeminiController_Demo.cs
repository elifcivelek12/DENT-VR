using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.Events;
using System.Text.RegularExpressions;

// --- API Sýnýflarý (Deðiþiklik Yok) ---
public class GeminiApiResponse { [JsonProperty("candidates")] public Candidate[] Candidates; }
public class Candidate { [JsonProperty("content")] public GeminiContent Content; }
public class GeminiContent { [JsonProperty("parts")] public GeminiPart[] Parts; }
public class GeminiPart { [JsonProperty("text")] public string Text; }
[System.Serializable]
public class CocukTepki { [JsonProperty("kategori")] public string Kategori; [JsonProperty("tepki")] public string Tepki; }


public class GeminiController : MonoBehaviour
{
    [Header("API Ayarlarý")]
    [SerializeField, Tooltip("Google Gemini API Anahtarýnýz")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    [Header("UI Baðlantýlarý")]
    [Tooltip("Sonucun gösterileceði TextMeshPro nesnesi")]
    public TMP_Text sonucText;

    // YENÝ: Observer Pattern için UnityEvent
    // Konuþma metnini ve konuþmacýnýn rolünü (örn: "Doktor") iletir.
    [Header("Olaylar (Observer Pattern)")]
    [Tooltip("Yeni bir konuþma parçasý eklendiðinde tetiklenir.")]
    public UnityEvent<string, string> onConversationPieceAdded;
    public UnityEvent<string> onChildReact;

    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;

    private const string SYSTEM_PRIMER = @"
Sen bir VR diþ hekimi simülasyonunda 8 yaþýnda hafif gýcýk bir kýz çocuðusun.
Görevin:
1) Doktorun cümlesini ""olumlu"", ""olumsuz"" veya ""notr"" (nötr) olarak sýnýflandýr.
2) Bu kategoriye uygun, doðal, kýsa ve çocukça TEK bir tepki cümlesi üret.
    - Basit kelimeler, 5-12 kelime.
    - En fazla 1 emoji.
    - Tehdit, suçlama, týbbi talimat yok.
    - Korkutucu veya yetiþkinvari ifadeler yok.

YANITINI **yalnýzca** þu JSON biçiminde ver:
{
  ""kategori"": ""olumlu|olumsuz|notr"",
  ""tepki"": ""<tek cümle>""
}
JSON dýþýnda metin yazma.";

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

                    string temizlenmisJson = modelinUrettigiText;
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
                    sonucText.text = $"Kategori: {sonuc.Kategori}\nÇocuk: {sonuc.Tepki}";

                Debug.Log($"Baþarýlý! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}");

                // YENÝ: Observer'lara olay fýrlatýlýyor.
                // Hem doktorun cümlesi hem de çocuðun tepkisi gönderiliyor.
                onConversationPieceAdded?.Invoke(doktorCumlesi, "Doktor");
                onConversationPieceAdded?.Invoke(sonuc.Tepki, "Çocuk");
                onChildReact?.Invoke(sonuc.Tepki);
               
            }
            else
            {
                Debug.LogError("Ýþlem sonunda 'sonuc' deðiþkeni null kaldý. Yanýt üretilemedi.");
            }
        }
    }
}





