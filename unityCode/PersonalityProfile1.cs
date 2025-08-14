using UnityEngine;

// Bu satır sayesinde Unity menüsünden kolayca yeni profiller oluşturabiliriz.
[CreateAssetMenu(fileName = "YeniKişilik", menuName = "TestProjesi/Kişilik Profili")]
public class PersonalityProfile : ScriptableObject
{
    public string personalityName;

    [Header("Seri Sayma Kuralları")]
    public int goodMoodThreshold = 10;
    public int badMoodThreshold = 3;
}