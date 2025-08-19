# Gerekli kütüphaneleri içe aktarır.
import os  # İşletim sistemiyle ilgili işlemler için (dosya yolları, ortam değişkenleri).
import json  # JSON verilerini işlemek için (metni Python sözlüğüne dönüştürme).
import re  # Metin içinde desen aramak için Regular Expressions (Regex) kütüphanesi.
import google.generativeai as genai  # Google'ın üretken yapay zeka modellerini kullanmak için.
from dotenv import load_dotenv  # .env dosyasından ortam değişkenlerini yüklemek için.

# ------------------ Ortam değişkenlerini yükle ------------------
# .env dosyasını bulur ve içindeki değişkenleri programa yükler.
load_dotenv()
# Yüklenen ortam değişkenlerinden "GOOGLE_API_KEY" adlı değişkeni alır.
API_KEY = os.getenv("GOOGLE_API_KEY")
# Eğer API anahtarı bulunamazsa, bir hata fırlatır ve programı durdurur.
if not API_KEY:
    raise RuntimeError("GOOGLE_API_KEY eksik! .env dosyasında ekle.")

# Alınan API anahtarı ile Google AI kütüphanesini yapılandırır.
genai.configure(api_key=API_KEY)

# ------------------ Prompt'u (sistem talimatını) dosyadan oku ------------------
# Bu script'in bulunduğu dizinin tam yolunu alır.
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
# Prompt dosyasının tam yolunu oluşturur (ör: C:/proje/prompts/dis_hekim_prompt.txt).
PROMPT_PATH = os.path.join(BASE_DIR, "prompts", "dis_hekim_prompt.txt")

# Prompt dosyasının var olup olmadığını kontrol eder.
if not os.path.exists(PROMPT_PATH):
    raise FileNotFoundError(f"Prompt dosyası bulunamadı: {PROMPT_PATH}")

# Prompt dosyasını okuma modunda ("r") ve utf-8 karakter kodlamasıyla açar.
with open(PROMPT_PATH, "r", encoding="utf-8") as f:
    # Dosyanın tüm içeriğini okuyup SYSTEM_PRIMER değişkenine atar.
    SYSTEM_PRIMER = f.read()

# ------------------ Model ------------------
# Kullanılacak olan Gemini modelinin adını belirtir.
MODEL_NAME = "gemini-1.5-flash"
# Belirtilen model adıyla bir üretken model nesnesi oluşturur.
model = genai.GenerativeModel(MODEL_NAME)

# ------------------ JSON parse (ayrıştırma) fonksiyonu ------------------
# Modelin ürettiği metinden JSON'u güvenli bir şekilde çıkaran ve doğrulayan fonksiyon.
def parse_json_strict(text: str) -> dict:
    # Metin içinde "{" ile başlayıp "}" ile biten ilk bloğu bulur. re.DOTALL, yeni satır karakterlerini de eşleşmeye dahil eder.
    m = re.search(r"\{.*\}", text, flags=re.DOTALL)
    # Eğer bir JSON bloğu bulunamazsa, hata fırlatır.
    if not m:
        raise ValueError("JSON bulunamadı.")
    # Bulunan JSON metnini Python sözlüğüne (dictionary) dönüştürür.
    obj = json.loads(m.group(0))

    # ---- Veri Doğrulama ----
    # "kategori" anahtarının değerini alır, küçük harfe çevirir ve başındaki/sonundaki boşlukları temizler.
    kat = obj.get("kategori", "").lower().strip()
    # Kategorinin beklenen değerlerden biri olup olmadığını kontrol eder.
    if kat not in {"pozitif", "negatif", "nötr"}:
        raise ValueError(f"Geçersiz kategori: {kat}")

    # "tepki" anahtarının değerini alır ve boşlukları temizler.
    tepki = obj.get("tepki", "").strip()
    # Tepkinin boş olup olmadığını kontrol eder.
    if not tepki:
        raise ValueError("Tepki boş.")

    # "animasyon" ve "duygu" değerlerini alır ve temizler (bu alanlar için katı bir kontrol yok).
    animasyon = obj.get("animasyon", "").strip()
    duygu = obj.get("duygu", "").strip()

    # "ikna" anahtarının değerini alır, küçük harfe çevirir ve boşlukları temizler.
    ikna = obj.get("ikna", "").lower().strip()
    # İkna durumunun beklenen değerlerden biri olup olmadığını kontrol eder.
    if ikna not in {"evet", "hayır", "kararsız"}:
        raise ValueError(f"Geçersiz ikna değeri: {ikna}")

    # Doğrulanmış ve temizlenmiş verileri içeren bir sözlük döndürür.
    return {
        "kategori": kat,
        "tepki": tepki,
        "animasyon": animasyon,
        "duygu": duygu,
        "ikna": ikna,
    }

