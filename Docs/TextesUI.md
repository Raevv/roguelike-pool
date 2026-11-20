# Inventaire des textes du jeu

État avant la branche `ui-text` : tous les textes étaient **dessinés pixel par pixel en code**
(police bitmap `PixelFont`) dans des textures, puis affichés par des `RawImage` ou cuits dans les
sprites des boutons. Aucun n'était visible ni modifiable dans la Hierarchy.

| # | Texte | Où il s'affiche | Script (avant) | Valeurs dynamiques |
|---|-------|-----------------|----------------|--------------------|
| 1 | `FORCE` | titre du panneau de droite | `ShotUI.Build` | — |
| 2 | `50%` | pourcentage sous la jauge de force | `PowerGauge.Build/Update` | puissance en % |
| 3 | `VISÉE FINE` | bouton visée fine (2 lignes) | `ShotUI.BuildFineAimButton` (cuit dans le sprite) | — |
| 4 | `TIRER` | bouton de tir | `ShotUI.BuildShootButton` (cuit dans le sprite) | — |
| 5 | `...` | bouton de tir désactivé (boules en mouvement) | `ShotUI.BuildShootButton` (sprite désactivé) | — |
| 6 | `Blanche empochée : tir annulé` | bandeau au centre de la table | `TableRules` → `ShotUI.ShowMessage` | — |
| 7 | `STAGE` / `n/N` | HUD en haut, colonne 1 | `StageHUD.Redraw` | stage actuel, nombre de stages |
| 8 | `SCORE` / `s/objectif` | HUD en haut, colonne 2 (vert quand l'objectif est atteint) | `StageHUD.Redraw` | score, objectif |
| 9 | `TIRS` / `restants/max` | HUD en haut, colonne 3 (rouge au dernier tir) | `StageHUD.Redraw` | tirs restants, tirs max |
| 10 | `VICTOIRE !` | titre de l'écran de fin, stage gagné | `StageScreens.OnStageWon` | — |
| 11 | `STAGE n RÉUSSI` | ligne 1, stage gagné | `StageScreens.OnStageWon` | n° du stage |
| 12 | `SCORE s/objectif - n TIRS RESTANTS` | ligne 2, stage gagné (singulier si 1 tir) | `StageScreens.OnStageWon` | score, objectif, tirs restants |
| 13 | `STAGE SUIVANT` | bouton, stage gagné | `StageScreens.OnStageWon` (cuit dans le sprite) | — |
| 14 | `BRAVO !` | titre, dernier stage gagné | `StageScreens.OnStageWon` | — |
| 15 | `PARTIE GAGNÉE` | ligne 1, dernier stage gagné | `StageScreens.OnStageWon` | — |
| 16 | `LES N STAGES SONT RÉUSSIS` | ligne 2, dernier stage gagné | `StageScreens.OnStageWon` | nombre de stages |
| 17 | `DÉFAITE` | titre (rouge), stage perdu | `StageScreens.OnStageLost` | — |
| 18 | `PLUS DE TIRS AU STAGE n` | ligne 1, stage perdu | `StageScreens.OnStageLost` | n° du stage |
| 19 | `SCORE s/objectif` | ligne 2, stage perdu | `StageScreens.OnStageLost` | score, objectif |
| 20 | `RECOMMENCER` | bouton, stage perdu ou partie gagnée | `StageScreens` (cuit dans le sprite) | — |

Hors périmètre : les numéros sur les boules (`BallArt`, chiffres 3×5 peints dans le sprite de la
boule). Ce sont des graphismes, pas du texte d'interface.
