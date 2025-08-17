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

    private string elevenlabsApiKey = "sk_fbe5f14a2dd1c7f632ac603e8172d9633c6ca0c2bbe819c8";    

    public AudioSource audioSource;

    public string voiceId = "NNn9dv8zq2kUo7d3JSGG";

    public static event Action<string> onTextTranscribed;

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
        if (audioSource == null || string.IsNullOrEmpty(elevenlabsApiKey))
        {
            Debug.LogError("Eksik parametreler! audioSource, ElevenLabs API Key ve Gemini API Key girilmelidir.");
            return;
        }

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

        Debug.Log("Mikrofon baglantısı hazır");

    }

    void Update()
    {
        if (GameManager.currentState == GameState.Playing )
        {
            startRecordingAction.action.started += ctx => StartRecording();
            startRecordingAction.action.canceled += ctx => StopRecording();
            startRecordingAction.action.Enable();
        }
    
    }
    void OnEnable()
    {
        // Gemini'den bir çocuk tepkisi üretildiğinde, onu sese çevirmek için dinle.
        GeminiController.onCocukTepkisiUretildi += GelenMetniSeseCevir;
    }

    void OnDisable()
    {
        // Script kapatıldığında dinlemeyi bırak.
        GeminiController.onCocukTepkisiUretildi -= GelenMetniSeseCevir;
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

            onTextTranscribed?.Invoke(transcript);
        }
    }
    
     public void GelenMetniSeseCevir(string cevap)
    {
        Debug.Log($"ELEVENLABS BİLDİRİYOR: Gemini 1.5'den alınan metin: '{cevap}'");

        if (string.IsNullOrEmpty(cevap))
        {
            Debug.LogWarning("Sese çevrilecek metin boş.");
            return;
        }else if (cevap != null)
        {
            StartCoroutine(GenerateSpeech(cevap, voiceId));
            Debug.LogWarning("Metin ses çevrilmek üzere yönlendirildi");
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