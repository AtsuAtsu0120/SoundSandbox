using System;
using UnityEngine;

namespace AtsuSoundProject
{
    [Serializable]
    public sealed class DiffractionModule
    {
        [Serializable]
        public sealed class DiffractionModel
        {
            [Tooltip("回折機能の有効/無効")]
            public bool enabled = true;

            [Tooltip("回折判定対象レイヤー")]
            public LayerMask diffractionLayerMask = ~0;

            [Tooltip("エッジ探索の水平プローブ数(片側)")]
            [Range(4, 16)]
            public int horizontalProbeCount = 8;

            [Tooltip("エッジ探索の垂直プローブ数(片側)")]
            [Range(2, 8)]
            public int verticalProbeCount = 4;

            [Tooltip("プローブの最大角度(度)")]
            [Range(30f, 150f)]
            public float maxProbeAngle = 90f;

            [Tooltip("最大回折次数(何回角を曲がれるか)")]
            [Range(1, 3)]
            public int maxDiffractionOrder = 2;

            [Tooltip("回折チェック間隔(秒)")]
            public float updateInterval = 0.15f;

            [Tooltip("フィルター変化の滑らかさ")]
            public float smoothSpeed = 6f;

            [Tooltip("回折角あたりの減衰(dB/rad)")]
            public float attenuationPerRadian = 6f;

            [Tooltip("回折時の最小LPFカットオフ周波数(Hz)")]
            public float minCutoffFrequency = 1000f;

            [Tooltip("非回折時のLPFカットオフ周波数(Hz)")]
            public float maxCutoffFrequency = 22000f;

            [Tooltip("回折時の最小音量倍率")]
            [Range(0f, 1f)]
            public float minVolumeScale = 0.15f;

            [Tooltip("音の定位を回折エッジ方向にリダイレクトする")]
            public bool redirectSpatialization = true;
        }

        [SerializeField] private DiffractionModel _model = new DiffractionModel();

        public DiffractionModel Model => _model;

        // Runtime state
        private float _timer;
        private float _currentCutoff;
        private float _currentVolumeScale;
        private float _currentPan;
        private float _targetCutoff;
        private float _targetVolumeScale;
        private float _targetPan;
        private bool _hasDiffractionPath;
        private DiffractionPath _lastPath;

        private struct DiffractionPath
        {
            public bool found;
            public int order;
            public Vector3 edgePoint1;
            public Vector3 edgePoint2;
            public float totalAngle;
            public float totalPathLength;
        }

        public void Initialize()
        {
            _timer = UnityEngine.Random.Range(0f, _model.updateInterval);
            _currentCutoff = _model.maxCutoffFrequency;
            _currentVolumeScale = 1f;
            _currentPan = 0f;
            _targetCutoff = _model.maxCutoffFrequency;
            _targetVolumeScale = 1f;
            _targetPan = 0f;
            _hasDiffractionPath = false;
            _lastPath = default;
        }

        public void Update(
            Transform sourceTransform,
            Transform listenerTransform,
            AudioLowPassFilter lowPassFilter,
            AudioSource audioSource,
            float originalVolume,
            float obstructionRatio)
        {
            if (!_model.enabled) return;

            // 遮蔽が弱ければ回折不要
            if (obstructionRatio < 0.1f)
            {
                ResetSmooth(lowPassFilter, audioSource, originalVolume);
                return;
            }

            var sourcePos = sourceTransform.position;
            var listenerPos = listenerTransform.position;

            // タイマーベースの回折パス探索
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _model.updateInterval;

                _lastPath = FindDiffractionPath(sourcePos, listenerPos, _model.maxDiffractionOrder);
                _hasDiffractionPath = _lastPath.found;

                if (_hasDiffractionPath)
                {
                    var directDistance = Vector3.Distance(sourcePos, listenerPos);
                    CalculateAttenuation(_lastPath, directDistance, out _targetVolumeScale, out _targetCutoff);

                    if (_model.redirectSpatialization)
                    {
                        _targetPan = CalculatePan(listenerTransform, _lastPath);
                    }
                    else
                    {
                        _targetPan = 0f;
                    }
                }
            }

