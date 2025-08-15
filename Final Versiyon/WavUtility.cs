using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    // AudioClip'i WAV formatýnda bir byte dizisine dönüþtürür.
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("WavUtility: AudioClip boþ.");
            return null;
        }

        MemoryStream stream = new MemoryStream();
        int HEADER_SIZE = 44;

        // Baþlýk için yer ayýr.
        for (int i = 0; i < HEADER_SIZE; i++)
            stream.WriteByte(0);

        int samplesCount = clip.samples * clip.channels;
        float[] samples = new float[samplesCount];
        clip.GetData(samples, 0);

        short[] intData = new short[samplesCount];
        byte[] bytesData = new byte[samplesCount * 2];

        for (int i = 0; i < samplesCount; i++)
        {
            intData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
            byte[] b = BitConverter.GetBytes(intData[i]);
            bytesData[i * 2] = b[0];
            bytesData[i * 2 + 1] = b[1];
        }

        stream.Write(bytesData, 0, bytesData.Length);

        // WAV baþlýðýný oluþturup stream'in baþýna yazar.
        WriteHeader(stream, clip);

        byte[] wavBytes = stream.ToArray();
        stream.Dispose();
        return wavBytes;
    }

    // WAV dosya baþlýðýný yazar.
    static void WriteHeader(Stream stream, AudioClip clip)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        stream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        stream.Write(riff, 0, 4);

        int subChunk2 = samples * channels * 2;
        int chunkSize = 36 + subChunk2;
        stream.Write(BitConverter.GetBytes(chunkSize), 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        stream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        stream.Write(fmt, 0, 4);

        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((short)1), 0, 2);
        stream.Write(BitConverter.GetBytes((short)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(hz), 0, 4);

        int byteRate = hz * channels * 2;
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);

        short blockAlign = (short)(channels * 2);
        stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);
        short bitsPerSample = 16;
        stream.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);

        byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        stream.Write(datastring, 0, 4);
        stream.Write(BitConverter.GetBytes(subChunk2), 0, 4);
    }
}
