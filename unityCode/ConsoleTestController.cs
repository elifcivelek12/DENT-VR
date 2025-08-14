// ConsoleTester.cs
using System;
using UnityEngine;

public class ConsoleTester : MonoBehaviour
{
    // OBSERVER: Bu, tüm sistemin dinleyeceği "anons kanalıdır".
    public static event Action<string> OnCategoryReceived;

    [ContextMenu("TEST -> Olumlu Yanıt Gönder")]
    void SendPositive()
    {
        Debug.Log("[YAYINCI] 'olumlu' kategorisi için anons yapılıyor...");
        OnCategoryReceived?.Invoke("olumlu");
    }

    [ContextMenu("TEST -> Kötü Yanıt Gönder")]
    void SendNegative()
    {
        Debug.Log("[YAYINCI] 'kotu' kategorisi için anons yapılıyor...");
        OnCategoryReceived?.Invoke("kotu");
    }
}