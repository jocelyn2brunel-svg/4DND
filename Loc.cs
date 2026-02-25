using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace _4DND;

public static class Loc
{
    public enum Language { English, French }
    public static Language CurrentLanguage { get; private set; } = Language.English;

    private static Dictionary<string, string> _en = new();
    private static Dictionary<string, string> _fr = new();

    static Loc()
    {
        InitializeStrings();
        LoadSettings();
    }

    public static void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        SaveSettings();
    }

    public static string Tr(string key)
    {
        var dict = CurrentLanguage == Language.French ? _fr : _en;
        if (dict.TryGetValue(key, out var val)) return val;
        return key;
    }

    public static string Tr(string key, params object[] args)
    {
        string translated = Tr(key);
        try
        {
            return string.Format(translated, args);
        }
        catch
        {
            return translated;
        }
    }

    private static void InitializeStrings()
    {
        // Main Menu & General
        _en["Single Player"] = "Single Player"; _fr["Single Player"] = "Solo";
        _en["Multiplayer"] = "Multiplayer"; _fr["Multiplayer"] = "Multijoueur";
        _en["Options"] = "Options"; _fr["Options"] = "Options";
        _en["Desktop"] = "Desktop"; _fr["Desktop"] = "Bureau";
        _en["Language"] = "Language"; _fr["Language"] = "Langue";
        _en["English"] = "English"; _fr["English"] = "Anglais";
        _en["French"] = "French"; _fr["French"] = "Français";
        _en["Back"] = "Back"; _fr["Back"] = "Retour";
        _en["Continue"] = "Continue"; _fr["Continue"] = "Continuer";
        _en["Main Menu"] = "Main Menu"; _fr["Main Menu"] = "Menu Principal";

        // Character Selection
        _en["Choose a Character (Single Player)"] = "Choose a Character (Single Player)"; _fr["Choose a Character (Single Player)"] = "Choisir un personnage (Solo)";
        _en["Choose a Character (Multiplayer)"] = "Choose a Character (Multiplayer)"; _fr["Choose a Character (Multiplayer)"] = "Choisir un personnage (Multijoueur)";
        _en["Create New Character"] = "Create New Character"; _fr["Create New Character"] = "Créer un personnage";
        _en["Delete"] = "Delete"; _fr["Delete"] = "Supprimer";
        _en["Cancel"] = "Cancel"; _fr["Cancel"] = "Annuler";
        _en["Confirm deletion"] = "Confirm deletion"; _fr["Confirm deletion"] = "Confirmer la suppression";
        _en["This action cannot be undone."] = "This action cannot be undone."; _fr["This action cannot be undone."] = "Action irréversible.";
        _en["Delete {0} '{1}'?"] = "Delete {0} '{1}'?"; _fr["Delete {0} '{1}'?"] = "Supprimer {0} '{1}' ?";
        _en["character"] = "character"; _fr["character"] = "le personnage";
        _en["campaign"] = "campaign"; _fr["campaign"] = "la campagne";
        _en["Preview"] = "Preview"; _fr["Preview"] = "Aperçu";
        _en["Select an existing character to see their quick profile."] = "Select an existing character to see their quick profile."; _fr["Select an existing character to see their quick profile."] = "Sélectionne un personnage pour voir son profil.";
        _en["Select a Campaign"] = "Select a Campaign"; _fr["Select a Campaign"] = "Choisir une Campagne";
        _en["Create New Campaign"] = "Create New Campaign"; _fr["Create New Campaign"] = "Créer une nouvelle Campagne";
        _en["Adventure Summary"] = "Adventure Summary"; _fr["Adventure Summary"] = "Résumé de l'Aventure";

        // Combat UI & Log
        _en["Combat started!"] = "Combat started!"; _fr["Combat started!"] = "Le combat commence !";
        _en["Round {0} begins!"] = "Round {0} begins!"; _fr["Round {0} begins!"] = "Le round {0} commence !";
        _en["Gained {0} XP!"] = "Gained {0} XP!"; _fr["Gained {0} XP!"] = "{0} XP gagnés !";
        _en["Level up! Now level {0}!"] = "Level up! Now level {0}!"; _fr["Level up! Now level {0}!"] = "Niveau supérieur ! Vous êtes niveau {0} !";
        _en["=== ROUND {0} ==="] = "=== ROUND {0} ==="; _fr["=== ROUND {0} ==="] = "=== ROUND {0} ===";
        _en["=== EXPLORATION ==="] = "=== EXPLORATION ==="; _fr["=== EXPLORATION ==="] = "=== EXPLORATION ===";
        _en["Turn:"] = "Turn:"; _fr["Turn:"] = "Tour :";
        _en["Active:"] = "Active:"; _fr["Active:"] = "Actif :";
        _en["HP: {0}/{1}"] = "HP: {0}/{1}"; _fr["HP: {0}/{1}"] = "PV : {0}/{1}";
        _en["Action:"] = "Action:"; _fr["Action:"] = "Action :";
        _en["Bonus:"] = "Bonus:"; _fr["Bonus:"] = "Bonus :";
        _en["Reaction:"] = "Reaction:"; _fr["Reaction:"] = "Réaction :";
        _en["Move:"] = "Move:"; _fr["Move:"] = "Mouv. :";
        _en["Ready"] = "Ready"; _fr["Ready"] = "Prêt";
        _en["Used"] = "Used"; _fr["Used"] = "Utilisé";
        _en["[HIDDEN]"] = "[HIDDEN]"; _fr["[HIDDEN]"] = "[CACHÉ]";
        _en["End Turn"] = "End Turn"; _fr["End Turn"] = "Fin de tour";
        _en["Attack"] = "Attack"; _fr["Attack"] = "Attaque";
        _en["Cast Spell"] = "Cast Spell"; _fr["Cast Spell"] = "Lancer Sort";
        _en["Bonus Action"] = "Bonus Action"; _fr["Bonus Action"] = "Action Bonus";
        _en["Dash?"] = "Dash?"; _fr["Dash?"] = "Foncer ?";
        _en["Dash (Action)"] = "Dash (Action)"; _fr["Dash (Action)"] = "Foncer (Action)";
        _en["Disengage"] = "Disengage"; _fr["Disengage"] = "Se désengager";
        _en["Dodge"] = "Dodge"; _fr["Dodge"] = "Esquiver";
        _en["Hide"] = "Hide"; _fr["Hide"] = "Se cacher";
        _en["Help"] = "Help"; _fr["Help"] = "Aider";
        _en["Grapple"] = "Grapple"; _fr["Grapple"] = "Lutte";

        // Tooltips
        _en["Action: Dash"] = "Action: Dash"; _fr["Action: Dash"] = "Action : Foncer";
        _en["Action: Disengage"] = "Action: Disengage"; _fr["Action: Disengage"] = "Action : Se désengager";
        _en["Action: Dodge"] = "Action: Dodge"; _fr["Action: Dodge"] = "Action : Esquiver";
        _en["Action: Help"] = "Action: Help"; _fr["Action: Help"] = "Action : Aider";
        _en["Action: Grapple"] = "Action: Grapple"; _fr["Action: Grapple"] = "Action : Lutte";
        _en["RAGE!"] = "RAGE!"; _fr["RAGE!"] = "RAGE !";
        _en["Hidden!"] = "Hidden!"; _fr["Hidden!"] = "Caché !";
        _en["{0} critically missed {1}!{2}"] = "{0} critically missed {1}!{2}"; _fr["{0} critically missed {1}!{2}"] = "Échec critique ! {0} a raté {1} !{2}";
        _en["{0} critically hit {1} for {2} damage!{3}"] = "{0} critically hit {1} for {2} damage!{3}"; _fr["{0} critically hit {1} for {2} damage!{3}"] = "Coup critique ! {0} a frappé {1} pour {2} dégâts !{3}";
        _en["{0} hit {1} for {2} damage! (AC {3}, rolled {4}+{5}={6}){7}"] = "{0} hit {1} for {2} damage! (AC {3}, rolled {4}+{5}={6}){7}"; _fr["{0} hit {1} for {2} damage! (AC {3}, rolled {4}+{5}={6}){7}"] = "{0} a frappé {1} pour {2} dégâts ! (CA {3}, jet {4}+{5}={6}){7}";
        _en["{0} missed {1}! (AC {2}, rolled {3}+{4}={5}){6}"] = "{0} missed {1}! (AC {2}, rolled {3}+{4}={5}){6}"; _fr["{0} missed {1}! (AC {2}, rolled {3}+{4}={5}){6}"] = "{0} a raté {1} ! (CA {2}, jet {3}+{4}={5}){6}";
        _en["{0} uses DASH via Move button."] = "{0} uses DASH via Move button."; _fr["{0} uses DASH via Move button."] = "{0} FONCE (via bouton Mouv.).";
        _en["{0} uses DASH."] = "{0} uses DASH."; _fr["{0} uses DASH."] = "{0} FONCE.";
        _en["Grapple: {0} {1}{2}={3} vs {4} {5}{6}={7}"] = "Grapple: {0} {1}{2}={3} vs {4} {5}{6}={7}"; _fr["Grapple: {0} {1}{2}={3} vs {4} {5}{6}={7}"] = "Lutte : {0} {1}{2}={3} vs {4} {5}{6}={7}";
        _en["{0} is GRAPPLED! (Speed=0)"] = "{0} is GRAPPLED! (Speed=0)"; _fr["{0} is GRAPPLED! (Speed=0)"] = "{0} est AGGRIPPÉ ! (Vitesse=0)";
        _en["{0} resists the grapple!"] = "{0} resists the grapple!"; _fr["{0} resists the grapple!"] = "{0} résiste à la lutte !";
        _en["{0} uses DASH via Move button."] = "{0} uses DASH via Move button."; _fr["{0} uses DASH via Move button."] = "{0} FONCE (via bouton Mouv.).";
        _en["{0} uses DASH."] = "{0} uses DASH."; _fr["{0} uses DASH."] = "{0} FONCE.";
        _en["Missed!"] = "Missed!"; _fr["Missed!"] = "Raté !";
        _en["Resists!"] = "Resists!"; _fr["Resists!"] = "Résiste !";
        _en["Grappled!"] = "Grappled!"; _fr["Grappled!"] = "Agrippé !";

        // Stats & Abilities
        _en["Strength"] = "Strength"; _fr["Strength"] = "Force";
        _en["Dexterity"] = "Dexterity"; _fr["Dexterity"] = "Dextérité";
        _en["Constitution"] = "Constitution"; _fr["Constitution"] = "Constitution";
        _en["Intelligence"] = "Intelligence"; _fr["Intelligence"] = "Intelligence";
        _en["Wisdom"] = "Wisdom"; _fr["Wisdom"] = "Sagesse";
        _en["Charisma"] = "Charisma"; _fr["Charisma"] = "Charisme";

        _en["STR"] = "STR"; _fr["STR"] = "FOR";
        _en["DEX"] = "DEX"; _fr["DEX"] = "DEX";
        _en["CON"] = "CON"; _fr["CON"] = "CON";
        _en["INT"] = "INT"; _fr["INT"] = "INT";
        _en["WIS"] = "WIS"; _fr["WIS"] = "SAG";
        _en["CHA"] = "CHA"; _fr["CHA"] = "CHA";

        // Classes
        _en["Barbarian"] = "Barbarian"; _fr["Barbarian"] = "Barbare";
        _en["Bard"] = "Bard"; _fr["Bard"] = "Barde";
        _en["Cleric"] = "Cleric"; _fr["Cleric"] = "Clerc";
        _en["Druid"] = "Druid"; _fr["Druid"] = "Druide";
        _en["Fighter"] = "Fighter"; _fr["Fighter"] = "Guerrier";
        _en["Monk"] = "Monk"; _fr["Monk"] = "Moine";
        _en["Paladin"] = "Paladin"; _fr["Paladin"] = "Paladin";
        _en["Ranger"] = "Ranger"; _fr["Ranger"] = "Rôdeur";
        _en["Rogue"] = "Rogue"; _fr["Rogue"] = "Roublard";
        _en["Sorcerer"] = "Sorcerer"; _fr["Sorcerer"] = "Ensorceleur";
        _en["Warlock"] = "Warlock"; _fr["Warlock"] = "Occultiste";
        _en["Wizard"] = "Wizard"; _fr["Wizard"] = "Magicien";

        _en["Human"] = "Human"; _fr["Human"] = "Humain";
        _en["Elf (High)"] = "Elf (High)"; _fr["Elf (High)"] = "Haut-elfe";
        _en["Elf (Wood)"] = "Elf (Wood)"; _fr["Elf (Wood)"] = "Elfe des bois";
        _en["Elf (Drow)"] = "Elf (Drow)"; _fr["Elf (Drow)"] = "Elfe noir (Drow)";
        _en["Dwarf (Hill)"] = "Dwarf (Hill)"; _fr["Dwarf (Hill)"] = "Nain des collines";
        _en["Dwarf (Mountain)"] = "Dwarf (Mountain)"; _fr["Dwarf (Mountain)"] = "Nain des montagnes";
        _en["Halfling (Lightfoot)"] = "Halfling (Lightfoot)"; _fr["Halfling (Lightfoot)"] = "Halfelin pied-léger";
        _en["Halfling (Stout)"] = "Halfling (Stout)"; _fr["Halfling (Stout)"] = "Halfelin robuste";
        _en["Half-Orc"] = "Half-Orc"; _fr["Half-Orc"] = "Demi-orc";
        _en["Tiefling"] = "Tiefling"; _fr["Tiefling"] = "Tieffelin";
        _en["Dragonborn"] = "Dragonborn"; _fr["Dragonborn"] = "Drakéide";
        _en["Gnome"] = "Gnome"; _fr["Gnome"] = "Gnome";
        _en["Half-Elf"] = "Half-Elf"; _fr["Half-Elf"] = "Demi-elfe";

        _en["A fierce warrior of primitive background who can enter a battle rage"] = "A fierce warrior of primitive background who can enter a battle rage";
        _fr["A fierce warrior of primitive background who can enter a battle rage"] = "Un guerrier féroce d'origine primitive capable d'entrer dans une rage de combat";
        _en["An inspiring magician whose power echoes the music of creation"] = "An inspiring magician whose power echoes the music of creation";
        _fr["An inspiring magician whose power echoes the music of creation"] = "Un magicien inspirant dont la puissance fait écho à la musique de la création";
        _en["A priestly champion who wields divine magic in service of a higher power"] = "A priestly champion who wields divine magic in service of a higher power";
        _fr["A priestly champion who wields divine magic in service of a higher power"] = "Un champion sacerdotal qui manie la magie divine au service d'une puissance supérieure";
        _en["A priest of the Old Faith, wielding the powers of nature - moonlight and plant growth, fire and lightning - and adopting animal forms"] = "A priest of the Old Faith, wielding the powers of nature - moonlight and plant growth, fire and lightning - and adopting animal forms";
        _fr["A priest of the Old Faith, wielding the powers of nature - moonlight and plant growth, fire and lightning - and adopting animal forms"] = "Un prêtre de l'Ancienne Foi, maniant les pouvoirs de la nature et adoptant des formes animales";
        _en["A master of martial combat, skilled with a variety of weapons and armor"] = "A master of martial combat, skilled with a variety of weapons and armor";
        _fr["A master of martial combat, skilled with a variety of weapons and armor"] = "Un maître du combat martial, expert dans une grande variété d'armes et d'armures";
        _en["A master of martial arts, harnessing the power of the body in pursuit of physical and spiritual perfection"] = "A master of martial arts, harnessing the power of the body in pursuit of physical and spiritual perfection";
        _fr["A master of martial arts, harnessing the power of the body in pursuit of physical and spiritual perfection"] = "Un maître des arts martiaux, exploitant le pouvoir du corps en quête de perfection physique et spirituelle";
        _en["A holy warrior bound to a sacred oath"] = "A holy warrior bound to a sacred oath";
        _fr["A holy warrior bound to a sacred oath"] = "Un guerrier saint lié par un serment sacré";
        _en["A warrior who uses martial prowess and nature magic to combat threats on the edges of civilization"] = "A warrior who uses martial prowess and nature magic to combat threats on the edges of civilization";
        _fr["A warrior who uses martial prowess and nature magic to combat threats on the edges of civilization"] = "Un guerrier qui utilise ses prouesses martiales et la magie de la nature pour combattre les menaces aux frontières de la civilisation";
        _en["A scoundrel who uses stealth and trickery to overcome obstacles and enemies"] = "A scoundrel who uses stealth and trickery to overcome obstacles and enemies";
        _fr["A scoundrel who uses stealth and trickery to overcome obstacles and enemies"] = "Un scélérat qui utilise la furtivité et la ruse pour surmonter les obstacles et les ennemis";
        _en["A spellcaster who draws on inherent magic from a gift or bloodline"] = "A spellcaster who draws on inherent magic from a gift or bloodline";
        _fr["A spellcaster who draws on inherent magic from a gift or bloodline"] = "Un lanceur de sorts qui puise dans une magie innée issue d'un don ou d'une lignée";
        _en["A wielder of magic that is derived from a bargain with an extraplanar entity"] = "A wielder of magic that is derived from a bargain with an extraplanar entity";
        _fr["A wielder of magic that is derived from a bargain with an extraplanar entity"] = "Un utilisateur de magie issue d'un pacte avec une entité extraplanaire";
        _en["A scholarly magic-user capable of manipulating the structures of reality"] = "A scholarly magic-user capable of manipulating the structures of reality";
        _fr["A scholarly magic-user capable of manipulating the structures of reality"] = "Un utilisateur de magie érudit capable de manipuler les structures de la réalité";

        _en["Versatile and adaptable (+1 to all abilities)"] = "Versatile and adaptable (+1 to all abilities)";
        _fr["Versatile and adaptable (+1 to all abilities)"] = "Polyvalent et adaptable (+1 à toutes les caractéristiques)";
        _en["Graceful and intelligent (+2 DEX, +1 INT, Darkvision 60 ft)"] = "Graceful and intelligent (+2 DEX, +1 INT, Darkvision 60 ft)";
        _fr["Graceful and intelligent (+2 DEX, +1 INT, Darkvision 60 ft)"] = "Gracieux et intelligent (+2 DEX, +1 INT, Vision dans le noir 18m)";
        _en["Swift and wise (+2 DEX, +1 WIS, 35 ft speed, Darkvision 60 ft)"] = "Swift and wise (+2 DEX, +1 WIS, 35 ft speed, Darkvision 60 ft)";
        _fr["Swift and wise (+2 DEX, +1 WIS, 35 ft speed, Darkvision 60 ft)"] = "Rapide et sage (+2 DEX, +1 SAG, vitesse 10.5m, Vision dans le noir 18m)";
        _en["Dark elf with superior darkvision (+2 DEX, +1 CHA, Darkvision 120 ft, Sunlight Sensitivity)"] = "Dark elf with superior darkvision (+2 DEX, +1 CHA, Darkvision 120 ft, Sunlight Sensitivity)";
        _fr["Dark elf with superior darkvision (+2 DEX, +1 CHA, Darkvision 120 ft, Sunlight Sensitivity)"] = "Elfe noir avec vision supérieure (+2 DEX, +1 CHA, Vision dans le noir 36m, Sensibilité au soleil)";
        _en["Tough and wise (+2 CON, +1 WIS, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)"] = "Tough and wise (+2 CON, +1 WIS, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)";
        _fr["Tough and wise (+2 CON, +1 WIS, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)"] = "Robuste et sage (+2 CON, +1 SAG, Vision dans le noir 18m, Résilience naine, Entraînement au combat nain)";
        _en["Strong and hardy (+2 STR, +2 CON, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)"] = "Strong and hardy (+2 STR, +2 CON, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)";
        _fr["Strong and hardy (+2 STR, +2 CON, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)"] = "Fort et vigoureux (+2 FOR, +2 CON, Vision dans le noir 18m, Résilience naine, Entraînement au combat nain)";
        _en["Nimble and charming (+2 DEX, +1 CHA)"] = "Nimble and charming (+2 DEX, +1 CHA)";
        _fr["Nimble and charming (+2 DEX, +1 CHA)"] = "Agile et charmant (+2 DEX, +1 CHA)";
        _en["Nimble and resilient (+2 DEX, +1 CON)"] = "Nimble and resilient (+2 DEX, +1 CON)";
        _fr["Nimble and resilient (+2 DEX, +1 CON)"] = "Agile et résistant (+2 DEX, +1 CON)";
        _en["Strong and tough (+2 STR, +1 CON, Darkvision 60 ft)"] = "Strong and tough (+2 STR, +1 CON, Darkvision 60 ft)";
        _fr["Strong and tough (+2 STR, +1 CON, Darkvision 60 ft)"] = "Fort et robuste (+2 FOR, +1 CON, Vision dans le noir 18m)";
        _en["Infernal heritage with darkvision (+2 CHA, +1 INT, Darkvision 60 ft)"] = "Infernal heritage with darkvision (+2 CHA, +1 INT, Darkvision 60 ft)";
        _fr["Infernal heritage with darkvision (+2 CHA, +1 INT, Darkvision 60 ft)"] = "Héritage infernal (+2 CHA, +1 INT, Vision dans le noir 18m)";
        _en["Draconic heritage (+2 STR, +1 CHA)"] = "Draconic heritage (+2 STR, +1 CHA)";
        _fr["Draconic heritage (+2 STR, +1 CHA)"] = "Héritage draconique (+2 FOR, +1 CHA)";
        _en["Small and clever (+2 INT, Darkvision 60 ft)"] = "Small and clever (+2 INT, Darkvision 60 ft)";
        _fr["Small and clever (+2 INT, Darkvision 60 ft)"] = "Petit et rusé (+2 INT, Vision dans le noir 18m)";
        _en["Versatile and charismatic (+2 CHA, +1 to two other abilities, Darkvision 60 ft)"] = "Versatile and charismatic (+2 CHA, +1 to two other abilities, Darkvision 60 ft)";
        _fr["Versatile and charismatic (+2 CHA, +1 to two other abilities, Darkvision 60 ft)"] = "Polyvalent et charismatique (+2 CHA, +1 à deux autres caractéristiques, Vision dans le noir 18m)";

        // Buttons
        _en["Inventaire [C]"] = "Inventory [C]"; _fr["Inventaire [C]"] = "Inventaire [C]";
        _en["Ouvrir map [M]"] = "Open Map [M]"; _fr["Ouvrir map [M]"] = "Ouvrir map [M]";
        _en["Fermer map [M]"] = "Close Map [M]"; _fr["Fermer map [M]"] = "Fermer map [M]";
    }

    private static void LoadSettings()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "saves", "settings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (settings != null && settings.TryGetValue("Language", out var lang))
                {
                    if (Enum.TryParse<Language>(lang, out var l))
                        CurrentLanguage = l;
                }
            }
        }
        catch { }
    }

    private static void SaveSettings()
    {
        try
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "saves");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "settings.json");
            var settings = new Dictionary<string, string> { ["Language"] = CurrentLanguage.ToString() };
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
