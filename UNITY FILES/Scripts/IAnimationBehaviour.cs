using UnityEngine;


/// Bir animasyon davranýþýnýn temel arayüzü. 
/// Bu arayüzü uygulayan her sýnýf, bir Animator üzerinde bir animasyonu oynatabilmelidir.
public interface IAnimationBehaviour
{
    void Play(Animator animator);
}