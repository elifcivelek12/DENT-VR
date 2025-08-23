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

    [Header("UI Referanslar� (Kay�t)")]
    public TMP_InputField emailRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_Text statusText; 

    [Header("UI Referanslar� (Giri�)")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;


    void Start()
    {
        // Firebase sistemini ba�lat ve haz�r oldu�unda devam et
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase haz�r, Auth servisini ba�latabiliriz
                InitializeFirebase();
            }
            else
            {
                Debug.LogError($"Firebase ba�lat�lamad�: {dependencyStatus}");
            }
        });
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
        Debug.Log("Firebase Auth ba�ar�yla ba�lat�ld�.");
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Kullan�c� oturumu kapatt�.");
                CurrentUserID = null; 
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log($"Oturum durumu de�i�ti: Kullan�c� giri� yapt� ({user.Email})");
                CurrentUserID = user.UserId; 
            }
        }
    }

    // Uygulama kapand���nda dinleyiciyi kald�r.
    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }


    #region Kay�t ��lemleri

    // RegisterConfirmButton'un OnClick() eventi
    public void RegisterUser()
    {
        RegisterUserAsync(emailRegisterField.text, passwordRegisterField.text);
    }

    private async Task RegisterUserAsync(string email, string password)
    {
        if (statusText != null) statusText.text = "Kay�t yap�l�yor...";
        try
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            user = result.User;
            Debug.Log($"Firebase kullan�c�s� ba�ar�yla olu�turuldu: {user.Email} ({user.UserId})");
            if (statusText != null) statusText.text = "Kay�t ba�ar�l�!";
        }
        catch (FirebaseException ex)
        {
            AuthError errorCode = (AuthError)ex.ErrorCode;
            string errorMessage = "Bilinmeyen bir hata olu�tu.";
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    errorMessage = "Bu e-posta adresi zaten kullan�l�yor.";
                    break;
                case AuthError.InvalidEmail:
                    errorMessage = "Ge�ersiz e-posta adresi format�.";
                    break;
                case AuthError.WeakPassword:
                    errorMessage = "�ifre en az 6 karakter olmal�d�r.";
                    break;
            }
            if (statusText != null) statusText.text = $"Hata: {errorMessage}";
        }
    }

    #endregion


    #region Giri� ve ��k�� ��lemleri

    // LoginPanel'deki "Giri� Yap" butonunun OnClick() event
    public void LoginUser()
    {
        LoginUserAsync(emailLoginField.text, passwordLoginField.text);
    }

    private async Task LoginUserAsync(string email, string password)
    {
        if (statusText != null) statusText.text = "Giri� yap�l�yor...";

        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);

            Debug.Log($"Firebase kullan�c�s� ba�ar�yla giri� yapt�: {result.User.Email} ({result.User.UserId})");
            if (statusText != null) statusText.text = "Giri� ba�ar�l�!";

            OnLoginSuccessful?.Invoke();
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"Giri� s�ras�nda hata olu�tu: {ex.Message}");
            AuthError errorCode = (AuthError)ex.ErrorCode;
            string errorMessage = "Bilinmeyen bir hata olu�tu.";
            switch (errorCode)
            {
                case AuthError.WrongPassword:
                    errorMessage = "Hatal� �ifre.";
                    break;
                case AuthError.UserNotFound:
                    errorMessage = "Bu e-posta adresine sahip bir kullan�c� bulunamad�.";
                    break;
                case AuthError.InvalidEmail:
                    errorMessage = "Ge�ersiz e-posta adresi format�.";
                    break;
            }
            if (statusText != null) statusText.text = $"Hata: {errorMessage}";
        }
    }

    // Ana men�deki "��k�� Yap" butonu
    public void SignOut()
    {
        if (auth.CurrentUser != null)
        {
            auth.SignOut();
        }
    }

    #endregion
}