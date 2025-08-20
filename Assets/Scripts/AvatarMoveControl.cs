using UnityEngine;


public class AvatarMoveControl : MonoBehaviour
{

    [Header("Baðlantýlar")]
    [Tooltip("Animasyonlarý kontrol edilecek avatarýn Animator bileþeni.")]
    public Animator animator;
    
    void Start()
    {
        GameManager.onPatientEntered += BaslatYurume;

    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
        {
            Debug.Log("Yurume Anýmasyonu Oynatýlýyor");
            
        }
    }

    public void BaslatYurume()
    {
        Debug.Log("StartWalk baþladý");
        animator.SetTrigger("startwalk");
        Invoke("DurdurYurume", 10f);
    }

    public void DurdurYurume()
    {
        Debug.Log("Stopwalk baþladý");
        animator.SetTrigger("stopwalk");
    }
}