using UnityEngine;

/// Belirli bir trigger ismini kullanarak animasyon tetikleyen genel ama�l� s�n�f.

public class AnimationBehaviour : IAnimationBehaviour
{
    // �al��t�r�lacak animasyon tetikleyicisinin ad�.
    private readonly string triggerName;

  
    /// Yap�c� metot, hangi animasyon trigger'�n�n kullan�laca��n� belirler.
    /// <param name="triggerName">Animator'de tan�mlanm�� olan trigger parametresinin ad�.</param>
    public AnimationBehaviour(string triggerName)
    {
        this.triggerName = triggerName;
    }

    /// Verilen animator bile�eni �zerinde animasyonu oynat�r.
    /// <param name="animator">Animasyonun oynat�laca�� Animator bile�eni.</param>
    public void Play(Animator animator)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
        {
            Debug.LogWarning($"[AnimationBehaviour] Animator veya triggerName bo�! Tetikleyici: '{triggerName}' �al��t�r�lamad�.");
            return;
        }

        Debug.Log($"<color=lime>!!!!!! AN�MASYON (FABR�KADAN) TET�KLEND� -> {triggerName} !!!!!!!</color>");
        animator.SetTrigger(triggerName);
    }
}