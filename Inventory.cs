using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public class Inventory
{
    public int Capacity { get; set; } = 50;
    
    public ItemInstance? EquippedWeapon { get; set; }
    public ItemInstance? OffhandWeapon { get; set; }
    public ItemInstance? EquippedArmor { get; set; }
    public ItemInstance? EquippedShield { get; set; }
    
    public List<ItemInstance> Items { get; set; } = new();
    
    public bool AddItem(string itemName)
    {
        if (Items.Count >= Capacity) return false;
        Items.Add(new ItemInstance(itemName));
        return true;
    }

    public bool AddItemInstance(ItemInstance instance)
    {
        if (Items.Count >= Capacity) return false;
        Items.Add(instance);
        return true;
    }
    
    public bool RemoveItem(string itemName)
    {
        var item = Items.FirstOrDefault(i => i.Name == itemName);
        if (item != null)
        {
            if (EquippedWeapon == item) EquippedWeapon = null;
            if (OffhandWeapon == item) OffhandWeapon = null;
            if (EquippedArmor == item) EquippedArmor = null;
            if (EquippedShield == item) EquippedShield = null;
            return Items.Remove(item);
        }
        return false;
    }

    public bool RemoveItemInstance(ItemInstance instance)
    {
        if (EquippedWeapon == instance) EquippedWeapon = null;
        if (OffhandWeapon == instance) OffhandWeapon = null;
        if (EquippedArmor == instance) EquippedArmor = null;
        if (EquippedShield == instance) EquippedShield = null;
        return Items.Remove(instance);
    }
    
    public bool HasItem(string itemName)
    {
        return Items.Any(i => i.Name == itemName);
    }
    
    public int GetItemCount(string itemName)
    {
        return Items.Count(i => i.Name == itemName);
    }
    
    public bool EquipItem(string itemName)
    {
        var itemInstance = Items.FirstOrDefault(i => i.Name == itemName);
        if (itemInstance == null) return false;
        return EquipItemInstance(itemInstance);
    }

    public bool EquipItemInstance(ItemInstance instance)
    {
        if (!Items.Contains(instance)) return false;
        
        var itemData = ItemDatabase.GetItem(instance.Name);
        if (!itemData.IsEquippable) return false;
        
        // Ensure not equipped in the other hand
        if (OffhandWeapon == instance) OffhandWeapon = null;

        switch (itemData.Type)
        {
            case ItemType.Weapon:
                EquippedWeapon = instance;
                return true;
            case ItemType.Armor:
                EquippedArmor = instance;
                return true;
            case ItemType.Shield:
                EquippedShield = instance;
                return true;
            default:
                return false;
        }
    }
    
    public bool EquipOffhandItem(string itemName)
    {
        var itemInstance = Items.FirstOrDefault(i => i.Name == itemName);
        if (itemInstance == null) return false;
        return EquipOffhandItemInstance(itemInstance);
    }

    public bool EquipOffhandItemInstance(ItemInstance instance)
    {
        if (!Items.Contains(instance)) return false;
        var itemData = ItemDatabase.GetItem(instance.Name);
        if (itemData.Type != ItemType.Weapon || !itemData.IsLight) return false;

        // Ensure not equipped in the other hand
        if (EquippedWeapon == instance) EquippedWeapon = null;

        OffhandWeapon = instance;
        return true;
    }

    public bool UnequipItem(string itemName)
    {
        if (EquippedWeapon?.Name == itemName)
        {
            EquippedWeapon = null;
            return true;
        }
        if (OffhandWeapon?.Name == itemName)
        {
            OffhandWeapon = null;
            return true;
        }
        if (EquippedArmor?.Name == itemName)
        {
            EquippedArmor = null;
            return true;
        }
        if (EquippedShield?.Name == itemName)
        {
            EquippedShield = null;
            return true;
        }
        return false;
    }

    public bool UnequipItemInstance(ItemInstance instance)
    {
        if (EquippedWeapon == instance)
        {
            EquippedWeapon = null;
            return true;
        }
        if (OffhandWeapon == instance)
        {
            OffhandWeapon = null;
            return true;
        }
        if (EquippedArmor == instance)
        {
            EquippedArmor = null;
            return true;
        }
        if (EquippedShield == instance)
        {
            EquippedShield = null;
            return true;
        }
        return false;
    }
    
    public int GetTotalWeight()
    {
        int total = 0;
        foreach (var itemInstance in Items)
        {
            var itemData = ItemDatabase.GetItem(itemInstance.Name);
            total += itemData.Weight;
        }
        return total;
    }
    
    public int CalculateArmorClass(int baseDexModifier)
    {
        int ac = 10;
        int dexBonus = baseDexModifier;
        
        // Equipped armor
        if (EquippedArmor != null)
        {
            var armorData = ItemDatabase.GetItem(EquippedArmor.Name);
            ac = armorData.ArmorClass;
            dexBonus = System.Math.Min(dexBonus, armorData.MaxDexBonus);
        }
        
        ac += dexBonus;
        
        // Equipped shield
        if (EquippedShield != null)
        {
            var shieldData = ItemDatabase.GetItem(EquippedShield.Name);
            ac += shieldData.ArmorClass;
        }
        
        return ac;
    }
}
