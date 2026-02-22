using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace _4DND;

public class SaveManager
{
    private readonly string _savesDir = "saves";
    private readonly string _charsFile = "characters.json";
    private readonly string _campaignsFile = "campaigns.json";

    public SaveManager()
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir));
        }
        catch { }
    }

    public List<Character> LoadCharacters()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _charsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Character>>(json) ?? new List<Character>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to load characters: " + ex.Message);
        }
        return new List<Character>();
    }

    public void SaveCharacters(List<Character> characters)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _charsFile);
            var json = JsonSerializer.Serialize(characters);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to save characters: " + ex.Message);
        }
    }

    public List<Campaign> LoadCampaigns()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Campaign>>(json) ?? new List<Campaign>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to load campaigns: " + ex.Message);
        }
        return new List<Campaign>();
    }

    public void SaveCampaigns(List<Campaign> campaigns)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            var json = JsonSerializer.Serialize(campaigns);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to save campaigns: " + ex.Message);
        }
    }

    public void SaveCampaign(Campaign currentCampaign)
    {
        if (currentCampaign == null) return;
        var campaigns = LoadCampaigns();
        var existing = campaigns.FindIndex(c => c.Name == currentCampaign.Name);
        if (existing >= 0)
            campaigns[existing] = currentCampaign;
        else
            campaigns.Add(currentCampaign);
        SaveCampaigns(campaigns);
    }
}
