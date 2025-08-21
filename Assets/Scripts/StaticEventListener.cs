using UnityEngine;

public class StaticEventListener : MonoBehaviour
{
    void OnEnable()
    {
        VRCrouchDetector.OnCrouchStartStatic += HandleCrouchStart;
        VRCrouchDetector.OnCrouchEndStatic += HandleCrouchEnd;
    }

    void OnDisable()
    {
        VRCrouchDetector.OnCrouchStartStatic -= HandleCrouchStart;
        VRCrouchDetector.OnCrouchEndStatic -= HandleCrouchEnd;
    }

    void HandleCrouchStart() => Debug.Log("Öðrenci eðildi!");
    void HandleCrouchEnd() => Debug.Log("Öðrenci kalktý!");
}
