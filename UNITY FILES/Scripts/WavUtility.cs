using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    // AudioClip'i WAV formatında bir byte dizisine dönüştürür.
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("WavUtility: AudioClip boş.");
            return null;
        }

        MemoryStream stream = new MemoryStream();
        int HEADER_SIZE = 44;

        // Başlık için yer ayırıyoruz (daha sonra doldurulacak)
        for (int i = 0; i < HEADER_SIZE; i++)
            stream.WriteByte(0);

        int samplesCount = clip.samples * clip.channels;
        float[] samples = new float[samplesCount];
        clip.GetData(samples, 0);

        short[] intData = new short[samplesCount];
        byte[] bytesData = new byte[samplesCount * 2];

        // Float değerleri 16-bit PCM formatına dönüştür
        for (int i = 0; i < samplesCount; i++)
        {
            intData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
            byte[] b = BitConverter.GetBytes(intData[i]);
            bytesData[i * 2] = b[0];
            bytesData[i * 2 + 1] = b[1];
        }

        // PCM verisini stream'e yaz
        stream.Write(bytesData, 0, bytesData.Length);

        // WAV başlığını oluşturup stream'in başına yaz
        WriteHeader(stream, clip);

        byte[] wavBytes = stream.ToArray();
        stream.Dispose();
        return wavBytes;
    }

    // WAV dosya başlığını yazar
    static void WriteHeader(Stream stream, AudioClip clip)
    {
        int hz = clip.frequency;     // Örnekleme frekansı
        int channels = clip.channels; // Kanal sayısı
        int samples = clip.samples;   // Toplam örnek sayısı

        stream.Seek(0, SeekOrigin.Begin);

        // RIFF başlığı
        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        stream.Write(riff, 0, 4);

        int subChunk2 = samples * channels * 2; // Veri boyutu
        int chunkSize = 36 + subChunk2;
        stream.Write(BitConverter.GetBytes(chunkSize), 0, 4);

        // WAVE format etiketi
        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        stream.Write(wave, 0, 4);

        // fmt alt bloğu
        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        stream.Write(fmt, 0, 4);

        stream.Write(BitConverter.GetBytes(16), 0, 4);         // Subchunk1Size (PCM)
        stream.Write(BitConverter.GetBytes((short)1), 0, 2);  // Audio format (PCM = 1)
        stream.Write(BitConverter.GetBytes((short)channels), 0, 2); // Kanal sayısı
        stream.Write(BitConverter.GetBytes(hz), 0, 4);         // Örnekleme frekansı

        int byteRate = hz * channels * 2;
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);   // Byte rate

        short blockAlign = (short)(channels * 2);
        stream.Write(BitConverter.GetBytes(blockAlign), 0, 2); // Block align
        short bitsPerSample = 16;
        stream.Write(BitConverter.GetBytes(bitsPerSample), 0, 2); // Bit/sample

        // data alt bloğu
        byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        stream.Write(datastring, 0, 4);
        stream.Write(BitConverter.GetBytes(subChunk2), 0, 4); // Veri boyutu
    }
}
