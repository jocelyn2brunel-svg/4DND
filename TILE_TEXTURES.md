# Guide des textures de tuiles de gazon

Ce guide décrit une approche pratique pour intégrer des textures de gazon lisibles et performantes dans **4DND** (vue isométrique en grille).

## 1) Objectif visuel pour 4DND

Pour un jeu tactique en grille, la texture de gazon doit :

- rester lisible à distance (on doit distinguer facilement les cases),
- ne pas dominer les unités et les infos de combat,
- conserver une cohérence de lumière avec la caméra isométrique.

Recommandation : un style **semi-stylisé** (détails modérés, contraste contrôlé).

## 2) Set de textures recommandé

Pour chaque matériau de "gazon", préparer :

- **Albedo/BaseColor** : couleur principale du sol,
- **Normal** : micro-relief léger,
- **Roughness** (ou Specular selon pipeline) : contrôle de la brillance,
- **AO** (facultatif) : ombrage doux dans les creux.

Tailles conseillées :

- 512x512 pour prototype,
- 1024x1024 pour qualité standard desktop,
- 2048x2048 uniquement si gros plans fréquents.

## 3) Variantes pour casser la répétition (tiling)

Un seul tile de gazon se répète trop vite. Prévoir au minimum :

- 3 à 5 variantes de gazon (teinte + densité légèrement différentes),
- 1 variante "usée" (terre visible),
- 1 variante "humide" ou plus sombre pour zones d'ombre.

Puis faire un mélange par :

- bruit procédural,
- ou motif pseudo-aléatoire basé sur les coordonnées de case,
- ou peinture de couche (splat map) sur les zones importantes.

## 4) Lisibilité gameplay (priorité)

Dans un tactical grid, la texture ne doit pas gêner :

- le surlignage de case active,
- la portée de déplacement/attaque,
- les indicateurs d'état (sélection, focus, danger).

Concrètement :

- limiter les contrastes extrêmes dans l'albedo,
- éviter des motifs "directionnels" trop marqués,
- garder une valeur moyenne de luminosité stable entre variantes.

## 5) Pipeline d'intégration conseillé (MonoGame)

1. Importer les textures en PNG/TGA sans compression destructive à la source.
2. Générer les mipmaps pour réduire le scintillement en zoom arrière.
3. Utiliser un sampler en **anisotropic filtering** pour les angles isométriques.
4. Mettre en place un shader terrain simple (base + normal + variation de teinte).
5. Ajouter un masque léger de "dirt" sur les zones de passage fréquent.

## 6) Budget performance indicatif

- Favoriser plusieurs textures 1024 bien utilisées plutôt qu'une seule 4K.
- Réduire les lectures texture par pixel sur matériel modeste.
- Préférer la variation par teinte/UV à des couches trop nombreuses.

Cible pratique :

- 2 à 4 échantillons texture principaux par pixel sur le terrain,
- atlas ou texture array si le nombre de variantes augmente.

## 7) Check-list qualité avant validation

- Le terrain reste propre visuellement en zoom proche et lointain.
- Les cases de grille restent lisibles pendant un combat chargé.
- Les unités ressortent clairement du fond.
- Aucun motif de répétition évident sur une zone de 20x20 cases.
- Les performances restent stables pendant les déplacements caméra.

## 8) Réglages de départ (safe defaults)

- Saturation du gazon : 85-90% de la texture source,
- Contraste albedo : faible à moyen,
- Intensité normal map : 20-35%,
- Roughness : plutôt élevée pour éviter un gazon "plastique".

---

Si besoin, prochaine étape : créer un preset "plaine tempérée" et un preset "lande sèche" avec des paramètres directement exploitables dans le shader terrain du projet.
