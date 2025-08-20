
/// "�yi" ruh hali i�in animasyon davran��lar� �retir.
public class IyiMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("goodSad");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("goodHappy");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("goodFear");
}


/// "K�t�" ruh hali i�in animasyon davran��lar� �retir.
public class KotuMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("badSad");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("badHappy");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("badFear");
}


/// "N�tr" ruh hali i�in animasyon davran��lar� �retir.
public class NotrMoodAnimationFactory : IMoodAnimationFactory
{
    public IAnimationBehaviour CreateAglamaAnimation() => new AnimationBehaviour("notrSad");
    public IAnimationBehaviour CreateGulmeAnimation() => new AnimationBehaviour("notrHappy");
    public IAnimationBehaviour CreateKorkmaAnimation() => new AnimationBehaviour("notrFear");
}