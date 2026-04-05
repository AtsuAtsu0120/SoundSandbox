using System.Collections;
using UnityEngine;

// コルーチン用
namespace GrooveWithTool.Runtime
{
    [RequireComponent(typeof(RectTransform))]
    public class UIGrooveAnimator : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Vector3 defaultScale;
    
        // 現在実行中のアニメーション（DOKillの代わりに使用）
        private Coroutine currentAnim;

        [Header("タメ（予備動作）の設定")]
        public Vector3 shrinkScale = new Vector3(0.85f, 0.85f, 1f);
        public float shrinkDuration = 0.15f;

        [Header("ハネ（ビート・ジャスト）の設定")]
        public Vector3 popScale = new Vector3(1.2f, 1.2f, 1f);
        public float popDuration = 0.3f;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            defaultScale = rectTransform.localScale;
        }

        public void PrepareForBeat()
        {
            // 実行中のアニメーションがあればキャンセル（DOKillの完全再現）
            if (currentAnim != null) StopCoroutine(currentAnim);
        
            // 現在のサイズから縮小目標へ、EaseOutQuadでアニメーション
            currentAnim = StartCoroutine(ScaleRoutine(rectTransform.localScale, shrinkScale, shrinkDuration, EaseOutQuad));
        }

        public void HitBeat()
        {
            if (currentAnim != null) StopCoroutine(currentAnim);
        
            // ビートの瞬間！一瞬で最大サイズにする
            rectTransform.localScale = popScale;
        
            // 最大サイズからデフォルトへ、EaseOutBack（ボヨーン）で戻る
            currentAnim = StartCoroutine(ScaleRoutine(popScale, defaultScale, popDuration, EaseOutBack));
        }

        // --- ここから下がDOTweenのコアロジックの自作部分 ---

        // 汎用アニメーションコルーチン
        private IEnumerator ScaleRoutine(Vector3 start, Vector3 end, float duration, System.Func<float, float> easeFunc)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
            
                // 0.0 〜 1.0 の進行度を計算
                float t = Mathf.Clamp01(time / duration);
            
                // イージング関数を通して動きにカーブ（タメ・ハネ）をつける
                float easedT = easeFunc(t);
            
                // Unclampedを使うことで、1.0を超えた「オーバーシュート」を許容する
                rectTransform.localScale = Vector3.LerpUnclamped(start, end, easedT);
            
                yield return null; // 1フレーム待機
            }
        
            // 最後はズレを防ぐためにカッチリ終わらせる
            rectTransform.localScale = end;
        }

        // イージング関数1: Ease.OutQuad (タメ用。自然に減速する)
        private float EaseOutQuad(float t)
        {
            return t * (2f - t);
        }

        // イージング関数2: Ease.OutBack (ハネ用。目標を一度通り越して戻る)
        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}