            if (!_hasDiffractionPath)
            {
                // 回折パスなし → 回折のフロア値で遮蔽の過剰減衰を緩和
                // （現実では音は常にある程度回折する）
                var dtFallback = Time.deltaTime * _model.smoothSpeed;
                var floorVolume = _model.minVolumeScale;
                if (audioSource.volume < originalVolume * floorVolume)
                {
                    _currentVolumeScale = Mathf.Lerp(_currentVolumeScale, floorVolume, dtFallback);
                    audioSource.volume = originalVolume * _currentVolumeScale;
                }
                var floorCutoff = _model.minCutoffFrequency;
                if (lowPassFilter.cutoffFrequency < floorCutoff)
                {
                    _currentCutoff = Mathf.Lerp(_currentCutoff, floorCutoff, dtFallback);
                    lowPassFilter.cutoffFrequency = _currentCutoff;
                }
                return;
            }

            // スムーズ補間して適用（遮蔽の値を上書き）
            var dt = Time.deltaTime * _model.smoothSpeed;

            _currentCutoff = Mathf.Lerp(_currentCutoff, _targetCutoff, dt);
            lowPassFilter.cutoffFrequency = _currentCutoff;

            _currentVolumeScale = Mathf.Lerp(_currentVolumeScale, _targetVolumeScale, dt);
            audioSource.volume = originalVolume * _currentVolumeScale;

            if (_model.redirectSpatialization)
            {
                _currentPan = Mathf.Lerp(_currentPan, _targetPan, dt);
                audioSource.panStereo = _currentPan;
            }
        }

        public void Reset(AudioLowPassFilter lowPassFilter, AudioSource audioSource, float originalVolume)
        {
            _currentCutoff = _model.maxCutoffFrequency;
            _currentVolumeScale = 1f;
            _currentPan = 0f;
            _hasDiffractionPath = false;

            if (audioSource != null)
                audioSource.panStereo = 0f;
        }

        public void DrawGizmos(Transform sourceTransform, Transform listenerTransform)
        {
            if (!_model.enabled || listenerTransform == null) return;
            if (!_lastPath.found) return;

            var sourcePos = sourceTransform.position;
            var listenerPos = listenerTransform.position;

            // 回折パスを黄色で描画
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(sourcePos, _lastPath.edgePoint1);
            Gizmos.DrawWireSphere(_lastPath.edgePoint1, 0.15f);

            if (_lastPath.order >= 2)
            {
                Gizmos.DrawLine(_lastPath.edgePoint1, _lastPath.edgePoint2);
                Gizmos.DrawWireSphere(_lastPath.edgePoint2, 0.15f);
                Gizmos.DrawLine(_lastPath.edgePoint2, listenerPos);
            }
            else
            {
                Gizmos.DrawLine(_lastPath.edgePoint1, listenerPos);
            }

            // 直線パス（遮蔽）を半透明赤で描画
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawLine(sourcePos, listenerPos);
        }

        private DiffractionPath FindDiffractionPath(Vector3 sourcePos, Vector3 listenerPos, int maxOrder)
        {
            var path = new DiffractionPath();

            // 1次回折: 音源からエッジを探す
            var toListener = listenerPos - sourcePos;
            var distance = toListener.magnitude;
            if (distance < 0.01f) return path;

            var direction = toListener / distance;
            var edge1 = FindEdgePoint(sourcePos, listenerPos, direction, distance);

            if (edge1 == Vector3.zero) return path;

            // Edge1からリスナーへの直線チェック
            var edge1ToListener = listenerPos - edge1;
            var edge1ToListenerDist = edge1ToListener.magnitude;

            if (edge1ToListenerDist < 0.01f)
            {
                // エッジがリスナーにほぼ一致
                path.found = true;
                path.order = 1;
                path.edgePoint1 = edge1;
                path.totalAngle = 0f;
                path.totalPathLength = Vector3.Distance(sourcePos, edge1);
                return path;
            }

            var edge1ToListenerDir = edge1ToListener / edge1ToListenerDist;
            var edge1ToListenerBlocked = Physics.Raycast(
                edge1, edge1ToListenerDir, edge1ToListenerDist, _model.diffractionLayerMask);

            if (!edge1ToListenerBlocked)
            {
                // 1次回折成功
                path.found = true;
                path.order = 1;
                path.edgePoint1 = edge1;
                path.totalAngle = CalculateDiffractionAngle(sourcePos, edge1, listenerPos);
                path.totalPathLength = Vector3.Distance(sourcePos, edge1) + edge1ToListenerDist;
                return path;
            }

            // 2次回折を試行
            if (maxOrder < 2) return path;

            var edge2 = FindEdgePoint(edge1, listenerPos, edge1ToListenerDir, edge1ToListenerDist);
            if (edge2 == Vector3.zero) return path;

            // Edge2からリスナーへの直線チェック
            var edge2ToListener = listenerPos - edge2;
            var edge2ToListenerDist = edge2ToListener.magnitude;

            if (edge2ToListenerDist < 0.01f)
            {
                path.found = true;
                path.order = 2;
                path.edgePoint1 = edge1;
                path.edgePoint2 = edge2;
                path.totalAngle = CalculateDiffractionAngle(sourcePos, edge1, edge2);
                path.totalPathLength = Vector3.Distance(sourcePos, edge1) + Vector3.Distance(edge1, edge2);
                return path;
            }

            var edge2ToListenerDir = edge2ToListener / edge2ToListenerDist;
            var edge2ToListenerBlocked = Physics.Raycast(
                edge2, edge2ToListenerDir, edge2ToListenerDist, _model.diffractionLayerMask);

            if (!edge2ToListenerBlocked)
            {
                // 2次回折成功
                path.found = true;
                path.order = 2;
                path.edgePoint1 = edge1;
                path.edgePoint2 = edge2;
                path.totalAngle = CalculateDiffractionAngle(sourcePos, edge1, edge2)
                                + CalculateDiffractionAngle(edge1, edge2, listenerPos);
                path.totalPathLength = Vector3.Distance(sourcePos, edge1)
                                     + Vector3.Distance(edge1, edge2)
                                     + edge2ToListenerDist;
                return path;
            }

            return path;
        }

