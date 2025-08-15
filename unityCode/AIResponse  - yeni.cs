// AIResponse.cs

// Bu basit sınıf, yapay zekadan gelen ayrıştırılmış veriyi tutar.
// Böylece eventi dinleyen her sistem, veriye temiz bir şekilde erişebilir.
public class AIResponse
{
    public string Duygu; // ör: "uzgun", "mutlu"
    public string Tepki; // ör: "aglama", "gulme"
    public string OriginalSentence; // İsteğe bağlı: Orijinal cümleyi de saklayabiliriz.
}