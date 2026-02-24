using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace _4DND;

public class LuteSynthesizer : IDisposable
{
    private const int SampleRate = 44100;
    private DynamicSoundEffectInstance _instance;
    private readonly List<LuteString> _strings = new();
    private readonly byte[] _audioBuffer;
    private const int BufferSamples = 2205; // 50ms buffer

    public LuteSynthesizer()
    {
        _instance = new DynamicSoundEffectInstance(SampleRate, AudioChannels.Mono);
        _instance.BufferNeeded += OnBufferNeeded;
        _audioBuffer = new byte[BufferSamples * 2]; // 16-bit mono
    }

    public void PlayNote(float frequency, float velocity)
    {
        lock (_strings)
        {
            _strings.Add(new LuteString(SampleRate, frequency, velocity));
        }

        if (_instance.State != SoundState.Playing)
        {
            // Submit an initial empty buffer to kickstart the BufferNeeded event if necessary
            FillAndSubmitBuffer();
            _instance.Play();
        }
    }

    public void Stop()
    {
        if (_instance.State == SoundState.Playing)
            _instance.Stop();

        lock (_strings)
        {
            _strings.Clear();
        }
    }

    private void OnBufferNeeded(object sender, EventArgs e)
    {
        FillAndSubmitBuffer();
    }

    private void FillAndSubmitBuffer()
    {
        while (_instance.PendingBufferCount < 3)
        {
            Array.Clear(_audioBuffer, 0, _audioBuffer.Length);

            lock (_strings)
            {
                if (_strings.Count == 0 && _instance.State == SoundState.Playing)
                {
                    // No strings playing, we can stop or just submit silence
                    // To keep it simple, just submit silence for a bit then let it stop if needed.
                }

                for (int i = 0; i < BufferSamples; i++)
                {
                    float mixedSample = 0;
                    for (int s = _strings.Count - 1; s >= 0; s--)
                    {
                        mixedSample += _strings[s].GetNextSample();
                        if (_strings[s].IsDead)
                        {
                            _strings.RemoveAt(s);
                        }
                    }

                    // Soft clipping
                    if (mixedSample > 1.0f) mixedSample = 1.0f;
                    if (mixedSample < -1.0f) mixedSample = -1.0f;

                    short pcm = (short)(mixedSample * 32767);
                    _audioBuffer[i * 2] = (byte)(pcm & 0xFF);
                    _audioBuffer[i * 2 + 1] = (byte)(pcm >> 8);
                }
            }

            _instance.SubmitBuffer(_audioBuffer);
        }
    }

    public void Update()
    {
        // If no more strings are active and instance is playing, we could stop it to save resources
        lock (_strings)
        {
            if (_strings.Count == 0 && _instance.State == SoundState.Playing && _instance.PendingBufferCount == 0)
            {
                _instance.Stop();
            }
        }
    }

    public void Dispose()
    {
        _instance?.Dispose();
        _instance = null!;
    }
}

internal class LuteString
{
    private readonly float[] _ringBuffer;
    private int _index;
    private readonly float _feedback = 0.992f; // Dampening factor
    public bool IsDead { get; private set; }
    private int _remainingSamples;

    public LuteString(int sampleRate, float frequency, float velocity)
    {
        // Avoid division by zero or negative frequencies
        if (frequency < 10f) frequency = 10f;

        int bufferLength = (int)(sampleRate / frequency);
        if (bufferLength < 2) bufferLength = 2;

        _ringBuffer = new float[bufferLength];
        Random rand = new Random();

        // Initial excitation with white noise
        for (int i = 0; i < bufferLength; i++)
        {
            _ringBuffer[i] = (float)(rand.NextDouble() * 2.0 - 1.0) * velocity;
        }

        _index = 0;
        // The note will last roughly 1-2 seconds
        _remainingSamples = sampleRate * 2;
    }

    public float GetNextSample()
    {
        if (_remainingSamples <= 0)
        {
            IsDead = true;
            return 0;
        }

        int nextIndex = (_index + 1) % _ringBuffer.Length;

        // Karplus-Strong: average current and next sample to simulate a low-pass filter (string vibration)
        float value = (_ringBuffer[_index] + _ringBuffer[nextIndex]) * 0.5f * _feedback;

        _ringBuffer[_index] = value;
        _index = nextIndex;

        _remainingSamples--;

        // Fade out at the very end to avoid clicks
        if (_remainingSamples < 1000)
        {
            return value * (_remainingSamples / 1000f);
        }

        return value;
    }
}
