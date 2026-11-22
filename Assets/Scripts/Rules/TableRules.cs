using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Shot-level rules. A snapshot of every ball on the table is taken when a shot is confirmed;
    /// if the cue ball is pocketed during the shot, the shot is cancelled and the snapshot restored
    /// (including any object ball pocketed by that shot).
    /// </summary>
    public class TableRules : MonoBehaviour
    {
        [Tooltip("Délai entre la chute de la blanche et la remise en place (pour voir ce qui s'est passé).")]
        public float restoreDelay = 0.9f;

        /// <summary>True while a cancelled shot is being rewound, or while <see cref="Locked"/>: shooting is blocked.</summary>
        public bool Busy => rewinding || Locked;

        /// <summary>True while a scratched shot is being rewound.</summary>
        public bool Rewinding => rewinding;

        /// <summary>Set by the stage flow to forbid shooting (stage won or lost).</summary>
        public bool Locked { get; set; }

        /// <summary>Raised when a shot is confirmed, right before the cue strikes.</summary>
        public event Action ShotTaken;
        /// <summary>Raised after a scratched shot has been rewound (every ball back where it was).</summary>
        public event Action ShotCancelled;

        bool rewinding;

        readonly List<(Ball ball, Vector2 position)> snapshot = new List<(Ball, Vector2)>();
        ShotUI ui;

        public void Init(ShotUI shotUI)
        {
            ui = shotUI;
            Ball.Pocketed += OnPocketed;
        }

        void OnDestroy() => Ball.Pocketed -= OnPocketed;

        /// <summary>Call right before the cue strikes.</summary>
        public void TakeSnapshot()
        {
            snapshot.Clear();
            foreach (var ball in Ball.All) snapshot.Add((ball, ball.Body.position));
            ShotTaken?.Invoke();
        }

        void OnPocketed(Ball ball, Pocket pocket)
        {
            if (ball.IsCue && !rewinding) StartCoroutine(CancelShot());
        }

        IEnumerator CancelShot()
        {
            rewinding = true;
            ui.ShowMessage(GameTexts.Current.scratchMessage, restoreDelay + 1.2f);
            yield return new WaitForSeconds(restoreDelay);
            foreach (var (ball, position) in snapshot) ball.ResetTo(position);
            rewinding = false;
            ShotCancelled?.Invoke();
        }
    }
}
