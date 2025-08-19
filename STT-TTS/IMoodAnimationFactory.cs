
/// Soyut Fabrika Arayüzü.
/// Bir ruh haline uygun animasyon davranýþlarý ailesini oluþturmak için gereken metotlarý tanýmlar.
public interface IMoodAnimationFactory
{
    IAnimationBehaviour CreateGulmeAnimation();
    IAnimationBehaviour CreateAglamaAnimation();
    IAnimationBehaviour CreateKorkmaAnimation();
}