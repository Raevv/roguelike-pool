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
  Les textes dont la couleur est trop proche de celle de l'ombre (texte noir sur bouton doré)
  passent automatiquement sur `VeniceClassic SDF NoShadow.mat`, sans ombre (`TextShadow`, seuil
  d'écart de luminosité 0.25).
  Tailles nettes : multiples de 19 (38, 57, 76, 95, 133 ; le canvas est en 1080×1920, 1 pixel d'art = 2 unités).
  Glyphes absents de VeniceClassic (affichés en LiberationSans) : `@ Â È Ê Ë Î Ï Ô Ù Û …`.

| Texte | Mots (GameTexts) | Objet TMP (Hierarchy) |
|-------|------------------|-----------------------|
| 50 % | Formats › Power Format `{0}%` | `UI/SafeArea/Content/PowerGauge/PercentText` |
| VISÉE FINE | Boutons › Fine Aim Button | `UI/SafeArea/Content/FineAimButton/Label` |
| TIRER / ... | Boutons › Shoot Button, Shoot Button Busy | `UI/SafeArea/Content/ShootButton/Label` |
| Blanche empochée | Messages › Scratch Message | `UI/SafeArea/Content/TableArea/Banner/Text` (objet inactif) |
| Stage 1 | Formats › Hud Stage Format `Stage {0}` | `UI/SafeArea/Content/TopBar/StageTitle` |
| 0 / 100 | Formats › Hud Score Format `{0} / {1}` | `UI/SafeArea/Content/TopBar/ScoreObjective` |
| 999 (argent) | Formats › Money Format `{0}` | `UI/SafeArea/Content/TopBar/Money/MoneyText` |
| +1 (texte à la poche) | Formats › Popup Points Format `+{0}` | prefab `Assets/Prefabs/UI/ScorePopup` (instances sous `UI/PopupLayer` en Play Mode) |
| Menu des paramètres | Menu des paramètres › Settings Title, Options/Restart/Quit/Close Button | `UI/Overlays/SettingsPanel/Placeholder_SettingsMenu/…` (objet inactif) |
| Titre de fin | Titres › Victory Titles, Run Won Titles, Defeat Titles (un au hasard) | `UI/Overlays/StageScreens/Veil/Panel/Title` |
| Ligne 1 | Formats › Stage Won Line 1, Run Won Line 1, Defeat Line 1 | `…/Veil/Panel/Line1` |
| Ligne 2 | Formats › Stage Won Line 2 (+ One Shot), Run Won Line 2, Defeat Line 2 | `…/Veil/Panel/Line2` |
| STAGE SUIVANT / RECOMMENCER | Boutons › Next Stage Button, Retry Button, New Run Button | `…/Veil/Panel/Button/Label` |

Les tirs restants ne sont plus un texte : ce sont les icônes de `UI/SafeArea/Content/ShotsCounter`.
Les anciens libellés STAGE / SCORE / TIRS (`TopBar/StageLabel`, `ScoreLabel`, `ShotsLabel`) et le
titre FORCE n'existent plus ou sont désactivés.

`Veil` (écrans de fin), `SettingsPanel` et `Banner` sont inactifs dans la scène : coche-les pour les
voir en mode édition, puis décoche-les (le jeu les cache de toute façon au lancement).

Formats : `{0}`, `{1}`, `{2}` sont remplacés par les valeurs indiquées dans l'info-bulle de chaque
champ. Une accolade seule s'écrit `{{` ou `}}`. Un format invalide s'affiche tel quel, avec un
avertissement dans la console.

Hors périmètre : les numéros sur les boules (peints dans le sprite par `BallArt`).
