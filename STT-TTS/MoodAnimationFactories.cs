
/// "�yi" ruh hali i�in animasyon davran��lar� �retir.
public class IyiMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("iyiAglamaTrigger");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("iyiGulmeTrigger");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("iyiKorkmaTrigger");
}


/// "K�t�" ruh hali i�in animasyon davran��lar� �retir.
public class KotuMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("kotuAglamaTrigger");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("kotuGulmeTrigger");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("kotuKorkmaTrigger");
}


/// "N�tr" ruh hali i�in animasyon davran��lar� �retir.
public class NotrMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("notrAglamaTrigger");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("notrGulmeTrigger");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("notrKorkmaTrigger");
}