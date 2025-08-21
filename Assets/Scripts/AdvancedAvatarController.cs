using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Gerekli t�m bile�enlerin oyun nesnesi �zerinde bulunmas�n� zorunlu k�lar.
// NavMeshAgent, Animator ve PlayerInput bile�enleri gereklidir.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))] // Input System i�in eklendi.
public class AdvancedAvatarController : MonoBehaviour
{
    [Header("Referanslar ve Hedefler")]
    [Tooltip("Avatar�n y�r�yece�i hedef nokta.")]
    [SerializeField] private Transform walkTarget;
    [Tooltip("Oturma animasyonunda hizalanacak sandalye referans�.")]
    [SerializeField] private Transform seatAnchor;
    [Tooltip("Karakterin ana k�k transformu (genellikle kendisi).")]
    [SerializeField] private Transform characterRoot;

    [Header("Hizalama (MatchTarget) Ayarlar�")]
    [SerializeField] private bool useMatchTarget = true;
    [SerializeField] private AvatarTarget bodyTarget = AvatarTarget.Root;
    [SerializeField, Range(0f, 1f)] private float matchStartNormTime = 0.15f;
    [SerializeField, Range(0f, 1f)] private float matchEndNormTime = 0.85f;
    [SerializeField] private bool snapToSeatOnSitFinish = true;

    // --- Bile�en Referanslar� ---
    private NavMeshAgent agent;
    private Animator animator;
    private PlayerInput playerInput;
    private InputAction walkAction;
    private InputAction sitAction;

    // --- Durum (State) De�i�kenleri ---
    private bool isSeated = false;
    private bool isWalking = false;
    private bool inTransition = false; // Oturma/kalkma animasyonu ge�i�inde mi?
    private bool matchedThisCycle = false;

    // --- Performans i�in Animator Parametre ID'leri ---
    private readonly int IsSeatedBoolID = Animator.StringToHash("IsSitting");
    private readonly int StartWalkTriggerID = Animator.StringToHash("startwalk");
    private readonly int StopWalkTriggerID = Animator.StringToHash("stopwalk");
    private readonly int SitTriggerID = Animator.StringToHash("sitdown");
    private readonly int StandTriggerID = Animator.StringToHash("standup");

    #region Unity Ya�am D�ng�s� Metotlar�

    void Awake()
    {
        // Gerekli bile�enleri al�yoruz.
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        // K�k transform atanmam��sa, bu nesnenin transformunu kullan.
        if (!characterRoot) characterRoot = transform;

        // Input Action'lar� isimleriyle bulup referanslar�n� al�yoruz.
        // "Avatar" sizin Action Map'inizin ad� olmal�. De�ilse buradan de�i�tirin.
        walkAction = playerInput.actions["Walk"];
        sitAction = playerInput.actions["SitToggle"];
    }

    void OnEnable()
    {
        // Input eylemleri ger�ekle�ti�inde ilgili fonksiyonlar� �a��r.
        walkAction.performed += _ => ToggleWalk();
        sitAction.performed += _ => ToggleSit();
    }

    void OnDisable()
    {
        // Bellek s�z�nt�lar�n� �nlemek i�in dinleyicileri kald�r.
        walkAction.performed -= _ => ToggleWalk();
        sitAction.performed -= _ => ToggleSit();
    }

