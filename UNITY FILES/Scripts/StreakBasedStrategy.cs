// StreakBasedStrategy.cs
using UnityEngine;

public class StreakBasedStrategy : IMoodStrategy
{
    public event MoodChangeRequired OnMoodShouldChange;

    private PersonalityProfile profile;
    private int goodStreak = 0;
    private int badStreak = 0;

    public void SetProfile(PersonalityProfile profile)
    {
        this.profile = profile;
        Debug.Log($"[Strateji] Profil ayarlandı: {profile.personalityName}. Kötü mod eşiği: {profile.badMoodThreshold}, İyi mod eşiği: {profile.goodMoodThreshold}");
    }

    public void ProcessCategory(string category)
    {
        if (profile == null) return;

        switch (category)
        {
            case "olumlu":
                badStreak = 0;
                goodStreak++;
                break;
            case "kötü":
                goodStreak = 0;
                badStreak++;
                break;
        }
        Debug.Log($"[Strateji] Kategori işlendi: '{category}'. Güncel seriler -> İyi: {goodStreak}, Kötü: {badStreak}");
        CheckForMoodChange();
    }

    private void CheckForMoodChange()
    {
        if (goodStreak >= profile.goodMoodThreshold)
        {
            OnMoodShouldChange?.Invoke("İYİ");
            Reset();
        }
        else if (badStreak >= profile.badMoodThreshold)
        {
            OnMoodShouldChange?.Invoke("KÖTÜ");
            Reset();
        }
    }

    public void Reset()
    {
        Debug.Log("[Strateji] Sayaçlar sıfırlandı.");
        goodStreak = 0;
        badStreak = 0;
    }
}