        private Vector3 FindEdgePoint(Vector3 origin, Vector3 targetPos, Vector3 blockedDir, float maxDistance)
        {
            // 座標フレームを構築
            var forward = blockedDir.normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(Vector3.forward, forward);
            right.Normalize();
            var up = Vector3.Cross(forward, right);

            var hCount = _model.horizontalProbeCount;
            var maxAngleRad = _model.maxProbeAngle * Mathf.Deg2Rad;

            var bestAngle = float.MaxValue;
            var bestOpenDir = Vector3.zero;
            var bestBlockedDir = forward;

            // Phase A: 水平粗探索
            for (var h = -hCount; h <= hCount; h++)
            {
                if (h == 0) continue; // 中心は遮蔽済み

                var azim = (h / (float)hCount) * maxAngleRad;
                var probeDir = Quaternion.AngleAxis(azim * Mathf.Rad2Deg, up) * forward;

                var hit = Physics.Raycast(origin, probeDir, maxDistance, _model.diffractionLayerMask);

                if (!hit)
                {
                    // この方向から目標に到達できるか簡易チェック
                    var angleToTarget = Vector3.Angle(probeDir, forward);
                    if (angleToTarget < bestAngle)
                    {
                        bestAngle = angleToTarget;
                        bestOpenDir = probeDir;
                    }
                }
                else
                {
                    // 遮蔽方向のうち、最もopenに近いものを追跡
                    if (bestOpenDir != Vector3.zero)
                    {
                        var angleToBest = Vector3.Angle(probeDir, bestOpenDir);
                        var currentBlockedAngle = Vector3.Angle(bestBlockedDir, bestOpenDir);
                        if (angleToBest < currentBlockedAngle)
                        {
                            bestBlockedDir = probeDir;
                        }
                    }
                }
            }

            if (bestOpenDir == Vector3.zero)
            {
                // 水平探索で見つからなければ垂直も試す
                return FindEdgePointVertical(origin, forward, up, right, maxDistance);
            }

            // Phase B: 二分精密化
            return RefineEdge(origin, bestBlockedDir, bestOpenDir, maxDistance, 3);
        }

        private Vector3 FindEdgePointVertical(
            Vector3 origin, Vector3 forward, Vector3 up, Vector3 right, float maxDistance)
        {
            var vCount = _model.verticalProbeCount;
            var maxAngleRad = _model.maxProbeAngle * Mathf.Deg2Rad * 0.5f;

            var bestAngle = float.MaxValue;
            var bestOpenDir = Vector3.zero;
            var bestBlockedDir = forward;

            for (var v = -vCount; v <= vCount; v++)
            {
                if (v == 0) continue;

                var elev = (v / (float)vCount) * maxAngleRad;
                var probeDir = Quaternion.AngleAxis(elev * Mathf.Rad2Deg, right) * forward;

                var hit = Physics.Raycast(origin, probeDir, maxDistance, _model.diffractionLayerMask);

                if (!hit)
                {
                    var angleToTarget = Vector3.Angle(probeDir, forward);
                    if (angleToTarget < bestAngle)
                    {
                        bestAngle = angleToTarget;
                        bestOpenDir = probeDir;
                    }
                }
                else
                {
                    if (bestOpenDir != Vector3.zero)
                    {
                        var angleToBest = Vector3.Angle(probeDir, bestOpenDir);
                        var currentBlockedAngle = Vector3.Angle(bestBlockedDir, bestOpenDir);
                        if (angleToBest < currentBlockedAngle)
                        {
                            bestBlockedDir = probeDir;
                        }
                    }
                }
            }

            if (bestOpenDir == Vector3.zero) return Vector3.zero;

            return RefineEdge(origin, bestBlockedDir, bestOpenDir, maxDistance, 3);
        }

