using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoguelikePool
{
    /// <summary>
    /// Aiming and shooting. Press and drag (mouse left button or finger) to swing the cue around the
    /// cue ball: the cue sits on the pointer side, the ball leaves the opposite way. Fine aim (Shift,
    /// or the VISÉE FINE toggle) slows the rotation down. Power comes from <see cref="ShotUI"/>; the
    /// shot is fired with the TIRER button (or Space). Shooting is only possible when every ball has
    /// stopped and no cancelled shot is being rewound. A table snapshot is taken right before each shot.
    /// </summary>
    public class CueController : MonoBehaviour
    {
        [Header("Tir")]
        [Tooltip("Vitesse de la blanche à 100 % (u/s, 1 u = 10 cm → 45 = 4,5 m/s).")]
        public float maxShotSpeed = 45f;
        public float strikeDuration = 0.07f;

        [Header("Visée")]
        [Tooltip("Multiplicateur de rotation en visée fine.")]
        public float fineAimFactor = 0.1f;
        [Tooltip("Rotation aux flèches gauche/droite (°/s). Divisée par 10 en visée fine.")]
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

        float aimAngle;          // radians, direction the cue ball will travel
        float lastPointerAngle;
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
            aimAngle = Mathf.PI / 2f; // toward the rack (up)
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

            cue.gameObject.SetActive(true);
            SetCuePose(AimDirection, maxPullBack * ui.Power);
            preview.Show(cueBall, AimDirection, CushionRestitution, table.Pockets);
        }

        void HandlePointer()
        {
            var pointer = Pointer.current; // mouse in the Editor, primary touch on mobile
            if (pointer == null) return;

            Vector2 screen = pointer.position.ReadValue();
            Vector2 world = PixelCameraRig.Instance != null
                ? PixelCameraRig.Instance.ScreenToWorld(screen) // world is rendered into a low-res texture
                : (Vector2)cam.ScreenToWorldPoint(screen);
            Vector2 toBall = cueBall.Body.position - world;
            float pointerAngle = Mathf.Atan2(toBall.y, toBall.x);
            bool usable = toBall.sqrMagnitude > cueBall.Radius * cueBall.Radius; // angle is unstable on top of the ball

            if (pointer.press.wasPressedThisFrame && !ui.IsOverUI(screen))
            {
                aiming = true;
                lastPointerAngle = pointerAngle;
                if (!FineAim && usable) aimAngle = pointerAngle;
            }

            if (aiming && pointer.press.isPressed && usable)
            {
                if (FineAim)
                    aimAngle += Mathf.DeltaAngle(lastPointerAngle * Mathf.Rad2Deg, pointerAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad * fineAimFactor;
                else
                    aimAngle = pointerAngle;
                lastPointerAngle = pointerAngle;
            }

            if (pointer.press.wasReleasedThisFrame) aiming = false;
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
            if (kb.leftArrowKey.isPressed) aimAngle += speed;
            if (kb.rightArrowKey.isPressed) aimAngle -= speed;
            if (kb.upArrowKey.wasPressedThisFrame) ui.Power += 0.01f;
            if (kb.downArrowKey.wasPressedThisFrame) ui.Power -= 0.01f;
            if (kb.spaceKey.wasPressedThisFrame) Shoot();
        }

        public void Shoot()
        {
            if (!CanShoot || ui.Power < 0.005f) return;
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

            cueBall.Strike(direction * (power * maxShotSpeed));
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
