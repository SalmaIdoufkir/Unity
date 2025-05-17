using System;
using UnityEngine;
using System.IO;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] bytes = ConvertToWav(samples, clip.channels, clip.frequency);
        return bytes;
    }

    private static byte[] ConvertToWav(float[] samples, int channels, int frequency)
    {
        // Convertir les échantillons en format WAV
        int sampleCount = samples.Length;
        int byteCount = sampleCount * 2; // 16 bits = 2 bytes
        byte[] wavFile = new byte[44 + byteCount];

        // Header WAV
        WriteWavHeader(wavFile, sampleCount, channels, frequency);

        // Audio data
        short[] shortSamples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            shortSamples[i] = (short)(samples[i] * short.MaxValue);
        }

        Buffer.BlockCopy(shortSamples, 0, wavFile, 44, byteCount);

        return wavFile;
    }

    private static void WriteWavHeader(byte[] wavFile, int sampleCount, int channels, int frequency)
    {
        // Format WAV header (simplifié)
        int byteRate = frequency * channels * 2; // 2 bytes par échantillon
        int blockAlign = channels * 2;
        int dataChunkSize = sampleCount * 2;

        // "RIFF" header
        wavFile[0] = (byte)'R';
        wavFile[1] = (byte)'I';
        wavFile[2] = (byte)'F';
        wavFile[3] = (byte)'F';
        BitConverter.GetBytes(36 + dataChunkSize).CopyTo(wavFile, 4);

        // "WAVE" format
        wavFile[8] = (byte)'W';
        wavFile[9] = (byte)'A';
        wavFile[10] = (byte)'V';
        wavFile[11] = (byte)'E';

        // "fmt " chunk
        wavFile[12] = (byte)'f';
        wavFile[13] = (byte)'m';
        wavFile[14] = (byte)'t';
        wavFile[15] = (byte)' ';
        BitConverter.GetBytes(16).CopyTo(wavFile, 16); // Subchunk size
        BitConverter.GetBytes((short)1).CopyTo(wavFile, 20); // PCM format
        BitConverter.GetBytes((short)channels).CopyTo(wavFile, 22);
        BitConverter.GetBytes(frequency).CopyTo(wavFile, 24);
        BitConverter.GetBytes(byteRate).CopyTo(wavFile, 28);
        BitConverter.GetBytes((short)blockAlign).CopyTo(wavFile, 32);
        BitConverter.GetBytes((short)16).CopyTo(wavFile, 34); // Bits per sample

        // "data" chunk
        wavFile[36] = (byte)'d';
        wavFile[37] = (byte)'a';
        wavFile[38] = (byte)'t';
        wavFile[39] = (byte)'a';
        BitConverter.GetBytes(dataChunkSize).CopyTo(wavFile, 40);
    }
}
