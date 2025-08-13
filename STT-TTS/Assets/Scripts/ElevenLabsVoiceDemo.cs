using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

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

public class ElevenLabsVoiceDemo : MonoBehaviour
{
    // ElevenLabs API Anahtarınızı buraya girin.
    public string elevenlabsApiKey = "BURAYA_API_KEY";
    // Gemini API Anahtarınızı buraya girin.
    public string geminiApiKey = "BURAYA_API_KEY";

    // Sesi çalacak AudioSource bileşeni.
    public AudioSource audioSource;
    // ElevenLabs için kullanılacak ses kimliği.
    // Bu değeri Unity Inspector'dan belirleyebilirsiniz.
    public string voiceId = "EXAVITQu4vr4xnSDxMaL";

    // Mikrofon kayıt ayarları.
    // Bu değeri boş bırakırsanız varsayılan mikrofon kullanılır.
    public string microphoneDeviceName;
    private AudioClip microphoneClip;
    private bool isRecording = false;

    // Karşılıklı konuşma için kontrol değişkenleri
    private bool isAwaitingResponse = false;

    void Start()
    {
        // Gerekli bileşenlerin ve parametrelerin tanımlı olup olmadığını kontrol et.
        if (audioSource == null || string.IsNullOrEmpty(elevenlabsApiKey) || string.IsNullOrEmpty(geminiApiKey))
        {
            Debug.LogError("Eksik parametreler! audioSource, ElevenLabs API Key ve Gemini API Key girilmelidir.");
            return;
        }

        // Eğer microphoneDeviceName boşsa, ilk bulunan mikrofonu varsayılan olarak kullan.
        if (string.IsNullOrEmpty(microphoneDeviceName))
        {
            if (Microphone.devices.Length > 0)
            {
                microphoneDeviceName = Microphone.devices[0];
            }
            else
            {
                Debug.LogError("Mikrofon bulunamadı! Lütfen bir mikrofon bağlayın.");
                return;
            }
        }

        Debug.Log("Uygulama hazır! Kayıt başlatmak için 'R' tuşuna basın.");
    }

    // Her frame'de çalışan Unity metodu.
    void Update()
    {
        // Eğer kayıt devam etmiyorsa, tuşa basarak kaydı başlatabiliriz.
        if (!isRecording && !isAwaitingResponse && Input.GetKeyDown(KeyCode.R))
        {
            StartRecording();
        }

        // Eğer kayıt devam ediyorsa ve tuşa basarak durdurulmak isteniyorsa.
        if (isRecording && Input.GetKeyUp(KeyCode.R))
        {
            StopRecording();
        }

        // Eğer bir yanıt bekleniyorsa ve ses çalma işlemi bittiyse, yeni bir kayıt başlat.
        if (isAwaitingResponse && !audioSource.isPlaying)
        {
            isAwaitingResponse = false;
            StartRecording();
        }
    }

    // Mikrofon kaydını başlatır.
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("Zaten bir kayıt devam ediyor.");
            return;
        }

        Debug.Log("🎙️ Mikrofon kaydı başladı... Konuşmanızı tamamlayıp tuşu bırakın.");
        // Mikrofonu 10 saniye süreyle 16000 Hz frekansta kaydetmeye başla.
        // ElevenLabs STT API'si 16000 Hz'yi tercih eder.
        microphoneClip = Microphone.Start(microphoneDeviceName, false, 10, 16000);
        isRecording = true;
    }

    // Mikrofon kaydını durdurur ve ses verisini işlemeye gönderir.
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("Kayıt devam etmiyor.");
            return;
        }

        Debug.Log("🛑 Mikrofon kaydı durduruldu. Yanıt bekleniyor...");

        // Mikrofonun pozisyonunu kaydı sonlandırmadan önce al.
        int position = Microphone.GetPosition(microphoneDeviceName);
        Microphone.End(microphoneDeviceName);
        isRecording = false;
        isAwaitingResponse = true;

        if (position > 0)
        {
            // Mikrofon verisinden bir alt-klip oluşturun (istenirse)
            AudioClip processedClip = GetSubClip(microphoneClip, 0, position);

            // Ses işleme sürecini başlat.
            StartCoroutine(ProcessAudio(processedClip));
        }
        else
        {
            Debug.LogError("Kayıt sırasında ses verisi alınamadı. Lütfen daha uzun süre konuşmayı deneyin.");
            // Hata durumunda yeni bir kayıt döngüsü başlat.
            isAwaitingResponse = false;
            StartRecording();
        }
    }

    // Mikrofon klibinden sadece konuşma olan kısmı almak için yardımcı metod.
    private AudioClip GetSubClip(AudioClip originalClip, int startSample, int endSample)
    {
        int length = endSample - startSample;
        if (length <= 0)
        {
            Debug.LogError("Kayıt verisi boş veya geçersiz.");
            return null;
        }

        float[] samples = new float[length];
        originalClip.GetData(samples, startSample);

        AudioClip subClip = AudioClip.Create("ProcessedClip", length, originalClip.channels, originalClip.frequency, false);
        subClip.SetData(samples, 0);

        return subClip;
    }

    IEnumerator ProcessAudio(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("İşlenecek ses klibi bulunamadı.");
            yield break;
        }

        Debug.Log("🎤 STT (Speech-to-Text) işlemi başlıyor...");

        byte[] wavData = WavUtility.FromAudioClip(clip);
        if (wavData == null)
        {
            Debug.LogError("WAV verisi alınamadı.");
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
            string aiResponse = "";
            try
            {
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
