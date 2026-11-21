using UnityEngine;
using UnityEngine.UI;
using RoguelikePool;
public static class SettingsTest {
  // 1 = click SettingsButton, 2 = click veil, 3 = click a menu placeholder (must stay open), 0 = state only
  public static string Run(int action) {
    var menu = Object.FindAnyObjectByType<SettingsMenu>(FindObjectsInactive.Include);
    var ui = Object.FindAnyObjectByType<ShotUI>();
    if (action == 1) menu.GetComponent<Button>().onClick.Invoke();
    if (action == 2) GameObject.Find("Overlays/SettingsPanel/Veil").GetComponent<Button>().onClick.Invoke();
    var b = menu.GetComponent<RectTransform>();
    Vector2 center = RectTransformUtility.WorldToScreenPoint(null, b.position);
    var stage = GameObject.Find("TopBar/StageTitle").GetComponent<TMPro.TMP_Text>().text;
    var score = GameObject.Find("TopBar/ScoreObjective").GetComponent<TMPro.TMP_Text>().text;
    var canvas = b.GetComponentInParent<Canvas>();
    return $"open={menu.IsOpen} overUI(settingsBtn)={ui.IsOverUI(center)} overUI(screen center)={ui.IsOverUI(new Vector2(Screen.width/2f, Screen.height/2f))} stage='{stage}' score='{score}' scale={canvas.scaleFactor:F3} settingsBtnScreen={center}";
  }
}
