
/// "Ýyi" ruh hali için animasyon davranýþlarý üretir.
public class IyiMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("iyiAglamaTrigger");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("iyiGulmeTrigger");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("iyiKorkmaTrigger");
}


/// "Kötü" ruh hali için animasyon davranýþlarý üretir.
public class KotuMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("kotuAglamaTrigger");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("kotuGulmeTrigger");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("kotuKorkmaTrigger");
}


/// "Nötr" ruh hali için animasyon davranýþlarý üretir.
public class NotrMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("notrAglamaTrigger");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("notrGulmeTrigger");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("notrKorkmaTrigger");
}