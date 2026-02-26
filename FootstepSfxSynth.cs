using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace _4DND;

public sealed class FootstepSfxSynth : IDisposable
{
    private const int SampleRate = 44100;
    private readonly Dictionary<TileType, SoundEffect> _terrainSteps = new();
    private readonly Random _rng = new();

    public FootstepSfxSynth()
    {
        Register(TileType.Floor, CreateStep(0.09f, noiseMix: 0.35f, toneHz: 210f, decay: 24f, seed: 1));
        Register(TileType.Rock, CreateStep(0.11f, noiseMix: 0.45f, toneHz: 260f, decay: 20f, seed: 2));
        Register(TileType.Grass, CreateStep(0.12f, noiseMix: 0.85f, toneHz: 120f, decay: 16f, seed: 3));
        Register(TileType.Tree, CreateStep(0.12f, noiseMix: 0.85f, toneHz: 120f, decay: 16f, seed: 4));
        Register(TileType.Shrub, CreateStep(0.11f, noiseMix: 0.9f, toneHz: 110f, decay: 14f, seed: 5));
        Register(TileType.Sand, CreateStep(0.14f, noiseMix: 1.0f, toneHz: 85f, decay: 10f, seed: 6));
        Register(TileType.Snow, CreateStep(0.13f, noiseMix: 0.95f, toneHz: 100f, decay: 12f, seed: 7));
        Register(TileType.Mud, CreateStep(0.15f, noiseMix: 1.0f, toneHz: 75f, decay: 9f, seed: 8));
        Register(TileType.Water, CreateStep(0.16f, noiseMix: 0.95f, toneHz: 70f, decay: 7f, seed: 9, watery: true));
        Register(TileType.DifficultTerrain, CreateStep(0.12f, noiseMix: 0.8f, toneHz: 145f, decay: 12f, seed: 10));
        Register(TileType.Ice, CreateStep(0.10f, noiseMix: 0.4f, toneHz: 420f, decay: 30f, seed: 11));

        // Fallbacks
        Register(TileType.Empty, _terrainSteps[TileType.Floor]);
        Register(TileType.Climbable, _terrainSteps[TileType.Rock]);
        Register(TileType.Wall, _terrainSteps[TileType.Rock]);
    }

    public void PlayStep(TileType tileType)
    {
        if (!_terrainSteps.TryGetValue(tileType, out var clip))
            clip = _terrainSteps[TileType.Floor];

        float volume = 0.18f + (float)_rng.NextDouble() * 0.08f;
        float pitch = -0.05f + (float)_rng.NextDouble() * 0.10f;
        clip.Play(volume, pitch, 0f);
    }

    private void Register(TileType tileType, SoundEffect effect)
    {
        _terrainSteps[tileType] = effect;
    }

    private static SoundEffect CreateStep(float durationSeconds, float noiseMix, float toneHz, float decay, int seed, bool watery = false)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] pcm16Buffer = new byte[sampleCount * 2];

        Random random = new(seed);
        float lowPass = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float env = MathF.Exp(-t * decay) * MathF.Min(1f, t / 0.004f);

            float white = (float)(random.NextDouble() * 2.0 - 1.0);
            lowPass += 0.12f * (white - lowPass);
            float grain = lowPass;
            float click = MathF.Sin(2f * MathF.PI * toneHz * t) * MathF.Exp(-t * (decay * 1.15f));

            float splash = 0f;
            if (watery)
            {
                float bubble = MathF.Sin(2f * MathF.PI * (240f + t * 260f) * t) * MathF.Exp(-t * 13f);
                float droplets = ((float)random.NextDouble() > 0.93f ? 1f : 0f) * white * MathF.Exp(-t * 9f);
                splash = bubble * 0.35f + droplets * 0.7f;
            }

            float sample = ((grain * noiseMix) + (click * (1f - noiseMix)) + splash) * env;
            sample = Math.Clamp(sample * 0.95f, -1f, 1f);

            short pcm = (short)(sample * short.MaxValue);
            pcm16Buffer[i * 2] = (byte)(pcm & 0xFF);
            pcm16Buffer[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        return new SoundEffect(pcm16Buffer, SampleRate, AudioChannels.Mono);
    }

    public void Dispose()
    {
        var disposed = new HashSet<SoundEffect>();
        foreach (var entry in _terrainSteps.Values)
        {
            if (disposed.Add(entry))
                entry.Dispose();
        }
        _terrainSteps.Clear();
    }
}
