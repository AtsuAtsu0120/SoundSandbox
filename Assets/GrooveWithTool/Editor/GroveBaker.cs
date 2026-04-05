using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using GrooveWithTool.Runtime;

namespace GrooveWithTool.Editor
{
    public class GrooveBaker : UnityEditor.Editor
    {
        [MenuItem("Assets/Bake Beat Data (Procedural Loop Ver)")]
        public static void BakeAudioClip()
        {
            var clip = Selection.activeObject as AudioClip;
            if (clip == null) return;

            var timestamps = AnalyzeClipWithFFT(clip);
            SaveToScriptableObject(clip, timestamps);
        }

        [MenuItem("Assets/Bake Beat Data (Procedural Loop Ver)", true)]
        public static bool ValidateBake()
        {
            return Selection.activeObject is AudioClip;
        }

        private static List<float> AnalyzeClipWithFFT(AudioClip clip)
        {
            var beats = new List<float>();

            var channels = clip.channels;
            var frequency = clip.frequency;
            var samples = new float[clip.samples * channels];
            clip.GetData(samples, 0);

            // --- パラメータ（ループ素材向け調整済み） ---
            var fftSize = 1024;
            var lowFreqBinEnd = 5;
            var historySize = 43;
            var thresholdMultiplier = 1.5f;
            var cooldownSeconds = 0.2f;

            var fluxHistory = new List<float>();

            // 修正点：ループ素材の「0.0秒の頭のキック」を絶対逃さないための初期値
            var lastBeatTime = -999f;
            var previousLowFreqEnergy = 0f;

            var real = new float[fftSize];
            var imag = new float[fftSize];

            for (var i = 0; i < samples.Length - (fftSize * channels); i += fftSize * channels)
            {
                for (var j = 0; j < fftSize; j++)
                {
                    var sample = samples[i + (j * channels)];
                    var multiplier = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * j / (fftSize - 1)));
                    real[j] = sample * multiplier;
                    imag[j] = 0f;
                }

                ExecuteFFT(real, imag);

                var currentLowFreqEnergy = 0f;
                for (var k = 0; k < lowFreqBinEnd; k++)
                {
                    var magnitude = Mathf.Sqrt(real[k] * real[k] + imag[k] * imag[k]);
                    currentLowFreqEnergy += magnitude;
                }

                var flux = Mathf.Max(0, currentLowFreqEnergy - previousLowFreqEnergy);
                previousLowFreqEnergy = currentLowFreqEnergy;

                var localAverageFlux = 0f;
                if (fluxHistory.Count > 0)
                {
                    var sum = 0f;
                    foreach (var f in fluxHistory)
                    {
                        sum += f;
                    }
                    localAverageFlux = sum / fluxHistory.Count;
                }

                var currentTime = (float)i / (frequency * channels);

                // 初回のfluxや、平均を大きく上回った場合を検知
                if (flux > localAverageFlux * thresholdMultiplier && (currentTime - lastBeatTime) > cooldownSeconds)
                {
                    beats.Add(currentTime);
                    lastBeatTime = currentTime;
                }

                fluxHistory.Add(flux);
                if (fluxHistory.Count > historySize) fluxHistory.RemoveAt(0);
            }

            Debug.Log($"ベイク完了: {clip.name} から {beats.Count} 個のビートを抽出しました。");
            return beats;
        }

        private static void ExecuteFFT(float[] real, float[] imag)
        {
            var n = real.Length;
            var j = 0;
            for (var i = 0; i < n - 1; i++)
            {
                if (i < j)
                {
                    float tr = real[i], ti = imag[i];
                    real[i] = real[j];
                    imag[i] = imag[j];
                    real[j] = tr;
                    imag[j] = ti;
                }

                var m = n / 2;
                while (m >= 1 && j >= m)
                {
                    j -= m;
                    m /= 2;
                }

                j += m;
            }

            for (var l = 1; l < n; l *= 2)
            {
                var angle = -Mathf.PI / l;
                float wtr = Mathf.Cos(angle), wti = Mathf.Sin(angle);
                for (var i = 0; i < n; i += 2 * l)
                {
                    float wr = 1, wi = 0;
                    for (var k = 0; k < l; k++)
                    {
                        int p = i + k, q = i + k + l;
                        var tr = wr * real[q] - wi * imag[q];
                        var ti = wr * imag[q] + wi * real[q];
                        real[q] = real[p] - tr;
                        imag[q] = imag[p] - ti;
                        real[p] += tr;
                        imag[p] += ti;
                        var nwr = wr * wtr - wi * wti;
                        wi = wr * wti + wi * wtr;
                        wr = nwr;
                    }
                }
            }
        }

        private static void SaveToScriptableObject(AudioClip clip, List<float> timestamps)
        {
            var clipPath = AssetDatabase.GetAssetPath(clip);
            var savePath = Path.Combine(Path.GetDirectoryName(clipPath), clip.name + "_BeatData.asset");
            var data = CreateInstance<AudioClipInfo>();
            data.SetData(clip, timestamps);
            
            AssetDatabase.CreateAsset(data, savePath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(data);
        }
    }
}