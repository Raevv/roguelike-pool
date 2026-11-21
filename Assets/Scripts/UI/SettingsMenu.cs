using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// The settings button (top left) opens <see cref="panel"/>, an overlay with a dark veil (it blocks
    /// aiming through <see cref="ShotUI.IsOverUI"/>) and placeholder buttons for Paramètres,
    /// Recommencer la partie and Quitter, which do nothing yet. Fermer or a tap on the veil closes it.
    /// Lives on the settings button, which stays active while the panel is hidden.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] Button openButton;
        [Tooltip("Panneau superposé (voile + menu), désactivé par défaut.")]
        [SerializeField] GameObject panel;
        [Tooltip("Voile plein écran : un appui dessus ferme le menu.")]
        [SerializeField] Button veilButton;
        [SerializeField] Button closeButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;

        [Header("Textes")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text optionsLabel;
        [SerializeField] TMP_Text restartLabel;
        [SerializeField] TMP_Text quitLabel;
        [SerializeField] TMP_Text closeLabel;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Start()
        {
            var texts = GameTexts.Current;
            GameTexts.Write(titleText, texts.settingsTitle);
            GameTexts.Write(optionsLabel, texts.settingsOptionsButton);
            GameTexts.Write(restartLabel, texts.settingsRestartButton);
            GameTexts.Write(quitLabel, texts.settingsQuitButton);
            GameTexts.Write(closeLabel, texts.settingsCloseButton);
            if (panel != null)
                foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true)) text.raycastTarget = false;

            if (openButton != null) openButton.onClick.AddListener(() => SetOpen(!IsOpen));
            if (veilButton != null) veilButton.onClick.AddListener(() => SetOpen(false));
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            // Placeholders: no behaviour yet.
            if (optionsButton != null) optionsButton.onClick.AddListener(() => Debug.Log("[Settings] Paramètres : pas encore fait."));
            if (restartButton != null) restartButton.onClick.AddListener(() => Debug.Log("[Settings] Recommencer la partie : pas encore fait."));
            if (quitButton != null) quitButton.onClick.AddListener(() => Debug.Log("[Settings] Quitter : pas encore fait."));
            SetOpen(false);
        }

        public void SetOpen(bool open)
        {
            if (panel != null) panel.SetActive(open);
            if (open && panel != null) panel.transform.SetAsLastSibling();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
