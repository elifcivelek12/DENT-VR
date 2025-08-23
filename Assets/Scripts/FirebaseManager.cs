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
                Debug.Log("Firebase baþarýyla baþlatýldý.");
            }
            else
            {
                Debug.LogError("Firebase baþlatýlamadý: " + task.Result);
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
            Debug.LogError("Firebase Kayýt Hatasý: " + e.Message);
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
            Debug.LogError("Firebase Giriþ Hatasý: " + e.Message);
            return false;
        }
    }

    public async Task SaveSession(List<string> conversation, AnalysisResult result)
    {
        var anlikKullanici = FirebaseAuth.DefaultInstance.CurrentUser;

        if (anlikKullanici == null)
        {
            Debug.LogError("!!! SEANS KAYDEDÝLEMEDÝ !!! Fonksiyon çaðrýldýðýnda giriþ yapmýþ bir kullanýcý bulunamadý. Lütfen giriþ yapýldýðýndan emin olun.");
            return;
        }

        Debug.Log($"Seans kaydetme iþlemi '{anlikKullanici.Email}' için baþlatýldý. Kullanýcý ID: {anlikKullanici.UserId}");

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
            Debug.Log($"<color=green>BAÞARILI:</color> '{anlikKullanici.Email}' kullanýcýsýnýn yeni seansý Firebase'e baþarýyla kaydedildi.");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>HATA:</color> Firestore'a veri yazýlýrken bir hata oluþtu: {e.Message}");
        }
    }
}