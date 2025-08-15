using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro; // TMP_Text i�in

public class GameManager : MonoBehaviour
{
    // Oyun durumlar�n� takip etmek i�in enum kullanabiliriz.
    public enum GameState { BeforeStart, Playing, GameOver }
    public static GameState currentState;

    [Header("UI Ba�lant�lar�")]
    [Tooltip("Oyun sonu�lar�n�n g�sterilece�i TextMeshPro nesnesi.")]
    public TMP_Text resultText; // Sonu�lar�n g�sterilece�i TextMeshPro nesnesi

    [Header("Olaylar")]
    // Oyun ba�lad���nda veya bitti�inde di�er script'lerin dinlemesi i�in olaylar.
    // Bu sayede di�er script'ler, bu olaylara abone olarak tepki verebilir.
    public static UnityEvent onLevelStart = new UnityEvent();
    public static UnityEvent onLevelEnd = new UnityEvent();

    // YEN� EKLEND�: GeminiController ve EmotionObserver referanslar�
    // Not: Bu referanslar art�k olaylar� ba�lamak i�in de�il,
    // sadece Inspector'dan atama kolayl��� ve kod i�inde eri�im i�in kalabilir.
    public GeminiController geminiController;
    public EmotionObserver emotionObserver;

    // Oyun s�resini tutacak de�i�ken
    private float startTime;

    // Tekil (Singleton) desenini kullanmak, bu script'e di�er script'lerden kolayca eri�memizi sa�lar.
    public static GameManager instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentState = GameState.BeforeStart;

        // Gerekli referanslar�n atand���ndan emin ol
        if (emotionObserver == null)
        {
            Debug.LogError("EmotionObserver referans� GameManager'da eksik!");
            return;
        }

        Debug.Log("GameManager ba�lat�ld�. Bile�enler aras� ba�lant�lar kuruluyor...");

        // �NCEK� HATANIN D�ZELT�LM�� HAL�: Olaylara do�rudan statik �ye �zerinden abone ol.
        // Art�k geminiController referans�n� kullanman�za gerek yok, ��nk� olaylar statik.
        GeminiController.onKonusmaGecmisiEklendi.AddListener(emotionObserver.AddConversation);
        GeminiController.onKonusmaBitti.AddListener(emotionObserver.HandleConversationEnded);
    }

    // Bu fonksiyonu VR g�zl��� takt���m�zda veya bir UI butonuyla �a��raca��z.
    public void StartLevel()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("[GameManager] Seviye Ba�lat�l�yor.");
            currentState = GameState.Playing;
            startTime = Time.time; // Sayac� ba�lat
            onLevelStart.Invoke(); // Oyunun ba�lad���n� duyur.

            // UI'� ba�lang��ta temizle
            if (resultText != null)
            {
                resultText.text = "Seans Ba�lad�...";
            }
        }
    }

    // Konu�ma bitti�inde �a�r�lacak fonksiyon.
    public void EndLevel()
    {
        if (currentState == GameState.Playing)
        {
            Debug.Log("[GameManager] Seviye Sonland�r�l�yor.");
            currentState = GameState.GameOver;

            float duration = Time.time - startTime;
            Debug.Log($"[GameManager] Oyun s�resi: {duration:F2} saniye.");

            onLevelEnd.Invoke(); // Oyunun bitti�ini duyur.
        }
    }

    void OnDestroy()
    {
        // Olay aboneliklerinden do�rudan statik �ye �zerinden ��k.
        // Bu, bellek s�z�nt�lar�n� �nler.
        GeminiController.onKonusmaGecmisiEklendi.RemoveListener(emotionObserver.AddConversation);
        GeminiController.onKonusmaBitti.RemoveListener(emotionObserver.HandleConversationEnded);
    }
}
