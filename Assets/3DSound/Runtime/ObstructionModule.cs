using System;
using UnityEngine;

namespace AtsuSoundProject
{
    [Serializable]
    public sealed class ObstructionModule
    {
        [Serializable]
        public sealed class ObstructionModel
        {
            [Tooltip("遮蔽機能の有効/無効")]
            public bool enabled = true;

            [Tooltip("遮蔽判定対象レイヤー")]
            public LayerMask obstructionLayerMask = ~0;

            [Tooltip("レイの本数(1=中心のみ, 5=中心+上下左右)")]
            [Range(1, 5)]
            public int rayCount = 5;

            [Tooltip("複数レイの広がり半径(m)")]
            public float raySpreadRadius = 0.8f;

            [Tooltip("遮蔽チェック間隔(秒)")]
            public float updateInterval = 0.1f;

            [Tooltip("フィルター変化の滑らかさ")]
            public float smoothSpeed = 8f;

            [Tooltip("完全遮蔽時のLPFカットオフ周波数(Hz)")]
            public float minCutoffFrequency = 800f;

            [Tooltip("非遮蔽時のLPFカットオフ周波数(Hz)")]
            public float maxCutoffFrequency = 22000f;

            [Tooltip("厚さ1mあたりの減衰量(dB)")]
            public float dbPerMeter = 20f;

            [Tooltip("減衰上限(dB)")]
            public float maxAttenuationDb = 24f;

            [Tooltip("最小音量倍率")]
            [Range(0f, 1f)]
            public float minVolumeScale = 0.12f;

            [Tooltip("1秒あたりの最大減衰変化量(dB/s)")]
            public float maxDbChangePerSecond = 30f;
        }

        [SerializeField] private ObstructionModel _model = new ObstructionModel();

        public ObstructionModel Model => _model;
        public float CurrentObstruction => _currentObstruction;

        // Runtime state (non-serialized)
        private RaycastHit[] _hitBuffer;
        private float _timer;
        private float _currentObstruction;
        private float _targetAttenuationDb;
        private float _currentAttenuationDb;
        private float _currentCutoff;
        private float _currentVolumeScale;

        // Ray directions (calculated once per raycast update)
        private static readonly Vector3[] RayOffsets =
        {
            Vector3.zero,   // center
            Vector3.up,     // up
            Vector3.down,   // down
            Vector3.left,   // left
            Vector3.right   // right
        };

        public void Initialize()
        {
            _hitBuffer = new RaycastHit[16];
            // Distribute timer start to avoid frame spikes
            _timer = UnityEngine.Random.Range(0f, _model.updateInterval);
            _currentObstruction = 0f;
            _targetAttenuationDb = 0f;
            _currentAttenuationDb = 0f;
            _currentCutoff = _model.maxCutoffFrequency;
            _currentVolumeScale = 1f;
        }

        public void Update(
            Transform sourceTransform,
            Transform listenerTransform,
            AudioLowPassFilter lowPassFilter,
            AudioSource audioSource,
            float originalVolume)
        {
            if (!_model.enabled) return;

            var sourcePos = sourceTransform.position;
            var listenerPos = listenerTransform.position;
            var toListener = listenerPos - sourcePos;
            var sqrDistance = toListener.sqrMagnitude;

            // Distance culling — AudioSource.maxDistance を超えたら音自体が届かないためスキップ
            var maxDist = audioSource.maxDistance;
            if (sqrDistance > maxDist * maxDist)
            {
                ApplyNoObstruction(lowPassFilter, audioSource, originalVolume);
                return;
            }

            // Timer-based raycast (not every frame)
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _model.updateInterval;
                _targetAttenuationDb = CalculateAttenuationDb(
                    sourcePos, listenerPos, toListener,
                    sourceTransform, listenerTransform);
            }

            // レート制限付きスムージング（急激な変化を防止）
            var maxChange = _model.maxDbChangePerSecond * Time.deltaTime;
            var diff = Mathf.Clamp(_targetAttenuationDb - _currentAttenuationDb, -maxChange, maxChange);
            _currentAttenuationDb += diff;

            // dBから直接音量変換
            var volumeScale = Mathf.Max(Mathf.Pow(10f, -_currentAttenuationDb / 20f), _model.minVolumeScale);
            _currentVolumeScale = volumeScale;
            audioSource.volume = originalVolume * _currentVolumeScale;

            // DiffractionModule互換の0〜1遮蔽率
            _currentObstruction = Mathf.Clamp01(_currentAttenuationDb / _model.maxAttenuationDb);

            // LPFカットオフ
            var targetCutoff = Mathf.Lerp(_model.maxCutoffFrequency, _model.minCutoffFrequency, _currentObstruction);
            var lpfSmooth = Time.deltaTime * _model.smoothSpeed;
            _currentCutoff = Mathf.Lerp(_currentCutoff, targetCutoff, lpfSmooth);
            lowPassFilter.cutoffFrequency = _currentCutoff;
        }

