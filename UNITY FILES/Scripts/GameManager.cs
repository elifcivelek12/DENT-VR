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
    [Tooltip("Oyun sonuçlarının gösterileceği TextMeshPro nesnesi.")]
    public TMP_Text resultText;



    public static event Action onLevelStart;
    public static event Action onLevelEnd;
    public static event Action onPatientEntered;

    private float startTime;

    void Start()
    {
        currentState = GameState.BeforeStart;
        if (startButton != null) startButton.SetActive(true);
        if (resultText != null) resultText.text = "Lütfen seansi baslatmak icin butona basın.";
    }

    void OnEnable()
    {
        GeminiController.onKonusmaBitti += EndLevel;
        EmotionObserver.onAnalizTamamlandı += DisplayFinalResults;
    }

    void OnDisable()
    {
        GeminiController.onKonusmaBitti -= EndLevel;
        EmotionObserver.onAnalizTamamlandı -= DisplayFinalResults;
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

    private void DisplayFinalResults(EmotionObserver.AnalysisResult result)
    {
        float duration = Time.time - startTime;
        Debug.Log("[GameManager] Analiz sonuçları alındı ve gösteriliyor.");

        if (resultText != null)
        {
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