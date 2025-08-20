using UnityEngine;

/// Belirli bir trigger ismini kullanarak animasyon tetikleyen genel amaçlý sýnýf.

public class AnimationBehaviour : IAnimationBehaviour
{
    // Çalýþtýrýlacak animasyon tetikleyicisinin adý.
    private readonly string triggerName;

  
    /// Yapýcý metot, hangi animasyon trigger'ýnýn kullanýlacaðýný belirler.
    /// <param name="triggerName">Animator'de tanýmlanmýþ olan trigger parametresinin adý.</param>
    public AnimationBehaviour(string triggerName)
    {
        this.triggerName = triggerName;
    }

    /// Verilen animator bileþeni üzerinde animasyonu oynatýr.
    /// <param name="animator">Animasyonun oynatýlacaðý Animator bileþeni.</param>
    public void Play(Animator animator)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
        {
            Debug.LogWarning($"[AnimationBehaviour] Animator veya triggerName boþ! Tetikleyici: '{triggerName}' çalýþtýrýlamadý.");
            return;
        }

        Debug.Log($"<color=lime>!!!!!! ANÝMASYON (FABRÝKADAN) TETÝKLENDÝ -> {triggerName} !!!!!!!</color>");
        animator.SetTrigger(triggerName);
    }
}