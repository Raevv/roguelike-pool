# Textes du jeu : où les modifier

Depuis la branche `ui-text`, chaque texte est un objet **TextMeshPro - Text (UI)** de la scène
`Pool.unity`, sous l'objet `UI`, et chaque phrase vient de l'asset **`Assets/Data/GameTexts.asset`**.

- **Les mots** (contenu, variantes, formats) → `Assets/Data/GameTexts.asset`.
- **L'apparence** (taille, couleur, alignement, position, marges) → l'objet TMP dans la Hierarchy.
  Ce que tu tapes dans le champ *Text* d'un objet TMP est remplacé au lancement par GameTexts : ce
  n'est qu'un aperçu. Clic droit sur l'asset GameTexts (ou sur son en-tête dans l'Inspector) →
  **Prévisualiser dans la scène** pour recopier les libellés fixes dans la scène.
- **Couleurs qui changent en jeu** (score atteint, dernier tir, titre de défaite, libellé d'un bouton
  activé) → champs « Couleurs » des composants `StageHUD`, `StageScreens` et `ShotUI`. Les autres
  couleurs se règlent dans le champ *Color* de l'objet TMP.
- **Police** : `Assets/Art/Font/VeniceClassic SDF.asset`, police par défaut de TextMeshPro
  (`Assets/TextMesh Pro/Resources/TMP Settings.asset`). Ombre portée : matériau
  `VeniceClassic SDF Material` (sous l'asset de police), section *Underlay*.
  Tailles nettes : multiples de 19 (19, 38, 57).
  Glyphes absents de VeniceClassic (affichés en LiberationSans) : `@ Â È Ê Ë Î Ï Ô Ù Û …`.

| Texte | Mots (GameTexts) | Objet TMP (Hierarchy) |
|-------|------------------|-----------------------|
| FORCE | Libellés fixes › Power Title | `UI/Panel/ForceTitle` |
| 50 % | Formats › Power Format `{0}%` | `UI/Panel/PowerGauge/PercentText` |
| VISÉE FINE | Boutons › Fine Aim Button | `UI/Panel/FineAimButton/Label` |
| TIRER / ... | Boutons › Shoot Button, Shoot Button Busy | `UI/Panel/ShootButton/Label` |
| Blanche empochée | Messages › Scratch Message | `UI/TableArea/Banner/Text` (objet inactif) |
| STAGE, SCORE, TIRS | Libellés fixes › Hud Stage/Score/Shots Label | `UI/TableArea/StageHUD/StageLabel`, `ScoreLabel`, `ShotsLabel` |
| 1/6, 0/100, 5/5 | Formats › Hud Stage/Score/Shots Format | `UI/TableArea/StageHUD/StageValue`, `ScoreValue`, `ShotsValue` |
| Titre de fin | Titres › Victory Titles, Run Won Titles, Defeat Titles (un au hasard) | `UI/TableArea/StageScreens/Veil/Panel/Title` |
| Ligne 1 | Formats › Stage Won Line 1, Run Won Line 1, Defeat Line 1 | `…/Veil/Panel/Line1` |
| Ligne 2 | Formats › Stage Won Line 2 (+ One Shot), Run Won Line 2, Defeat Line 2 | `…/Veil/Panel/Line2` |
| STAGE SUIVANT / RECOMMENCER | Boutons › Next Stage Button, Retry Button, New Run Button | `…/Veil/Panel/Button/Label` |

`Veil` (écrans de fin) et `Banner` sont inactifs dans la scène : coche-les pour les voir en mode
édition, puis décoche-les (le jeu les cache de toute façon au lancement).

Formats : `{0}`, `{1}`, `{2}` sont remplacés par les valeurs indiquées dans l'info-bulle de chaque
champ. Une accolade seule s'écrit `{{` ou `}}`. Un format invalide s'affiche tel quel, avec un
avertissement dans la console.

Hors périmètre : les numéros sur les boules (peints dans le sprite par `BallArt`).
