using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Entry point of the Pool scene: builds the table, the cue ball and the 10-ball rack, the shot UI,
    /// the rules, the stage loop and the cue controller at runtime, plus the pixel-art rendering setup (low-res
    /// camera rig, post-processing, ambient and impact effects).
    /// Physics2D project settings (zero gravity, low bounce threshold, 120 Hz) live in the project.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Part de la largeur d'écran occupée par le panneau de droite.")]
        [Range(0f, 0.4f)] public float uiPanelFraction = 0.2f;
        public Color backgroundColor = new Color(0.02f, 0.02f, 0.035f);
        [Tooltip("Liste ordonnée des stages de la partie (Assets/Data/StageCatalog).")]
        public StageCatalog stageCatalog;
        [Tooltip("Réglages physiques des boules : puissance max, masse, frottement et rebond (blanche / couleurs), rebond des bandes (Assets/Data/BallPhysicsSettings).")]
        public BallPhysicsSettings ballPhysics;
        [Tooltip("Prefab de la table (Assets/Prefabs/PoolTable), instancié si la scène ne contient pas déjà une PoolTable.")]
        public PoolTable tablePrefab;
        [Tooltip("Prefab de la queue (Assets/Prefabs/Cue), instancié si la scène ne contient pas déjà une queue.")]
        public CueStick cuePrefab;

        void Awake()
        {
            if (ballPhysics != null) BallPhysicsSettings.Current = ballPhysics;
            else Debug.LogWarning("GameBootstrap : BallPhysicsSettings non assigné, valeurs par défaut du code utilisées.");

            // The table placed in the scene, otherwise an instance of the prefab.
            var table = FindAnyObjectByType<PoolTable>();
            if (table == null)
            {
                if (tablePrefab == null)
                {
                    Debug.LogError("GameBootstrap : aucune PoolTable dans la scène et Table Prefab non assigné.");
                    return;
                }
                table = Instantiate(tablePrefab);
                table.name = tablePrefab.name;
            }

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

            if (stageCatalog != null && stageCatalog.Count > 0 && stageCatalog.stages.TrueForAll(s => s != null))
            {
                var stages = new GameObject("StageManager").AddComponent<StageManager>();
                stages.Init(stageCatalog, rules);
                new GameObject("StageHUD").AddComponent<StageHUD>().Build(ui.TableArea, stages);
                new GameObject("StageScreens").AddComponent<StageScreens>().Build(ui, stages);
            }
            else
                Debug.LogError("GameBootstrap : StageCatalog manquant, vide ou avec un stage vide : pas de boucle de stages.");

            // The cue placed in the scene, otherwise an instance of the prefab.
            var cueStick = FindAnyObjectByType<CueStick>(FindObjectsInactive.Include);
            if (cueStick == null)
            {
                if (cuePrefab == null)
                {
                    Debug.LogError("GameBootstrap : aucune queue (CueStick) dans la scène et Cue Prefab non assigné.");
                    return;
                }
                cueStick = Instantiate(cuePrefab);
                cueStick.name = cuePrefab.name;
            }

            var cue = new GameObject("CueController").AddComponent<CueController>();
            cue.Init(cueBall, ui, preview, table, rules, cueStick);
        }
    }
}
