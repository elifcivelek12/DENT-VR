using UnityEngine;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System; 

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
   

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase ba�ar�yla ba�lat�ld�.");
            }
            else
            {
                Debug.LogError("Firebase ba�lat�lamad�: " + task.Result);
            }
        });
    }

    public async Task<bool> RegisterUser(string email, string password)
    {
        try
        {
            await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Firebase Kay�t Hatas�: " + e.Message);
            return false;
        }
    }

    public async Task<bool> LoginUser(string email, string password)
    {
        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Firebase Giri� Hatas�: " + e.Message);
            return false;
        }
    }

    public async Task SaveSession(List<string> conversation, AnalysisResult result)
    {
        var anlikKullanici = FirebaseAuth.DefaultInstance.CurrentUser;

        if (anlikKullanici == null)
        {
            Debug.LogError("!!! SEANS KAYDED�LEMED� !!! Fonksiyon �a�r�ld���nda giri� yapm�� bir kullan�c� bulunamad�. L�tfen giri� yap�ld���ndan emin olun.");
            return;
        }

        Debug.Log($"Seans kaydetme i�lemi '{anlikKullanici.Email}' i�in ba�lat�ld�. Kullan�c� ID: {anlikKullanici.UserId}");

        SessionData newSession = new SessionData
        {
            SessionTimestamp = Timestamp.GetCurrentTimestamp(),
            ConversationHistory = conversation,
            PositiveScore = result.PositiveScore,
            NegativeScore = result.NegativeScore,
            NeutralScore = result.NeutralScore,
            Feedback = result.feedback
        };

        DocumentReference userDocRef = db.Collection("kullanicilar").Document(anlikKullanici.UserId);

        try
        {
            await userDocRef.Collection("seanslar").AddAsync(newSession);
            Debug.Log($"<color=green>BA�ARILI:</color> '{anlikKullanici.Email}' kullan�c�s�n�n yeni seans� Firebase'e ba�ar�yla kaydedildi.");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>HATA:</color> Firestore'a veri yaz�l�rken bir hata olu�tu: {e.Message}");
        }
    }
}