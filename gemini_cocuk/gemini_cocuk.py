import os
import json
import re
import google.generativeai as genai
from dotenv import load_dotenv

# ------------------ Ortam değişkenlerini yükle ------------------
load_dotenv()
API_KEY = os.getenv("GOOGLE_API_KEY")
if not API_KEY:
    raise RuntimeError("GOOGLE_API_KEY eksik! .env dosyasında ekle.")

genai.configure(api_key=API_KEY)

# ------------------ Modeli bir kez oluştur ------------------
MODEL_NAME = "gemini-1.5-flash"
model = genai.GenerativeModel(MODEL_NAME)

# ------------------ Sabit prompt ------------------
SYSTEM_PRIMER = """
Sen bir VR diş hekimi simülasyonunda 8 yaşında bir kız çocuğusun.
Görev:
1) Doktorun cümlesini 'pozitif', 'negatif' veya 'nötr' olarak sınıflandır.
2) Kısa, doğal ve çocukça bir tepki üret.
3) Tepkiye uygun animasyon seç.
4) Duyguyu belirt.

JSON formatında dön:
{
    "kategori": "pozitif|negatif|nötr",
    "tepki": "<çocuğun kısa cevabı>",
    "animasyon": "<oynatılacak animasyon>",
    "duygu": "<çocuğun duygusu>"
}
JSON dışında metin yazma.

Örnekler:
- Pozitif: "Çok cesursun, aferin sana.", animasyon: "gülümseme", duygu: "mutlu"
- Negatif: "Hareket edersen acıyabilir.", animasyon: "korku", duygu: "endişeli"
- Nötr: "Lütfen koltuğa otur.", animasyon: "bekleme", duygu: "nötr"
"""

# ------------------ JSON parse fonksiyonu ------------------
def parse_json_strict(text: str) -> dict:
    m = re.search(r"\{.*\}", text, flags=re.DOTALL)
    if not m:
        raise ValueError("JSON bulunamadı.")
    obj = json.loads(m.group(0))
    kat = obj.get("kategori", "").lower().strip()
    if kat not in {"pozitif", "negatif", "nötr"}:
        raise ValueError(f"Geçersiz kategori: {kat}")
    tepki = obj.get("tepki", "").strip()
    if not tepki:
        raise ValueError("Tepki boş.")
    animasyon = obj.get("animasyon", "").strip()
    duygu = obj.get("duygu", "").strip()
    return {"kategori": kat, "tepki": tepki, "animasyon": animasyon, "duygu": duygu}

# ------------------ Diyalog döngüsü ------------------
def main():
    print("VR Çocuk Tepki Simülatörü (çıkmak için: çıkış)\n")

    while True:
        doktor_cumlesi = input("Doktor > ").strip()
        if doktor_cumlesi.lower() in {"çıkış", "quit"}:
            print("Programdan çıkılıyor...")
            break

        prompt = f"{SYSTEM_PRIMER}\nDoktorun cümlesi: \"{doktor_cumlesi}\""

        # Modeli çalıştır
        response = model.generate_content(prompt)
        text = response.text or ""

        try:
            result = parse_json_strict(text)
        except Exception:
            # Eğer JSON değilse yedek: modeli tekrar zorla
            retry_prompt = prompt + "\nLütfen **yalnızca** geçerli JSON üret. Açıklama yazma."
            resp2 = model.generate_content(retry_prompt)
            try:
                result = parse_json_strict(resp2.text)
            except Exception:
                result = {"kategori": "hata", "tepki": "", "animasyon": "", "duygu": ""}

        print(f"\nKategori  : {result['kategori']}")
        print(f"Tepki     : {result['tepki']}")
        print(f"Animasyon : {result['animasyon']}")
        print(f"Duygu     : {result['duygu']}\n")

if __name__ == "__main__":
    main()
