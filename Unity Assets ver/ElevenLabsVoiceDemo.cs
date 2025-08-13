using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Events;

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
    private string elevenlabsApiKey = "sk_55696848c629da3ac68178616759727b23e362434fcbd56f";    
    
    // Sesi çalacak AudioSource bileşeni.
    public AudioSource audioSource;
    // ElevenLabs için kullanılacak ses kimliği.
    // Bu değeri Unity Inspector'dan belirleyebilirsiniz.
    public string voiceId = "NNn9dv8zq2kUo7d3JSGG";

    [Header("Olaylar (Events)")]
    public UnityEvent<string> onTextTranscribed;

    // Mikrofon kayıt ayarları.
    public string microphoneDeviceName;
    private AudioClip microphoneClip;
    private bool isRecording = false;

    // Karşılıklı konuşma için kontrol değişkenleri
    private bool isAwaitingResponse = false;
    
    // VR kontrolcü girdileri için değişkenler.
    // Bu InputActionReference'ları Unity'de atamanız gerekecek.
    public InputActionReference startRecordingAction;

    void Start()
    {
        // Gerekli bileşenlerin ve parametrelerin tanımlı olup olmadığını kontrol et.
        if (audioSource == null || string.IsNullOrEmpty(elevenlabsApiKey))
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
        
        Debug.Log("Uygulama hazır! VR kontrolcünüzün belirlenen tuşuna basarak kaydı başlatın.");
        
        // VR kontrolcü girdilerini dinlemeyi başlat.
        startRecordingAction.action.started += ctx => StartRecording();
        startRecordingAction.action.canceled += ctx => StopRecording();
        startRecordingAction.action.Enable();
    }
    
    void OnDestroy()
    {
        // Script yok edildiğinde dinleyicileri kapat.
        startRecordingAction.action.started -= ctx => StartRecording();
        startRecordingAction.action.canceled -= ctx => StopRecording();
        startRecordingAction.action.Disable();
    }

    // Mikrofon kaydını başlatır.
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("Zaten bir kayıt devam ediyor.");
            return;
        }

        Debug.Log("🎙 Mikrofon kaydı başladı... Konuşmanızı tamamlayıp tuşu bırakın.");
        // Mikrofonu 10 saniye süreyle 16000 Hz frekansta kaydetmeye başla.
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
        
        int position = Microphone.GetPosition(microphoneDeviceName);
        Microphone.End(microphoneDeviceName);
        isRecording = false;
        isAwaitingResponse = true;

        if (position > 0)
        {
            AudioClip processedClip = GetSubClip(microphoneClip, 0, position);
            StartCoroutine(ProcessAudio(processedClip));
        }
        else
        {
            Debug.LogError("Kayıt sırasında ses verisi alınamadı. Lütfen daha uzun süre konuşmayı deneyin.");
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

            //yield return StartCoroutine(GenerateResponseWithAI(transcript));
            if (onTextTranscribed != null)
            {
                onTextTranscribed.Invoke(transcript);
            }
        }
    }
    /*
    IEnumerator GenerateResponseWithAI(string userPrompt)
    {
        Debug.Log("🧠 Yapay zeka yanıtı oluşturuluyor...");

        string apiUrl = 

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
    */

    public void GelenMetniSeseCevir(string metin)
    {
        Debug.Log($"ELEVENLABS BİLDİRİYOR: Sese çevirmek için şu metni aldım: '{metin}'");

        if (string.IsNullOrEmpty(metin))
        {
            Debug.LogWarning("Sese çevrilecek metin boş.");
            return;
        }
    }


        IEnumerator GenerateSpeech(string metin, string voiceId)
    {
        Debug.Log($"🗣 TTS (Text-to-Speech) işlemi başlıyor... Kullanılan ses kimliği: {voiceId}");



        string ttsUrl = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        TTSRequest requestData = new TTSRequest { text = metin };
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