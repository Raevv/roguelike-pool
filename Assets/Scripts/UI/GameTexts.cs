using TMPro;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Every sentence the game writes on screen (Assets/Data/GameTexts.asset, assigned to GameBootstrap).
    /// The code never hard-codes a text: it reads it here. Font, size, color and position are set on
    /// the TextMeshPro objects of the scene (under "UI").
    /// Titles have several variants, one is picked at random each time the screen appears.
    /// VeniceClassic lacks @ Â È Ê Ë Î Ï Ô Ù Û and … : those fall back to LiberationSans.
    /// </summary>
    [CreateAssetMenu(fileName = "GameTexts", menuName = "Roguelike Pool/Game Texts")]
    public class GameTexts : ScriptableObject
    {
        [Header("Titres des écrans de fin (un au hasard)")]
        [Tooltip("Stage gagné.")]
        public string[] victoryTitles = { "VICTOIRE !", "BIEN JOUÉ !", "SUPERBE !", "STAGE NETTOYÉ !" };
        [Tooltip("Dernier stage gagné : partie gagnée.")]
        public string[] runWonTitles = { "BRAVO !", "CHAMPION !", "QUELLE PARTIE !" };
        [Tooltip("Stage perdu (plus de tirs).")]
        public string[] defeatTitles = { "DÉFAITE", "PERDU !", "RATÉ...", "DOMMAGE !" };

        [Header("Boutons")]
        public string shootButton = "TIRER";
        [Tooltip("Bouton de tir pendant que les boules roulent (tir impossible).")]
        public string shootButtonBusy = "...";
        [Tooltip("Visée fine (\\n = retour à la ligne).")]
        [TextArea(1, 3)] public string fineAimButton = "VISÉE\nFINE";
        [Tooltip("Écran de victoire d'un stage.")]
        public string nextStageButton = "STAGE SUIVANT";
        [Tooltip("Écran de défaite.")]
        public string retryButton = "RECOMMENCER";
        [Tooltip("Écran de partie gagnée.")]
        public string newRunButton = "RECOMMENCER";

        [Header("Libellés fixes")]
        [Tooltip("Titre au-dessus de la jauge de force.")]
        public string powerTitle = "FORCE";
        public string hudStageLabel = "STAGE";
        public string hudScoreLabel = "SCORE";
        public string hudShotsLabel = "TIRS";

        [Header("Menu des paramètres")]
        public string settingsTitle = "MENU";
        public string settingsOptionsButton = "Paramètres";
        public string settingsRestartButton = "Recommencer la partie";
        public string settingsQuitButton = "Quitter";
        public string settingsCloseButton = "Fermer";

        [Header("Messages")]
        [Tooltip("Bandeau quand la blanche tombe : le tir est annulé.")]
        public string scratchMessage = "Blanche empochée : tir annulé";

        [Header("Formats : {0}, {1}, {2} sont remplacés par les valeurs")]
        [Tooltip("Au-dessus de la jauge. {0} = puissance en %.")]
        public string powerFormat = "{0}%";
        [Tooltip("Titre du stage au-dessus de la table. {0} = stage actuel, {1} = nombre de stages.")]
        public string hudStageFormat = "Stage {0}";
        [Tooltip("Score et objectif au-dessus de la table. {0} = score, {1} = objectif.")]
        public string hudScoreFormat = "{0} / {1}";
        [Tooltip("Argent à côté de la pièce (dollars). {0} = montant.")]
        public string moneyFormat = "{0}";
        [Tooltip("HUD, colonne TIRS. {0} = tirs restants, {1} = tirs max.")]
        public string hudShotsFormat = "{0}/{1}";
        [Tooltip("Victoire, ligne 1. {0} = n° du stage.")]
        public string stageWonLine1 = "STAGE {0} RÉUSSI";
        [Tooltip("Victoire, ligne 2, plusieurs tirs restants. {0} = score, {1} = objectif, {2} = tirs restants.")]
        public string stageWonLine2 = "SCORE {0}/{1} - {2} TIRS RESTANTS";
        [Tooltip("Victoire, ligne 2, 0 ou 1 tir restant. {0} = score, {1} = objectif, {2} = tirs restants.")]
        public string stageWonLine2OneShot = "SCORE {0}/{1} - {2} TIR RESTANT";
        [Tooltip("Partie gagnée, ligne 1. {0} = nombre de stages.")]
        public string runWonLine1 = "PARTIE GAGNÉE";
        [Tooltip("Partie gagnée, ligne 2. {0} = nombre de stages.")]
        public string runWonLine2 = "LES {0} STAGES SONT RÉUSSIS";
        [Tooltip("Défaite, ligne 1. {0} = n° du stage.")]
        public string defeatLine1 = "PLUS DE TIRS AU STAGE {0}";
        [Tooltip("Défaite, ligne 2. {0} = score, {1} = objectif.")]
        public string defeatLine2 = "SCORE {0}/{1}";

        static GameTexts current;

        /// <summary>The asset assigned to GameBootstrap; code defaults if none.</summary>
        public static GameTexts Current
        {
            get
            {
                if (current == null)
                {
                    Debug.LogWarning("GameTexts non assigné à GameBootstrap : textes par défaut du code utilisés.");
                    current = CreateInstance<GameTexts>();
                }
                return current;
            }
            set => current = value;
        }

        /// <summary>One entry at random (empty entries skipped), or "" if there is none.</summary>
        public static string Pick(string[] variants)
        {
            if (variants == null || variants.Length == 0) return "";
            int start = Random.Range(0, variants.Length);
            for (int i = 0; i < variants.Length; i++)
            {
                string s = variants[(start + i) % variants.Length];
                if (!string.IsNullOrEmpty(s)) return s;
            }
            return "";
        }

        /// <summary>
        /// string.Format that never throws: a format broken in the Inspector (e.g. a lone brace) is shown
        /// as is, with a warning in the console.
        /// </summary>
        public static string Format(string format, params object[] values)
        {
            if (string.IsNullOrEmpty(format)) return "";
            try { return string.Format(format, values); }
            catch (System.FormatException)
            {
                Debug.LogWarning($"GameTexts : format invalide \"{format}\" (utiliser {{0}}, {{1}}… ; une accolade seule s'écrit {{{{ ou }}}}).");
                return format;
            }
        }

        /// <summary>Sets a TextMeshPro text (null-safe). In edit mode, records an Undo step and marks the object dirty.</summary>
        public static void Write(TMP_Text text, string value, bool inEditor = false)
        {
            if (text == null) return;
#if UNITY_EDITOR
            if (inEditor) UnityEditor.Undo.RecordObject(text, "GameTexts");
#endif
            text.text = value;
#if UNITY_EDITOR
            if (inEditor) UnityEditor.EditorUtility.SetDirty(text);
#endif
        }

#if UNITY_EDITOR
        /// <summary>Writes the fixed texts into the scene's TextMeshPro objects, to preview them in edit mode.</summary>
        [ContextMenu("Prévisualiser dans la scène")]
        void PreviewInScene()
        {
            foreach (var ui in FindObjectsByType<ShotUI>(FindObjectsInactive.Include)) ui.ApplyFixedTexts(this, true);
            foreach (var hud in FindObjectsByType<StageHUD>(FindObjectsInactive.Include)) hud.ApplyFixedTexts(this, true);
        }
#endif
    }
}
