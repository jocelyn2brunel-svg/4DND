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
        _en["Donning {0}"] = "Donning {0}"; _fr["Donning {0}"] = "Enfile : {0}";
        _en["Doffing {0}"] = "Doffing {0}"; _fr["Doffing {0}"] = "Retire : {0}";
        _en["{0} rounds"] = "{0} rounds"; _fr["{0} rounds"] = "{0} rounds";
        _en["{0} min"] = "{0} min"; _fr["{0} min"] = "{0} min";
        _en["Busy Members:"] = "Busy Members:"; _fr["Busy Members:"] = "Membres occupés :";
        _en["Donning"] = "Donning"; _fr["Donning"] = "Enfilage";
        _en["Doffing"] = "Doffing"; _fr["Doffing"] = "Retrait";
        _en["doff"] = "doff"; _fr["doff"] = "retirer";
        _en["don"] = "don"; _fr["don"] = "enfiler";
        _en["1 action"] = "1 action"; _fr["1 action"] = "1 action";
        _en["1 minute"] = "1 minute"; _fr["1 minute"] = "1 minute";
        _en["{0} minutes"] = "{0} minutes"; _fr["{0} minutes"] = "{0} minutes";
        _en["with help"] = "with help"; _fr["with help"] = "avec de l'aide";
        _en["It will take {0} to {1} {2}. Do you want to continue?"] = "It will take {0} to {1} {2}. Do you want to continue?"; _fr["It will take {0} to {1} {2}. Do you want to continue?"] = "Cela prendra {0} pour {1} {2}. Voulez-vous continuer ?";
        _en["Yes"] = "Yes"; _fr["Yes"] = "Oui";
        _en["No"] = "No"; _fr["No"] = "Non";
        _en["{0} helps {1} with armor!"] = "{0} helps {1} with armor!"; _fr["{0} helps {1} with armor!"] = "{0} aide {1} avec son armure !";
        _en["{0} helps {1}!"] = "{0} helps {1}!"; _fr["{0} helps {1}!"] = "{0} aide {1} !";
        _en["{0} finished doffing {1}."] = "{0} finished doffing {1}."; _fr["{0} finished doffing {1}."] = "{0} a fini de retirer {1}.";
        _en["{0} finished donning {1}."] = "{0} finished donning {1}."; _fr["{0} finished donning {1}."] = "{0} a fini d'enfiler {1}.";
        _en["{0} is doffing {1} ({2} rounds left)."] = "{0} is doffing {1} ({2} rounds left)."; _fr["{0} is doffing {1} ({2} rounds left)."] = "{0} retire {1} ({2} rounds restants).";
        _en["{0} is donning {1} ({2} rounds left)."] = "{0} is donning {1} ({2} rounds left)."; _fr["{0} is donning {1} ({2} rounds left)."] = "{0} enfile {1} ({2} rounds restants).";
        _en["Cannot travel: {0} is busy."] = "Cannot travel: {0} is busy."; _fr["Cannot travel: {0} is busy."] = "Voyage impossible : {0} est occupé.";
        _en["This action cannot be undone."] = "This action cannot be undone."; _fr["This action cannot be undone."] = "Action irréversible.";
        _en["Delete {0} '{1}'?"] = "Delete {0} '{1}'?"; _fr["Delete {0} '{1}'?"] = "Supprimer {0} '{1}' ?";
        _en["character"] = "character"; _fr["character"] = "le personnage";
        _en["campaign"] = "campaign"; _fr["campaign"] = "la campagne";
        _en["Preview"] = "Preview"; _fr["Preview"] = "Aperçu";
        _en["Select an existing character to see their quick profile."] = "Select an existing character to see their quick profile."; _fr["Select an existing character to see their quick profile."] = "Sélectionne un personnage pour voir son profil.";
        _en["Select a Campaign"] = "Select a Campaign"; _fr["Select a Campaign"] = "Choisir une Campagne";
        _en["Create New Campaign"] = "Create New Campaign"; _fr["Create New Campaign"] = "Créer une nouvelle Campagne";
        _en["Adventure Summary"] = "Adventure Summary"; _fr["Adventure Summary"] = "Résumé de l'Aventure";
        _en["Apprentice"] = "Apprentice"; _fr["Apprentice"] = "Apprenti";
        _en["Adventurer"] = "Adventurer"; _fr["Adventurer"] = "Aventurier";
        _en["Heroic"] = "Heroic"; _fr["Heroic"] = "Héroïque";
        _en["Legendary"] = "Legendary"; _fr["Legendary"] = "Légendaire";
        _en["Mythic"] = "Mythic"; _fr["Mythic"] = "Mythique";
        _en["Campaign Difficulty"] = "Campaign Difficulty"; _fr["Campaign Difficulty"] = "Difficulté de la Campagne";
        _en["Select campaign difficulty:"] = "Select campaign difficulty:"; _fr["Select campaign difficulty:"] = "Sélectionnez la difficulté de la campagne :";
        _en["Apprentice: Easy encounters, great for learning."] = "Apprentice: Easy encounters, great for learning."; _fr["Apprentice: Easy encounters, great for learning."] = "Apprenti : Rencontres faciles, idéal pour apprendre.";
        _en["Adventurer: Standard D&D challenge."] = "Adventurer: Standard D&D challenge."; _fr["Adventurer: Standard D&D challenge."] = "Aventurier : Défi D&D standard.";
        _en["Heroic: Hard encounters, requires tactical thinking."] = "Heroic: Hard encounters, requires tactical thinking."; _fr["Heroic: Hard encounters, requires tactical thinking."] = "Héroïque : Rencontres difficiles, nécessite une réflexion tactique.";
        _en["Legendary: Deadly encounters, survival is not guaranteed."] = "Legendary: Deadly encounters, survival is not guaranteed."; _fr["Legendary: Deadly encounters, survival is not guaranteed."] = "Légendaire : Rencontres mortelles, la survie n'est pas garantie.";
        _en["Mythic: Beyond deadly, only for the most experienced heroes."] = "Mythic: Beyond deadly, only for the most experienced heroes."; _fr["Mythic: Beyond deadly, only for the most experienced heroes."] = "Mythique : Au-delà du mortel, réservé aux héros les plus expérimentés.";

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

        // Ball Bearings
        _en["{0} spills ball bearings covering a 10-foot square area!"] = "{0} spills ball bearings covering a 10-foot square area!"; _fr["{0} spills ball bearings covering a 10-foot square area!"] = "{0} répand des billes sur une zone carrée de 3 mètres !";
        _en["{0} slips on ball bearings and falls prone! (DEX save {1} vs DC 10)"] = "{0} slips on ball bearings and falls prone! (DEX save {1} vs DC 10)"; _fr["{0} slips on ball bearings and falls prone! (DEX save {1} vs DC 10)"] = "{0} glisse sur les billes et tombe à terre ! (jet DEX {1} vs DD 10)";
        _en["{0} navigates through ball bearings safely. (DEX save {1} vs DC 10)"] = "{0} navigates through ball bearings safely. (DEX save {1} vs DC 10)"; _fr["{0} navigates through ball bearings safely. (DEX save {1} vs DC 10)"] = "{0} traverse les billes sans tomber. (jet DEX {1} vs DD 10)";

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


        // Additional UI & gameplay localization
        _en["Press 'C' to close | Mouse wheel to scroll"] = "Press 'C' to close | Mouse wheel to scroll"; _fr["Press 'C' to close | Mouse wheel to scroll"] = "Appuyez sur 'C' pour fermer | Molette pour défiler";
        _en["Press 'J' to close | Mouse wheel to scroll"] = "Press 'J' to close | Mouse wheel to scroll"; _fr["Press 'J' to close | Mouse wheel to scroll"] = "Appuyez sur 'J' pour fermer | Molette pour défiler";
        _en["Unlimited"] = "Unlimited"; _fr["Unlimited"] = "Illimité";
        _en["Current proficiency bonus: {0}."] = "Current proficiency bonus: {0}."; _fr["Current proficiency bonus: {0}."] = "Bonus de maîtrise actuel : {0}.";
        _en["Passive Perception = 10 + Wisdom mod + proficiency if proficient = {0}."] = "Passive Perception = 10 + Wisdom mod + proficiency if proficient = {0}."; _fr["Passive Perception = 10 + Wisdom mod + proficiency if proficient = {0}."] = "Perception passive = 10 + mod. Sagesse + maîtrise éventuelle = {0}.";
        _en["Armor proficiency: {0}."] = "Armor proficiency: {0}."; _fr["Armor proficiency: {0}."] = "Maîtrise d'armure : {0}.";
        _en["Weapon proficiency: {0}."] = "Weapon proficiency: {0}."; _fr["Weapon proficiency: {0}."] = "Maîtrise d'arme : {0}.";
        _en["Class hit die: d{0}."] = "Class hit die: d{0}."; _fr["Class hit die: d{0}."] = "Dé de vie de classe : d{0}.";
        _en["Class primary ability: {0}."] = "Class primary ability: {0}."; _fr["Class primary ability: {0}."] = "Capacité principale de la classe : {0}.";
        _en["Rage damage bonus: +{0}."] = "Rage damage bonus: +{0}."; _fr["Rage damage bonus: +{0}."] = "Bonus de dégâts en rage : +{0}.";
        _en["Level {0} features: {1}."] = "Level {0} features: {1}."; _fr["Level {0} features: {1}."] = "Capacités de niveau {0} : {1}.";
        _en["Bardic Inspiration: {0} remaining out of {1}. Die: d{2}."] = "Bardic Inspiration: {0} remaining out of {1}. Die: d{2}."; _fr["Bardic Inspiration: {0} remaining out of {1}. Die: d{2}."] = "Inspiration bardique : {0} restante(s) sur {1}. Dé : d{2}.";
        _en["Channel Divinity: {0} out of {1} uses."] = "Channel Divinity: {0} out of {1} uses."; _fr["Channel Divinity: {0} out of {1} uses."] = "Divinité canalisée : {0} sur {1} utilisations.";
        _en["Level {0} spell slots: {1} remaining out of {2}."] = "Level {0} spell slots: {1} remaining out of {2}."; _fr["Level {0} spell slots: {1} remaining out of {2}."] = "Emplacements de sort niveau {0} : {1} restant(s) sur {2}.";
        _en["proficient"] = "proficient"; _fr["proficient"] = "maîtrisé";
        _en["not proficient"] = "not proficient"; _fr["not proficient"] = "non maîtrisé";
        _en["{0}: score {1} ({2}). Saving throw {3}."] = "{0}: score {1} ({2}). Saving throw {3}."; _fr["{0}: score {1} ({2}). Saving throw {3}."] = "{0} : score {1} ({2}). Jet de sauvegarde {3}.";
        _en["DAMAGE/TYPE"] = "DAMAGE/TYPE"; _fr["DAMAGE/TYPE"] = "DÉGÂTS/TYPE";
        _en["Two-weapon fighting (bonus action): {0}\nBonus: {1}\nDamage: {2} (positive modifier not added)\n{3}"] = "Two-weapon fighting (bonus action): {0}\nBonus: {1}\nDamage: {2} (positive modifier not added)\n{3}"; _fr["Two-weapon fighting (bonus action): {0}\nBonus: {1}\nDamage: {2} (positive modifier not added)\n{3}"] = "Combat à deux armes (action bonus) : {0}\nBonus : {1}\nDégâts : {2} (modificateur positif non ajouté)\n{3}";
        _en["Unarmed strike: bonus {0}, damage 1{1} bludgeoning, range 5 ft."] = "Unarmed strike: bonus {0}, damage 1{1} bludgeoning, range 5 ft."; _fr["Unarmed strike: bonus {0}, damage 1{1} bludgeoning, range 5 ft."] = "Frappe à mains nues : bonus {0}, dégâts 1{1} contondants, portée 5 ft.";
        _en["Grapple: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nRequires one free hand. Success: target grappled (speed 0).\n[Left click to use]"] = "Grapple: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nRequires one free hand. Success: target grappled (speed 0).\n[Left click to use]"; _fr["Grapple: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nRequires one free hand. Success: target grappled (speed 0).\n[Left click to use]"] = "Lutte : jet de Force (Athlétisme) {0} opposé à Force (Athlétisme) ou Dextérité (Acrobaties).\nCible en mêlée, taille max : une catégorie au-dessus de vous.\nNécessite une main libre. Succès : cible agrippée (vitesse 0).\n[Clic gauche pour lancer]";
        _en["Shove: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nSuccess: target knocked prone OR pushed 5 ft.\n[Left click to use]"] = "Shove: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nSuccess: target knocked prone OR pushed 5 ft.\n[Left click to use]"; _fr["Shove: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nSuccess: target knocked prone OR pushed 5 ft.\n[Left click to use]"] = "Bousculade : jet de Force (Athlétisme) {0} opposé à Force (Athlétisme) ou Dextérité (Acrobaties).\nCible en mêlée, taille max : une catégorie au-dessus de vous.\nSuccès : cible à terre OU repoussée de 5 ft.\n[Clic gauche pour lancer]";
        _en["Temporary HP: {0}. Absorb damage before normal HP. Expire after a long rest."] = "Temporary HP: {0}. Absorb damage before normal HP. Expire after a long rest."; _fr["Temporary HP: {0}. Absorb damage before normal HP. Expire after a long rest."] = "PV temporaires : {0}. Absorbent les dégâts avant les PV normaux. Expirent après un repos long.";
        _en["Temporary HP: none. Granted by some spells or abilities; absorb damage before normal HP."] = "Temporary HP: none. Granted by some spells or abilities; absorb damage before normal HP."; _fr["Temporary HP: none. Granted by some spells or abilities; absorb damage before normal HP."] = "PV temporaires : aucun. Accordés par certains sorts ou capacités ; absorbent les dégâts avant les PV normaux.";
        _en["HIT DICE"] = "HIT DICE"; _fr["HIT DICE"] = "DÉS DE VIE";
        _en["Hit dice: {0} (d{1}). Used during short rests."] = "Hit dice: {0} (d{1}). Used during short rests."; _fr["Hit dice: {0} (d{1}). Used during short rests."] = "Dés de vie : {0} (d{1}). Utilisés pendant les repos courts.";
        _en["Successes"] = "Successes"; _fr["Successes"] = "Succès";
        _en["Failures"] = "Failures"; _fr["Failures"] = "Échecs";
        _en["Death saves: {0} successes, {1} failures."] = "Death saves: {0} successes, {1} failures."; _fr["Death saves: {0} successes, {1} failures."] = "Jets de mort : {0} succès, {1} échecs.";
        _en["{0}: no note provided."] = "{0}: no note provided."; _fr["{0}: no note provided."] = "{0} : aucune note renseignée.";
        _en["Inspect Weapon"] = "Inspect Weapon"; _fr["Inspect Weapon"] = "Examiner l'arme";
        _en["(Click outside to close)"] = "(Click outside to close)"; _fr["(Click outside to close)"] = "(Cliquez à l'extérieur pour fermer)";
        _en["Range: {0}/{1} ft."] = "Range: {0}/{1} ft."; _fr["Range: {0}/{1} ft."] = "Portée : {0}/{1} ft.";
        _en["Melee Range: 5 ft."] = "Melee Range: 5 ft."; _fr["Melee Range: 5 ft."] = "Portée de mêlée : 5 ft.";
        _en["(equipped)"] = "(equipped)"; _fr["(equipped)"] = "(équipé)";
        _en["{0} {1}\nBonus: {2}\nDamage: {3}\n{4}"] = "{0} {1}\nBonus: {2}\nDamage: {3}\n{4}"; _fr["{0} {1}\nBonus: {2}\nDamage: {3}\n{4}"] = "{0} {1}\nBonus : {2}\nDégâts : {3}\n{4}";
        _en["{0}{1}\nWeight: {2} lbs | Value: {3} gp"] = "{0}{1}\nWeight: {2} lbs | Value: {3} gp"; _fr["{0}{1}\nWeight: {2} lbs | Value: {3} gp"] = "{0}{1}\nPoids : {2} lbs | Valeur : {3} po";
        _en["Inspect"] = "Inspect"; _fr["Inspect"] = "Examiner";
        _en["No dry tile found for spawn; default position used."] = "No dry tile found for spawn; default position used."; _fr["No dry tile found for spawn; default position used."] = "Aucune case sèche trouvée pour le spawn ; position par défaut utilisée.";
        _en["Spawn: {0} appears at ({1}, {2}, {3})."] = "Spawn: {0} appears at ({1}, {2}, {3})."; _fr["Spawn: {0} appears at ({1}, {2}, {3})."] = "Spawn : {0} apparaît en ({1}, {2}, {3}).";
        _en["Spawn failed: no valid tile found within 20 tiles."] = "Spawn failed: no valid tile found within 20 tiles."; _fr["Spawn failed: no valid tile found within 20 tiles."] = "Spawn impossible : aucune case valide trouvée dans un rayon de 20 cases.";
        _en["Grapple: d20({0}){1} = {2} (Athletics)"] = "Grapple: d20({0}){1} = {2} (Athletics)"; _fr["Grapple: d20({0}){1} = {2} (Athletics)"] = "Lutte : d20({0}){1} = {2} (Athlétisme)";
        _en["Spell: Eldritch Blast"] = "Spell: Eldritch Blast"; _fr["Spell: Eldritch Blast"] = "Sort : Décharge occulte";
        _en["Darkvision: none"] = "Darkvision: none"; _fr["Darkvision: none"] = "Vision nocturne : aucune";
        _en["Alignment: {0}\n"] = "Alignment: {0}\n"; _fr["Alignment: {0}\n"] = "Alignement : {0}\n";
        _en["Size: {0} ({1})\n"] = "Size: {0} ({1})\n"; _fr["Size: {0} ({1})\n"] = "Taille : {0} ({1})\n";
        _en["HP: {0}/{1} | AC: {2} | Speed: {3}ft\n"] = "HP: {0}/{1} | AC: {2} | Speed: {3}ft\n"; _fr["HP: {0}/{1} | AC: {2} | Speed: {3}ft\n"] = "PV : {0}/{1} | CA : {2} | Vitesse : {3}ft\n";
        _en["Attack: {0} +{1} ({2}+{3} {4})\n"] = "Attack: {0} +{1} ({2}+{3} {4})\n"; _fr["Attack: {0} +{1} ({2}+{3} {4})\n"] = "Attaque : {0} +{1} ({2}+{3} {4})\n";
        _en["Senses: {0} | Passive Perception: {1}\n"] = "Senses: {0} | Passive Perception: {1}\n"; _fr["Senses: {0} | Passive Perception: {1}\n"] = "Sens : {0} | Perception passive : {1}\n";
        _en["Conditions: {0}"] = "Conditions: {0}"; _fr["Conditions: {0}"] = "Conditions : {0}";
        _en["Cancel Travel"] = "Cancel Travel"; _fr["Cancel Travel"] = "Annuler le voyage";
        _en["Travel Here"] = "Travel Here"; _fr["Travel Here"] = "Voyager ici";
        _en["Long Rest (8h)"] = "Long Rest (8h)"; _fr["Long Rest (8h)"] = "Repos long (8h)";
        _en["PARTY"] = "PARTY"; _fr["PARTY"] = "GROUPE";
        _en["{0} is exhausted from forced march (CON save DC {1} failed)!"] = "{0} is exhausted from forced march (CON save DC {1} failed)!"; _fr["{0} is exhausted from forced march (CON save DC {1} failed)!"] = "{0} est épuisé par la marche forcée (jet CON DD {1} échoué) !";
        _en["{0} suffers from hunger!"] = "{0} suffers from hunger!"; _fr["{0} suffers from hunger!"] = "{0} souffre de la faim !";
        _en["A new day begins. Day {0}."] = "A new day begins. Day {0}."; _fr["A new day begins. Day {0}."] = "Une nouvelle journée commence. Jour {0}.";
        _en["Random encounter! Travel stops."] = "Random encounter! Travel stops."; _fr["Random encounter! Travel stops."] = "Rencontre aléatoire ! Le voyage s'arrête.";
        _en["Traveling... {0:P0}"] = "Traveling... {0:P0}"; _fr["Traveling... {0:P0}"] = "Voyage en cours... {0:P0}";
        _en["ADVENTURE JOURNAL: {0}"] = "ADVENTURE JOURNAL: {0}"; _fr["ADVENTURE JOURNAL: {0}"] = "JOURNAL D'AVENTURE : {0}";
        _en["THE BEGINNING (HOOK)"] = "THE BEGINNING (HOOK)"; _fr["THE BEGINNING (HOOK)"] = "LE COMMENCEMENT (ACCROCHE)";
        _en["THE DEVELOPMENT"] = "THE DEVELOPMENT"; _fr["THE DEVELOPMENT"] = "LE DÉROULEMENT";
        _en["THE CONCLUSION (CLIMAX)"] = "THE CONCLUSION (CLIMAX)"; _fr["THE CONCLUSION (CLIMAX)"] = "LA CONCLUSION (CLIMAX)";
        _en["No detail available."] = "No detail available."; _fr["No detail available."] = "Aucun détail disponible.";
        _en["Equip"] = "Equip"; _fr["Equip"] = "Équiper";
        _en["Equip (Offhand)"] = "Equip (Offhand)"; _fr["Equip (Offhand)"] = "Équiper (secondaire)";
        _en["Unequip"] = "Unequip"; _fr["Unequip"] = "Déséquiper";
        _en["Drop"] = "Drop"; _fr["Drop"] = "Jeter";
        _en["Throw"] = "Throw"; _fr["Throw"] = "Lancer";
        _en["Play"] = "Play"; _fr["Play"] = "Jouer";
        _en["Light"] = "Light"; _fr["Light"] = "Allumer";
        _en["You dropped {0}."] = "You dropped {0}."; _fr["You dropped {0}."] = "Vous avez jeté {0}.";
        _en["{0}'s torch has burned out!"] = "{0}'s torch has burned out!"; _fr["{0}'s torch has burned out!"] = "La torche de {0} s'est éteinte !";
        _en["A torch on the ground has burned out!"] = "A torch on the ground has burned out!"; _fr["A torch on the ground has burned out!"] = "Une torche au sol s'est éteinte !";
        _en["Lit (Remaining: {0}m)"] = "Lit (Remaining: {0}m)"; _fr["Lit (Remaining: {0}m)"] = "Allumée (Restant : {0}m)";
        _en["{0}: field to fill in."] = "{0}: field to fill in."; _fr["{0}: field to fill in."] = "{0} : champ à renseigner.";
        _en["Click Delete button to remove campaign | Esc to go back"] = "Click Delete button to remove campaign | Esc to go back"; _fr["Click Delete button to remove campaign | Esc to go back"] = "Cliquez sur Supprimer pour retirer la campagne | Échap pour revenir";
        _en["Click Delete button to remove character | Esc to go back"] = "Click Delete button to remove character | Esc to go back"; _fr["Click Delete button to remove character | Esc to go back"] = "Cliquez sur Supprimer pour retirer le personnage | Échap pour revenir";
        _en["Click Delete to confirm, Esc = cancel"] = "Click Delete to confirm, Esc = cancel"; _fr["Click Delete to confirm, Esc = cancel"] = "Cliquez sur Supprimer pour confirmer, Échap = annuler";

        _en["Armor Class: {0}."] = "Armor Class: {0}."; _fr["Armor Class: {0}."] = "Classe d'armure : {0}.";
        _en["Initiative: {0}."] = "Initiative: {0}."; _fr["Initiative: {0}."] = "Initiative : {0}.";
        _en["Speed: {0} ft."] = "Speed: {0} ft."; _fr["Speed: {0} ft."] = "Vitesse : {0} ft.";
        _en["NAME"] = "NAME"; _fr["NAME"] = "NOM";
        _en["Initiative: "] = "Initiative: "; _fr["Initiative: "] = "Initiative : ";
        _en["Darkvision: {0} ft"] = "Darkvision: {0} ft"; _fr["Darkvision: {0} ft"] = "Vision nocturne : {0} ft";
        _en["Attack: {0}"] = "Attack: {0}"; _fr["Attack: {0}"] = "Attaque : {0}";
        _en["CRIT! -{0} HP"] = "CRIT! -{0} HP"; _fr["CRIT! -{0} HP"] = "CRIT ! -{0} PV";
        _en["-{0} HP"] = "-{0} HP"; _fr["-{0} HP"] = "-{0} PV";
        _en["Sunlight Sensitivity"] = "Sunlight Sensitivity"; _fr["Sunlight Sensitivity"] = "Sensibilité au soleil";
        _en["+{0} HP"] = "+{0} HP"; _fr["+{0} HP"] = "+{0} PV";
        _en["Max HP"] = "Max HP"; _fr["Max HP"] = "PV Max";
        _en["TEMPORARY HP"] = "TEMPORARY HP"; _fr["TEMPORARY HP"] = "PV TEMPORAIRES";
        _en["Spell: {0}"] = "Spell: {0}"; _fr["Spell: {0}"] = "Sort : {0}";
        _en["View Level: Z{0}"] = "View Level: Z{0}"; _fr["View Level: Z{0}"] = "Niveau de vue : Z{0}";
        _en["Distracted!"] = "Distracted!"; _fr["Distracted!"] = "Distrait !";
        // Buttons (canonical English keys)
        _en["Inventory [C]"] = "Inventory [C]"; _fr["Inventory [C]"] = "Inventaire [C]";
        _en["Open Map [M]"] = "Open Map [M]"; _fr["Open Map [M]"] = "Ouvrir map [M]";
        _en["Close Map [M]"] = "Close Map [M]"; _fr["Close Map [M]"] = "Fermer map [M]";

        // Map Scale UI
        _en["Map Scale"] = "Map Scale"; _fr["Map Scale"] = "Échelle de la carte";
        _en["Visible at this scale:"] = "Visible at this scale:"; _fr["Visible at this scale:"] = "Visible à cette échelle :";
        _en["Province"] = "Province"; _fr["Province"] = "Province";
        _en["Kingdom"] = "Kingdom"; _fr["Kingdom"] = "Royaume";
        _en["Continent"] = "Continent"; _fr["Continent"] = "Continent";
        _en["Detailed local exploration"] = "Detailed local exploration"; _fr["Detailed local exploration"] = "Exploration locale détaillée";
        _en["Regional travel overview"] = "Regional travel overview"; _fr["Regional travel overview"] = "Aperçu du voyage régional";
        _en["Continental geography"] = "Continental geography"; _fr["Continental geography"] = "Géographie continentale";
        _en["Hamlets"] = "Hamlets"; _fr["Hamlets"] = "Hameaux";
        _en["Villages"] = "Villages"; _fr["Villages"] = "Villages";
        _en["Towns"] = "Towns"; _fr["Towns"] = "Bourgs";
        _en["Cities"] = "Cities"; _fr["Cities"] = "Villes";
        _en["Metropolises"] = "Metropolises"; _fr["Metropolises"] = "Métropoles";
        _en["Forts"] = "Forts"; _fr["Forts"] = "Forts";
        _en["Castles"] = "Castles"; _fr["Castles"] = "Châteaux";
        _en["Monasteries"] = "Monasteries"; _fr["Monasteries"] = "Monastères";
        _en["Dungeons"] = "Dungeons"; _fr["Dungeons"] = "Donjons";
        _en["Wilderness"] = "Wilderness"; _fr["Wilderness"] = "Nature";
        _en["Travel Pace"] = "Travel Pace"; _fr["Travel Pace"] = "Allure de voyage";
        _en["Pace"] = "Pace"; _fr["Pace"] = "Allure";
        _en["Effect"] = "Effect"; _fr["Effect"] = "Effet";
        _en["Stealth available"] = "Stealth available"; _fr["Stealth available"] = "Discrétion possible";
        _en["-5 passive Percep."] = "-5 passive Percep."; _fr["-5 passive Percep."] = "-5 Perception passive";
        _en["Fast"] = "Fast"; _fr["Fast"] = "Rapide";
        _en["Normal"] = "Normal"; _fr["Normal"] = "Normale";
        _en["Slow"] = "Slow"; _fr["Slow"] = "Lente";
        _en["Map Legend"] = "WASD: Pan | Zoom: Wheel | J: Journal | M: Close"; _fr["Map Legend"] = "ZQSD : Déplacement | Molette : Zoom | J : Journal | M : Fermer";
        _en["Home Base: {0}"] = "Home Base: {0}"; _fr["Home Base: {0}"] = "Base : {0}";
        _en["Locations (visible): {0} / {1}"] = "Locations (visible): {0} / {1}"; _fr["Locations (visible): {0} / {1}"] = "Lieux (visibles) : {0} / {1}";
        _en["Session: {0}"] = "Session: {0}"; _fr["Session: {0}"] = "Session : {0}";
        _en["Objective:"] = "Objective:"; _fr["Objective:"] = "Objectif :";
        _en["Selected: {0}"] = "Selected: {0}"; _fr["Selected: {0}"] = "Sélection : {0}";
        _en["Terrain: {0}"] = "Terrain: {0}"; _fr["Terrain: {0}"] = "Terrain : {0}";
        _en["Scale"] = "Scale"; _fr["Scale"] = "Échelle";
        _en["1 hex = {0} mile{1}"] = "1 hex = {0} mile{1}"; _fr["1 hex = {0} mile{1}"] = "1 hex = {0} mille{1}";
        _en["{0}mi/hex"] = "{0}mi/hex"; _fr["{0}mi/hex"] = "{0} mi/hex";

        // Legacy aliases (temporary: remove after global key cleanup)
        _en["Inventaire [C]"] = _en["Inventory [C]"]; _fr["Inventaire [C]"] = _fr["Inventory [C]"];
        _en["Ouvrir map [M]"] = _en["Open Map [M]"]; _fr["Ouvrir map [M]"] = _fr["Open Map [M]"];
        _en["Fermer map [M]"] = _en["Close Map [M]"]; _fr["Fermer map [M]"] = _fr["Close Map [M]"];
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