    void Update()
    {
        // Sadece y�r�me durumundaysa hedefe ula��p ula�mad���n� kontrol et.
        if (isWalking)
        {
            // NavMeshAgent hedefe olan mesafesi, durma mesafesinden k���k veya e�itse
            // VE hedefe ula�mak i�in bekleyen bir yol hesaplamas� yoksa (hedefe �ok yakla�m��sa)
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                StopWalk();
            }
        }
    }

    void OnAnimatorMove()
    {
        // Oturma animasyonu s�ras�nda MatchTarget kullanarak karakteri sandalyeye m�kemmel �ekilde hizala.
        if (!useMatchTarget || !inTransition || isSeated == false || animator == null || seatAnchor == null) return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        // Animator'daki oturma state'inizin ad�yla e�le�meli.
        if (info.IsName("Sitting"))
        {
            float t = info.normalizedTime % 1f;
            if (t >= matchStartNormTime && t <= matchEndNormTime)
            {
                if (matchedThisCycle) return;

                animator.MatchTarget(
                    seatAnchor.position,
                    seatAnchor.rotation,
                    bodyTarget,
                    new MatchTargetWeightMask(Vector3.one, 1f),
                    matchStartNormTime,
                    matchEndNormTime
                );
                matchedThisCycle = true;
            }
        }
    }

    #endregion

    #region Y�r�me Kontrolleri

    /// <summary>
    /// Y�r�me tu�una bas�ld���nda �a�r�l�r. Karakteri y�r�t�r veya durdurur.
    /// </summary>
    public void ToggleWalk()
    {
        if (isWalking)
        {
            StopWalk();
        }
        else
        {
            StartWalk();
        }
    }

    /// <summary>
    /// Karakteri y�r�tmeye ba�latan ana fonksiyon.
    /// </summary>
    private void StartWalk()
    {
        // Oturuyorsa, animasyon ge�i�indeyse veya hedef atanmam��sa y�r�me.
        if (isSeated || inTransition)
        {
            Debug.LogWarning("[AdvancedAvatarController] Otururken veya ge�i� an�nda y�r�nemez.");
            return;
        }

        if (walkTarget != null)
        {
            Debug.Log("[AdvancedAvatarController] Y�r�me ba�lat�l�yor. Hedef: " + walkTarget.name);

            agent.isStopped = false; // Agent'in hareket etmesini sa�la.
            agent.SetDestination(walkTarget.position); // NavMesh Agent'e hedefi ata.
            animator.SetTrigger(StartWalkTriggerID); // Y�r�me animasyonunu tetikle.

            isWalking = true;
        }
        else
        {
            Debug.LogError("[AdvancedAvatarController] Y�R�ME HEDEF� (WalkTarget) ATANMAMI�! Avatar hareket edemez.");
        }
    }

    /// <summary>
    /// Hedefe ula��ld���nda veya manuel olarak �a�r�ld���nda y�r�meyi durdurur.
    /// </summary>
    private void StopWalk()
    {
        if (!isWalking) return;

        Debug.Log("[AdvancedAvatarController] Y�r�me durduruluyor.");

        agent.isStopped = true; // NavMesh Agent'i durdur.
        agent.ResetPath(); // Mevcut yolu temizle.
        animator.SetTrigger(StopWalkTriggerID); // Durma animasyonunu tetikle.

        isWalking = false;
    }

    #endregion

    #region Oturma Kontrolleri

    /// <summary>
    /// Oturma tu�una bas�ld���nda �a�r�l�r. Oturuyorsa kald�r�r, ayaktaysa oturtur.
    /// </summary>
    public void ToggleSit()
    {
        if (isSeated)
        {
            Stand();
        }
        else
        {
            Sit();
        }
    }

    private void Sit()
    {
        // Y�r�yorsa, ge�i�teyse veya zaten oturuyorsa tekrar oturma.
        if (isWalking || inTransition || isSeated) return;

        Debug.Log("[AdvancedAvatarController] Oturma eylemi ba�lat�l�yor.");
        inTransition = true;
        isSeated = true; // Oturma animasyonu ba�lad��� anda oturdu�unu varsay�yoruz
        matchedThisCycle = false;

        animator.SetTrigger(SitTriggerID);
        animator.SetBool(IsSeatedBoolID, true);
    }

    private void Stand()
    {
        if (inTransition || !isSeated) return;

        Debug.Log("[AdvancedAvatarController] Aya�a kalkma eylemi ba�lat�l�yor.");
        inTransition = true;
        isSeated = false; // Kalkma animasyonu ba�lad��� anda ayakta oldu�unu varsay�yoruz

        animator.SetTrigger(StandTriggerID);
        animator.SetBool(IsSeatedBoolID, false);
    }

    #endregion

    #region Animasyon Olaylar� (Animation Events)
    // Bu fonksiyonlar� ilgili animasyonlar�n sonuna Event olarak eklemeniz gerekir.

    public void OnSitFinished()
    {
        inTransition = false;

        // Oturma bitince karakteri tam olarak sandalyenin pozisyonuna ve rotasyonuna sabitle.
        if (snapToSeatOnSitFinish && seatAnchor && characterRoot)
        {
            characterRoot.position = seatAnchor.position;
            characterRoot.rotation = seatAnchor.rotation;
        }
    }

    public void OnStandFinished()
    {
        inTransition = false;
        matchedThisCycle = false; // Bir sonraki oturma i�in hizalama bayra��n� s�f�rla.
    }

    #endregion
}