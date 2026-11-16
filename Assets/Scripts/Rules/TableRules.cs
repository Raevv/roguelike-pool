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

        /// <summary>True while a cancelled shot is being rewound: shooting is blocked.</summary>
        public bool Busy { get; private set; }

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
        }

        void OnPocketed(Ball ball, Pocket pocket)
        {
            if (ball.IsCue && !Busy) StartCoroutine(CancelShot());
        }

        IEnumerator CancelShot()
        {
            Busy = true;
            ui.ShowMessage("Blanche empochée : tir annulé", restoreDelay + 1.2f);
            yield return new WaitForSeconds(restoreDelay);
            foreach (var (ball, position) in snapshot) ball.ResetTo(position);
            Busy = false;
        }
    }
}
