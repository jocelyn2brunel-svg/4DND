using System.Collections.Generic;

namespace _4DND;

public static class LuteSongs
{
    public static List<LuteSong> AllSongs { get; } = new();

    static LuteSongs()
    {
        AllSongs.Add(CreateGreensleeves());
        AllSongs.Add(CreateScarboroughFair());
        AllSongs.Add(CreateCantiga100());
        AllSongs.Add(CreateDouceDameJolie());
        AllSongs.Add(CreateTrotto());
    }

    private static LuteSong CreateGreensleeves()
    {
        var s = new LuteSong("Greensleeves", 100f);
        // Simplified melody
        // Quarter = 1.0, Half = 2.0, Eighth = 0.5
        // A2, C3, D3, E3, F3, E3, D3, B2, G2, A2, B2, C3, A2, A2, G#2, A2, B2, G#2, E2
        int[] notes = { 45, 48, 50, 52, 53, 52, 50, 47, 43, 45, 47, 48, 45, 45, 44, 45, 47, 44, 40 };
        float[] durations = { 1f, 2f, 1f, 1.5f, 0.5f, 1f, 2f, 1f, 1.5f, 0.5f, 1f, 2f, 1f, 1.5f, 0.5f, 1f, 2f, 1f, 3f };

        for (int i = 0; i < notes.Length; i++)
            s.AddNote(notes[i] + 12, durations[i]); // Up an octave for lute feel

        return s;
    }

    private static LuteSong CreateScarboroughFair()
    {
        var s = new LuteSong("Scarborough Fair", 90f);
        // D2, D2, A2, A2, A2, E3, D3, C3, A2...
        int[] notes = { 50, 50, 57, 57, 57, 62, 60, 57, 55, 57, 53, 52, 50 };
        float[] durations = { 2f, 1f, 2f, 1f, 1f, 1f, 1f, 3f, 2f, 1f, 1f, 1f, 3f };

        for (int i = 0; i < notes.Length; i++)
            s.AddNote(notes[i], durations[i]);

        return s;
    }

    private static LuteSong CreateCantiga100()
    {
        var s = new LuteSong("Cantiga 100", 120f);
        // Santa Maria, strela do dia
        int[] notes = { 62, 62, 62, 64, 65, 64, 62, 60, 62, 64, 62, 62 };
        float[] durations = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f };

        for (int i = 0; i < notes.Length; i++)
            s.AddNote(notes[i], durations[i]);

        return s;
    }

    private static LuteSong CreateDouceDameJolie()
    {
        var s = new LuteSong("Douce Dame Jolie", 130f);
        int[] notes = { 62, 62, 65, 64, 62, 60, 62, 58, 60, 62, 62 };
        float[] durations = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f };

        for (int i = 0; i < notes.Length; i++)
            s.AddNote(notes[i], durations[i]);

        return s;
    }

    private static LuteSong CreateTrotto()
    {
        var s = new LuteSong("Trotto", 140f);
        int[] notes = { 62, 64, 65, 67, 65, 64, 62, 60, 62, 62 };
        float[] durations = { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 1f, 1f };

        for (int i = 0; i < notes.Length; i++)
            s.AddNote(notes[i], durations[i]);

        return s;
    }
}
