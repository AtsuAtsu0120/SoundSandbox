using UnityEngine;

namespace GrooveWithTool.Runtime
{
    [RequireComponent(typeof(RectTransform))]
    public class UIGrooveContinuousReactor : MonoBehaviour
    {
        [Header("参照")]
        public MasterGrooveConductor conductor;

        [Header("アニメーション設定")]
        [Tooltip("拍の進行度(0-1)に対する動きの波形")]
        public AnimationCurve grooveCurve; 
    
        [Tooltip("スケールの最大倍率")]
        public float scaleMultiplier = 1.2f;

        private RectTransform rectTransform;
        private Vector3 initialScale;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            initialScale = rectTransform.localScale;
        
            // カーブが未設定の場合は、バウンドするデフォルトの山なりカーブを作成
            if (grooveCurve.length == 0) {
                grooveCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),   // ビートの瞬間（最大）
                    new Keyframe(0.2f, 0f), // すぐに元に戻る
                    new Keyframe(1f, 0f)    // 次のビートまで待機
                );
            }
        }

        private void Update()
        {
            if (conductor == null) return;

            // 1. コンダクターから進行度（0.0 〜 1.0）を取得
            float progress = conductor.beatProgress;

            // 2. カーブを通して「うねり」の値に変換
            float curveValue = grooveCurve.Evaluate(progress);

            // 3. 目標スケールを計算（curveValue が 1 なら scaleMultiplier倍、0 なら等倍）
            Vector3 targetScale = initialScale * Mathf.Lerp(1.0f, scaleMultiplier, curveValue);

            // 4. Lerpを使って滑らかに追従させる（ここでマイルドさ・余韻が出る）
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * 20f);
        }
    }
}