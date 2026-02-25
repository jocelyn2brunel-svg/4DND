using System.Collections.Generic;

namespace _4DND;

public record LuteNote(int MidiNote, float Duration, float Velocity = 0.4f, bool IsChord = false);

public class LuteSong
{
    public string Name { get; }
    public List<LuteNote> Notes { get; }
    public float Tempo { get; }

    public LuteSong(string name, float tempo = 120f)
    {
        Name = name;
        Notes = new List<LuteNote>();
        Tempo = tempo;
    }

    public void AddNote(int midiNote, float duration, float velocity = 0.4f)
    {
        Notes.Add(new LuteNote(midiNote, duration, velocity));
    }

    public void AddChord(int[] midiNotes, float duration, float velocity = 0.3f)
    {
        for (int i = 0; i < midiNotes.Length; i++)
        {
            // Only the last note of the chord carries the duration delay
            Notes.Add(new LuteNote(midiNotes[i], i == midiNotes.Length - 1 ? duration : 0, velocity, i > 0));
        }
    }
}
