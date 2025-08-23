using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;
using System.Threading.Tasks;
using System; 

public class AuthManager : MonoBehaviour
{
    [Header("Firebase")]
    private FirebaseAuth auth;
    private FirebaseUser user;

    public static string CurrentUserID { get; private set; }

    public static event Action OnLoginSuccessful;

    [Header("UI Referanslarý (Kayýt)")]
    public TMP_InputField emailRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_Text statusText; 

    [Header("UI Referanslarý (Giriþ)")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;


    void Start()
    {
        // Firebase sistemini baþlat ve hazýr olduðunda devam et
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase hazýr, Auth servisini baþlatabiliriz
                InitializeFirebase();
            }
            else
            {
                Debug.LogError($"Firebase baþlatýlamadý: {dependencyStatus}");
            }
        });
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
        Debug.Log("Firebase Auth baþarýyla baþlatýldý.");
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Kullanýcý oturumu kapattý.");
                CurrentUserID = null; 
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log($"Oturum durumu deðiþti: Kullanýcý giriþ yaptý ({user.Email})");
                CurrentUserID = user.UserId; 
            }
        }
    }

    // Uygulama kapandýðýnda dinleyiciyi kaldýr.
    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }


    #region Kayýt Ýþlemleri

    // RegisterConfirmButton'un OnClick() eventi
    public void RegisterUser()
    {
        RegisterUserAsync(emailRegisterField.text, passwordRegisterField.text);
    }

    private async Task RegisterUserAsync(string email, string password)
    {
        if (statusText != null) statusText.text = "Kayýt yapýlýyor...";
        try
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            user = result.User;
            Debug.Log($"Firebase kullanýcýsý baþarýyla oluþturuldu: {user.Email} ({user.UserId})");
            if (statusText != null) statusText.text = "Kayýt baþarýlý!";
        }
        catch (FirebaseException ex)
        {
            AuthError errorCode = (AuthError)ex.ErrorCode;
            string errorMessage = "Bilinmeyen bir hata oluþtu.";
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    errorMessage = "Bu e-posta adresi zaten kullanýlýyor.";
                    break;
                case AuthError.InvalidEmail:
                    errorMessage = "Geçersiz e-posta adresi formatý.";
                    break;
                case AuthError.WeakPassword:
                    errorMessage = "Þifre en az 6 karakter olmalýdýr.";
                    break;
            }
            if (statusText != null) statusText.text = $"Hata: {errorMessage}";
        }
    }

    #endregion


    #region Giriþ ve Çýkýþ Ýþlemleri

    // LoginPanel'deki "Giriþ Yap" butonunun OnClick() event
    public void LoginUser()
    {
        LoginUserAsync(emailLoginField.text, passwordLoginField.text);
    }

    private async Task LoginUserAsync(string email, string password)
    {
        if (statusText != null) statusText.text = "Giriþ yapýlýyor...";

        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);

            Debug.Log($"Firebase kullanýcýsý baþarýyla giriþ yaptý: {result.User.Email} ({result.User.UserId})");
            if (statusText != null) statusText.text = "Giriþ baþarýlý!";

            OnLoginSuccessful?.Invoke();
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"Giriþ sýrasýnda hata oluþtu: {ex.Message}");
            AuthError errorCode = (AuthError)ex.ErrorCode;
            string errorMessage = "Bilinmeyen bir hata oluþtu.";
            switch (errorCode)
            {
                case AuthError.WrongPassword:
                    errorMessage = "Hatalý þifre.";
                    break;
                case AuthError.UserNotFound:
                    errorMessage = "Bu e-posta adresine sahip bir kullanýcý bulunamadý.";
                    break;
                case AuthError.InvalidEmail:
                    errorMessage = "Geçersiz e-posta adresi formatý.";
                    break;
            }
            if (statusText != null) statusText.text = $"Hata: {errorMessage}";
        }
    }

    // Ana menüdeki "Çýkýþ Yap" butonu
    public void SignOut()
    {
        if (auth.CurrentUser != null)
        {
            auth.SignOut();
        }
    }

    #endregion
}