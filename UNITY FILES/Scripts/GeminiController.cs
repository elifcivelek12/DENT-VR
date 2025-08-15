using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.Events;
using System.Text.RegularExpressions;
using System;

// --- API S�n�flar� (De�i�iklik Yok) ---
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


    [Header("API Ayarlar�")]
    // De�i�keni private yap�p [SerializeField] eklemek, onu Inspector'da g�sterir
    // ama di�er script'lerden do�rudan eri�ilmesini engeller. Bu daha iyi bir pratiktir.
    [SerializeField, Tooltip("Google Gemini API Anahtar�n�z")]
    private string geminiApiKey = "AIzaSyAJGEMjR2D5QgBDCUjznoF2fCzgfIWLmi0";

    [Header("UI Ba�lant�lar�")]
    [Tooltip("Sonucun g�sterilece�i TextMeshPro nesnesi")]
    public TMP_Text sonucText;

    public static event Action<string, string> onKonusmaGecmisiEklendi;
    public static event Action<string> onCocukTepkisiUretildi;
    public static event Action<string> onDuyguBelirlendi;
    public static event Action onKonusmaBitti;

    [Header("Oyun Ak��� Ayarlar�")]
    [SerializeField, Tooltip("Seviyenin bitmesi i�in gereken toplam konu�ma say�s�.")]
    private int seviyeBitisKonusmaSayisi = 3;
    private int konusmaSayaci = 0;


    private const string MODEL_NAME = "gemini-1.5-flash";
    private string _apiURL;

    private const string SYSTEM_PRIMER = @"
Sen bir VR di� hekimi sim�lasyonunda 8 ya��nda bir k�z �ocu�usun.
G�rev:
1) Doktorun c�mlesini 'pozitif', 'negatif' veya 'n�tr' olarak s�n�fland�r.
2) K�sa, do�al ve �ocuk�a bir tepki �ret.
3) Tepkiye uygun animasyon se�.
4) Duyguyu belirt.

JSON format�nda d�n:
{
    ""kategori"": ""pozitif|negatif|n�tr"",
    ""tepki"": ""<�ocu�un k�sa cevab�>"",
    ""animasyon"": ""<oynat�lacak animasyon>"",
    ""duygu"": ""olumlu|k�t�""
}
JSON d���nda metin yazma.

�rnekler:
- Pozitif: ""�ok cesursun, aferin sana."", animasyon: ""g�l�mseme"", duygu: ""mutlu""
- Negatif: ""Hareket edersen ac�yabilir."", animasyon: ""korku"", duygu: ""endi�eli""
- N�tr: ""L�tfen koltu�a otur."", animasyon: ""bekleme"", duygu: ""n�tr"" ";

    void Awake()
    {
        _apiURL = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={geminiApiKey}";
    }

    void OnEnable()
    {
        ElevenLabsVoiceDemo.onTextTranscribed += MetniAlVeGeminiyeGonder;
        GameManager.onLevelStart += KonusmaSayaciniSifirla;
    }
    void OnDisable()
    {
        ElevenLabsVoiceDemo.onTextTranscribed -= MetniAlVeGeminiyeGonder;
        GameManager.onLevelStart -= KonusmaSayaciniSifirla;
    }

        public void KonusmaSayaciniSifirla() // Artık public olması şart değil, private olabilir.
    {
        konusmaSayaci = 0;
        Debug.Log("GeminiController: Konuşma sayacı sıfırlandı.");
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

                    string temizlenmisJson = modelinUrettigiText; // Varsay�lan de�er
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
                {
                    sonucText.text = $"Kategori: {sonuc.Kategori}\n" +
                                     $"Tepki: {sonuc.Tepki}\n" +
                                     $"Animasyon: {sonuc.Animasyon}\n" +
                                     $"Duygu: {sonuc.Duygu}";
                }

                Debug.Log($"Ba�ar�l�! Kategori: {sonuc.Kategori}, Tepki: {sonuc.Tepki}, Animasyon: {sonuc.Animasyon}, Duygu: {sonuc.Duygu}");

                onCocukTepkisiUretildi?.Invoke(sonuc.Tepki);
                onDuyguBelirlendi?.Invoke(sonuc.Duygu);
                onKonusmaGecmisiEklendi?.Invoke(doktorCumlesi, "Doktor");
                onKonusmaGecmisiEklendi?.Invoke(sonuc.Tepki, "Çocuk");

                konusmaSayaci++;
                Debug.Log($"Konuşma tamamlandı. Toplam konuşma sayısı: {konusmaSayaci}/{seviyeBitisKonusmaSayisi}");

                // ... (konuşma sayacı mantığı) ...
                if (konusmaSayaci >= seviyeBitisKonusmaSayisi)
                {
                    onKonusmaBitti?.Invoke(); // onKonusmaBitti'yi de static olarak tetikle
                }


            }
        }
    }
}