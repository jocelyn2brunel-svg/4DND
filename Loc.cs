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

        // Skills
        _en["Acrobatics"] = "Acrobatics"; _fr["Acrobatics"] = "Acrobaties";
        _en["Animal Handling"] = "Animal Handling"; _fr["Animal Handling"] = "Dressage";
        _en["Arcana"] = "Arcana"; _fr["Arcana"] = "Arcanes";
        _en["Athletics"] = "Athletics"; _fr["Athletics"] = "Athlétisme";
        _en["Deception"] = "Deception"; _fr["Deception"] = "Tromperie";
        _en["History"] = "History"; _fr["History"] = "Histoire";
        _en["Insight"] = "Insight"; _fr["Insight"] = "Intuition";
        _en["Intimidation"] = "Intimidation"; _fr["Intimidation"] = "Intimidation";
        _en["Investigation"] = "Investigation"; _fr["Investigation"] = "Investigation";
        _en["Medicine"] = "Medicine"; _fr["Medicine"] = "Médecine";
        _en["Nature"] = "Nature"; _fr["Nature"] = "Nature";
        _en["Perception"] = "Perception"; _fr["Perception"] = "Perception";
        _en["Performance"] = "Performance"; _fr["Performance"] = "Représentation";
        _en["Persuasion"] = "Persuasion"; _fr["Persuasion"] = "Persuasion";
        _en["Religion"] = "Religion"; _fr["Religion"] = "Religion";
        _en["Sleight of Hand"] = "Sleight of Hand"; _fr["Sleight of Hand"] = "Escamotage";
        _en["Stealth"] = "Stealth"; _fr["Stealth"] = "Discrétion";
        _en["Survival"] = "Survival"; _fr["Survival"] = "Survie";

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
        _en["Inventory [C]"] = "Inventory [C]"; _fr["Inventory [C]"] = "Inventaire [C]";
        _en["Open Map [M]"] = "Open Map [M]"; _fr["Open Map [M]"] = "Ouvrir map [M]";
        _en["Close Map [M]"] = "Close Map [M]"; _fr["Close Map [M]"] = "Fermer map [M]";

        // Character Sheet
        _en["Equip"] = "Equip"; _fr["Equip"] = "Équiper";
        _en["Equip (Offhand)"] = "Equip (Offhand)"; _fr["Equip (Offhand)"] = "Équiper (secondaire)";
        _en["Unequip"] = "Unequip"; _fr["Unequip"] = "Déséquiper";
        _en["Throw"] = "Throw"; _fr["Throw"] = "Lancer";
        _en["Drop"] = "Drop"; _fr["Drop"] = "Jeter";
        _en["Play"] = "Play"; _fr["Play"] = "Jouer";
        _en["Examine"] = "Examine"; _fr["Examine"] = "Examiner";
        _en["Press 'C' to close | Scroll wheel to scroll"] = "Press 'C' to close | Scroll wheel to scroll"; _fr["Press 'C' to close | Scroll wheel to scroll"] = "Appuyez sur 'C' pour fermer | Molette pour défiler";
        _en["Close the character sheet (shortcut: C)."] = "Close the character sheet (shortcut: C)."; _fr["Close the character sheet (shortcut: C)."] = "Fermer la feuille de personnage (raccourci: C).";
        _en["Exit"] = "Exit"; _fr["Exit"] = "Quitter";
        _en["Max HP"] = "Max HP"; _fr["Max HP"] = "PV Max";
        _en["CURRENT HIT POINTS"] = "CURRENT HIT POINTS"; _fr["CURRENT HIT POINTS"] = "POINTS DE VIE ACTUELS";
        _en["TEMPORARY HIT POINTS"] = "TEMPORARY HIT POINTS"; _fr["TEMPORARY HIT POINTS"] = "PV TEMPORAIRES";
        _en["Temporary HP: {0}. Absorbs damage before normal HP. Expires after a long rest."] = "Temporary HP: {0}. Absorbs damage before normal HP. Expires after a long rest."; _fr["Temporary HP: {0}. Absorbs damage before normal HP. Expires after a long rest."] = "PV temporaires: {0}. Absorbent les dégâts avant les PV normaux. Expirent après un repos long.";
        _en["Temporary HP: none. Granted by certain spells or abilities; absorbs damage before normal HP."] = "Temporary HP: none. Granted by certain spells or abilities; absorbs damage before normal HP."; _fr["Temporary HP: none. Granted by certain spells or abilities; absorbs damage before normal HP."] = "PV temporaires: aucun. Accordés par certains sorts ou capacités; absorbent les dégâts avant les PV normaux.";
        _en["HIT DICE"] = "HIT DICE"; _fr["HIT DICE"] = "DÉS DE VIE";
        _en["Hit Dice: {0} (d{1}). Used during short rests."] = "Hit Dice: {0} (d{1}). Used during short rests."; _fr["Hit Dice: {0} (d{1}). Used during short rests."] = "Dés de vie: {0} (d{1}). Utilisés pendant les repos courts.";
        _en["DEATH SAVES"] = "DEATH SAVES"; _fr["DEATH SAVES"] = "JETS DE MORT";
        _en["Successes"] = "Successes"; _fr["Successes"] = "Succès";
        _en["Failures"] = "Failures"; _fr["Failures"] = "Échecs";
        _en["Death saves: {0} successes, {1} failures."] = "Death saves: {0} successes, {1} failures."; _fr["Death saves: {0} successes, {1} failures."] = "Jets de mort: {0} succès, {1} échecs.";
        _en["Armor proficiency: {0}."] = "Armor proficiency: {0}."; _fr["Armor proficiency: {0}."] = "Maîtrise d'armure: {0}.";
        _en["Weapon proficiency: {0}."] = "Weapon proficiency: {0}."; _fr["Weapon proficiency: {0}."] = "Maîtrise d'arme: {0}.";
        _en["Class hit die: d{0}."] = "Class hit die: d{0}."; _fr["Class hit die: d{0}."] = "Dé de vie de classe: d{0}.";
        _en["Primary class ability: {0}."] = "Primary class ability: {0}."; _fr["Primary class ability: {0}."] = "Capacité principale de la classe: {0}.";
        _en["Unlimited"] = "Unlimited"; _fr["Unlimited"] = "Illimité";
        _en["Rages per day: {0}. Remaining: {1}."] = "Rages per day: {0}. Remaining: {1}."; _fr["Rages per day: {0}. Remaining: {1}."] = "Rages par jour: {0}. Restantes: {1}.";
        _en["Rage damage bonus: +{0}."] = "Rage damage bonus: +{0}."; _fr["Rage damage bonus: +{0}."] = "Bonus de dégâts en rage: +{0}.";
        _en["Level {0} features: {1}."] = "Level {0} features: {1}."; _fr["Level {0} features: {1}."] = "Capacités de niveau {0}: {1}.";
        _en["Bardic Inspiration: {0} remaining out of {1}. Die: d{2}."] = "Bardic Inspiration: {0} remaining out of {1}. Die: d{2}."; _fr["Bardic Inspiration: {0} remaining out of {1}. Die: d{2}."] = "Inspiration Bardique: {0} restante(s) sur {1}. Dé: d{2}.";
        _en["Level {0} spell slots: {1} remaining out of {2}."] = "Level {0} spell slots: {1} remaining out of {2}."; _fr["Level {0} spell slots: {1} remaining out of {2}."] = "Emplacements de sort de niveau {0}: {1} restant(s) sur {2}.";
        _en["Channel Divinity: {0} out of {1} uses."] = "Channel Divinity: {0} out of {1} uses."; _fr["Channel Divinity: {0} out of {1} uses."] = "Divinité canalisée: {0} sur {1} utilisations.";
        _en["{0}: score {1} ({2}). {3} saving throw."] = "{0}: score {1} ({2}). {3} saving throw."; _fr["{0}: score {1} ({2}). {3} saving throw."] = "{0}: score {1} ({2}). Jet de sauvegarde {3}.";
        _en["proficient"] = "proficient"; _fr["proficient"] = "maîtrisé";
        _en["not proficient"] = "not proficient"; _fr["not proficient"] = "non maîtrisé";
        _en["Armor class: {0}."] = "Armor class: {0}."; _fr["Armor class: {0}."] = "Classe d'armure: {0}.";
        _en["Initiative: {0}."] = "Initiative: {0}."; _fr["Initiative: {0}."] = "Initiative: {0}.";
        _en["Speed: {0} ft."] = "Speed: {0} ft."; _fr["Speed: {0} ft."] = "Vitesse: {0} ft.";
        _en["NAME"] = "NAME"; _fr["NAME"] = "NOM";
        _en["ATK BONUS"] = "ATK BONUS"; _fr["ATK BONUS"] = "BONUS ATK";
        _en["DAMAGE/TYPE"] = "DAMAGE/TYPE"; _fr["DAMAGE/TYPE"] = "DÉGÂTS/TYPE";
        _en["Unarmed Strike: bonus {0}, damage 1{1} bludgeoning, range 5 ft."] = "Unarmed Strike: bonus {0}, damage 1{1} bludgeoning, range 5 ft."; _fr["Unarmed Strike: bonus {0}, damage 1{1} bludgeoning, range 5 ft."] = "Frappe à mains nues: bonus {0}, dégâts 1{1} contondants, portée 5 ft.";
        _en["Grapple Description"] = "Grapple: Strength (Athletics) check {0} contested by target's Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nRequires a free hand. Success: target is grappled (speed 0).\n[Left click to use]"; _fr["Grapple Description"] = "Lutte: jet de Force (Athlétisme) {0} contré par Force (Athlétisme) ou Dextérité (Acrobaties) de la cible.\nCible à portée de mêlée, taille max: une catégorie de plus que vous.\nNécessite une main libre. Succès: cible agrippée (vitesse 0).\n[Clic gauche pour lancer]";
        _en["Shove Description"] = "Shove: Strength (Athletics) check {0} contested by target's Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nSuccess: target is knocked prone OR pushed 5 ft away.\n[Left click to use]"; _fr["Shove Description"] = "Bousculade: jet de Force (Athlétisme) {0} contré par Force (Athlétisme) ou Dextérité (Acrobaties) de la cible.\nCible à portée de mêlée, taille max: une catégorie de plus que vous.\nSuccès: cible renversée (à terre) OU repoussée de 5 ft.\n[Clic gauche pour lancer]";
        _en["Total Weight: {0} lbs"] = "Total Weight: {0} lbs"; _fr["Total Weight: {0} lbs"] = "Poids Total: {0} lbs";
        _en["Gold: {0} gp"] = "Gold: {0} gp"; _fr["Gold: {0} gp"] = "Or: {0} gp";
        _en["{0}: field to fill."] = "{0}: field to fill."; _fr["{0}: field to fill."] = "{0}: champ à renseigner.";
        _en["{0}: {1}."] = "{0}: {1}."; _fr["{0}: {1}."] = "{0}: {1}.";
        _en["Inspiration Note"] = "Inspiration: advantage on an important roll when granted by the DM."; _fr["Inspiration Note"] = "Inspiration: avantage sur un jet important quand le MJ l'accorde.";
        _en["Current proficiency bonus: {0}."] = "Current proficiency bonus: {0}."; _fr["Current proficiency bonus: {0}."] = "Bonus de maîtrise actuel: {0}.";
        _en["Passive Perception Note"] = "Passive Perception = 10 + Wis mod + proficiency if any = {0}."; _fr["Passive Perception Note"] = "Perception passive = 10 + mod. Sagesse + maîtrise éventuelle = {0}.";
        _en["{0}: no notes provided."] = "{0}: no notes provided."; _fr["{0}: no notes provided."] = "{0}: aucune note renseignée.";
        _en["Examine Weapon"] = "Examine Weapon"; _fr["Examine Weapon"] = "Examiner l'arme";
        _en["(Click outside to close)"] = "(Click outside to close)"; _fr["(Click outside to close)"] = "(Cliquez à l'extérieur pour fermer)";
        _en["Range: {0}/{1} ft."] = "Range: {0}/{1} ft."; _fr["Range: {0}/{1} ft."] = "Portée: {0}/{1} ft.";
        _en["Melee range: 5 ft."] = "Melee range: 5 ft."; _fr["Melee range: 5 ft."] = "Portée de mêlée: 5 ft.";
        _en["{0} (equipped)"] = "{0} (equipped)"; _fr["{0} (equipped)"] = "{0} (équipé)";
        _en["Weight: {0} lbs | Value: {1} gp"] = "Weight: {0} lbs | Value: {1} gp"; _fr["Weight: {0} lbs | Value: {1} gp"] = "Poids: {0} lbs | Valeur: {1} gp";
        _en["Two-Weapon Fighting (bonus action): {0}\nBonus: {1}\nDamage: {2} (modifier not added if positive)\n{3}"] = "Two-Weapon Fighting (bonus action): {0}\nBonus: {1}\nDamage: {2} (modifier not added if positive)\n{3}"; _fr["Two-Weapon Fighting (bonus action): {0}\nBonus: {1}\nDamage: {2} (modifier not added if positive)\n{3}"] = "Combat à deux armes (action bonus): {0}\nBonus: {1}\nDégâts: {2} (modificateur non ajouté si positif)\n{3}";

        // Journal
        _en["ADVENTURE JOURNAL: "] = "ADVENTURE JOURNAL: "; _fr["ADVENTURE JOURNAL: "] = "JOURNAL D'AVENTURE : ";
        _en["THE BEGINNING (HOOK)"] = "THE BEGINNING (HOOK)"; _fr["THE BEGINNING (HOOK)"] = "LE COMMENCEMENT (HOOK)";
        _en["THE DEVELOPMENT"] = "THE DEVELOPMENT"; _fr["THE DEVELOPMENT"] = "LE DÉROULEMENT (DEVELOPMENT)";
        _en["THE CONCLUSION (CLIMAX)"] = "THE CONCLUSION (CLIMAX)"; _fr["THE CONCLUSION (CLIMAX)"] = "LA CONCLUSION (CLIMAX)";
        _en["Press 'J' to close | Scroll wheel to scroll"] = "Press 'J' to close | Scroll wheel to scroll"; _fr["Press 'J' to close | Scroll wheel to scroll"] = "Appuyez sur 'J' pour fermer | Molette pour défiler";
        _en["Close"] = "Close"; _fr["Close"] = "Fermer";
        _en["No details available."] = "No details available."; _fr["No details available."] = "Aucun détail disponible.";

        // World Map
        _en["Night"] = "Night"; _fr["Night"] = "Nuit";
        _en["Twilight/Dawn"] = "Twilight/Dawn"; _fr["Twilight/Dawn"] = "Crépuscule/Aube";
        _en["Travel Here"] = "Travel Here"; _fr["Travel Here"] = "Voyager ici";
        _en["Cancel Travel"] = "Cancel Travel"; _fr["Cancel Travel"] = "Annuler le voyage";
        _en["Long Rest (8h)"] = "Long Rest (8h)"; _fr["Long Rest (8h)"] = "Repos Long (8h)";
        _en["The party took a long rest (8h)."] = "The party took a long rest (8h)."; _fr["The party took a long rest (8h)."] = "Le groupe a pris un repos long (8h).";
        _en["PARTY"] = "PARTY"; _fr["PARTY"] = "GROUPE";
        _en["Traveling... {0}"] = "Traveling... {0}"; _fr["Traveling... {0}"] = "Voyage en cours... {0}";
        _en["{0} is exhausted by the forced march (CON Save DC {1} failed)!"] = "{0} is exhausted by the forced march (CON Save DC {1} failed)!"; _fr["{0} is exhausted by the forced march (CON Save DC {1} failed)!"] = "{0} est epuise par la marche forcee (Jet CON DC {1} echoue) !";
        _en["{0} suffers from hunger!"] = "{0} suffers from hunger!"; _fr["{0} suffers from hunger!"] = "{0} souffre de la faim !";
        _en["A new day begins. Day {0}."] = "A new day begins. Day {0}."; _fr["A new day begins. Day {0}."] = "Une nouvelle journée commence. Jour {0}.";
        _en["Random encounter! Travel stops."] = "Random encounter! Travel stops."; _fr["Random encounter! Travel stops."] = "Rencontre aléatoire ! Le voyage s'arrête.";

        // Combat tooltips & extras
        _en["CRIT! -{0} HP"] = "CRIT! -{0} HP"; _fr["CRIT! -{0} HP"] = "CRIT ! -{0} PV";
        _en["-{0} HP"] = "-{0} HP"; _fr["-{0} HP"] = "-{0} PV";
        _en["+{0} HP"] = "+{0} HP"; _fr["+{0} HP"] = "+{0} PV";
        _en["Attack: {0}"] = "Attack: {0}"; _fr["Attack: {0}"] = "Attaque : {0}";
        _en["Spell: Eldritch Blast"] = "Spell: Eldritch Blast"; _fr["Spell: Eldritch Blast"] = "Sort : Décharge occulte";
        _en["Action: Help"] = "Action: Help"; _fr["Action: Help"] = "Action : Aider";
        _en["Distracted!"] = "Distracted!"; _fr["Distracted!"] = "Distrait !";
        _en["Grapple: d20({0}){1} = {2} (Athl.)"] = "Grapple: d20({0}){1} = {2} (Athl.)"; _fr["Grapple: d20({0}){1} = {2} (Athl.)"] = "Lutte : d20({0}){1} = {2} (Athl.)";
        _en["Fly"] = "Fly"; _fr["Fly"] = "Vol";
        _en["No dry tile found for spawn; using default position."] = "No dry tile found for spawn; using default position."; _fr["No dry tile found for spawn; using default position."] = "Aucune case seche trouvee pour le spawn; position par defaut utilisee.";
        _en["Spawn: {0} appears at ({1}, {2}, {3})."] = "Spawn: {0} appears at ({1}, {2}, {3})."; _fr["Spawn: {0} appears at ({1}, {2}, {3})."] = "Spawn : {0} apparait en ({1}, {2}, {3}).";
        _en["Spawn impossible: no valid tile found within 20 tiles."] = "Spawn impossible: no valid tile found within 20 tiles."; _fr["Spawn impossible: no valid tile found within 20 tiles."] = "Spawn impossible: aucune case valide trouvee a 20 cases.";
        _en["Alignment: {0}"] = "Alignment: {0}"; _fr["Alignment: {0}"] = "Alignement : {0}";
        _en["Size: {0} ({1})"] = "Size: {0} ({1})"; _fr["Size: {0} ({1})"] = "Taille : {0} ({1})";
        _en["Senses: {0} | Passive Perception: {1}"] = "Senses: {0} | Passive Perception: {1}"; _fr["Senses: {0} | Passive Perception: {1}"] = "Sens : {0} | Perception passive : {1}";
        _en["Conditions: {0}"] = "Conditions: {0}"; _fr["Conditions: {0}"] = "Conditions : {0}";
        _en["{0} enters RAGE!"] = "{0} enters RAGE!"; _fr["{0} enters RAGE!"] = "{0} entre en RAGE !";
        _en["Random name"] = "Random name"; _fr["Random name"] = "Nom aléatoire";
        _en["Abilities:"] = "Abilities:"; _fr["Abilities:"] = "Caractéristiques :";
        _en["Darkvision: {0} ft"] = "Darkvision: {0} ft"; _fr["Darkvision: {0} ft"] = "Vision nocturne : {0} ft";
        _en["Darkvision: none"] = "Darkvision: none"; _fr["Darkvision: none"] = "Vision nocturne : aucune";
        _en["Sunlight Sensitivity"] = "Sunlight Sensitivity"; _fr["Sunlight Sensitivity"] = "Sensibilité au soleil";
        _en["Name"] = "Name"; _fr["Name"] = "Nom";
        _en["Race"] = "Race"; _fr["Race"] = "Race";
        _en["Class"] = "Class"; _fr["Class"] = "Classe";
        _en["Tools"] = "Tools"; _fr["Tools"] = "Outils";
        _en["Skills"] = "Skills"; _fr["Skills"] = "Compétences";
        _en["Abilities"] = "Abilities"; _fr["Abilities"] = "Caractéristiques";
        _en["Review"] = "Review"; _fr["Review"] = "Révision";
        _en["PROFICIENCIES & LANGUAGES"] = "PROFICIENCIES & LANGUAGES"; _fr["PROFICIENCIES & LANGUAGES"] = "MAÎTRISES & LANGUES";
        _en["Armor:"] = "Armor:"; _fr["Armor:"] = "Armure :";
        _en["Weapons:"] = "Weapons:"; _fr["Weapons:"] = "Armes :";
        _en["Class Info:"] = "Class Info:"; _fr["Class Info:"] = "Infos de classe :";
        _en["Barbarian:"] = "Barbarian:"; _fr["Barbarian:"] = "Barbare :";
        _en["Bard:"] = "Bard:"; _fr["Bard:"] = "Barde :";
        _en["Cleric:"] = "Cleric:"; _fr["Cleric:"] = "Clerc :";
        _en["Spell Slots:"] = "Spell Slots:"; _fr["Spell Slots:"] = "Emplacements de sort :";
        _en["ATTACKS & SPELLCASTING"] = "ATTACKS & SPELLCASTING"; _fr["ATTACKS & SPELLCASTING"] = "ATTAQUES & SORTS";
        _en["EQUIPMENT"] = "EQUIPMENT"; _fr["EQUIPMENT"] = "ÉQUIPEMENT";
        _en["SKILLS"] = "SKILLS"; _fr["SKILLS"] = "COMPÉTENCES";
        _en["PERSONALITY TRAITS"] = "PERSONALITY TRAITS"; _fr["PERSONALITY TRAITS"] = "TRAITS DE PERSONNALITÉ";
        _en["IDEALS"] = "IDEALS"; _fr["IDEALS"] = "IDÉAUX";
        _en["BONDS"] = "BONDS"; _fr["BONDS"] = "LIENS";
        _en["FLAWS"] = "FLAWS"; _fr["FLAWS"] = "DÉFAUTS";
        _en["SAVING THROWS"] = "SAVING THROWS"; _fr["SAVING THROWS"] = "JETS DE SAUVEGARDE";
        _en["{0} spotted you! Combat started automatically."] = "{0} spotted you! Combat started automatically."; _fr["{0} spotted you! Combat started automatically."] = "{0} vous a repéré ! Le combat commence automatiquement.";
        _en["Viewing level {0}"] = "Viewing level {0}"; _fr["Viewing level {0}"] = "Affichage du niveau {0}";
        _en["Examine: {0}"] = "Examine: {0}"; _fr["Examine: {0}"] = "Examiner : {0}";
        _en["Combat ended!"] = "Combat ended!"; _fr["Combat ended!"] = "Combat terminé !";
        _en["No action available!"] = "No action available!"; _fr["No action available!"] = "Pas d'action disponible !";
        _en["{0} moved to ({1}, {2}, {3}) [{4}ft, {5}ft remaining]"] = "{0} moved to ({1}, {2}, {3}) [{4}ft, {5}ft remaining]"; _fr["{0} moved to ({1}, {2}, {3}) [{4}ft, {5}ft remaining]"] = "{0} s'est déplacé en ({1}, {2}, {3}) [{4}ft, {5}ft restants]";
        _en["Out of movement range!"] = "Out of movement range!"; _fr["Out of movement range!"] = "Hors de portée de déplacement !";
        _en["{0} retreats"] = "{0} retreats"; _fr["{0} retreats"] = "{0} se replie";
        _en["{0} moved"] = "{0} moved"; _fr["{0} moved"] = "{0} s'est déplacé";
        _en["=== Round {0} ==="] = "=== Round {0} ==="; _fr["=== Round {0} ==="] = "=== Round {0} ===";
        _en["{0} ended turn"] = "{0} ended turn"; _fr["{0} ended turn"] = "{0} a fini son tour";
        _en["Combat Log"] = "Combat Log"; _fr["Combat Log"] = "Journal de combat";
        _en["Gameplay Hints"] = "Press Tab to toggle HUD | ESC for menu | PageUp/Down: Change level"; _fr["Gameplay Hints"] = "Tab : interface | ESC : menu | PageUp/Down : niveau";
        _en["View Level: Z{0}"] = "View Level: Z{0}"; _fr["View Level: Z{0}"] = "Niveau de vue : Z{0}";
        _en["View Level Detail"] = "View Level: Z{0} | Player: Z{1} {2}"; _fr["View Level Detail"] = "Niveau de vue : Z{0} | Joueur : Z{1} {2}";
        _en["[FLYING]"] = "[FLYING]"; _fr["[FLYING]"] = "[VOLANT]";
        _en["[GROUND]"] = "[GROUND]"; _fr["[GROUND]"] = "[AU SOL]";
        _en["Tile Tooltip"] = "Tile ({0}, {1}, Z{2}) [{3}] | Light: {4} | Visible: {5}"; _fr["Tile Tooltip"] = "Case ({0}, {1}, Z{2}) [{3}] | Lumière : {4} | Visible : {5}";
        _en["Creature Tooltip"] = " | {0} ({1}, {2}) HP: {3}/{4}{5}"; _fr["Creature Tooltip"] = " | {0} ({1}, {2}) PV : {3}/{4}{5}";
        _en["Flying Status"] = " [FLYING]"; _fr["Flying Status"] = " [VOLANT]";
        _en["Click Delete button to remove character | Esc to go back"] = "Click Delete button to remove character | Esc to go back"; _fr["Click Delete button to remove character | Esc to go back"] = "Supprimer : bouton Suppr. | Echap : retour";
        _en["Click Delete button to remove campaign | Esc to go back"] = "Click Delete button to remove campaign | Esc to go back"; _fr["Click Delete button to remove campaign | Esc to go back"] = "Supprimer : bouton Suppr. | Echap : retour";
        _en["{0} ({1} members)"] = "{0} ({1} members)"; _fr["{0} ({1} members)"] = "{0} ({1} membres)";
        _en["HOOK"] = "HOOK"; _fr["HOOK"] = "DÉBUT (HOOK)";
        _en["MIDDLE"] = "MIDDLE"; _fr["MIDDLE"] = "MILIEU (MIDDLE)";
        _en["ENDING"] = "ENDING"; _fr["ENDING"] = "FIN (ENDING)";
        _en["No data available."] = "No data available."; _fr["No data available."] = "Aucune donnée disponible.";
        _en["Click Delete to confirm, Esc = cancel"] = "Click Delete to confirm, Esc = cancel"; _fr["Click Delete to confirm, Esc = cancel"] = "Supprimer pour confirmer, Echap pour annuler";
        _en["Campaign Instructions"] = "WASD: Pan | Zoom: Wheel | [1][2][3]: Scale | [F4]Fast [F5]Normal [F6]Slow: Pace | J: Journal | M: Close"; _fr["Campaign Instructions"] = "ZQSD : Caméra | Molette : Zoom | [1][2][3] : Échelle | [F4/F5/F6] : Allure | J : Journal | M : Fermer";
        _en["Click to type"] = "Click to type"; _fr["Click to type"] = "Cliquer pour écrire";
        _en["Example campaigns"] = "Examples: 'Lost Mine of Phandelver', 'Dragon's Curse'"; _fr["Example campaigns"] = "Exemples : 'Mine oubliée de Phancreux', 'La Malédiction du Dragon'";
        _en["Enter a name for your campaign:"] = "Enter a name for your campaign:"; _fr["Enter a name for your campaign:"] = "Entrez un nom pour votre campagne :";
        _en["Campaign name info"] = "This will be the name of your adventure."; _fr["Campaign name info"] = "Ce sera le nom de votre aventure.";
        _en["Name your home base:"] = "Name your home base:"; _fr["Name your home base:"] = "Nommez votre base :";
        _en["Home base name info"] = "A safe place where your party starts and can rest."; _fr["Home base name info"] = "Un endroit sûr où le groupe commence et peut se reposer.";
        _en["Select the type of settlement:"] = "Select the type of settlement:"; _fr["Select the type of settlement:"] = "Sélectionnez le type d'établissement :";
        _en["Settlement type info"] = "Influences available services and population."; _fr["Settlement type info"] = "Influence les services disponibles et la population.";
        _en["Describe the local region terrain:"] = "Describe the local region terrain:"; _fr["Describe the local region terrain:"] = "Décrivez le terrain de la région :";
        _en["Region terrain info"] = "The biome surrounding your home base."; _fr["Region terrain info"] = "Le biome entourant votre base.";
        _en["Select an adventure hook:"] = "Select an adventure hook:"; _fr["Select an adventure hook:"] = "Sélectionnez une amorce d'aventure :";
        _en["Adventure hook info"] = "What draws the heroes into the story?"; _fr["Adventure hook info"] = "Qu'est-ce qui attire les héros dans l'histoire ?";
        _en["Select the middle development:"] = "Select the middle development:"; _fr["Select the middle development:"] = "Sélectionnez le développement central :";
        _en["Middle development info"] = "The core complications and twists."; _fr["Middle development info"] = "Les complications et rebondissements centraux.";
        _en["Select the adventure conclusion:"] = "Select the adventure conclusion:"; _fr["Select the adventure conclusion:"] = "Sélectionnez la conclusion de l'aventure :";
        _en["Adventure conclusion info"] = "The potential resolution and impact."; _fr["Adventure conclusion info"] = "La résolution potentielle et l'impact.";
        _en["Type name, then press Enter"] = "Type name, then press Enter"; _fr["Type name, then press Enter"] = "Tapez le nom, puis Entrée";
        _en["Creation Navigation"] = "Click or use Arrow keys | Enter to continue | Esc to cancel"; _fr["Creation Navigation"] = "Clic ou flèches | Entrée pour continuer | Echap pour annuler";
        _en["Campaign Name"] = "Campaign Name"; _fr["Campaign Name"] = "Nom de la Campagne";
        _en["Home Base"] = "Home Base"; _fr["Home Base"] = "Base";
        _en["Local Region"] = "Local Region"; _fr["Local Region"] = "Région Locale";
        _en["Adventure Hook"] = "Adventure Hook"; _fr["Adventure Hook"] = "Amorce d'Aventure";
        _en["Adventure Middle"] = "Adventure Middle"; _fr["Adventure Middle"] = "Milieu d'Aventure";
        _en["Adventure Ending"] = "Adventure Ending"; _fr["Adventure Ending"] = "Fin d'Aventure";
        _en["Create Campaign - {0}"] = "Create Campaign - {0}"; _fr["Create Campaign - {0}"] = "Créer Campagne - {0}";
        _en["Campaign Creation"] = "Campaign Creation"; _fr["Campaign Creation"] = "Création de Campagne";
        _en["Random: Generate a complete campaign instantly"] = "Random: Generate a complete campaign instantly"; _fr["Random: Generate a complete campaign instantly"] = "Aléatoire : Générer une campagne complète instantanément";
        _en["Perfect for quick play and testing!"] = "Perfect for quick play and testing!"; _fr["Perfect for quick play and testing!"] = "Idéal pour jouer rapidement et tester !";
        _en["Custom: Create your campaign step by step"] = "Custom: Create your campaign step by step"; _fr["Custom: Create your campaign step by step"] = "Personnalisée : Créez votre campagne étape par étape";
        _en["Full control over every detail!"] = "Full control over every detail!"; _fr["Full control over every detail!"] = "Contrôle total sur chaque détail !";
        _en["Click to select | Enter to confirm | Esc to cancel"] = "Click to select | Enter to confirm | Esc to cancel"; _fr["Click to select | Enter to confirm | Esc to cancel"] = "Clic pour sélectionner | Entrée pour confirmer | Echap pour annuler";
        _en["Enter your character's name:"] = "Enter your character's name:"; _fr["Enter your character's name:"] = "Entrez le nom du personnage :";
        _en["Type your character's name..."] = "Type your character's name..."; _fr["Type your character's name..."] = "Tapez le nom du personnage...";
        _en["Name Tip"] = "?? Tips: Choose a name that fits your character's background and personality."; _fr["Name Tip"] = "?? Conseil : Choisissez un nom qui correspond au passé et à la personnalité du personnage.";
        _en["Choose your character's race:"] = "Choose your character's race:"; _fr["Choose your character's race:"] = "Choisissez la race du personnage :";
        _en["Ability Score Increases:"] = "Ability Score Increases:"; _fr["Ability Score Increases:"] = "Augmentation de caractéristiques :";
        _en["Traits:"] = "Traits:"; _fr["Traits:"] = "Traits :";
        _en["Speed: {0} feet"] = "Speed: {0} feet"; _fr["Speed: {0} feet"] = "Vitesse : {0} pieds";
        _en["Superior Darkvision: {0} feet"] = "Superior Darkvision: {0} feet"; _fr["Superior Darkvision: {0} feet"] = "Vision dans le noir supérieure : {0} pieds";
        _en["Darkvision: {0} feet"] = "Darkvision: {0} feet"; _fr["Darkvision: {0} feet"] = "Vision dans le noir : {0} pieds";
        _en["You must have at least 1 hit point to benefit from a short rest."] = "You must have at least 1 hit point to benefit from a short rest."; _fr["You must have at least 1 hit point to benefit from a short rest."] = "Vous devez avoir au moins 1 point de vie pour bénéficier d'un repos court.";
        _en["Hit Die: d{0} → {1} + {2} (CON) = {3} HP restored."] = "Hit Die: d{0} → {1} + {2} (CON) = {3} HP restored."; _fr["Hit Die: d{0} → {1} + {2} (CON) = {3} HP restored."] = "Dé de vie : d{0} → {1} + {2} (CON) = {3} PV restaurés.";
        _en["Font of Inspiration: Bardic Inspiration uses restored ({0})."] = "Font of Inspiration: Bardic Inspiration uses restored ({0})."; _fr["Font of Inspiration: Bardic Inspiration uses restored ({0})."] = "Source d'Inspiration : utilisations d'Inspiration Bardique restaurées ({0}).";
        _en["Channel Divinity uses restored ({0})."] = "Channel Divinity uses restored ({0})."; _fr["Channel Divinity uses restored ({0})."] = "Utilisations de Divinité Canalisée restaurées ({0}).";
        _en["Short rest summary"] = "{0} takes a short rest, spending {1} Hit {2} and regaining {3} HP."; _fr["Short rest summary"] = "{0} prend un repos court, dépense {1} Dé(s) de vie et récupère {3} PV.";
        _en["{0} takes a short rest."] = "{0} takes a short rest."; _fr["{0} takes a short rest."] = "{0} prend un repos court.";
        _en["You must have at least 1 hit point to benefit from a long rest."] = "You must have at least 1 hit point to benefit from a long rest."; _fr["You must have at least 1 hit point to benefit from a long rest."] = "Vous devez avoir au moins 1 point de vie pour bénéficier d'un repos long.";
        _en["A character can't benefit from more than one long rest in a 24-hour period."] = "A character can't benefit from more than one long rest in a 24-hour period."; _fr["A character can't benefit from more than one long rest in a 24-hour period."] = "Un personnage ne peut pas bénéficier de plus d'un repos long par période de 24 heures.";
        _en["Hit points fully restored: {0} → {1}."] = "Hit points fully restored: {0} → {1}."; _fr["Hit points fully restored: {0} → {1}."] = "Points de vie entièrement restaurés : {0} → {1}.";
        _en["Temporary hit points expired."] = "Temporary hit points expired."; _fr["Temporary hit points expired."] = "Points de vie temporaires expirés.";
        _en["Hit Dice restored: {0} (now {1}/{2})."] = "Hit Dice restored: {0} (now {1}/{2})."; _fr["Hit Dice restored: {0} (now {1}/{2})."] = "Dés de vie restaurés : {0} (actuellement {1}/{2}).";
        _en["Exhaustion reduced to level {0}."] = "Exhaustion reduced to level {0}."; _fr["Exhaustion reduced to level {0}."] = "Épuisement réduit au niveau {0}.";
        _en["{0} completes a long rest."] = "{0} completes a long rest."; _fr["{0} completes a long rest."] = "{0} termine un repos long.";
        _en["Rage uses: unlimited (level 20)."] = "Rage uses: unlimited (level 20)."; _fr["Rage uses: unlimited (level 20)."] = "Utilisations de Rage : illimitées (niveau 20).";
        _en["Rage uses restored: {0}."] = "Rage uses restored: {0}."; _fr["Rage uses restored: {0}."] = "Utilisations de Rage restaurées : {0}.";
        _en["Bardic Inspiration uses restored: {0}."] = "Bardic Inspiration uses restored: {0}."; _fr["Bardic Inspiration uses restored: {0}."] = "Utilisations d'Inspiration Bardique restaurées : {0}.";
        _en["Spell slots fully restored."] = "Spell slots fully restored."; _fr["Spell slots fully restored."] = "Emplacements de sort entièrement restaurés.";
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
