using UnityEngine;
using TMPro; 
using System;

public class GameManager : MonoBehaviour
{
    public enum GameState { BeforeStart, Playing, GameOver }
    public static GameState currentState;

    [Header("UI Bağlantıları")]
    [Tooltip("Konuşmayı başlatacak olan buton.")]
    public GameObject startButton; // Butonu açıp kapatmak için referans
    [Tooltip("Oyun sonuçlarının gösterileceği TextMeshPro nesnesi.")]
    public TMP_Text resultText;

    public static event Action onLevelStart;
    public static event Action onLevelEnd;

    private float startTime;

    void Start()
    {
        // Oyun başlangıcında her şeyi hazırla
        currentState = GameState.BeforeStart;
        if (startButton != null) startButton.SetActive(true);
        if (resultText != null) resultText.text = "Seansı başlatmak için butona basın.";
    }

    void OnEnable()
    {
        // Gerekli olayları dinlemeye başla
        GeminiController.onKonusmaBitti += EndLevel;
        EmotionObserver.onAnalizTamamlandı += DisplayFinalResults;
    }

    void OnDisable()
    {
        // Dinlemeyi bırak
        GeminiController.onKonusmaBitti -= EndLevel;
        EmotionObserver.onAnalizTamamlandı -= DisplayFinalResults;
    }

    // Bu metodu Unity'deki butona bağlayacağız
    public void StartLevel()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("[GameManager] Seviye Başlatılıyor.");
            currentState = GameState.Playing;
            startTime = Time.time;
            
            if (startButton != null) startButton.SetActive(false);
            if (resultText != null) resultText.text = "Seans devam ediyor...";
            
            onLevelStart?.Invoke();
        }
    }

    public void EndLevel()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.GameOver;
            Debug.Log("[GameManager] Seviye Bitti. Analiz sonuçları bekleniyor...");
            if (resultText != null) resultText.text = "Seans bitti. Analiz sonuçları hazırlanıyor...";
            onLevelEnd?.Invoke();
        }
    }

    // YENİ: EmotionObserver'dan gelen analiz sonucunu ekrana yazdıran metot
    private void DisplayFinalResults(EmotionObserver.AnalysisResult result)
    {
        float duration = Time.time - startTime;
        Debug.Log("[GameManager] Analiz sonuçları alındı ve gösteriliyor.");
        
        if (resultText != null)
        {
            // Tüm skorları ve özeti içeren final raporunu oluştur
            string finalReport = 
                $"SEANS TAMAMLANDI\n" +
                $"Süre: {duration:F1} saniye\n\n" +
                $"<b>DOKTOR DEĞERLENDİRMESİ</b>\n" +
                $"Pozitif: <color=green>%{result.PositiveScore:F0}</color>\n" +
                $"Nötr: <color=grey>%{result.NeutralScore:F0}</color>\n" +
                $"Negatif: <color=red>%{result.NegativeScore:F0}</color>\n\n" +
                $"<b>Özet:</b>\n<i>{result.Summary}</i>";

            resultText.text = finalReport;
        }
        
        // İsteğe bağlı: Yeni bir seans için başlatma butonunu tekrar aktif edebilirsiniz.
        // if (startButton != null) startButton.SetActive(true);
    }
}