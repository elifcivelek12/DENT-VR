// StreakBasedStrategy.cs
using UnityEngine;

// Ruh hali değişim stratejisi: art arda olumlu veya olumsuz kategorilere göre mood değişimini tetikler
public class StreakBasedStrategy : IMoodStrategy
{
    // Mood değişimi gerektiğinde tetiklenecek olay
    public event MoodChangeRequired OnMoodShouldChange;

    // Kişilik profili referansı
    private PersonalityProfile profile;
    // Art arda olumlu yanıt sayacı
    private int goodStreak = 0;
    // Art arda olumsuz yanıt sayacı
    private int badStreak = 0;

    // Profil atanır ve eşikler debug log ile gösterilir
    public void SetProfile(PersonalityProfile profile)
    {
        this.profile = profile;
        Debug.Log($"[Strateji] Profil ayarlandı: {profile.personalityName}. Kötü mod eşiği: {profile.badMoodThreshold}, İyi mod eşiği: {profile.goodMoodThreshold}");
    }

    // Alınan kategoriye göre serileri günceller
    public void ProcessCategory(string category)
    {
        if (profile == null) return;

        switch (category)
        {
            case "olumlu":
                badStreak = 0; // Olumsuz seri sıfırlanır
                goodStreak++;  // Olumlu seri artırılır
                break;
            case "kötü":
                goodStreak = 0; // Olumlu seri sıfırlanır
                badStreak++;    // Olumsuz seri artırılır
                break;
        }
        Debug.Log($"[Strateji] Kategori işlendi: '{category}'. Güncel seriler -> İyi: {goodStreak}, Kötü: {badStreak}");
        CheckForMoodChange();
    }

    // Eşikleri kontrol ederek mood değişimini tetikler
    private void CheckForMoodChange()
    {
        if (goodStreak >= profile.goodMoodThreshold)
        {
            OnMoodShouldChange?.Invoke("İYİ"); // Mood değişim olayı tetiklenir
            Reset();
        }
        else if (badStreak >= profile.badMoodThreshold)
        {
            OnMoodShouldChange?.Invoke("KÖTÜ"); // Mood değişim olayı tetiklenir
            Reset();
        }
    }

    // Sayaçları sıfırlar
    public void Reset()
    {
        Debug.Log("[Strateji] Sayaçlar sıfırlandı.");
        goodStreak = 0;
        badStreak = 0;
    }
}
