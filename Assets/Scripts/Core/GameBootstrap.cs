using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Entry point of the Pool scene: builds the table, the cue ball and the 10-ball rack, the shot UI,
    /// the rules and the cue controller at runtime, plus the pixel-art rendering setup (low-res
    /// camera rig, post-processing, ambient and impact effects).
    /// Physics2D project settings (zero gravity, low bounce threshold, 120 Hz) live in the project.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Part de la largeur d'écran occupée par le panneau de droite.")]
        [Range(0f, 0.4f)] public float uiPanelFraction = 0.2f;
        public Color backgroundColor = new Color(0.02f, 0.02f, 0.035f);

        void Awake()
        {
            var table = new GameObject("PoolTable").AddComponent<PoolTable>();

            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            var rig = cam.GetComponent<PixelCameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<PixelCameraRig>();
            rig.table = table;
            rig.rightPanelFraction = uiPanelFraction;
            PixelCameraRig.CreatePostProcessing(out var vignette);
            rig.Vignette = vignette;
            rig.Refresh();

            new GameObject("ImpactFX").AddComponent<ImpactFX>();
            AmbientMotes.Create(table);

            var cueBall = BallFactory.CreateCue(table.HeadSpot);
            BallFactory.CreateRack(table.FootSpot);

            var preview = new GameObject("TrajectoryPreview").AddComponent<TrajectoryPreview>();

            var ui = new GameObject("ShotUI").AddComponent<ShotUI>();
            ui.Build(uiPanelFraction, rig);

            var rules = new GameObject("TableRules").AddComponent<TableRules>();
            rules.Init(ui);

            var cue = new GameObject("CueController").AddComponent<CueController>();
            cue.Init(cueBall, ui, preview, table, rules);
        }
    }
}