        public void Reset(AudioLowPassFilter lowPassFilter, AudioSource audioSource, float originalVolume)
        {
            if (lowPassFilter != null)
            {
                lowPassFilter.cutoffFrequency = _model.maxCutoffFrequency;
            }
            if (audioSource != null)
            {
                audioSource.volume = originalVolume;
            }

            _currentObstruction = 0f;
            _targetAttenuationDb = 0f;
            _currentAttenuationDb = 0f;
            _currentCutoff = _model.maxCutoffFrequency;
            _currentVolumeScale = 1f;
        }

        public void DrawGizmos(Transform sourceTransform, Transform listenerTransform)
        {
            if (!_model.enabled || listenerTransform == null) return;

            var sourcePos = sourceTransform.position;
            var listenerPos = listenerTransform.position;
            var toListener = listenerPos - sourcePos;
            var distance = toListener.magnitude;

            if (distance < 0.01f) return;

            var direction = toListener / distance;
            var rotation = Quaternion.LookRotation(direction);

            var count = Mathf.Clamp(_model.rayCount, 1, RayOffsets.Length);
            for (var i = 0; i < count; i++)
            {
                var offset = rotation * (RayOffsets[i] * _model.raySpreadRadius);
                var rayOrigin = sourcePos + offset;

                var hit = Physics.Raycast(rayOrigin, direction, distance, _model.obstructionLayerMask);
                Gizmos.color = hit ? Color.red : Color.green;
                Gizmos.DrawLine(rayOrigin, listenerPos + offset);
            }
        }

        private float CalculateAttenuationDb(
            Vector3 sourcePos, Vector3 listenerPos, Vector3 toListener,
            Transform sourceTransform, Transform listenerTransform)
        {
            var distance = toListener.magnitude;
            if (distance < 0.01f) return 0f;

            var direction = toListener / distance;
            var rotation = Quaternion.LookRotation(direction);

            var count = Mathf.Clamp(_model.rayCount, 1, RayOffsets.Length);
            var totalDb = 0f;

            var sourceRoot = sourceTransform.root;
            var listenerRoot = listenerTransform.root;

            for (var i = 0; i < count; i++)
            {
                var offset = rotation * (RayOffsets[i] * _model.raySpreadRadius);
                var rayOrigin = sourcePos + offset;

                var hitCount = Physics.RaycastNonAlloc(
                    rayOrigin, direction, _hitBuffer, distance, _model.obstructionLayerMask);

                var rayDb = 0f;
                for (var h = 0; h < hitCount; h++)
                {
                    // 音源自身・リスナー自身のコライダーは除外
                    var hitRoot = _hitBuffer[h].collider.transform.root;
                    if (hitRoot == sourceRoot || hitRoot == listenerRoot)
                    {
                        continue;
                    }

                    var thickness = EstimateThickness(_hitBuffer[h].collider);
                    rayDb += thickness * _model.dbPerMeter;
                }

                rayDb = Mathf.Min(rayDb, _model.maxAttenuationDb);
                totalDb += rayDb;
            }

            // 全レイの平均dB
            var averageDb = totalDb / count;
            return Mathf.Min(averageDb, _model.maxAttenuationDb);
        }

        private float EstimateThickness(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                var localSize = box.size;
                var scale = box.transform.lossyScale;
                var worldSize = new Vector3(
                    localSize.x * Mathf.Abs(scale.x),
                    localSize.y * Mathf.Abs(scale.y),
                    localSize.z * Mathf.Abs(scale.z));
                return Mathf.Min(worldSize.x, Mathf.Min(worldSize.y, worldSize.z));
            }

            if (collider is SphereCollider sphere)
            {
                var scale = sphere.transform.lossyScale;
                var maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                return sphere.radius * 2f * maxScale;
            }

            if (collider is CapsuleCollider capsule)
            {
                var scale = capsule.transform.lossyScale;
                var maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                return capsule.radius * 2f * maxScale;
            }

            // MeshCollider等: boundsの最小寸法
            var bounds = collider.bounds;
            var size = bounds.size;
            return Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        }

        private void ApplyNoObstruction(AudioLowPassFilter lowPassFilter, AudioSource audioSource, float originalVolume)
        {
            _targetAttenuationDb = 0f;
            var dt = Time.deltaTime * _model.smoothSpeed;
            _currentAttenuationDb = Mathf.Lerp(_currentAttenuationDb, 0f, dt);

            var volumeScale = Mathf.Max(Mathf.Pow(10f, -_currentAttenuationDb / 20f), _model.minVolumeScale);
            _currentVolumeScale = volumeScale;
            audioSource.volume = originalVolume * _currentVolumeScale;

            _currentObstruction = Mathf.Clamp01(_currentAttenuationDb / _model.maxAttenuationDb);

            var targetCutoff = Mathf.Lerp(_model.maxCutoffFrequency, _model.minCutoffFrequency, _currentObstruction);
            _currentCutoff = Mathf.Lerp(_currentCutoff, targetCutoff, dt);
            lowPassFilter.cutoffFrequency = _currentCutoff;
        }
    }
}
