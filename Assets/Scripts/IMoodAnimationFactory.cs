
/// Soyut Fabrika Aray�z�.
/// Bir ruh haline uygun animasyon davran��lar� ailesini olu�turmak i�in gereken metotlar� tan�mlar.
public interface IMoodAnimationFactory
{
    IAnimationBehaviour CreateGulmeAnimation();
    IAnimationBehaviour CreateAglamaAnimation();
    IAnimationBehaviour CreateKorkmaAnimation();
}