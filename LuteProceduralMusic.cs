using System;

namespace _4DND;

public class LuteProceduralMusic
{
    private readonly LuteSynthesizer _synth;
    private readonly Random _random = new();
    private float _noteTimer = 0;
    private float _totalDuration = 0;
    private bool _isPlaying = false;

    // D Dorian scale: D3, E3, F3, G3, A3, B3, C4, D4, E4, F4, G4, A4
    private readonly int[] _scale = { 50, 52, 53, 55, 57, 59, 60, 62, 64, 65, 67, 69 };
    private readonly int[] _bassNotes = { 38, 45, 50 }; // D2, A2, D3

    private float _nextNoteDelay = 0;
    private const float MaxDuration = 15.0f;
    private float _bassTimer = 0;

    public LuteProceduralMusic(LuteSynthesizer synth)
    {
        _synth = synth;
    }

    public void PlayRandomTune()
    {
        _isPlaying = true;
        _noteTimer = 0;
        _totalDuration = 0;
        _nextNoteDelay = 0;
        _bassTimer = 0;
    }

    public void Stop()
    {
        _isPlaying = false;
        _synth.Stop();
    }

    public bool IsPlaying => _isPlaying;

    public void Update(float deltaTime)
    {
        if (!_isPlaying) return;

        _totalDuration += deltaTime;
        if (_totalDuration >= MaxDuration)
        {
            _isPlaying = false;
            // Let the last notes ring out naturally
            return;
        }

        _noteTimer += deltaTime;
        _bassTimer += deltaTime;

        // Bass line: simple drone or periodic notes
        if (_bassTimer >= 1.0f + _random.NextDouble() * 2.0)
        {
            PlayBassNote();
            _bassTimer = 0;
        }

        // Melody line
        if (_noteTimer >= _nextNoteDelay)
        {
            PlayMelodyNote();
            _noteTimer = 0;

            // Random rhythm based on eighth notes (0.15s)
            float[] rhythms = { 0.15f, 0.15f, 0.3f, 0.3f, 0.45f, 0.6f };
            _nextNoteDelay = rhythms[_random.Next(rhythms.Length)];

            // Occasionally pause for a bit
            if (_random.NextDouble() < 0.1)
                _nextNoteDelay += 0.6f;
        }
    }

    private void PlayMelodyNote()
    {
        int midiNote = _scale[_random.Next(_scale.Length)];
        float freq = MidiToFreq(midiNote);
        float velocity = (float)(0.2 + _random.NextDouble() * 0.3);

        _synth.PlayNote(freq, velocity);

        // Small chance of a chord/harmony
        if (_random.NextDouble() < 0.2)
        {
            int offset = _random.NextDouble() < 0.5 ? 2 : 4; // 3rd or 5th in scale
            int harmonyIdx = (Array.IndexOf(_scale, midiNote) + offset) % _scale.Length;
            _synth.PlayNote(MidiToFreq(_scale[harmonyIdx]), velocity * 0.6f);
        }
    }

    private void PlayBassNote()
    {
        int midiNote = _bassNotes[_random.Next(_bassNotes.Length)];
        float freq = MidiToFreq(midiNote);
        _synth.PlayNote(freq, 0.4f);
    }

    private float MidiToFreq(int midi)
    {
        return (float)(440.0 * Math.Pow(2.0, (midi - 69) / 12.0));
    }
}