# ------------------ Diyalog döngüsü ------------------
# Programın ana işlevini yürüten fonksiyon.
def main():
    print("VR Çocuk Tepki Simülatörü (çıkmak için: çıkış)\n")

    # Kullanıcı "çıkış" yazana kadar devam edecek sonsuz bir döngü başlatır.
    while True:
        # Kullanıcıdan "Doktor > " istemiyle bir girdi alır ve boşlukları temizler.
        doktor_cumlesi = input("Doktor > ").strip()
        # Kullanıcının girdisinin çıkış komutlarından biri olup olmadığını kontrol eder.
        if doktor_cumlesi.lower() in {"çıkış", "quit"}:
            print("Programdan çıkılıyor...")
            break  # Döngüyü sonlandırır.

        # Sistemin ana talimatı (SYSTEM_PRIMER) ile doktorun son cümlesini birleştirerek modele gönderilecek son prompt'u oluşturur.
        prompt = f"{SYSTEM_PRIMER}\nDoktorun cümlesi: \"{doktor_cumlesi}\""

        # Hazırlanan prompt ile modeli çalıştırır ve yanıtını alır.
        response = model.generate_content(prompt)
        # Yanıttan metin içeriğini alır. Eğer yanıt boşsa, boş bir string atar.
        text = response.text or ""

        try:
            # İlk olarak, gelen yanıtı katı JSON parse fonksiyonu ile işlemeyi dener.
            result = parse_json_strict(text)
        except Exception:
            # Eğer ilk deneme başarısız olursa (model açıklama vb. eklemişse),
            # modele "sadece JSON üret" diyen daha ısrarcı bir prompt hazırlar.
            retry_prompt = prompt + "\nLütfen **yalnızca** geçerli JSON üret. Açıklama yazma."
            # İkinci deneme için modeli tekrar çalıştırır.
            resp2 = model.generate_content(retry_prompt)
            try:
                # İkinci yanıtı parse etmeyi dener.
                result = parse_json_strict(resp2.text)
            except Exception:
                # Eğer ikinci deneme de başarısız olursa, bir hata durumu sonucu oluşturur.
                result = {
                    "kategori": "hata",
                    "tepki": "Modelden geçerli bir JSON alınamadı.",
                    "animasyon": "bekleme",
                    "duygu": "nötr",
                    "ikna": "kararsız",
                }

        # Sonucu formatlı bir şekilde ekrana yazdırır.
        print(f"\nKategori  : {result['kategori']}")
        print(f"Tepki     : {result['tepki']}")
        print(f"Animasyon : {result['animasyon']}")
        print(f"Duygu     : {result['duygu']}")
        print(f"İkna      : {result['ikna']}\n")

# Bu standart Python yapısı, script'in doğrudan çalıştırılıp çalıştırılmadığını kontrol eder.
# Eğer doğrudan çalıştırıldıysa (başka bir dosya tarafından import edilmediyse), main() fonksiyonunu çağırır.
if __name__ == "__main__":
    main()
