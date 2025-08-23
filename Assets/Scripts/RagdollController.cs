//using UnityEngine;

//public class RagdollController : MonoBehaviour
//{
//    private Rigidbody[] ragdollRigidbodies;
//    private Collider[] ragdollColliders;
//    private Animator animator;

//    void Start()
//    {
//        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
//        ragdollColliders = GetComponentsInChildren<Collider>();
//        animator = GetComponent<Animator>();

//        DeactivateRagdoll();
//    }

//    public void ActivateRagdoll()
//    {
//        animator.enabled = false;
//        foreach (Rigidbody rb in ragdollRigidbodies)
//        {
//            rb.isKinematic = false;
//        }
//    }

//    public void DeactivateRagdoll()
//    {
//        foreach (Rigidbody rb in ragdollRigidbodies)
//        {
//            rb.isKinematic = true;
//        }
//        animator.enabled = true;
//    }

//    void OnCollisionEnter(Collision collision)
//    {
//        if (collision.gameObject.CompareTag("PlayerHand")) 
//        {
//            ActivateRagdoll();
//        }
//    }
//}