# 4DND - Système 3D et Vol

## Vue d'ensemble
Le jeu a été transformé en un véritable environnement 3D avec support de la hauteur (axe Z), du vol, et d'une projection isométrique 3D.

## Nouvelles fonctionnalités 3D

### 1. **Grille 3D** (`InfiniteGrid3D<T>`)
- Grille tridimensionnelle avec coordonnées (X, Y, Z)
- Z = 0 : Niveau du sol
- Z > 0 : Niveaux supérieurs (plateformes, étages)
- Support des voisins 3D incluant vertical

### 2. **Créatures en 3D**
Toutes les créatures ont maintenant :
- **Position Z** : Hauteur actuelle
- **Vol** : `CanFly`, `FlySpeed`, `IsFlying`
- Les créatures volantes (ex: Couatl) peuvent se déplacer verticalement

### 3. **Contrôles 3D**

#### Navigation de caméra
- **PageUp** : Visualiser le niveau supérieur (+Z)
- **PageDown** : Visualiser le niveau inférieur (-Z)
- Les niveaux en dessous du niveau actuel sont affichés en transparence (30%)

#### Vol (pour créatures volantes uniquement)
- **Space** : Toggle mode vol ON/OFF
- **R** : Ascension (+Z) - seulement en mode vol
- **T** : Descente (-Z) - seulement en mode vol
- Indicateur visuel : ? à côté du nom quand en vol

### 4. **Système de vision 3D**
Le système de vision a été étendu pour supporter la 3D :
- Calcul de lumière en 3D (distance Manhattan 3D)
- Visibilité calculée en 3D
- Les sources de lumière et effets de zone ont une coordonnée Z
- La distance inclut maintenant la composante verticale

### 5. **Combat en 3D**

#### Portée de mêlée 3D
- Combat de mêlée possible avec créatures adjacentes **ET** sur un niveau différent (±1 Z)
- Permet les attaques vers le haut/bas

#### Mouvement 3D en combat
- Le mouvement prend en compte la distance 3D (X + Y + Z)
- Les créatures volantes peuvent se déplacer verticalement
- Coût de mouvement uniforme dans toutes les directions

#### Affichage des créatures
- **Ombre au sol** : Les créatures en hauteur projettent une ombre au niveau Z=0
- **Ligne verticale** : Connecte la créature à son ombre
- **Indicateur [Z#]** : Affiche le niveau Z à côté du nom
- **Indicateur ?** : Montre si la créature vole

### 6. **Rendu 3D isométrique**
- Projection isométrique vraie 3D
- Chaque niveau Z décale l'affichage verticalement
- Formule : `yScreen = yIso - Z * (tileHeight * 0.5)`
- Grilles multiples superposées avec transparence

### 7. **Sources de lumière 3D**
- Les torches, lanternes, etc. ont une position Z
- Le rayonnement lumineux est sphérique en 3D
- Les créatures volantes transportant des lumières éclairent leur niveau

### 8. **Effets de zone 3D**
- Fog Cloud, Darkness affectent une sphère 3D
- Les effets ont une coordonnée Z
- L'obscurcissement affecte tous les niveaux dans le rayon

## Exemples d'utilisation

### Créer une créature volante
```csharp
var couatl = Creature.CreateCouatl(5, 5, 2); // Spawn à Z=2
couatl.IsFlying = true; // En vol
```

### Créer une source de lumière en hauteur
```csharp
var torch = LightSource.Torch(x, y, 3); // Torche au niveau Z=3
```

### Créer un effet de zone à un niveau spécifique
```csharp
var fog = AreaEffect.FogCloud(x, y, 2, 20); // Brouillard au niveau Z=2
```

## Structure multi-niveaux
Le jeu génère automatiquement :
- **Niveau 0** : Sol principal avec ennemis
- **Niveaux 1-3** : Plateformes diagonales pour exploration verticale
- Les ennemis peuvent spawner à n'importe quel niveau

## Contrôles de test en jeu

| Touche | Action |
|--------|--------|
| PageUp | Monter la vue d'un niveau |
| PageDown | Descendre la vue d'un niveau |
| Space | Toggle vol (si capable) |
| R | Ascension en vol |
| T | Descente en vol |
| B | Toggle condition Aveuglé (test) |
| F | Créer Fog Cloud au niveau actuel |
| K | Créer sort Darkness au niveau actuel |
| V | Toggle vision overlay |
| L | Toggle lumière du jour |

## Avantages tactiques de la 3D

1. **Attaques par le haut** : Avantage de combattre depuis une position élevée
2. **Vol tactique** : Les créatures volantes peuvent éviter le combat de mêlée
3. **Couverture verticale** : Les plateformes offrent protection
4. **Effets de zone multi-niveaux** : Fog Cloud peut bloquer vision sur plusieurs étages
5. **Éclairage 3D** : Les lanternes en hauteur éclairent différemment

## Notes techniques

- Distance 3D = |?X| + |?Y| + |?Z| (Manhattan 3D)
- 1 tile = 5 pieds (selon règles D&D 5e)
- Chaque niveau Z = environ 10 pieds de hauteur
- Les créatures volantes ignorent le terrain au sol
- La vision fonctionne en 3D sphérique

## Exemples de créatures avec capacités spéciales 3D

- **Couatl** : Vol 90ft, peut attaquer depuis les airs
- **Wolf** : Blindsight 30ft, détecte en 3D même aveuglé
- **Umber Hulk** : Tremorsense 60ft, sent vibrations sur tous niveaux

## Future améliorations possibles
- Gravité et chutes
- Escalade sur les murs
- Projectiles avec arc parabolique
- Effets de terrain (falaises, précipices)
- Sorts de vol temporaire
