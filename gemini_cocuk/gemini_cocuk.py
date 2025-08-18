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

# ------------------ Prompt'u dosyadan oku ------------------
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROMPT_PATH = os.path.join(BASE_DIR, "prompts", "dis_hekim_prompt.txt")

if not os.path.exists(PROMPT_PATH):
    raise FileNotFoundError(f"Prompt dosyası bulunamadı: {PROMPT_PATH}")

with open(PROMPT_PATH, "r", encoding="utf-8") as f:
    SYSTEM_PRIMER = f.read()

# ------------------ Model ------------------
MODEL_NAME = "gemini-1.5-flash"
model = genai.GenerativeModel(MODEL_NAME)

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

    ikna = obj.get("ikna", "").lower().strip()
    if ikna not in {"evet", "hayır", "kararsız"}:
        raise ValueError(f"Geçersiz ikna değeri: {ikna}")

    return {
        "kategori": kat,
        "tepki": tepki,
        "animasyon": animasyon,
        "duygu": duygu,
        "ikna": ikna,
    }

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
            retry_prompt = prompt + "\nLütfen **yalnızca** geçerli JSON üret. Açıklama yazma."
            resp2 = model.generate_content(retry_prompt)
            try:
                result = parse_json_strict(resp2.text)
            except Exception:
                result = {
                    "kategori": "hata",
                    "tepki": "",
                    "animasyon": "",
                    "duygu": "",
                    "ikna": "",
                }

        print(f"\nKategori  : {result['kategori']}")
        print(f"Tepki     : {result['tepki']}")
        print(f"Animasyon : {result['animasyon']}")
        print(f"Duygu     : {result['duygu']}")
        print(f"İkna      : {result['ikna']}\n")

if __name__ == "__main__":
    main()
