using System;
using Microsoft.Xna.Framework.Audio;

namespace _4DND;

public sealed class PaperMapSfxSynth : IDisposable
{
    private const int SampleRate = 44100;
    private readonly SoundEffect _openSound;
    private readonly SoundEffect _closeSound;

    public PaperMapSfxSynth()
    {
        _openSound = CreatePaperRustleSound(durationSeconds: 0.38f, reverseEnvelope: false, seed: 101);
        _closeSound = CreatePaperRustleSound(durationSeconds: 0.30f, reverseEnvelope: true, seed: 202);
    }

    public void PlayOpen()
    {
        _openSound.Play(volume: 0.55f, pitch: 0.08f, pan: 0.0f);
    }

    public void PlayClose()
    {
        _closeSound.Play(volume: 0.5f, pitch: -0.12f, pan: 0.0f);
    }

    private static SoundEffect CreatePaperRustleSound(float durationSeconds, bool reverseEnvelope, int seed)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] pcm16Buffer = new byte[sampleCount * 2];

        Random random = new Random(seed);
        float lp = 0f;
        float hp = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;

            float attack = MathF.Min(1f, t / 0.08f);
            float release = MathF.Min(1f, (1f - t) / 0.35f);
            float envelope = attack * release;
            if (reverseEnvelope)
                envelope = MathF.Sqrt(MathF.Max(0f, 1f - t)) * release;

            float white = (float)(random.NextDouble() * 2.0 - 1.0);
            float burstGate = (float)random.NextDouble() > 0.62f ? 1f : 0f;
            float burst = white * burstGate;

            lp += 0.22f * (burst - lp);
            hp = burst - lp;

            float crinkle = hp * 0.7f + lp * 0.3f;
            float sample = crinkle * envelope * 0.55f;

            sample = MathF.Clamp(sample, -1f, 1f);
            short pcm = (short)(sample * short.MaxValue);
            pcm16Buffer[i * 2] = (byte)(pcm & 0xFF);
            pcm16Buffer[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        return new SoundEffect(pcm16Buffer, SampleRate, AudioChannels.Mono);
    }

    public void Dispose()
    {
        _openSound.Dispose();
        _closeSound.Dispose();
    }
}
