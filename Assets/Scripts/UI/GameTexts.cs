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

        [Header("Messages")]
        [Tooltip("Bandeau quand la blanche tombe : le tir est annulé.")]
        public string scratchMessage = "Blanche empochée : tir annulé";

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
