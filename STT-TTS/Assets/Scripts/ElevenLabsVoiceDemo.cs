using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

public class ElevenLabsVoiceDemo : MonoBehaviour
{
    // ElevenLabs API Anahtarınızı buraya girin.
    public string elevenlabsApiKey = "BURAYA_API_KEY";
    // Gemini API Anahtarınızı buraya girin.
    public string geminiApiKey = "BURAYA_API_KEY";

    // İşlenecek WAV dosyası (Unity'deki AudioClip).
    public AudioClip inputClip;
    // Sesi çalacak AudioSource bileşeni.
    public AudioSource audioSource;
    // ElevenLabs için kullanılacak ses kimliği.
    // Bu değeri Unity Inspector'dan belirleyebilirsiniz.
    public string voiceId = "EXAVITQu4vr4xnSDxMaL"; // Varsayılan olarak Rachel sesini kullanır.

    // ElevenLabs STT API'sinden gelen JSON yanıtı için sınıf.
    [System.Serializable]
    public class STTResponse
    {
        public string text;
    }

    // ElevenLabs TTS API'sine gönderilecek JSON isteği için sınıf.
    [System.Serializable]
    public class TTSRequest
    {
        public string text;
        public string model_id = "eleven_multilingual_v2";
        public object voice_settings = new { stability = 0.5f, similarity_boost = 0.75f };
    }

    // Gemini API'sine gönderilecek JSON isteği için sınıflar.
    [System.Serializable]
    public class Part
    {
        public string text;
    }

    [System.Serializable]
    public class Content
    {
        public string role;
        public Part[] parts;
    }

    [System.Serializable]
    public class GeminiRequest
    {
        public Content[] contents;
    }

    void Start()
    {
        // Gerekli bileşenlerin ve parametrelerin tanımlı olup olmadığını kontrol et.
        if (inputClip == null || audioSource == null || string.IsNullOrEmpty(elevenlabsApiKey) || string.IsNullOrEmpty(geminiApiKey))
        {
            Debug.LogError("Eksik parametreler! inputClip, audioSource, ElevenLabs API Key ve Gemini API Key girilmelidir.");
            return;
        }
        // Ses işleme sürecini başlatan Coroutine.
        StartCoroutine(ProcessAudio());
    }

    IEnumerator ProcessAudio()
    {
        Debug.Log("🎤 STT (Speech-to-Text) işlemi başlıyor...");

        byte[] wavData = WavUtility.FromAudioClip(inputClip);
        if (wavData == null)
        {
            Debug.LogError("WAV verisi alınamadı. Lütfen geçerli bir AudioClip atandığından emin olun.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        form.AddField("model_id", "scribe_v1");

        using (UnityWebRequest sttRequest = UnityWebRequest.Post("https://api.elevenlabs.io/v1/speech-to-text", form))
        {
            sttRequest.SetRequestHeader("xi-api-key", elevenlabsApiKey);
            yield return sttRequest.SendWebRequest();

            if (sttRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"STT Hatası: {sttRequest.error}\nYanıt Metni: {sttRequest.downloadHandler.text}");
                yield break;
            }

            STTResponse sttData = JsonUtility.FromJson<STTResponse>(sttRequest.downloadHandler.text);
            string transcript = sttData.text;
            Debug.Log($"📜 Çözümlenen Metin: {transcript}");

            // Yapay zekaya göndermek üzere yeni bir coroutine başlat.
            yield return StartCoroutine(GenerateResponseWithAI(transcript));
        }
    }

    IEnumerator GenerateResponseWithAI(string userPrompt)
    {
        Debug.Log("🧠 Yapay zeka yanıtı oluşturuluyor...");

        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20:generateContent?key={geminiApiKey}";

        GeminiRequest requestData = new GeminiRequest
        {
            contents = new Content[]
            {
                new Content
                {
                    role = "user",
                    parts = new Part[]
                    {
                        new Part { text = userPrompt }
                    }
                }
            }
        };

        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log($"Gemini'ye gönderilen JSON: {jsonData}");

        using (UnityWebRequest geminiRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            geminiRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            geminiRequest.downloadHandler = new DownloadHandlerBuffer();
            geminiRequest.SetRequestHeader("Content-Type", "application/json");

            yield return geminiRequest.SendWebRequest();

            if (geminiRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini API Hatası: {geminiRequest.error}\nYanıt Metni: {geminiRequest.downloadHandler.text}");
                yield break;
            }

            string responseJson = geminiRequest.downloadHandler.text;
            // Yanıt JSON'unu dinamik olarak işleme
            // Dictionary<string, object> jsonObject = JsonUtility.FromJson<Dictionary<string, object>>(responseJson);

            // Yanıttaki metni bul ve al
            string aiResponse = "";
            try
            {
                // Basit bir JSON parse işlemi, gerçek API yanıtı karmaşık olabilir.
                int start = responseJson.IndexOf("\"text\": \"") + 9;
                int end = responseJson.IndexOf("\"", start);
                aiResponse = responseJson.Substring(start, end - start);
            }
            catch (Exception e)
            {
                Debug.LogError($"Gemini yanıtı parse edilirken hata oluştu: {e.Message}");
                Debug.Log($"Yanıt: {responseJson}");
                aiResponse = "Üzgünüm, yanıtınızı işleyemedim.";
            }

            Debug.Log($"🤖 Yapay Zeka Yanıtı: {aiResponse}");

            // Yeni yanıtı, belirlediğiniz tek bir ses kimliği kullanarak oku.
            yield return StartCoroutine(GenerateSpeech(aiResponse, voiceId));
        }
    }

    IEnumerator GenerateSpeech(string text, string voiceId)
    {
        Debug.Log($"🗣️ TTS (Text-to-Speech) işlemi başlıyor... Kullanılan ses kimliği: {voiceId}");

        string ttsUrl = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        TTSRequest requestData = new TTSRequest { text = text };
        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest ttsRequest = new UnityWebRequest(ttsUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            ttsRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            ttsRequest.downloadHandler = new DownloadHandlerAudioClip(ttsUrl, AudioType.MPEG);
            ttsRequest.SetRequestHeader("Content-Type", "application/json");
            ttsRequest.SetRequestHeader("xi-api-key", elevenlabsApiKey);

            yield return ttsRequest.SendWebRequest();

            if (ttsRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"TTS Hatası: {ttsRequest.error}\nYanıt Metni: {ttsRequest.downloadHandler.text}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(ttsRequest);

            if (clip == null)
            {
                Debug.LogError("Ses verisi alınamadı veya dönüştürülemedi.");
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();

            Debug.Log("✅ TTS tamamlandı ve oynatılıyor.");
        }
    }
}
