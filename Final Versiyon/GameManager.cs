using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro; // TMP_Text için

public class GameManager : MonoBehaviour
{
    // Oyun durumlarýný takip etmek için enum kullanabiliriz.
    public enum GameState { BeforeStart, Playing, GameOver }
    public static GameState currentState;

    [Header("UI Baðlantýlarý")]
    [Tooltip("Oyun sonuçlarýnýn gösterileceði TextMeshPro nesnesi.")]
    public TMP_Text resultText; // Sonuçlarýn gösterileceði TextMeshPro nesnesi

    [Header("Olaylar")]
    // Oyun baþladýðýnda veya bittiðinde diðer script'lerin dinlemesi için olaylar.
    // Bu sayede diðer script'ler, bu olaylara abone olarak tepki verebilir.
    public static UnityEvent onLevelStart = new UnityEvent();
    public static UnityEvent onLevelEnd = new UnityEvent();

    // YENÝ EKLENDÝ: GeminiController ve EmotionObserver referanslarý
    // Not: Bu referanslar artýk olaylarý baðlamak için deðil,
    // sadece Inspector'dan atama kolaylýðý ve kod içinde eriþim için kalabilir.
    public GeminiController geminiController;
    public EmotionObserver emotionObserver;

    // Oyun süresini tutacak deðiþken
    private float startTime;

    // Tekil (Singleton) desenini kullanmak, bu script'e diðer script'lerden kolayca eriþmemizi saðlar.
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

        // Gerekli referanslarýn atandýðýndan emin ol
        if (emotionObserver == null)
        {
            Debug.LogError("EmotionObserver referansý GameManager'da eksik!");
            return;
        }

        Debug.Log("GameManager baþlatýldý. Bileþenler arasý baðlantýlar kuruluyor...");

        // ÖNCEKÝ HATANIN DÜZELTÝLMÝÞ HALÝ: Olaylara doðrudan statik üye üzerinden abone ol.
        // Artýk geminiController referansýný kullanmanýza gerek yok, çünkü olaylar statik.
        GeminiController.onKonusmaGecmisiEklendi.AddListener(emotionObserver.AddConversation);
        GeminiController.onKonusmaBitti.AddListener(emotionObserver.HandleConversationEnded);
    }

    // Bu fonksiyonu VR gözlüðü taktýðýmýzda veya bir UI butonuyla çaðýracaðýz.
    public void StartLevel()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("[GameManager] Seviye Baþlatýlýyor.");
            currentState = GameState.Playing;
            startTime = Time.time; // Sayacý baþlat
            onLevelStart.Invoke(); // Oyunun baþladýðýný duyur.

            // UI'ý baþlangýçta temizle
            if (resultText != null)
            {
                resultText.text = "Seans Baþladý...";
            }
        }
    }

    // Konuþma bittiðinde çaðrýlacak fonksiyon.
    public void EndLevel()
    {
        if (currentState == GameState.Playing)
        {
            Debug.Log("[GameManager] Seviye Sonlandýrýlýyor.");
            currentState = GameState.GameOver;

            float duration = Time.time - startTime;
            Debug.Log($"[GameManager] Oyun süresi: {duration:F2} saniye.");

            onLevelEnd.Invoke(); // Oyunun bittiðini duyur.
        }
    }

    void OnDestroy()
    {
        // Olay aboneliklerinden doðrudan statik üye üzerinden çýk.
        // Bu, bellek sýzýntýlarýný önler.
        GeminiController.onKonusmaGecmisiEklendi.RemoveListener(emotionObserver.AddConversation);
        GeminiController.onKonusmaBitti.RemoveListener(emotionObserver.HandleConversationEnded);
    }
}
