using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoguelikePool
{
    /// <summary>
    /// Aiming and shooting. Aim is relative: press anywhere on the table and drag around the cue ball;
    /// only the finger motion tangent to the cue ball turns the cue, at a fixed sensitivity (degrees per
    /// screen height), so precision no longer depends on how close the finger is to the ball. A quick
    /// tap (no drag) points the shot at the tapped spot. Fine aim (Shift, or the VISÉE FINE toggle)
    /// divides the sensitivity. The displayed/shot angle eases toward the input angle over a few
    /// milliseconds so integer pointer steps never show as notches.
    /// Power comes from <see cref="ShotUI"/>; the shot is fired with the TIRER button (or Space).
    /// Shooting is only possible when every ball has stopped and no cancelled shot is being rewound.
    /// A table snapshot is taken right before each shot.
    /// </summary>
    public class CueController : MonoBehaviour
    {
        [Header("Tir")]
        // Max Shot Speed lives in BallPhysicsSettings (Assets/Data/BallPhysicsSettings).
        public float strikeDuration = 0.07f;

        [Header("Visée")]
        [Tooltip("Rotation (°) pour un glissé tangentiel de toute la hauteur de l'écran.")]
        public float aimDegreesPerScreenHeight = 150f;
        [Tooltip("Multiplicateur de sensibilité en visée fine.")]
        public float fineAimFactor = 0.15f;
        [Tooltip("Lissage de la rotation affichée (s). 0 = aucun.")]
        public float aimSmoothing = 0.035f;
        [Tooltip("Un appui relâché sans glisser plus que ça (en fraction de hauteur d'écran) vise le point touché.")]
        public float tapMoveTolerance = 0.012f;
        public float tapMaxDuration = 0.3f;
        [Tooltip("Rotation aux flèches gauche/droite (°/s). Multipliée par le facteur de visée fine.")]
        public float keyboardAimSpeed = 30f;

        [Header("Canne (visuel)")]
        public float tipGap = 0.15f;
        public float maxPullBack = 2.4f;

        Ball cueBall;
        ShotUI ui;
        TrajectoryPreview preview;
        PoolTable table;
        TableRules rules;
        Camera cam;
        Transform cue;

        float aimAngle;          // radians, displayed/shot direction (eases toward targetAngle)
        float targetAngle;       // radians, direction set by the input
        Vector2 lastPointerScreen;
        Vector2 pressScreen;
        float pressTime;
        bool tapCandidate;
        bool aiming;
        bool striking;

        public Vector2 AimDirection => new Vector2(Mathf.Cos(aimAngle), Mathf.Sin(aimAngle));
        bool CanShoot => !striking && !rules.Busy && cueBall.gameObject.activeInHierarchy && Ball.AllStopped;
        bool FineAim => ui.FineAim || (Keyboard.current != null && Keyboard.current.shiftKey.isPressed);
        float CushionRestitution => cueBall.Material.bounciness * table.CushionMaterial.bounciness;

        public void Init(Ball cueBall, ShotUI ui, TrajectoryPreview preview, PoolTable table, TableRules rules)
        {
            this.cueBall = cueBall;
            this.ui = ui;
            this.preview = preview;
            this.table = table;
            this.rules = rules;
            cam = Camera.main;
            aimAngle = targetAngle = Mathf.PI / 2f; // toward the rack (up)
            ui.ShootClicked += Shoot;
            BuildCue();
        }

        void OnDestroy()
        {
            if (ui != null) ui.ShootClicked -= Shoot;
        }

        void Update()
        {
            if (cueBall == null) return;

            bool canShoot = CanShoot;
            ui.SetCanShoot(canShoot);
            if (!canShoot)
            {
                aiming = false;
                preview.Hide();
                if (!striking) cue.gameObject.SetActive(false);
                return;
            }

            HandlePointer();
            HandleKeyboardAndWheel();
            EaseAim();

            cue.gameObject.SetActive(true);
            SetCuePose(AimDirection, maxPullBack * ui.Power);
            preview.Show(cueBall, AimDirection, CushionRestitution, table.Pockets);
        }

        void HandlePointer()
        {
            var pointer = Pointer.current; // mouse in the Editor, primary touch on mobile
            if (pointer == null) return;
            Vector2 screen = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame && !ui.IsOverUI(screen))
            {
                aiming = true;
                tapCandidate = true;
                pressScreen = lastPointerScreen = screen;
                pressTime = Time.unscaledTime;
            }
            if (!aiming) return;

            if (pointer.press.isPressed)
            {
                if ((screen - pressScreen).magnitude > tapMoveTolerance * Screen.height) tapCandidate = false;

                // Rotation from the finger motion tangent to the cue ball, at a constant sensitivity.
                Vector2 ballScreen = WorldToScreen(cueBall.Body.position);
                Vector2 radial = lastPointerScreen - ballScreen;
                Vector2 delta = screen - lastPointerScreen;
                if (radial.sqrMagnitude > 1f && delta.sqrMagnitude > 0f)
                {
                    float tangential = (radial.x * delta.y - radial.y * delta.x) / radial.magnitude; // px, + = counter-clockwise
                    float degPerPixel = aimDegreesPerScreenHeight / Mathf.Max(1, Screen.height) * (FineAim ? fineAimFactor : 1f);
                    targetAngle += tangential * degPerPixel * Mathf.Deg2Rad;
                }
                lastPointerScreen = screen;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                // Quick tap without dragging: aim the cue ball at the tapped spot.
                if (tapCandidate && !FineAim && Time.unscaledTime - pressTime <= tapMaxDuration)
                {
                    Vector2 toTap = ScreenToWorld(screen) - cueBall.Body.position;
                    if (toTap.sqrMagnitude > cueBall.Radius * cueBall.Radius)
                        targetAngle = aimAngle + Mathf.DeltaAngle(aimAngle * Mathf.Rad2Deg, Mathf.Atan2(toTap.y, toTap.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                }
                aiming = false;
            }
        }

        /// <summary>Eases the displayed angle toward the input angle (frame-rate independent).</summary>
        void EaseAim()
        {
            if (aimSmoothing <= 0f) { aimAngle = targetAngle; return; }
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / aimSmoothing);
            aimAngle += (targetAngle - aimAngle) * k;
            if (Mathf.Abs(targetAngle - aimAngle) < 1e-5f) aimAngle = targetAngle;
        }

        Vector2 ScreenToWorld(Vector2 screen) => PixelCameraRig.Instance != null
            ? PixelCameraRig.Instance.ScreenToWorld(screen) // world is rendered into a low-res texture
            : (Vector2)cam.ScreenToWorldPoint(screen);

        Vector2 WorldToScreen(Vector2 world)
        {
            return PixelCameraRig.Instance != null
                ? PixelCameraRig.Instance.WorldToScreen(world)
                : (Vector2)cam.WorldToScreenPoint(world);
        }

        void HandleKeyboardAndWheel()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) ui.Power += Mathf.Sign(scroll) * 0.01f;
            }

            var kb = Keyboard.current;
            if (kb == null) return;
            float speed = keyboardAimSpeed * (FineAim ? fineAimFactor : 1f) * Mathf.Deg2Rad * Time.deltaTime;
            if (kb.leftArrowKey.isPressed) targetAngle += speed;
            if (kb.rightArrowKey.isPressed) targetAngle -= speed;
            if (kb.upArrowKey.wasPressedThisFrame) ui.Power += 0.01f;
            if (kb.downArrowKey.wasPressedThisFrame) ui.Power -= 0.01f;
            if (kb.spaceKey.wasPressedThisFrame) Shoot();
        }

        public void Shoot()
        {
            if (!CanShoot || ui.Power < 0.005f) return;
            aimAngle = targetAngle; // shoot exactly where the input points, even mid-ease
            rules.TakeSnapshot();
            StartCoroutine(StrikeRoutine(AimDirection, ui.Power));
        }

        IEnumerator StrikeRoutine(Vector2 direction, float power)
        {
            striking = true;
            aiming = false;
            preview.Hide();

            float startPull = maxPullBack * power;
            for (float t = 0f; t < strikeDuration; t += Time.deltaTime)
            {
                SetCuePose(direction, Mathf.Lerp(startPull, -tipGap, t / strikeDuration));
                yield return null;
            }
            SetCuePose(direction, -tipGap);

            cueBall.Strike(direction * (power * BallPhysicsSettings.Current.maxShotSpeed));
            striking = false;
        }

        /// <summary>Places the cue tip behind the ball; pull is the extra distance from the resting gap.</summary>
        void SetCuePose(Vector2 direction, float pull)
        {
            Vector2 tip = cueBall.Body.position - direction * (cueBall.Radius + tipGap + pull);
            cue.position = tip;
            cue.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        void BuildCue()
        {
            // Root sits at the tip; the cue extends along local -X. Total length ≈ 1.47 m.
            cue = new GameObject("Cue").transform;
            cue.SetParent(transform, false);
            CueArt.Build(cue);
            cue.gameObject.SetActive(false);
        }
    }
}
