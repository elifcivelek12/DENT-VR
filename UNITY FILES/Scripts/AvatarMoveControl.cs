using UnityEngine;


public class AvatarMoveControl : MonoBehaviour
{

    [Header("Ba�lant�lar")]
    [Tooltip("Animasyonlar� kontrol edilecek avatar�n Animator bile�eni.")]
    public Animator animator;
    
    void Start()
    {
        GameManager.onPatientEntered += BaslatYurume;

    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
        {
            Debug.Log("Yurume An�masyonu Oynat�l�yor");
            
        }
    }

    public void BaslatYurume()
    {
        Debug.Log("StartWalk ba�lad�");
        animator.SetTrigger("startwalk");
        Invoke("DurdurYurume", 10f);
    }

    public void DurdurYurume()
    {
        Debug.Log("Stopwalk ba�lad�");
        animator.SetTrigger("stopwalk");
    }
}