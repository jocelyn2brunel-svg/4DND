using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace _4DND;

public partial class Game1
{
    private void LoadCharacters()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _charsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _characters = JsonSerializer.Deserialize<List<Character>>(json) ?? new List<Character>();
                MigrateCharacterData(_characters);
            }
            else
            {
                _characters = new List<Character>();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to load characters: " + ex.Message);
            _characters = new List<Character>();
        }
    }

    private static void MigrateCharacterData(List<Character> characters)
    {
        foreach (var character in characters)
        {
            // Re-link equipped items to the instances in the character's inventory list.
            // This is necessary because JSON deserialization creates separate objects for
            // EquippedWeapon/OffhandWeapon and the same items in the Items list.
            var inv = character.InventoryData;
            if (inv.EquippedWeapon != null)
                inv.EquippedWeapon = inv.Items.FirstOrDefault(i => i.Name == inv.EquippedWeapon.Name && i.IsLit == inv.EquippedWeapon.IsLit && i.RemainingMinutes == inv.EquippedWeapon.RemainingMinutes) ?? inv.EquippedWeapon;
            if (inv.OffhandWeapon != null)
                inv.OffhandWeapon = inv.Items.FirstOrDefault(i => i.Name == inv.OffhandWeapon.Name && i.IsLit == inv.OffhandWeapon.IsLit && i.RemainingMinutes == inv.OffhandWeapon.RemainingMinutes) ?? inv.OffhandWeapon;
            if (inv.EquippedArmor != null)
                inv.EquippedArmor = inv.Items.FirstOrDefault(i => i.Name == inv.EquippedArmor.Name) ?? inv.EquippedArmor;
            if (inv.EquippedShield != null)
                inv.EquippedShield = inv.Items.FirstOrDefault(i => i.Name == inv.EquippedShield.Name) ?? inv.EquippedShield;

            // Migration for older saves created before barbarian rages were persisted/initialized correctly.
            // Restrict to fresh level-1-style saves to avoid refilling legitimately spent resources.
            if (character.Class == "Barbarian" && character.XP == 0 && character.RagesRemaining == 0)
            {
                var barbarianData = ClassData.GetClass("Barbarian");
                var levelData = barbarianData?.GetLevelData(character.Level);
                if (levelData != null && levelData.Rages > 0)
                {
                    character.RagesRemaining = levelData.Rages;
                }
            }
        }
    }
    
    private void LoadCampaigns()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _campaigns = JsonSerializer.Deserialize<List<Campaign>>(json) ?? new List<Campaign>();
            }
            else
            {
                _campaigns = new List<Campaign>();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to load campaigns: " + ex.Message);
            _campaigns = new List<Campaign>();
        }
    }

    private void SaveCharacters()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _charsFile);
            var json = JsonSerializer.Serialize(_characters);
            File.WriteAllText(path, json);

            // Keep campaign/map exploration in sync when character updates trigger a save.
            if (_state == AppState.Playing && _currentCampaign != null)
            {
                SaveCampaign();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to save characters: " + ex.Message);
        }
    }
    
    private void SaveCampaigns()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            var json = JsonSerializer.Serialize(_campaigns);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to save campaigns: " + ex.Message);
        }
    }


    private void SaveCampaign()
    {
        try
        {
            if (_currentCampaign == null) return;

            SyncVisionExplorationToCampaign();
            SyncGroundItemsToCampaign();
            
            // Load existing campaigns
            LoadCampaigns();
            
            // Update or add current campaign
            var existing = _campaigns.FindIndex(c => c.Name == _currentCampaign.Name);
            if (existing >= 0)
                _campaigns[existing] = _currentCampaign;
            else
                _campaigns.Add(_currentCampaign);
            
            // Save
            SaveCampaigns();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to save campaign: " + ex.Message);
        }
    }
    
    private void LoadCampaign(string campaignName)
    {
        try
        {
            _groundItems.Clear();

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var campaigns = JsonSerializer.Deserialize<List<Campaign>>(json) ?? new List<Campaign>();
                _currentCampaign = campaigns.FirstOrDefault(c => c.Name == campaignName)!;
                RestoreVisionExplorationFromCampaign();
                RestoreGroundItemsFromCampaign();
                // RestoreTacticalStateFromCampaign(); // Will be called later after creatures are initialized
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to load campaign: " + ex.Message);
        }
    }

    private void SyncVisionExplorationToCampaign()
    {
        if (_currentCampaign == null)
            return;

        _currentCampaign.ExploredTiles = _visionSystem.GetExploredTilesSnapshot();
        _currentCampaign.ExploredHexes = _visionSystem.GetExploredHexesSnapshot();
    }

    private void SyncTacticalStateToCampaign()
    {
        if (_currentCampaign == null)
            return;

        // 1. Sync Party Tactical Positions
        _currentCampaign.PartyTacticalPositions.Clear();
        if (_playerCreature != null)
        {
            _currentCampaign.PartyTacticalPositions[_playerCreature.Name] = new TacticalPosition { X = _playerCreature.X, Y = _playerCreature.Y, Z = _playerCreature.Z };
        }
        _currentCampaign.CurrentViewLevel = _currentViewLevel;

        // 2. Sync Combat State
        if (_combatManager.InCombat)
        {
            _currentCampaign.CurrentCombat = new CombatSaveData
            {
                CurrentRound = _combatManager.CurrentRound,
                CurrentTurnIndex = _combatManager.CurrentTurnIndex,
                Combatants = _combatManager.Combatants.Select(c => new CreatureSaveData
                {
                    Name = c.Name,
                    Type = c.Type,
                    X = c.X,
                    Y = c.Y,
                    Z = c.Z,
                    CurrentHP = c.CurrentHP,
                    MaxHP = c.MaxHP,
                    Initiative = c.Initiative,
                    IsPlayer = c.IsPlayer
                }).ToList()
            };
        }
        else
        {
            _currentCampaign.CurrentCombat = null;
        }
    }

    private void RestoreTacticalStateFromCampaign()
    {
        if (_currentCampaign == null)
            return;

        // 1. Restore Party Tactical Positions
        if (_playerCreature != null && _currentCampaign.PartyTacticalPositions.TryGetValue(_playerCreature.Name, out var pos))
        {
            _playerCreature.TeleportTo(pos.X, pos.Y, pos.Z);
            _cameraTarget = new Vector3(pos.X, pos.Y, pos.Z);
        }
        _currentViewLevel = _currentCampaign.CurrentViewLevel;

        // 2. Restore Combat State
        if (_currentCampaign.CurrentCombat != null)
        {
            var saved = _currentCampaign.CurrentCombat;
            var combatants = new List<Creature>();

            foreach (var cData in saved.Combatants)
            {
                Creature c;
                if (cData.IsPlayer)
                {
                    // Find existing character to sync
                    var charData = _characters.FirstOrDefault(ch => ch.Name == cData.Name);
                    if (charData != null)
                    {
                        c = Creature.FromCharacter(charData, cData.X, cData.Y, cData.Z);
                        // Use saved tactical HP instead of persistent character HP if they differ
                        c.CurrentHP = cData.CurrentHP;
                        c.MaxHP = cData.MaxHP;
                        if (_playerCreature != null && _playerCreature.Name == c.Name)
                            _playerCreature = c;
                    }
                    else
                    {
                        // Fallback if character missing
                        c = Creature.Create(CreatureType.Player, cData.X, cData.Y, cData.Z);
                        c.Name = cData.Name;
                    }
                }
                else
                {
                    c = Creature.Create(cData.Type, cData.X, cData.Y, cData.Z);
                    c.Name = cData.Name;
                    c.CurrentHP = cData.CurrentHP;
                    c.MaxHP = cData.MaxHP;
                }

                c.Initiative = cData.Initiative;
                c.IsPlayer = cData.IsPlayer;
                combatants.Add(c);
            }

            // Directly inject state into CombatManager
            _combatManager.RestoreCombat(saved.CurrentRound, saved.CurrentTurnIndex, combatants);
            _showCombatUI = _hudAlwaysVisible;
            _selectedAction = CombatAction.Move;

            var active = _combatManager.CurrentCombatant;
            if (active != null)
                _cameraTarget = new Vector3(active.X, active.Y, active.Z);
        }
    }

    private void SyncGroundItemsToCampaign()
    {
        if (_currentCampaign == null)
            return;

        _currentCampaign.GroundItems = _groundItems
            .Select(gi => new GroundItemData
            {
                X = gi.X,
                Y = gi.Y,
                Z = gi.Z,
                Item = gi.Item
            })
            .ToList();
    }

    private void RestoreGroundItemsFromCampaign()
    {
        _groundItems.Clear();

        if (_currentCampaign?.GroundItems == null)
            return;

        foreach (var gi in _currentCampaign.GroundItems)
        {
            if (gi.Item == null)
                continue;

            _groundItems.Add(new GroundItem
            {
                X = gi.X,
                Y = gi.Y,
                Z = gi.Z,
                Item = gi.Item
            });
        }

        RefreshLightSources();
    }

    private void RestoreVisionExplorationFromCampaign()
    {
        if (_currentCampaign == null)
        {
            _visionSystem.ResetExploration();
            return;
        }

        _visionSystem.SetExploredState(_currentCampaign.ExploredTiles, _currentCampaign.ExploredHexes);
    }

    /// <summary>
    /// Saves the current campaign and characters to disk.
    /// </summary>
    private void SaveCurrentProgress()
    {
        if (_state == AppState.Playing && _currentCampaign != null)
        {
            // Update character data from current creature state (HP, etc.)
            if (_playerCreature != null && _currentCharacter != null)
            {
                _playerCreature.UpdateCharacter(_currentCharacter);
            }

            // Tactical state
            SyncTacticalStateToCampaign();

            // Persistence
            SaveCampaign();
            SaveCharacters();

            _autoSaveTimer = 0; // Reset timer after any save
        }
    }

}
