using System;
using System.Collections.Generic;

namespace _4DND;

public class LuteProceduralMusic
{
    private readonly LuteSynthesizer _synth;
    private readonly Random _random = new();
    private float _noteTimer = 0;
    private bool _isPlaying = false;
    private LuteSong _currentSong;
    private int _noteIndex = 0;

    // D Dorian scale for procedural generation
    private readonly int[] _scale = { 50, 52, 53, 55, 57, 59, 60, 62, 64, 65, 67, 69 };
    private readonly int[] _bassNotes = { 38, 45, 50 }; // D2, A2, D3

    public LuteProceduralMusic(LuteSynthesizer synth)
    {
        _synth = synth;
    }

    public void PlayRandomTune()
    {
        _isPlaying = true;
        _noteTimer = 0;
        _noteIndex = 0;

        if (_random.NextDouble() < 0.5)
        {
            // Pick a real song
            var songs = LuteSongs.AllSongs;
            _currentSong = songs[_random.Next(songs.Count)];
        }
        else
        {
            // Generate a structured procedural song
            _currentSong = GenerateProceduralSong();
        }
    }

    private LuteSong GenerateProceduralSong()
    {
        var s = new LuteSong("Procedural Tune", 110f + (float)_random.NextDouble() * 40f);

        // Generate 4 phrases of 8 notes each (A-A-B-A pattern)
        List<LuteNote> phraseA = GeneratePhrase(8);
        List<LuteNote> phraseB = GeneratePhrase(8);

        AddPhraseToSong(s, phraseA);
        AddPhraseToSong(s, phraseA);
        AddPhraseToSong(s, phraseB);
        AddPhraseToSong(s, phraseA);

        return s;
    }

    private List<LuteNote> GeneratePhrase(int length)
    {
        var notes = new List<LuteNote>();
        int currentMidi = _scale[_random.Next(_scale.Length)];

        for (int i = 0; i < length; i++)
        {
            // Random walk on scale
            int scaleIdx = Array.IndexOf(_scale, currentMidi);
            scaleIdx += _random.Next(-2, 3);
            scaleIdx = Math.Clamp(scaleIdx, 0, _scale.Length - 1);
            currentMidi = _scale[scaleIdx];

            float duration = _random.NextDouble() < 0.2 ? 1.0f : 0.5f; // Quarter or Eighth (relative to tempo)
            float velocity = (float)(0.25 + _random.NextDouble() * 0.2);

            notes.Add(new LuteNote(currentMidi, duration, velocity));
        }
        return notes;
    }

    private void AddPhraseToSong(LuteSong s, List<LuteNote> phrase)
    {
        foreach (var note in phrase)
        {
            // Occasional bass note played simultaneously with melody
            if (_random.NextDouble() < 0.3)
            {
                int bass = _bassNotes[_random.Next(_bassNotes.Length)];
                s.Notes.Add(new LuteNote(bass, 0, 0.3f, false)); // Play bass first, no delay
                s.Notes.Add(note with { IsChord = true }); // Play melody with delay
            }
            else
            {
                s.Notes.Add(note);
            }
        }
    }

    public void Stop()
    {
        _isPlaying = false;
        _synth.Stop();
    }

    public bool IsPlaying => _isPlaying;

    public void Update(float deltaTime)
    {
        if (!_isPlaying || _currentSong == null) return;

        if (_noteIndex >= _currentSong.Notes.Count)
        {
            _isPlaying = false;
            return;
        }

        _noteTimer -= deltaTime;

        while (_isPlaying && _noteTimer <= 0)
        {
            var note = _currentSong.Notes[_noteIndex];

            float freq = MidiToFreq(note.MidiNote);
            _synth.PlayNote(freq, note.Velocity);

            // Advance timer by note's duration
            float secPerBeat = 60f / _currentSong.Tempo;
            _noteTimer += note.Duration * secPerBeat;

            _noteIndex++;
            if (_noteIndex < _currentSong.Notes.Count)
            {
                if (_currentSong.Notes[_noteIndex].IsChord)
                {
                    // Continue immediately to play the simultaneous note
                    continue;
                }
                else
                {
                    // Exit while loop if we have a delay to wait for
                    if (_noteTimer > 0) break;
                }
            }
            else
            {
                _isPlaying = false;
            }
        }
    }

    private float MidiToFreq(int midi)
    {
        return (float)(440.0 * Math.Pow(2.0, (midi - 69) / 12.0));
    }
}
