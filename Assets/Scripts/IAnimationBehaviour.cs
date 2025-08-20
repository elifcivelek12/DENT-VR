using UnityEngine;


/// Bir animasyon davran���n�n temel aray�z�. 
/// Bu aray�z� uygulayan her s�n�f, bir Animator �zerinde bir animasyonu oynatabilmelidir.
public interface IAnimationBehaviour
{
    void Play(Animator animator);
}