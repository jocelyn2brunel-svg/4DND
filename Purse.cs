using System;

namespace _4DND;

public class Purse
{
    public int CopperPieces { get; set; } = 0;
    public int SilverPieces { get; set; } = 0;
    public int ElectrumPieces { get; set; } = 0;
    public int GoldPieces { get; set; } = 0;
    public int PlatinumPieces { get; set; } = 0;
    public const int CpPerSp = 10;
    public const int CpPerEp = 50;
    public const int CpPerGp = 100;
    public const int CpPerPp = 1000;
    public int TotalValueInCopper => CopperPieces + SilverPieces * CpPerSp + ElectrumPieces * CpPerEp + GoldPieces * CpPerGp + PlatinumPieces * CpPerPp;
    public static Purse FromGold(int gp) => new Purse { GoldPieces = gp };
    public void Add(Purse other) { CopperPieces += other.CopperPieces; SilverPieces += other.SilverPieces; ElectrumPieces += other.ElectrumPieces; GoldPieces += other.GoldPieces; PlatinumPieces += other.PlatinumPieces; }
    public bool TrySpend(int copperAmount) { if (TotalValueInCopper < copperAmount) return false; int rem = copperAmount; int useCp = Math.Min(CopperPieces, rem); rem -= useCp; int useSp = Math.Min(SilverPieces, rem > 0 ? (rem + CpPerSp - 1) / CpPerSp : 0); rem -= useSp * CpPerSp; int useEp = Math.Min(ElectrumPieces, rem > 0 ? (rem + CpPerEp - 1) / CpPerEp : 0); rem -= useEp * CpPerEp; int useGp = Math.Min(GoldPieces, rem > 0 ? (rem + CpPerGp - 1) / CpPerGp : 0); rem -= useGp * CpPerGp; int usePp = rem > 0 ? (rem + CpPerPp - 1) / CpPerPp : 0; CopperPieces -= useCp; SilverPieces -= useSp; ElectrumPieces -= useEp; GoldPieces -= useGp; PlatinumPieces -= usePp; int change = useCp + useSp * CpPerSp + useEp * CpPerEp + useGp * CpPerGp + usePp * CpPerPp - copperAmount; CopperPieces += change; return true; }
}