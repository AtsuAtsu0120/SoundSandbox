using UnityEngine;

namespace Groove
{
    public class GrooveReactor : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private GrooveConductor conductor; // 前述のConductorクラス

        [Header("Settings")]
        [Tooltip("拍の進行度(0-1)をどう動きに変換するか")]
        [SerializeField] private AnimationCurve grooveCurve; 
    
        [Tooltip("動きの強さ")]
        [SerializeField] private Vector3 punchScale = new Vector3(0.5f, 0.5f, 0.5f);
    
        private Vector3 initialScale;

        void Start()
        {
            initialScale = transform.localScale;
        
            // デフォルトでバウンドするようなカーブを作っておく（インスペクタで調整推奨）
            if (grooveCurve.length == 0) {
                grooveCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.1f, 0), new Keyframe(1, 0));
            }
        }

        void Update()
        {
            if (conductor == null) return;

            // 1. 拍の進行度（0.0 〜 1.0）を計算
            // 例: 4.25拍目なら 0.25 を取得
            float beatProgress = conductor.SongPositionInBeats % 1.0f;

            // 2. カーブを通して「グルーヴ値」に変換
            // BeatProgressが 0 のとき（拍の頭）、カーブの値に応じて最大化させるなど
            float curveValue = grooveCurve.Evaluate(beatProgress);

            // 3. オブジェクトに適用（ここではスケール）
            // curveValue が 1 なら大きく、0 なら元のサイズ
            transform.localScale = initialScale + (punchScale * curveValue);
        }
    }
}