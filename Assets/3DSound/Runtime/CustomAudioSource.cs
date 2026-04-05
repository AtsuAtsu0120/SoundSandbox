using UnityEngine;

namespace AtsuSoundProject
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(AudioLowPassFilter))]
    public sealed class CustomAudioSource : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioLowPassFilter _lowPassFilter;
        private Transform _listener;
        private float _originalVolume;

        [SerializeField] private ObstructionModule _obstruction = new ObstructionModule();
        [SerializeField] private DiffractionModule _diffraction = new DiffractionModule();

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _lowPassFilter = GetComponent<AudioLowPassFilter>();
            _originalVolume = _audioSource.volume;

            // Find AudioListener in scene
            var listenerComponent = FindFirstObjectByType<AudioListener>();
            if (listenerComponent != null)
            {
                _listener = listenerComponent.transform;
            }

            // Playerレイヤーをレイキャスト対象から除外
            var playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                var excludeMask = ~(1 << playerLayer);
                _obstruction.Model.obstructionLayerMask &= excludeMask;
                _diffraction.Model.diffractionLayerMask &= excludeMask;
            }

            _obstruction.Initialize();
            _diffraction.Initialize();
        }

        private void Update()
        {
            if (!_audioSource.isPlaying) return;
            if (_listener == null) return;

            // 1. 遮蔽を先に計算・適用
            _obstruction.Update(transform, _listener, _lowPassFilter, _audioSource, _originalVolume);

            // 2. 遮蔽されていれば回折を計算し、値を上書き
            _diffraction.Update(
                transform, _listener, _lowPassFilter, _audioSource,
                _originalVolume, _obstruction.CurrentObstruction);
        }

        private void OnDisable()
        {
            _obstruction.Reset(_lowPassFilter, _audioSource, _originalVolume);
            _diffraction.Reset(_lowPassFilter, _audioSource, _originalVolume);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_listener == null)
            {
                // Editor preview: try to find listener
                var listenerComponent = FindFirstObjectByType<AudioListener>();
                if (listenerComponent != null)
                {
                    _obstruction.DrawGizmos(transform, listenerComponent.transform);
                    _diffraction.DrawGizmos(transform, listenerComponent.transform);
                }
                return;
            }

            _obstruction.DrawGizmos(transform, _listener);
            _diffraction.DrawGizmos(transform, _listener);
        }
#endif
    }
}
