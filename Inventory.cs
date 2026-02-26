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

    public bool HasActiveLightSource()
    {
        return HasActiveLantern() || HasActiveTorch();
    }

    public bool HasActiveLantern()
    {
        return Items.Any(i => IsLantern(i.Name) && i.IsLit);
    }

    public bool HasActiveTorch()
    {
        return (EquippedWeapon?.Name == "Torch" && EquippedWeapon.IsLit)
            || (OffhandWeapon?.Name == "Torch" && OffhandWeapon.IsLit);
    }

    private static bool IsLantern(string itemName)
    {
        return itemName == "Lantern, hooded" || itemName == "Lantern, bullseye";
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
                // TWF: if the new main-hand weapon is not light, unequip the offhand (PHB "Two-Weapon Fighting")
                if (!itemData.IsLight && OffhandWeapon != null)
                    OffhandWeapon = null;
                // Two-Handed: requires both hands, so a shield cannot be used simultaneously (PHB "Two-Handed")
                if (itemData.IsTwoHanded && EquippedShield != null)
                    EquippedShield = null;
                EquippedWeapon = instance;
                return true;
            case ItemType.Armor:
                EquippedArmor = instance;
                return true;
            case ItemType.Shield:
                // A shield requires a free hand; cannot be equipped alongside a two-handed weapon (PHB "Two-Handed")
                if (EquippedWeapon != null && ItemDatabase.GetItem(EquippedWeapon.Name)?.IsTwoHanded == true)
                    return false;
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

        // TWF: the main-hand weapon must also be light (PHB "Two-Weapon Fighting")
        if (EquippedWeapon != null)
        {
            var mainHandData = ItemDatabase.GetItem(EquippedWeapon.Name);
            if (!mainHandData.IsLight) return false;
        }

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
            // Heavy armor: DEX modifier is never applied (neither positive nor negative)
            // Light/Medium armor: DEX modifier added, capped at MaxDexBonus
            if (armorData.ArmorCategory == ArmorType.Heavy)
                dexBonus = 0;
            else
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

    /// <summary>
    /// Returns the total number of ammunition units available for the given ammo-item key
    /// (e.g. "Ammunition - Arrows (20)"). Items loaded from old saves with Quantity == 0
    /// are initialised to their default pack size on first access.
    /// </summary>
    public int GetAmmoCount(string ammoType)
    {
        int total = 0;
        foreach (var instance in Items.Where(i => i.Name == ammoType))
        {
            if (instance.Quantity == 0)
            {
                var data = ItemDatabase.GetItem(ammoType);
                instance.Quantity = data.DefaultQuantity > 0 ? data.DefaultQuantity : 1;
            }
            total += instance.Quantity;
        }
        return total;
    }

    /// <summary>
    /// Returns true when the inventory contains at least one unit of the ammunition
    /// required by <paramref name="weapon"/>. Always true for weapons without the
    /// Ammunition property (PHB "Ammunition").
    /// </summary>
    public bool HasAmmoForWeapon(Item weapon)
    {
        if (!weapon.IsAmmunition || string.IsNullOrEmpty(weapon.AmmoType))
            return true;
        return GetAmmoCount(weapon.AmmoType) > 0;
    }

    /// <summary>
    /// Removes one unit of the specified ammunition from the inventory.
    /// The pack item is removed entirely when its quantity reaches zero.
    /// Returns false if no ammunition of that type was found.
    /// </summary>
    public bool ConsumeAmmo(string ammoType)
    {
        var pack = Items.FirstOrDefault(i => i.Name == ammoType && i.Quantity > 0);
        if (pack == null)
        {
            // Backward compat: old-save items where Quantity was not yet tracked
            pack = Items.FirstOrDefault(i => i.Name == ammoType);
            if (pack == null) return false;
            var data = ItemDatabase.GetItem(ammoType);
            pack.Quantity = data.DefaultQuantity > 0 ? data.DefaultQuantity : 1;
        }
        pack.Quantity--;
        if (pack.Quantity <= 0)
            Items.Remove(pack);
        return true;
    }

    /// <summary>
    /// Adds <paramref name="count"/> units of the given ammo type back to the inventory
    /// (used for post-battle recovery — PHB "Ammunition": recover half expended ammo).
    /// If a pack of that type already exists its quantity is increased; otherwise a new
    /// partial pack is created.
    /// </summary>
    public void RecoverAmmo(string ammoType, int count)
    {
        if (count <= 0) return;
        var existing = Items.FirstOrDefault(i => i.Name == ammoType);
        if (existing != null)
            existing.Quantity += count;
        else
            Items.Add(new ItemInstance(ammoType) { Quantity = count });
    }
}
