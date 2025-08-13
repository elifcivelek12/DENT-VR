print("program başladı")
import json
import re
import google.generativeai as genai

# -------------- Yapılandırma --------------
API_KEY = "AIzaSyDjPgiv58mb32zcPEMkTDJVuCM5l8EMcs4"  # Buraya kendi API key'ini yaz
genai.configure(api_key=API_KEY)

MODEL_NAME = "gemini-1.5-flash"
model = genai.GenerativeModel(MODEL_NAME)

# -------------- Sabit yönlendirici (persona + çıktı şeması) --------------
SYSTEM_PRIMER = """
Sen bir VR diş hekimi simülasyonunda 8 yaşında hafif gıcık bir kız çocuğusun.
Görevin:
1) Doktorun cümlesini "olumlu", "olumsuz" veya "notr" (nötr) olarak sınıflandır.
2) Bu kategoriye uygun, doğal, kısa ve çocukça TEK bir tepki cümlesi üret.
   - Basit kelimeler, 5-12 kelime.
   - En fazla 1 emoji.
   - Tehdit, suçlama, tıbbi talimat yok.
   - Korkutucu veya yetişkinvari ifadeler yok.

Tanımlar (rehber):
- olumlu: destekleyici, cesaretlendirici, yatıştırıcı, ödüllendirici.
- olumsuz: korkutucu, tehditkâr, suçlayıcı, azarlayıcı, baskıcı.
- notr: yönerge, açıklama, bilgi verme; duygusal ton taşımayan.

YANITINI **yalnızca** şu JSON biçiminde ver:
{
  "kategori": "olumlu|olumsuz|notr",
  "tepki": "<tek cümle>"
}
JSON dışında metin yazma.
# Örnekler
Örnekler:
- Pozitif: "Çok cesursun, aferin sana.", "İşlem bittikten sonra oyun oynayacağız."
- Negatif: "Hareket edersen acıyabilir.", "Dişini çektirmeyi reddedersen problem büyür."
- Nötr: "Lütfen koltuğa otur.", "Ağzını açar mısın?"

"""

def build_prompt(doktor_cumlesi: str) -> str:
    return f"""{SYSTEM_PRIMER}

Doktorun cümlesi: "{doktor_cumlesi}"
"""

def parse_json_strict(text: str) -> dict:
    m = re.search(r"\{.*\}", text, flags=re.DOTALL)
    if not m:
        raise ValueError("JSON bulunamadı.")
    obj = json.loads(m.group(0))
    kat = obj.get("kategori", "").lower().strip()
    if kat not in {"olumlu", "olumsuz", "notr"}:
        raise ValueError(f"Geçersiz kategori: {kat}")
    tepki = obj.get("tepki", "").strip()
    if not tepki:
        raise ValueError("Tepki boş.")
    return {"kategori": kat, "tepki": tepki}

def classify_and_respond(doktor_cumlesi: str) -> dict:
    prompt = build_prompt(doktor_cumlesi)
    resp = model.generate_content(prompt)
    text = resp.text or ""
    try:
        return parse_json_strict(text)
    except Exception:
        retry_prompt = prompt + "\nLütfen sadece geçerli JSON üret. Açıklama yazma."
        resp2 = model.generate_content(retry_prompt)
        return parse_json_strict(resp2.text or "")

def main():
    print("VR Çocuk Tepki Simülatörü (çıkmak için: çıkış)")
    while True:
        doktor = input("Doktor > ").strip()
        if doktor.lower() in {"çıkış", "cikis", "exit", "quit"}:
            break
        try:
            sonuc = classify_and_respond(doktor)
            print(f"Kategori: {sonuc['kategori']}")
            print(f"Çocuk  : {sonuc['tepki']}\n")
        except Exception as e:
            print(f"Hata: {e}\nHam çıktı: {locals().get('text','(yok)')}\n")

if __name__ == "__main__":
    main()
print("program bitti")