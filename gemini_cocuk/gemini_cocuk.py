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

# ------------------ Prompt ve test ------------------
doktor_cumlesi = input("Doktorun cümlesini yaz: ")

prompt = f"""
Sen bir VR diş hekimi simülasyonunda 8 yaşında bir kız çocuğusun.
Görev:
1) Doktorun cümlesini 'pozitif', 'negatif' veya 'nötr' olarak sınıflandır.
2) Kısa, doğal ve çocukça bir tepki üret.
3) Tepkiye uygun animasyon seç.
4) Duyguyu belirt.

Doktorun cümlesi: "{doktor_cumlesi}"

JSON formatında dön:
{{
    "kategori": "pozitif|negatif|nötr",
    "tepki": "<çocuğun kısa cevabı>",
    "animasyon": "<oynatılacak animasyon>",
    "duygu": "<çocuğun duygusu>"
}}
JSON dışında metin yazma.

Örnekler:
- Pozitif: "Çok cesursun, aferin sana.", animasyon: "gülümseme", duygu: "mutlu"
- Negatif: "Hareket edersen acıyabilir.", animasyon: "korku", duygu: "endişeli"
- Nötr: "Lütfen koltuğa otur.", animasyon: "bekleme", duygu: "nötr"
"""

# Modeli çalıştır
response = model.generate_content(prompt)
text = response.text or ""

# JSON parse
try:
    text = response.text or ""
    # Backtick veya boşlukları temizle
    clean_text = re.search(r"\{.*\}", text, flags=re.DOTALL)
    if clean_text:
        clean_text = clean_text.group(0)
    else:
        clean_text = "{}"  # Geçersizse boş JSON

    result = json.loads(clean_text)
except Exception:
    # Eğer JSON değilse yedek: modeli tekrar zorla
    retry_prompt = prompt + "\nLütfen **yalnızca** geçerli JSON üret. Açıklama yazma."
    resp2 = model.generate_content(retry_prompt)
    print("Ham çıktı:", resp2.text)

    try:
        result = json.loads(resp2.text)
    except json.JSONDecodeError:
        print("Geçersiz JSON geldi, ham çıktı:", resp2.text)
        result = {"kategori": "hata", "tepki": "", "animasyon": "", "duygu": ""}


print("Sonuç:", result)
