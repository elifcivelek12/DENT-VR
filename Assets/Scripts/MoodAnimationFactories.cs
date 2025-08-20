
/// "Ýyi" ruh hali için animasyon davranýþlarý üretir.
public class IyiMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("goodSad");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("goodHappy");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("goodFear");
}


/// "Kötü" ruh hali için animasyon davranýþlarý üretir.
public class KotuMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("badSad");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("badHappy");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("badFear");
}


/// "Nötr" ruh hali için animasyon davranýþlarý üretir.
public class NotrMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("notrSad");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("notrHappy");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("notrFear");
}