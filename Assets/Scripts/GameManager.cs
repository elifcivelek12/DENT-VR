using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.UI;

public enum GameState { BeforeStart, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameState currentState;

    [Header("Buton Bağlantıları")]
    [Tooltip("Konuşmayı başlatacak olan buton.")]
    public GameObject startButton;
    private Button startButtonComponent;

    [Header("Menü Panelleri")]
    [Tooltip("Giriş, Kayıt Ol gibi ana butonları içeren panel.")]
    public GameObject anaPanel;
    [Tooltip("Kullanıcı kayıt bilgilerinin girileceği panel.")]
    public GameObject registerPanel;
    [Tooltip("Kullanıcı giriş bilgilerinin girileceği panel.")]
    public GameObject loginPanel;

    [Header("Sonuc Panelleri")]
    [Tooltip("Pozitif, Nötr, Negatif yüzdeklerini içeren panel")]
    public GameObject ResultPanel;
    [Tooltip("Feedback yazısını içeren panel")]
    public GameObject FeedbackPanel;
    [Tooltip("En negatif ve pozitif cümleyi içeren panel")]
    public GameObject SentencePanel;
    public GameObject SonucPanel;

    [Header("Text Bağlantıları")]
    [Tooltip("Oyun sonuçlarının gösterileceği TextMeshPro nesneleri.")]
    public TMP_Text resultText;
    public TMP_Text feedbackText;
    public TMP_Text bestSentenceText;
    public TMP_Text worstSentenceText;

    public static event Action onLevelStart;
    public static event Action onLevelEnd;
    public static event Action onPatientEntered;

    private float startTime;

    void Awake()
    {
        if (startButton != null)
        {

            startButtonComponent = startButton.GetComponent<Button>();
            if (startButtonComponent == null)
            {
                Debug.LogError("'startButton' olarak atanan objede Button component'i bulunamadı!");
            }
        }
        else
        {
            Debug.LogError("GameManager'daki 'Start Button' referansı atanmamış (boş)!");
        }
    }

    void Start()
    {
        currentState = GameState.BeforeStart;

        if (anaPanel != null) anaPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);

        if (SonucPanel != null) SonucPanel.SetActive(false);

        if (startButtonComponent != null)
        {
            startButton.SetActive(false);
            startButtonComponent.interactable = false;
        }

        //if (resultText != null)
        //    resultText.text = "Lütfen seansı başlatmak için butona basın.";
    }

    void OnEnable()
    {
        GeminiManager.onAnalizTamamlandı += DisplayFinalResults;
        GeminiManager.onFinalHazir += HandleFinalPackage;
        GeminiManager.onKonusmaBitti += EndLevel;
        AuthManager.OnLoginSuccessful += StartLoginSuccessCoroutine;
    }

    void OnDisable()
    {
        GeminiManager.onAnalizTamamlandı -= DisplayFinalResults;
        GeminiManager.onFinalHazir -= HandleFinalPackage;
        GeminiManager.onKonusmaBitti -= EndLevel;
        AuthManager.OnLoginSuccessful -= StartLoginSuccessCoroutine;
    }

    private void StartLoginSuccessCoroutine()
    {
        StartCoroutine(HandleLoginSuccessCoroutine());
    }

    private IEnumerator HandleLoginSuccessCoroutine()
    {
        Debug.Log("Giriş başarılı! Ana menüye dönmeden önce 0.5 saniye bekleniyor...");
        yield return new WaitForSeconds(0.5f);

        ShowMainMenu();

        if (startButtonComponent != null)
        {
            Debug.Log("'startButtonComponent' referansı geçerli. Buton şimdi tıklanabilir yapılıyor.");
            startButton.SetActive(true); 
            startButtonComponent.interactable = true;
        }
        else
        {
            Debug.LogError("Coroutine içinde 'startButtonComponent' referansı hala null! Awake() metodu kontrol edilmeli.");
        }
    }

    #region Menü Navigasyonu
    public void ShowLoginPanel()
    {
        Debug.Log("Giriş paneli gösteriliyor...");
        if (anaPanel != null) anaPanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        Debug.Log("Kayıt paneli gösteriliyor...");
        if (anaPanel != null) anaPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        Debug.Log("Ana menü gösteriliyor...");
        if (registerPanel != null) registerPanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (anaPanel != null) anaPanel.SetActive(true);
    }
    #endregion

    #region Seviye Kontrolü ve Sonuç Panelleri
    public void StartLevel()
    {
        if (currentState == GameState.Playing) return;
        Debug.Log("[GameManager] Seviye başlatılıyor…");
        currentState = GameState.Playing;
        startTime = Time.time;
        if (anaPanel != null) anaPanel.SetActive(false);
        if (startButton != null) startButton.SetActive(false);

        if (SonucPanel != null) SonucPanel.SetActive(false);

        //if (ResultPanel != null) ResultPanel.SetActive(false);
        //if (FeedbackPanel != null) FeedbackPanel.SetActive(false);
        //if (SentencePanel != null) SentencePanel.SetActive(false);

        //if (resultText != null) resultText.text = "Seans devam ediyor…";
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

    private void HandleFinalPackage(GeminiManager.FinalResultData data)
    {
        //if (SentencePanel != null) SentencePanel.SetActive(true);
        if (bestSentenceText != null) bestSentenceText.text = string.IsNullOrWhiteSpace(data.EnPozitifCumle) ? "Pozitif cümle kullanılmadı" : data.EnPozitifCumle;
        if (worstSentenceText != null) worstSentenceText.text = string.IsNullOrWhiteSpace(data.EnNegatifCumle) ? "Negatif Cümle kullanılmadı" : data.EnNegatifCumle;
    }

    private void DisplayFinalResults(AnalysisResult result)
    {
        float duration = Time.time - startTime;
        Debug.Log("[GameManager] Analiz sonuçları alındı ve gösteriliyor.");
        if (SonucPanel != null) SonucPanel.SetActive(true);
       
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
        if (feedbackText != null) feedbackText.text = $"<b>Özet:</b>\n<i>{result.feedback}</i>";
    }
    #endregion
}