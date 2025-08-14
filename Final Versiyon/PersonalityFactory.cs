// PersonalityFactory.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum PersonalityType
{
    Sakin,
    Tripli,
    Notr
}

public class PersonalityFactory : MonoBehaviour
{
    // Editörden oluşturduğun tüm profilleri buraya sürükle
    public List<PersonalityProfile> profiles;

    public PersonalityProfile GetProfile(PersonalityType type)
    {
        // Gelen tipe göre doğru profili bulup döndürür.
        // Örneğin, Sakin tipi için "Sakin" isimli profili arar.
        return profiles.FirstOrDefault(p => p.personalityName.ToLower() == type.ToString().ToLower());
    }
}