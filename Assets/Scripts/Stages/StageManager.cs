using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Runs the stage loop: current stage, score, shots left, victory and defeat.
    ///
    /// - A shot starts when <see cref="TableRules.ShotTaken"/> fires and ends once every ball is still
    ///   (and no scratch rewind is running). Victory / defeat are only checked at the end of a shot.
    /// - Score: <see cref="pointsPerBall"/> per colored ball pocketed (deliberately simple for now).
    /// - Scratch: the shot is rewound by <see cref="TableRules"/>, so the points it scored are removed.
    ///   With <see cref="scratchCostsShot"/> the shot still counts as used.
    /// - Victory: score ≥ target before the shots run out. Defeat: shots exhausted below target.
    ///   Either way shooting is locked until <see cref="NextStage"/> or <see cref="RestartRun"/>.
    /// - Every stage start (first stage, next stage, restart) puts every ball back where it was when the
    ///   game started (cue ball on the head spot, the same rack), pocketed balls included.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public enum State { Playing, StageWon, RunWon, Lost }

        [Tooltip("Points par boule de couleur empochée.")]
        public int pointsPerBall = 1;
        [Tooltip("Une blanche empochée annule le tir (les boules reviennent) : ce tir compte-t-il quand même ?")]
        public bool scratchCostsShot = true;

        [Header("État (lecture seule, pour le debug)")]
        [SerializeField] int stageNumber;
        [SerializeField] int score;
        [Tooltip("Score montré dans le HUD : il ne monte que pendant la fusion des textes de score.")]
        [SerializeField] int displayedScore;
        [SerializeField] int target;
        [SerializeField] int shotsLeft;
        [SerializeField] State state;

        /// <summary>Raised whenever stage, score, shots left or state change (for the HUD).</summary>
        public event Action Changed;
        /// <summary>Raised once when the current stage is won (also on the last stage, with state RunWon).</summary>
        public event Action StageWon;
        /// <summary>Raised once when the shots run out below the target.</summary>
        public event Action StageLost;
        /// <summary>
        /// Raised when a colored ball scores during a shot, with the points it gave (built on
        /// <see cref="Ball.Pocketed"/>, which knows nothing about points). For the score popups.
        /// </summary>
        public event Action<Ball, Pocket, int> BallPocketed;
        /// <summary>Raised once per shot when every ball is still and no scratch rewind runs.</summary>
        public event Action ShotEnded;
        /// <summary>Raised at every stage start (first stage, next stage, restart), after the reset.</summary>
        public event Action StageStarted;

        public StageCatalog Catalog { get; private set; }
        /// <summary>0-based index of the current stage in the catalog.</summary>
        public int StageIndex { get; private set; }
        public StageData Stage => Catalog.Get(StageIndex);
        public int StageNumber => StageIndex + 1;
        public int StageCount => Catalog.Count;
        /// <summary>Real score, updated as soon as a ball is pocketed (used by the rules).</summary>
        public int Score => score;
        /// <summary>Score shown in the HUD ("Score / Objectif"): catches up with <see cref="Score"/> only during the merge.</summary>
        public int DisplayedScore => displayedScore;
        public int Target => Stage.targetScore;
        public int ShotsLeft => shotsLeft;
        public int MaxShots => Stage.maxShots;
        public State CurrentState => state;

        TableRules rules;
        bool shotInProgress;
        int shotPoints;
        float shotTime;
        readonly List<(Ball ball, Vector2 position)> startLayout = new List<(Ball, Vector2)>();

        public void Init(StageCatalog catalog, TableRules tableRules)
        {
            Catalog = catalog;
            rules = tableRules;
            // The balls were just created by GameBootstrap: remember the starting layout.
            startLayout.Clear();
            foreach (var ball in Ball.All) startLayout.Add((ball, ball.Body.position));
            rules.ShotTaken += OnShotTaken;
            rules.ShotCancelled += OnShotCancelled;
            Ball.Pocketed += OnPocketed;
            StartStage(0);
        }

        void OnDestroy()
        {
            if (rules != null)
            {
                rules.ShotTaken -= OnShotTaken;
                rules.ShotCancelled -= OnShotCancelled;
            }
            Ball.Pocketed -= OnPocketed;
        }

        /// <summary>After a won stage: go to the next one.</summary>
        [ContextMenu("Stage suivant")]
        public void NextStage()
        {
            if (state != State.StageWon) { Debug.Log("[Stage] Stage suivant ignoré : le stage n'est pas gagné."); return; }
            StartStage(StageIndex + 1);
        }

        /// <summary>Start the run over from stage 1.</summary>
        [ContextMenu("Recommencer")]
        public void RestartRun() => StartStage(0);

        void StartStage(int index)
        {
            StageIndex = index;
            score = 0;
            displayedScore = 0;
            shotsLeft = Stage.maxShots;
            shotInProgress = false;
            shotPoints = 0;
            state = State.Playing;
            rules.Locked = false;
            ResetBalls();
            Debug.Log($"[Stage] Stage {StageNumber}/{StageCount} : {Target} points en {MaxShots} tirs.");
            Refresh();
            StageStarted?.Invoke();
        }

        /// <summary>Puts every ball back at its starting position, at rest (pocketed balls come back too).</summary>
        void ResetBalls()
        {
            foreach (var (ball, position) in startLayout)
                if (ball != null) ball.ResetTo(position);
        }

        void OnShotTaken()
        {
            if (state != State.Playing) return;
            shotInProgress = true;
            shotPoints = 0;
            shotTime = 0f;
            shotsLeft--;
            Refresh();
        }

        void OnPocketed(Ball ball, Pocket pocket)
        {
            if (!shotInProgress || ball.IsCue) return;
            shotPoints += pointsPerBall;
            score += pointsPerBall;
            Refresh();
            BallPocketed?.Invoke(ball, pocket, pointsPerBall);
        }

        /// <summary>Adds merged points to the displayed score (never above the real score).</summary>
        public void AddDisplayedScore(int points)
        {
            displayedScore = Mathf.Min(displayedScore + points, score);
            Refresh();
        }

        /// <summary>Snaps the displayed score to the real score.</summary>
        public void SyncDisplayedScore()
        {
            if (displayedScore == score) return;
            displayedScore = score;
            Refresh();
        }

        void OnShotCancelled()
        {
            if (!shotInProgress) return;
            score -= shotPoints;
            Debug.Log($"[Stage] Blanche empochée : tir annulé, {shotPoints} point(s) retiré(s)" +
                      (scratchCostsShot ? ", le tir compte quand même." : ", le tir est rendu."));
            shotPoints = 0;
            displayedScore = Mathf.Min(displayedScore, score); // in case points of that shot were already merged
            if (!scratchCostsShot) shotsLeft++;
            Refresh();
        }

        void Update()
        {
            if (!shotInProgress) return;
            shotTime += Time.deltaTime;
            TryEndShot();
        }

        /// <summary>
        /// Ends the shot (and checks victory / defeat) if it is over: every ball still, no scratch rewind
        /// and no score merge running (both are part of rules.Busy). Called every frame and by the score
        /// popups' ScoreMergeCompleted, so the check runs right after the merge, never before.
        /// </summary>
        public void TryEndShot()
        {
            // The strike lands a few frames after ShotTaken (cue animation), so wait a moment before trusting "all stopped".
            if (!shotInProgress || shotTime < 0.25f || rules.Busy || !Ball.AllStopped) return;
            EndShot();
        }

        void EndShot()
        {
            shotInProgress = false;
            ShotEnded?.Invoke();
            // Normally the score popups' merge raises the displayed score; without them, catch up here.
            if (BallPocketed == null) SyncDisplayedScore();
            Debug.Log($"[Stage] Tir terminé : +{shotPoints} point(s), score {score}/{Target}, tirs restants {shotsLeft}/{MaxShots}.");
            if (score >= Target)
            {
                bool last = StageIndex >= StageCount - 1;
                state = last ? State.RunWon : State.StageWon;
                rules.Locked = true;
                Debug.Log(last ? $"[Stage] VICTOIRE : stage {StageNumber} gagné, c'était le dernier, partie gagnée !"
                               : $"[Stage] VICTOIRE : stage {StageNumber} gagné.");
                Refresh();
                StageWon?.Invoke();
            }
            else if (shotsLeft <= 0)
            {
                state = State.Lost;
                rules.Locked = true;
                Debug.Log($"[Stage] DÉFAITE : plus de tirs au stage {StageNumber} ({score}/{Target}).");
                Refresh();
                StageLost?.Invoke();
            }
        }

        void Refresh()
        {
            stageNumber = StageNumber;
            target = Target;
            Changed?.Invoke();
        }
    }
}
