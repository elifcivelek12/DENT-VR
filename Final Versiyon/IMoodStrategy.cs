// IMoodStrategy.cs
using System;

// Stratejinin, ruh hali değiştiğinde ana kontrolcüye haber vermesi için bir event tipi.
public delegate void MoodChangeRequired(string moodName);

// Bütün ruh hali hesaplama stratejilerinin bu kurallara uyması gerekir.
public interface IMoodStrategy
{
    event MoodChangeRequired OnMoodShouldChange;
    void SetProfile(PersonalityProfile profile);
    void ProcessCategory(string category);
    void Reset();
}