using UnityEngine;
using TMPro;
using System;


public enum GameState { BeforeStart, Playing, GameOver }
public class GameManager : MonoBehaviour
{

    public static GameState currentState;

    [Header("UI Bağlantıları")]
    [Tooltip("Konuşmayı başlatacak olan buton.")]
    public GameObject startButton;
    public GameObject ResultPanel;
    public GameObject FeedbackPanel;
    [Tooltip("Oyun sonuçlarının gösterileceği TextMeshPro nesnesi.")]
    public TMP_Text resultText;
    public TMP_Text feedbackText;



    public static event Action onLevelStart;
    public static event Action onLevelEnd;
    public static event Action onPatientEntered;

    private float startTime;

    void Start()
    {
        currentState = GameState.BeforeStart;
        if (startButton != null) startButton.SetActive(true);
        if (resultText != null) resultText.text = "Lütfen seansı baslatmak icin butona basın.";
    }

    void OnEnable()
    {
        // Eski GeminiController ve EmotionObserver referansları kaldırıldı,
        // çünkü her ikisi de GeminiManager içinde birleştirildi.
        GeminiManager.onKonusmaBitti += EndLevel;
        GeminiManager.onAnalizTamamlandı += DisplayFinalResults;
    }

    void OnDisable()
    {
        GeminiManager.onKonusmaBitti -= EndLevel;
        GeminiManager.onAnalizTamamlandı -= DisplayFinalResults;
    }

    public void StartLevel()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("[GameManager] Seviye Başlatılıyor. GameState Playing olarak değiştiriliyor ");
            currentState = GameState.Playing;
            startTime = Time.time;

            if (startButton != null) startButton.SetActive(false);
            if (resultText != null) ResultPanel.SetActive(false);
            if (resultText != null) resultText.text = "Seans devam ediyor...";

            onLevelStart?.Invoke();
            onPatientEntered?.Invoke();
        }
    }

    public void EndLevel()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.GameOver;
            Debug.Log("[GameManager] Seviye Bitti. Analiz sonuçları bekleniyor...Game State GameOver olarak değiştirildi");
            if (resultText != null) ResultPanel.SetActive(true);
            if (resultText != null) resultText.text = "Seans bitti. Analiz sonuçları hazırlanıyor...";
            onLevelEnd?.Invoke();
        }
    }

    private void DisplayFinalResults(AnalysisResult result)
    {
        float duration = Time.time - startTime;
        Debug.Log("[GameManager] Analiz sonuçları alındı ve gösteriliyor.");

        // Sonuçları gösterecek panelleri aktif et
        if (ResultPanel != null) ResultPanel.SetActive(true);
        if (FeedbackPanel != null) FeedbackPanel.SetActive(true);

        // Skor ve süre bilgilerini 'resultText'e yazdır
        if (resultText != null)
        {
            string resultReport =
                $"SEANS TAMAMLANDI\n" +
                $"Süre: {duration:F1} saniye\n\n" +
                $"<b>DOKTOR DEĞERLENDİRMESİ</b>\n" +
                $"Pozitif: <color=green>%{result.PositiveScore:F0}</color>\n" +
                $"Nötr: <color=grey>%{result.NeutralScore:F0}</color>\n" +
                $"Negatif: <color=red>%{result.NegativeScore:F0}</color>";

            resultText.text = resultReport;
        }

        // Geri bildirim (özet) metnini 'feedbackText'e yazdır
        if (feedbackText != null)
        {
            string feedbackReport = $"<b>Özet:</b>\n<i>{result.feedback}</i>";
            feedbackText.text = feedbackReport;
        }
    }
}