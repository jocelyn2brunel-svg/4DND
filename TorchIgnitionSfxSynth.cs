using System;
using Microsoft.Xna.Framework.Audio;

namespace _4DND;

public sealed class TorchIgnitionSfxSynth : IDisposable
{
    private const int SampleRate = 44100;
    private readonly SoundEffect _igniteSound;

    public TorchIgnitionSfxSynth()
    {
        _igniteSound = CreateTorchIgnitionSound(durationSeconds: 0.55f, seed: 4242);
    }

    public void Play()
    {
        _igniteSound.Play(volume: 0.6f, pitch: 0.0f, pan: 0.0f);
    }

    private static SoundEffect CreateTorchIgnitionSound(float durationSeconds, int seed)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] pcm16Buffer = new byte[sampleCount * 2];

        Random random = new Random(seed);
        float lowpass = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;

            float strike = MathF.Exp(-t * 30f) * MathF.Sin(2f * MathF.PI * 1800f * t);
            float whoosh = MathF.Exp(-t * 7.5f) * MathF.Sin(2f * MathF.PI * (220f + t * 380f) * t);

            float white = (float)(random.NextDouble() * 2.0 - 1.0);
            lowpass += 0.04f * (white - lowpass);
            float crackleGate = (float)random.NextDouble() > 0.89f ? 1f : 0f;
            float crackle = (white - lowpass) * crackleGate * MathF.Exp(-t * 4f);

            float flameBed = lowpass * 0.35f * MathF.Exp(-t * 1.8f);

            float envelope = MathF.Min(1f, t / 0.02f) * MathF.Min(1f, (durationSeconds - t) / 0.2f);
            float sample = (strike * 0.4f) + (whoosh * 0.35f) + (crackle * 0.9f) + flameBed;
            sample *= envelope;

            sample = Math.Clamp(sample, -1f, 1f);
            short pcm = (short)(sample * short.MaxValue);
            pcm16Buffer[i * 2] = (byte)(pcm & 0xFF);
            pcm16Buffer[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        return new SoundEffect(pcm16Buffer, SampleRate, AudioChannels.Mono);
    }

    public void Dispose()
    {
        _igniteSound.Dispose();
    }
}