        private Vector3 RefineEdge(
            Vector3 origin, Vector3 blockedDir, Vector3 openDir, float distance, int iterations)
        {
            var blocked = blockedDir.normalized;
            var open = openDir.normalized;

            for (var i = 0; i < iterations; i++)
            {
                var midDir = Vector3.Slerp(blocked, open, 0.5f);
                var hit = Physics.Raycast(origin, midDir, distance, _model.diffractionLayerMask);

                if (hit)
                    blocked = midDir;
                else
                    open = midDir;
            }

            // エッジ位置を推定: 遮蔽側のヒットポイントからわずかにオープン側へオフセット
            var edgeDir = Vector3.Slerp(blocked, open, 0.5f);
            RaycastHit hitInfo;
            if (Physics.Raycast(origin, blocked, out hitInfo, distance, _model.diffractionLayerMask))
            {
                // ヒットポイントからオープン方向に少しずらす
                var offsetDir = (open - blocked).normalized;
                return hitInfo.point + offsetDir * 0.1f;
            }

            // フォールバック: 中間方向に距離の半分の位置
            return origin + edgeDir * distance * 0.5f;
        }

        private float CalculateDiffractionAngle(Vector3 from, Vector3 edge, Vector3 to)
        {
            var inDir = (edge - from).normalized;
            var outDir = (to - edge).normalized;
            // 回折角 = 入射方向と出射方向のなす角
            var angleDeg = Vector3.Angle(inDir, outDir);
            return angleDeg * Mathf.Deg2Rad;
        }

        private void CalculateAttenuation(
            DiffractionPath path, float directDistance,
            out float volumeScale, out float cutoffFrequency)
        {
            // 回折角による減衰 (dB)
            var attenuationDb = path.totalAngle * _model.attenuationPerRadian;
            volumeScale = Mathf.Pow(10f, -attenuationDb / 20f);

            // 経路長による追加減衰（逆距離則）
            var pathRatio = path.totalPathLength / Mathf.Max(directDistance, 0.01f);
            if (pathRatio > 1f)
            {
                volumeScale /= pathRatio;
            }

            volumeScale = Mathf.Clamp(volumeScale, _model.minVolumeScale, 1f);

            // LPF: 回折角が大きいほど高域が減衰
            var angleRatio = Mathf.Clamp01(path.totalAngle / Mathf.PI);
            cutoffFrequency = Mathf.Lerp(_model.maxCutoffFrequency, _model.minCutoffFrequency, angleRatio);
        }

        private float CalculatePan(Transform listenerTransform, DiffractionPath path)
        {
            // エッジ方向をリスナーのローカル座標に変換し、左右のパンに反映
            var edgePos = path.order >= 2 ? path.edgePoint2 : path.edgePoint1;
            var toEdge = edgePos - listenerTransform.position;
            var localDir = listenerTransform.InverseTransformDirection(toEdge.normalized);

            // localDir.x が左右方向 (-1=左, +1=右)
            return Mathf.Clamp(localDir.x, -1f, 1f);
        }

        private void ResetSmooth(AudioLowPassFilter lowPassFilter, AudioSource audioSource, float originalVolume)
        {
            _hasDiffractionPath = false;

            var dt = Time.deltaTime * _model.smoothSpeed;

            // panStereoを0に戻す
            if (_model.redirectSpatialization && Mathf.Abs(_currentPan) > 0.001f)
            {
                _currentPan = Mathf.Lerp(_currentPan, 0f, dt);
                audioSource.panStereo = _currentPan;
            }

            _targetCutoff = _model.maxCutoffFrequency;
            _targetVolumeScale = 1f;
            _targetPan = 0f;
        }
    }
}
