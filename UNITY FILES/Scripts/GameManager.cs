using UnityEngine;
using TMPro; 
using System;

// Oyun durumlarını temsil eden enum
public enum GameState { BeforeStart, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    // Mevcut oyun durumu
    public static GameState currentState;

    [Header("UI Bağlantıları")]
    [Tooltip("Konuşmayı başlatacak olan buton.")]
    public GameObject startButton;
    public GameObject ResultPanel;
    [Tooltip("Oyun sonuçlarının gösterileceği TextMeshPro nesnesi.")]
    public TMP_Text resultText;

    // Oyun başladığında ve bittiğinde tetiklenen eventler
    public static event Action onLevelStart;
    public static event Action onLevelEnd;
    public static event Action onPatientEntered;

    // Seans başlangıç zamanı
    private float startTime;

    void Start()
    {
        // Başlangıç durumu
        currentState = GameState.BeforeStart;

        // Başlangıç UI ayarları
        if (startButton != null) startButton.SetActive(true);
        if (resultText != null) resultText.text = "Lütfen seansi baslatmak icin butona basın.";
    }

    void OnEnable()
    {
        // Konuşma bitişi ve analiz tamamlandığında eventlere abone ol
        GeminiController.onKonusmaBitti += EndLevel;
        EmotionObserver.onAnalizTamamlandı += DisplayFinalResults;
    }

    void OnDisable()
    {
        // Script devre dışı bırakıldığında abonelikleri kaldır
        GeminiController.onKonusmaBitti -= EndLevel;
        EmotionObserver.onAnalizTamamlandı -= DisplayFinalResults;
    }

    // Seansı başlatır
    public void StartLevel()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("[GameManager] Seviye Başlatılıyor. GameState Playing olarak değiştiriliyor ");
            currentState = GameState.Playing;
            startTime = Time.time;

            // UI güncellemeleri
            if (startButton != null) startButton.SetActive(false);
            if (resultText != null) ResultPanel.SetActive(false);
            if (resultText != null) resultText.text = "Seans devam ediyor...";

            // Eventleri tetikle
            onLevelStart?.Invoke();
            onPatientEntered?.Invoke();
        }
    }

    // Seansı bitirir
    public void EndLevel()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.GameOver;
            Debug.Log("[GameManager] Seviye Bitti. Analiz sonuçları bekleniyor...Game State GameOver olarak değiştirildi");

            // UI güncellemeleri
            if (resultText != null) ResultPanel.SetActive(true);
            if (resultText != null) resultText.text = "Seans bitti. Analiz sonuçları hazırlanıyor...";

            // Event tetikle
            onLevelEnd?.Invoke();
        }
    }

    // Duygu analizi tamamlandığında sonuçları gösterir
    private void DisplayFinalResults(EmotionObserver.AnalysisResult result)
    {
        // Seans süresini hesapla
        float duration = Time.time - startTime;
        Debug.Log("[GameManager] Analiz sonuçları alındı ve gösteriliyor.");

        if (resultText != null)
        {
            // Sonuç raporunu formatla ve UI'ya yaz
            string finalReport =
                $"SEANS TAMAMLANDI\n" +
                $"Süre: {duration:F1} saniye\n\n" +
                $"<b>DOKTOR DEĞERLENDİRMESİ</b>\n" +
                $"Pozitif: <color=green>%{result.PositiveScore:F0}</color>\n" +
                $"Nötr: <color=grey>%{result.NeutralScore:F0}</color>\n" +
                $"Negatif: <color=red>%{result.NegativeScore:F0}</color>\n\n" +
                $"<b>Özet:</b>\n<i>{result.feedback}</i>";

            resultText.text = finalReport;
        }

    }
}
