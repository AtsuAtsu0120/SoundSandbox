using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GrooveWithTool.Runtime
{
    public class ScheduledBeat
    {
        public double absoluteTime;
        public bool lookaheadTriggered;
    }

    public class MasterGrooveConductor : MonoBehaviour
    {
        // 【線】他のスクリプトが毎フレーム読み取るための進行度（連続波用）
        public float beatProgress { get; private set; }
        
        [Header("タイミング設定")] [Tooltip("ビートの何秒前に予備動作（Lookahead）を発火するか")] 
        [SerializeField] private float _lookaheadTime = 0.15f;

        [Header("【点】イベント登録（トリガー用）")] 
        [SerializeField] private UnityEvent _onLookahead; // タメ
        [SerializeField] private UnityEvent _onBeatJust; // ハネ

        // スケジューラーから追加される「未来のビート予定表」
        private List<ScheduledBeat> upcomingBeats = new List<ScheduledBeat>();

        // 進行度(0.0~1.0)の計算に使う「最後に通過したビートの絶対時間」
        private double lastBeatAbsoluteTime = 0.0;

        private void Start()
        {
            // 初期化：ゲーム開始時の時間を最初の基準点にする
            lastBeatAbsoluteTime = AudioSettings.dspTime;
        }

        /// <summary>
        /// ProceduralMusicScheduler等の外部スクリプトから予定を追加するメソッド
        /// </summary>
        public void ScheduleBeats(AudioClipInfo data, double startDspTime)
        {
            if (data == null) return;

            foreach (var localTime in data.BeatTimestamps)
            {
                upcomingBeats.Add(new ScheduledBeat
                {
                    absoluteTime = startDspTime + localTime,
                    lookaheadTriggered = false
                });
            }

            // 時間順にソート（予定が前後しないように）
            upcomingBeats.Sort((a, b) => a.absoluteTime.CompareTo(b.absoluteTime));
        }

        private void Update()
        {
            if (upcomingBeats.Count == 0) return;

            var currentDspTime = AudioSettings.dspTime;

            // --- 1. 【線】進行度（BeatProgress）の計算 ---
            // 常にリストの先頭 upcomingBeats[0] が「次のビート」
            var nextBeatTime = upcomingBeats[0].absoluteTime;
            var interval = nextBeatTime - lastBeatAbsoluteTime;

            if (interval > 0)
            {
                // 前回ビート 〜 次回ビート の間で今どこにいるか (0.0 〜 1.0)
                beatProgress = Mathf.Clamp01((float)((currentDspTime - lastBeatAbsoluteTime) / interval));
            }

            // --- 2. 【点】イベントの監視と発火 ---
            var i = 0;
            while (i < upcomingBeats.Count)
            {
                var beat = upcomingBeats[i];

                // 予備動作（Lookahead）
                if (!beat.lookaheadTriggered && currentDspTime >= beat.absoluteTime - _lookaheadTime)
                {
                    _onLookahead?.Invoke();
                    beat.lookaheadTriggered = true;
                }

                // ジャストタイミング
                if (currentDspTime >= beat.absoluteTime)
                {
                    _onBeatJust?.Invoke();

                    // ★次の波の計算のために、通過したビートの時間を記録
                    lastBeatAbsoluteTime = beat.absoluteTime;

                    // 瞬間的に進行度をリセット
                    beatProgress = 0f;

                    // 消化した予定をリストから削除
                    upcomingBeats.RemoveAt(i);
                }
                else
                {
                    // まだ時間が来ていなければ次の予定の確認へ
                    i++;
                }
            }
        }
    }
}