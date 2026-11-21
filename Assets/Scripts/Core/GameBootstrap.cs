using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Entry point of the Pool scene: builds the table, the cue ball and the 10-ball rack, the shot UI,
    /// the rules, the stage loop and the cue controller at runtime, plus the pixel-art rendering setup (low-res
    /// camera rig, post-processing, ambient and impact effects).
    /// Physics2D project settings (zero gravity, low bounce threshold, 120 Hz) live in the project.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // before the table and cue placed in the scene, so BallPhysicsSettings.Current is set first
    public class GameBootstrap : MonoBehaviour
    {
        public Color backgroundColor = new Color(0.02f, 0.02f, 0.035f);
        [Tooltip("Liste ordonnée des stages de la partie (Assets/Data/StageCatalog).")]
        public StageCatalog stageCatalog;
        [Tooltip("Réglages physiques des boules : puissance max, masse, frottement et rebond (blanche / couleurs), rebond des bandes (Assets/Data/BallPhysicsSettings).")]
        public BallPhysicsSettings ballPhysics;
        [Tooltip("Prefab de la table (Assets/Prefabs/PoolTable), instancié si la scène ne contient pas déjà une PoolTable.")]
        public PoolTable tablePrefab;
        [Tooltip("Prefab de la queue (Assets/Prefabs/Cue), instancié si la scène ne contient pas déjà une queue.")]
        public CueStick cuePrefab;
        [Header("Interface (objets de la scène, Tools > Roguelike Pool > 8 Build UI)")]
        public ShotUI shotUI;
        public StageHUD stageHUD;
        public StageScreens stageScreens;
        [Tooltip("Tous les textes du jeu : titres, phrases, boutons, formats (Assets/Data/GameTexts).")]
        public GameTexts gameTexts;
        [Tooltip("Zone de l'UI où la table doit tenir (TableAnchor, avec son TableFitter).")]
        public TableFitter tableFitter;
        [Tooltip("Shader d'agrandissement net du rendu basse résolution (Assets/Art/Shaders/PixelUpscale).")]
        public Shader pixelUpscaleShader;

        void Awake()
        {
            if (ballPhysics != null) BallPhysicsSettings.Current = ballPhysics;
            else Debug.LogWarning("GameBootstrap : BallPhysicsSettings non assigné, valeurs par défaut du code utilisées.");
            if (gameTexts != null) GameTexts.Current = gameTexts; // otherwise GameTexts.Current warns and uses code defaults

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
            rig.upscaleShader = pixelUpscaleShader;
            PixelCameraRig.CreatePostProcessing(out var vignette);
            rig.Vignette = vignette;
            rig.Refresh();

            new GameObject("ImpactFX").AddComponent<ImpactFX>();
            AmbientMotes.Create(table);

            var cueBall = BallFactory.CreateCue(table.HeadSpot);
            BallFactory.CreateRack(table.FootSpot);

            var preview = new GameObject("TrajectoryPreview").AddComponent<TrajectoryPreview>();

            var ui = shotUI != null ? shotUI : FindAnyObjectByType<ShotUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogError("GameBootstrap : aucune ShotUI dans la scène (Tools > Roguelike Pool > 8 Build UI).");
                return;
            }
            ui.Init();

            // The camera frames the table inside the UI's TableAnchor (needs the canvas layout computed).
            var fitter = tableFitter != null ? tableFitter : FindAnyObjectByType<TableFitter>(FindObjectsInactive.Include);
            Canvas.ForceUpdateCanvases();
            if (fitter != null) fitter.Init(table, rig);
            else Debug.LogWarning("GameBootstrap : aucun TableFitter dans l'UI, la table remplit l'écran.");

            var rules = new GameObject("TableRules").AddComponent<TableRules>();
            rules.Init(ui);

            if (stageCatalog != null && stageCatalog.Count > 0 && stageCatalog.stages.TrueForAll(s => s != null))
            {
                var stages = new GameObject("StageManager").AddComponent<StageManager>();
                stages.Init(stageCatalog, rules);
                var hud = stageHUD != null ? stageHUD : FindAnyObjectByType<StageHUD>(FindObjectsInactive.Include);
                var screens = stageScreens != null ? stageScreens : FindAnyObjectByType<StageScreens>(FindObjectsInactive.Include);
                if (hud != null) hud.Init(stages);
                else Debug.LogError("GameBootstrap : aucun StageHUD dans la scène.");
                if (screens != null) screens.Init(ui, stages);
                else Debug.LogError("GameBootstrap : aucun StageScreens dans la scène.");
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
