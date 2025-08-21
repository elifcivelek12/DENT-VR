using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Gerekli tüm bileþenlerin oyun nesnesi üzerinde bulunmasýný zorunlu kýlar.
// NavMeshAgent, Animator ve PlayerInput bileþenleri gereklidir.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))] // Input System için eklendi.
public class AdvancedAvatarController : MonoBehaviour
{
    [Header("Referanslar ve Hedefler")]
    [Tooltip("Avatarýn yürüyeceði hedef nokta.")]
    [SerializeField] private Transform walkTarget;
    [Tooltip("Oturma animasyonunda hizalanacak sandalye referansý.")]
    [SerializeField] private Transform seatAnchor;
    [Tooltip("Karakterin ana kök transformu (genellikle kendisi).")]
    [SerializeField] private Transform characterRoot;

    [Header("Hizalama (MatchTarget) Ayarlarý")]
    [SerializeField] private bool useMatchTarget = true;
    [SerializeField] private AvatarTarget bodyTarget = AvatarTarget.Root;
    [SerializeField, Range(0f, 1f)] private float matchStartNormTime = 0.15f;
    [SerializeField, Range(0f, 1f)] private float matchEndNormTime = 0.85f;
    [SerializeField] private bool snapToSeatOnSitFinish = true;

    // --- Bileþen Referanslarý ---
    private NavMeshAgent agent;
    private Animator animator;
    private PlayerInput playerInput;
    private InputAction walkAction;
    private InputAction sitAction;

    // --- Durum (State) Deðiþkenleri ---
    private bool isSeated = false;
    private bool isWalking = false;
    private bool inTransition = false; // Oturma/kalkma animasyonu geçiþinde mi?
    private bool matchedThisCycle = false;

    // --- Performans için Animator Parametre ID'leri ---
    private readonly int IsSeatedBoolID = Animator.StringToHash("IsSitting");
    private readonly int StartWalkTriggerID = Animator.StringToHash("startwalk");
    private readonly int StopWalkTriggerID = Animator.StringToHash("stopwalk");
    private readonly int SitTriggerID = Animator.StringToHash("sitdown");
    private readonly int StandTriggerID = Animator.StringToHash("standup");

    #region Unity Yaþam Döngüsü Metotlarý

    void Awake()
    {
        // Gerekli bileþenleri alýyoruz.
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        // Kök transform atanmamýþsa, bu nesnenin transformunu kullan.
        if (!characterRoot) characterRoot = transform;

        // Input Action'larý isimleriyle bulup referanslarýný alýyoruz.
        // "Avatar" sizin Action Map'inizin adý olmalý. Deðilse buradan deðiþtirin.
        walkAction = playerInput.actions["Walk"];
        sitAction = playerInput.actions["SitToggle"];
    }

    void OnEnable()
    {
        // Input eylemleri gerçekleþtiðinde ilgili fonksiyonlarý çaðýr.
        walkAction.performed += _ => ToggleWalk();
        sitAction.performed += _ => ToggleSit();
    }

    void OnDisable()
    {
        // Bellek sýzýntýlarýný önlemek için dinleyicileri kaldýr.
        walkAction.performed -= _ => ToggleWalk();
        sitAction.performed -= _ => ToggleSit();
    }

    void Update()
    {
        // Sadece yürüme durumundaysa hedefe ulaþýp ulaþmadýðýný kontrol et.
        if (isWalking)
        {
            // NavMeshAgent hedefe olan mesafesi, durma mesafesinden küçük veya eþitse
            // VE hedefe ulaþmak için bekleyen bir yol hesaplamasý yoksa (hedefe çok yaklaþmýþsa)
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                StopWalk();
            }
        }
    }

    void OnAnimatorMove()
    {
        // Oturma animasyonu sýrasýnda MatchTarget kullanarak karakteri sandalyeye mükemmel þekilde hizala.
        if (!useMatchTarget || !inTransition || isSeated == false || animator == null || seatAnchor == null) return;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        // Animator'daki oturma state'inizin adýyla eþleþmeli.
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

    #region Yürüme Kontrolleri

    /// <summary>
    /// Yürüme tuþuna basýldýðýnda çaðrýlýr. Karakteri yürütür veya durdurur.
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
    /// Karakteri yürütmeye baþlatan ana fonksiyon.
    /// </summary>
    private void StartWalk()
    {
        // Oturuyorsa, animasyon geçiþindeyse veya hedef atanmamýþsa yürüme.
        if (isSeated || inTransition)
        {
            Debug.LogWarning("[AdvancedAvatarController] Otururken veya geçiþ anýnda yürünemez.");
            return;
        }

        if (walkTarget != null)
        {
            Debug.Log("[AdvancedAvatarController] Yürüme baþlatýlýyor. Hedef: " + walkTarget.name);

            agent.isStopped = false; // Agent'in hareket etmesini saðla.
            agent.SetDestination(walkTarget.position); // NavMesh Agent'e hedefi ata.
            animator.SetTrigger(StartWalkTriggerID); // Yürüme animasyonunu tetikle.

            isWalking = true;
        }
        else
        {
            Debug.LogError("[AdvancedAvatarController] YÜRÜME HEDEFÝ (WalkTarget) ATANMAMIÞ! Avatar hareket edemez.");
        }
    }

    /// <summary>
    /// Hedefe ulaþýldýðýnda veya manuel olarak çaðrýldýðýnda yürümeyi durdurur.
    /// </summary>
    private void StopWalk()
    {
        if (!isWalking) return;

        Debug.Log("[AdvancedAvatarController] Yürüme durduruluyor.");

        agent.isStopped = true; // NavMesh Agent'i durdur.
        agent.ResetPath(); // Mevcut yolu temizle.
        animator.SetTrigger(StopWalkTriggerID); // Durma animasyonunu tetikle.

        isWalking = false;
    }

    #endregion

    #region Oturma Kontrolleri

    /// <summary>
    /// Oturma tuþuna basýldýðýnda çaðrýlýr. Oturuyorsa kaldýrýr, ayaktaysa oturtur.
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
        // Yürüyorsa, geçiþteyse veya zaten oturuyorsa tekrar oturma.
        if (isWalking || inTransition || isSeated) return;

        Debug.Log("[AdvancedAvatarController] Oturma eylemi baþlatýlýyor.");
        inTransition = true;
        isSeated = true; // Oturma animasyonu baþladýðý anda oturduðunu varsayýyoruz
        matchedThisCycle = false;

        animator.SetTrigger(SitTriggerID);
        animator.SetBool(IsSeatedBoolID, true);
    }

    private void Stand()
    {
        if (inTransition || !isSeated) return;

        Debug.Log("[AdvancedAvatarController] Ayaða kalkma eylemi baþlatýlýyor.");
        inTransition = true;
        isSeated = false; // Kalkma animasyonu baþladýðý anda ayakta olduðunu varsayýyoruz

        animator.SetTrigger(StandTriggerID);
        animator.SetBool(IsSeatedBoolID, false);
    }

    #endregion

    #region Animasyon Olaylarý (Animation Events)
    // Bu fonksiyonlarý ilgili animasyonlarýn sonuna Event olarak eklemeniz gerekir.

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
        matchedThisCycle = false; // Bir sonraki oturma için hizalama bayraðýný sýfýrla.
    }

    #endregion
}