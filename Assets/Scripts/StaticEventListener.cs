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

    void HandleCrouchStart() => Debug.Log("��renci e�ildi!");
    void HandleCrouchEnd() => Debug.Log("��renci kalkt�!");
}
