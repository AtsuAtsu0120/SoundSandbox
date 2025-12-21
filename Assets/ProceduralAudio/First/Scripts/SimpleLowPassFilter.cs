using System;
using System.Runtime.CompilerServices;

namespace ProceduralAudio.First
{
    public struct SimpleLowPassFilter
    {
        private float _prevOutput;
        private float _coefficient;

        /// <summary>
        /// フィルタのパラメータを更新します
        /// </summary>
        /// <param name="cutoffFreq">カットオフ周波数 (Hz)</param>
        /// <param name="sampleRate">サンプリングレート (Hz)</param>
        public void UpdateCoefficients(float cutoffFreq, float sampleRate)
        {
            if (cutoffFreq >= sampleRate * 0.5f)
            {
                // カットオフがナイキスト周波数以上の場合はスルーさせる
                _coefficient = 1.0f; 
                return;
            }

            // 時定数から係数を計算 (Time Constant approach)
            float dt = 1.0f / sampleRate;
            float rc = 1.0f / (2.0f * MathF.PI * cutoffFreq);
            _coefficient = dt / (rc + dt);
        }

        /// <summary>
        /// 1サンプルごとの処理
        /// インライン展開を推奨して関数呼び出しコストを下げる
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Process(float input)
        {
            // y[n] = y[n-1] + a * (x[n] - y[n-1])
            float output = _prevOutput + _coefficient * (input - _prevOutput);
            _prevOutput = output;
            return output;
        }

        /// <summary>
        /// 内部状態のリセット（音が途切れた時などに呼ぶ）
        /// </summary>
        public void Reset()
        {
            _prevOutput = 0.0f;
        }
    }
}