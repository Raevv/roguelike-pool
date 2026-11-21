using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoguelikePool
{
    /// <summary>
    /// Aiming and power with the cue. Press on the table and drag: the finger motion is split along the
    /// cue axis. Only a drag that starts on the cue itself (within cueTouchMargin of its axis) can change
    /// the power; a drag started anywhere else only aims, and presses outside the screen are ignored.
    /// Motion across the cue turns it at a fixed sensitivity (degrees per screen height):
    /// a drag that starts behind the cue ball (on the cue side) moves the cue itself (drag left → the butt
    /// goes left → the shot aims right), a drag that starts in front of it moves the aim point instead
    /// (drag left → aim left). Motion along the cue pulls it back (away from the shot
    /// direction: more power) or pushes it toward the ball (less power), at a fixed rate (power per
    /// screen height). With <see cref="lockDragAxis"/>, the first pixels of each drag decide whether it
    /// aims or sets the power, so aiming never changes the power by accident. A quick tap (no drag)
    /// points the shot at the tapped spot. Fine aim (Shift, or the VISÉE FINE toggle) divides both
    /// sensitivities. The displayed/shot angle eases toward the input angle over a few milliseconds so
    /// integer pointer steps never show as notches.
    /// The power gauge (<see cref="ShotUI"/>) shows the power and can also set it directly; the shot is fired with the TIRER
    /// button (or Space).
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

        [Header("Puissance (reculer la queue)")]
        [Tooltip("Puissance gagnée en reculant la queue de toute la hauteur de l'écran (2.5 = 0 → 100 % sur 40 % de l'écran).")]
        public float powerPerScreenHeight = 2.5f;
        [Tooltip("Multiplicateur de la vitesse de réglage de la puissance en visée fine.")]
        public float finePowerFactor = 0.25f;
        [Tooltip("Chaque glissé fait soit viser, soit régler la puissance, selon sa direction au départ.")]
        public bool lockDragAxis = true;
        [Tooltip("Distance (fraction de hauteur d'écran) au-delà de laquelle le glissé choisit son axe.")]
        public float axisLockDistance = 0.015f;
        [Tooltip("Pour régler la puissance, le glissé doit commencer sur la queue : distance max entre le doigt et l'axe de la queue (pixels d'écran à 1080×1920, proportionnel ailleurs). Ailleurs, le glissé ne fait que viser.")]
        public float cueTouchMargin = 70f;

        [Header("Canne (visuel)")]
        public float tipGap = 0.15f;
        public float maxPullBack = 2.4f;

        Ball cueBall;
        ShotUI ui;
        TrajectoryPreview preview;
        PoolTable table;
        TableRules rules;
        Camera cam;
        CueStick cue;

        float aimAngle;          // radians, displayed/shot direction (eases toward targetAngle)
        float targetAngle;       // radians, direction set by the input
        Vector2 lastPointerScreen;
        Vector2 pressScreen;
        float pressTime;
        bool tapCandidate;
        bool dragOnCue;
        enum DragAxis { Undecided, Aim, Power }
        DragAxis dragAxis;
        float aimSign = 1f;      // +1: the drag moves the cue (pressed behind the ball), -1: it moves the aim point (in front)
        bool aiming;
        bool striking;

        public Vector2 AimDirection => new Vector2(Mathf.Cos(aimAngle), Mathf.Sin(aimAngle));
        bool CanShoot => !striking && !rules.Busy && cueBall.gameObject.activeInHierarchy && Ball.AllStopped;
        bool FineAim => ui.FineAim || (Keyboard.current != null && Keyboard.current.shiftKey.isPressed);
        float CushionRestitution => cueBall.Material.bounciness * table.CushionMaterial.bounciness;

        public void Init(Ball cueBall, ShotUI ui, TrajectoryPreview preview, PoolTable table, TableRules rules, CueStick cue)
        {
            this.cueBall = cueBall;
            this.ui = ui;
            this.preview = preview;
            this.table = table;
            this.rules = rules;
            cam = Camera.main;
            aimAngle = targetAngle = Mathf.PI / 2f; // toward the rack (up)
            ui.ShootClicked += Shoot;
            this.cue = cue;
            cue.gameObject.SetActive(false);
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

            // Presses outside the game screen (e.g. Editor toolbars of the Game view) are ignored.
            if (pointer.press.wasPressedThisFrame && OnScreen(screen) && !ui.IsOverUI(screen))
            {
                aiming = true;
                // Only a drag that starts on the cue (within cueTouchMargin of its axis) can set the power;
                // anywhere else the drag only aims. A tap on the cue does not aim at it.
                dragOnCue = IsOnCue(screen);
                tapCandidate = !dragOnCue;
                dragAxis = dragOnCue && lockDragAxis ? DragAxis.Undecided : DragAxis.Aim;
                // Pressed in front of the cue ball (shot side): the drag moves the aim point, so the
                // rotation is reversed compared with a drag from behind, which moves the cue itself.
                Vector2 fromBall = screen - WorldToScreen(cueBall.Body.position);
                aimSign = Vector2.Dot(fromBall, new Vector2(Mathf.Cos(targetAngle), Mathf.Sin(targetAngle))) > 0f ? -1f : 1f;
                pressScreen = lastPointerScreen = screen;
                pressTime = Time.unscaledTime;
            }
            if (!aiming) return;

            if (pointer.press.isPressed)
            {
                float screenHeight = Mathf.Max(1, Screen.height);
                Vector2 fromPress = screen - pressScreen;
                if (fromPress.magnitude > tapMoveTolerance * screenHeight) tapCandidate = false;

                // Cue axis on screen (the world is rendered with a uniform scale, so directions match).
                Vector2 back = new Vector2(-Mathf.Cos(targetAngle), -Mathf.Sin(targetAngle)); // toward the cue butt
                Vector2 side = new Vector2(-back.y, back.x);                                   // butt moving this way turns the aim counter-clockwise

                if (dragAxis == DragAxis.Undecided)
                {
                    // Wait until the drag is long enough, then keep the dominant axis for the whole drag.
                    if (fromPress.magnitude > axisLockDistance * screenHeight)
                    {
                        dragAxis = Mathf.Abs(Vector2.Dot(fromPress, back)) > Mathf.Abs(Vector2.Dot(fromPress, side))
                            ? DragAxis.Power : DragAxis.Aim;
                        ApplyDrag(fromPress, back, side, screenHeight); // the lock distance itself counts
                    }
                }
                else
                {
                    ApplyDrag(screen - lastPointerScreen, back, side, screenHeight);
                }
                if (dragAxis != DragAxis.Undecided) lastPointerScreen = screen;
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

        /// <summary>
        /// Applies a pointer motion (screen pixels) to the cue: the part across the cue turns it, the part
        /// along it pulls it back (more power) or pushes it in (less). With the axis lock only the drag's
        /// chosen axis is used.
        /// </summary>
        void ApplyDrag(Vector2 delta, Vector2 back, Vector2 side, float screenHeight)
        {
            if (delta.sqrMagnitude <= 0f) return;
            bool both = !lockDragAxis && dragOnCue; // power only from a drag that started on the cue
            if (both || dragAxis == DragAxis.Aim)
            {
                float degPerPixel = aimDegreesPerScreenHeight / screenHeight * (FineAim ? fineAimFactor : 1f);
                targetAngle += aimSign * Vector2.Dot(delta, side) * degPerPixel * Mathf.Deg2Rad;
            }
            if (both || dragAxis == DragAxis.Power)
            {
                float powerPerPixel = powerPerScreenHeight / screenHeight * (FineAim ? finePowerFactor : 1f);
                ui.Power += Vector2.Dot(delta, back) * powerPerPixel;
            }
        }

        static bool OnScreen(Vector2 p) => p.x >= 0f && p.y >= 0f && p.x < Screen.width && p.y < Screen.height;

        /// <summary>True if the screen point is within <see cref="cueTouchMargin"/> of the drawn cue (tip → butt).</summary>
        bool IsOnCue(Vector2 screen)
        {
            if (cue == null || !cue.gameObject.activeInHierarchy || cue.stick == null) return false;
            Vector2 dir = new Vector2(Mathf.Cos(aimAngle), Mathf.Sin(aimAngle));
            Vector2 tip = cue.stick.position;
            Vector2 butt = tip - dir * cue.Length;
            Vector2 a = WorldToScreen(tip), b = WorldToScreen(butt);
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(screen - a, ab) / ab.sqrMagnitude) : 0f;
            float margin = cueTouchMargin * Screen.height / 1920f;
            return (screen - (a + ab * t)).sqrMagnitude <= margin * margin;
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
            float power = ui.Power;
            ui.Power = 0f; // every shot starts with the cue at rest
            StartCoroutine(StrikeRoutine(AimDirection, power));
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
            cue.Pose(cueBall.Body.position, direction, cueBall.Radius + tipGap + pull);
        }
    }
}
