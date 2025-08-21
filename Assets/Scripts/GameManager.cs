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

    public GameObject SonucPanel;
    public GameObject ResultPanel;    // Kart-1 (özet/puan/süre)
    public GameObject FeedbackPanel;  // Kart-2 (uzun feedback)
    public GameObject SentencePanel;  // Kart-3 (en pozitif/en negatif)

    [Tooltip("Oyun sonuçlarının gösterileceği TextMeshPro nesnesi.")]
    public TMP_Text resultText;       // Kart-1 metinleri
    public TMP_Text feedbackText;     // Kart-2 metni

    [Header("Kart 3: En Pozitif / En Negatif")]
    public TMP_Text bestSentenceText;   // Kart-3 pozitif
    public TMP_Text worstSentenceText;  // Kart-3 negatif

    public static event Action onLevelStart;
    public static event Action onLevelEnd;
    public static event Action onPatientEntered;

    private float startTime;

    void Start()
    {
        currentState = GameState.BeforeStart;

        // Başlangıç UI durumu
        if (startButton != null) startButton.SetActive(true);
        if (SonucPanel != null) SonucPanel.SetActive(false);
        //if (ResultPanel != null) ResultPanel.SetActive(false);
        //if (FeedbackPanel != null) FeedbackPanel.SetActive(false);
        //if (SentencePanel != null) SentencePanel.SetActive(false);

        if (resultText != null)
            resultText.text = "Lütfen seansı başlatmak için butona basın.";
    }

    void OnEnable()
    {
        // Sıra: 1) onAnalizTamamlandı (puan/süre & uzun feedback) -> 2) onFinalHazir (en iyi/kötü cümleler) -> 3) onKonusmaBitti (panel aç)
        GeminiManager.onAnalizTamamlandı += DisplayFinalResults; // Kart-1 & Kart-2
        GeminiManager.onFinalHazir += HandleFinalPackage;  // Kart-3
        GeminiManager.onKonusmaBitti += EndLevel;            // Panellerin görünürlüğünü finalize et
    }

    void OnDisable()
    {
        GeminiManager.onAnalizTamamlandı -= DisplayFinalResults;
        GeminiManager.onFinalHazir -= HandleFinalPackage;
        GeminiManager.onKonusmaBitti -= EndLevel;
    }

    public void StartLevel()
    {
        if (currentState == GameState.Playing) return;

        Debug.Log("[GameManager] Seviye başlatılıyor…");
        currentState = GameState.Playing;
        startTime = Time.time;

        // Seans başlarken tüm sonuç panellerini kapat
        if (startButton != null) startButton.SetActive(false);
        if (SonucPanel != null) SonucPanel.SetActive(false);
        //if (ResultPanel != null) ResultPanel.SetActive(false);
        //if (FeedbackPanel != null) FeedbackPanel.SetActive(false);
        //if (SentencePanel != null) SentencePanel.SetActive(false);

        if (resultText != null)
            resultText.text = "Seans devam ediyor…";

        onLevelStart?.Invoke();
        onPatientEntered?.Invoke();
    }

    public void EndLevel()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.GameOver;
        Debug.Log("[GameManager] Seviye bitti. Son paneller gösteriliyor…");

        if (SonucPanel != null) SonucPanel.SetActive(true);

        onLevelEnd?.Invoke();
    }

    // --- Kart-3: En pozitif / En negatif cümleler ---
    private void HandleFinalPackage(GeminiManager.FinalResultData data)
    {
        if (SonucPanel != null) SonucPanel.SetActive(true);

        if (bestSentenceText != null)
            bestSentenceText.text = string.IsNullOrWhiteSpace(data.EnPozitifCumle) ? "—" : data.EnPozitifCumle;

        if (worstSentenceText != null)
            worstSentenceText.text = string.IsNullOrWhiteSpace(data.EnNegatifCumle) ? "—" : data.EnNegatifCumle;
    }

    // --- Kart-1 & Kart-2: Puanlar/süre ve uzun feedback ---
    private void DisplayFinalResults(AnalysisResult result)
    {
        float duration = Time.time - startTime;
        Debug.Log("[GameManager] Analiz sonuçları alındı ve gösteriliyor.");

        if (SonucPanel != null) SonucPanel.SetActive(true);
        //if (ResultPanel != null) ResultPanel.SetActive(true);
        //if (FeedbackPanel != null) FeedbackPanel.SetActive(true);

        if (resultText != null)
        {
            string resultReport =
                "SEANS TAMAMLANDI\n" +
                $"Süre: {duration:F1} saniye\n\n" +
                "<b>DOKTOR DEĞERLENDİRMESİ</b>\n" +
                $"Pozitif: <color=green>%{result.PositiveScore:F0}</color>\n" +
                $"Nötr: <color=grey>%{result.NeutralScore:F0}</color>\n" +
                $"Negatif: <color=red>%{result.NegativeScore:F0}</color>";
            resultText.text = resultReport;
        }

        if (feedbackText != null)
            feedbackText.text = $"<b>Özet:</b>\n<i>{result.feedback}</i>";
    }
}