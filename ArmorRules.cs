using System;

namespace _4DND;

public static class ArmorRules
{
    public static (int DonMinutes, int DoffMinutes) GetArmorTimes(ArmorType category)
    {
        return category switch
        {
            ArmorType.Light => (1, 1),
            ArmorType.Medium => (5, 1),
            ArmorType.Heavy => (10, 5),
            _ => (0, 0)
        };
    }

    public static bool IsShield(ArmorType category)
    {
        return category == ArmorType.Shield;
    }

    public static double GetDonDoffTimeMinutes(ArmorType category, bool isDoffing, bool hasHelp)
    {
        if (category == ArmorType.Shield) return 0.1; // 1 action = 6 seconds

        var (don, doff) = GetArmorTimes(category);
        double time = isDoffing ? doff : don;

        if (hasHelp)
        {
            return time / 2.0;
        }

        return time;
    }

    public static int GetDonDoffRounds(ArmorType category, bool isDoffing, bool hasHelp)
    {
        if (category == ArmorType.Shield) return 1;

        double minutes = GetDonDoffTimeMinutes(category, isDoffing, hasHelp);
        return (int)Math.Ceiling(minutes * 10); // 1 minute = 10 rounds
    }
}
