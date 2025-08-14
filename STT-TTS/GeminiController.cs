using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.Events;
using System.Text.RegularExpressions;

// --- API S�n�flar� (De�i�iklik Yok) ---
public class GeminiApiResponse { [JsonProperty("candidates")] public Candidate[] Candidates; }
public class Candidate { [JsonProperty("content")] public GeminiContent Content; }
public class GeminiContent { [JsonProperty("parts")] public GeminiPart[] Parts; }
public class GeminiPart { [JsonProperty("text")] public string Text; }
[System.Serializable]
public class CocukTepki { [JsonProperty("kategori")] public string Kategori; [JsonProperty("tepki")] public string Tepki; }


public class GeminiController : MonoBehaviour
{
    [Header("API Ayarlar�")]
    [SerializeField, Tooltip("Google Gemini API Anahtar�n�z")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    [Header("UI Ba�lant�lar�")]
    [Tooltip("Sonucun g�sterilece�i TextMeshPro nesnesi")]
    public TMP_Text sonucText;

    // YEN�: Observer Pattern i�in UnityEvent
    // Konu�ma metnini ve konu�mac�n�n rol�n� (�rn: "Doktor") iletir.
    [Header("Olaylar (Observer Pattern)")]
    [Tooltip("Yeni bir konu�ma par�as� eklendi�inde tetiklenir.")]
    public UnityEvent<string, string> onConversationPieceAdded;
    public UnityEvent<string> onChildReact;

    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;

    private const string SYSTEM_PRIMER = @"
Sen bir VR di� hekimi sim�lasyonunda 8 ya��nda hafif g�c�k bir k�z �ocu�usun.
G�revin:
1) Doktorun c�mlesini ""olumlu"", ""olumsuz"" veya ""notr"" (n�tr) olarak s�n�fland�r.
2) Bu kategoriye uygun, do�al, k�sa ve �ocuk�a TEK bir tepki c�mlesi �ret.
    - Basit kelimeler, 5-12 kelime.
    - En fazla 1 emoji.
    - Tehdit, su�lama, t�bbi talimat yok.
    - Korkutucu veya yeti�kinvari ifadeler yok.

YANITINI **yaln�zca** �u JSON bi�iminde ver:
{
  ""kategori"": ""olumlu|olumsuz|notr"",
  ""tepki"": ""<tek c�mle>""
}
JSON d���nda metin yazma.";

    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";
    }

    public void MetniAlVeGeminiyeGonder(string gelenMetin)
    {
        Debug.Log($"GeminiController, �u metni i�lemek �zere ald�: '{gelenMetin}'");

        if (string.IsNullOrWhiteSpace(gelenMetin))
        {
            if (sonucText != null) sonucText.text = "Gelen metin bo�, i�lem yap�lamad�.";
            Debug.LogWarning("��lenecek metin bo� oldu�u i�in i�lem iptal edildi.");
            return;
        }

        StartCoroutine(ClassifyAndRespond(gelenMetin));
    }

    private IEnumerator ClassifyAndRespond(string doktorCumlesi)
    {
        if (sonucText != null) sonucText.text = "D���n�yor...";

        string prompt = $"{SYSTEM_PRIMER}\n\nDoktorun c�mlesi: \"{doktorCumlesi}\"";
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
                Debug.LogError($"Gemini API Hatas�: {request.error}\nYan�t: {request.downloadHandler.text}");
                if (sonucText != null) sonucText.text = $"Bir API hatas� olu�tu: {request.error}";
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
                    Debug.LogError($"JSON parse hatas�: {e.Message}\nHam ��kt�: {hamCikti}");
                    if (sonucText != null) sonucText.text = "Yan�tta ge�erli bir format bulunamad�.";
                }
            }

            if (sonuc != null)
            {
                if (sonucText != null)
                    sonucText.text = $"Kategori: {sonuc.Kategori}\n�ocuk: {sonuc.Tepki}";

                Debug.Log($"Ba�ar�l�! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}");

                // YEN�: Observer'lara olay f�rlat�l�yor.
                // Hem doktorun c�mlesi hem de �ocu�un tepkisi g�nderiliyor.
                onConversationPieceAdded?.Invoke(doktorCumlesi, "Doktor");
                onConversationPieceAdded?.Invoke(sonuc.Tepki, "�ocuk");
                onChildReact?.Invoke(sonuc.Tepki);
               
            }
            else
            {
                Debug.LogError("��lem sonunda 'sonuc' de�i�keni null kald�. Yan�t �retilemedi.");
            }
        }
    }
}





