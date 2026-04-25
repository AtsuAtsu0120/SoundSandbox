using UnityEditor;
using UnityEngine;

namespace AtsuSoundProject.Editor
{
    [CustomEditor(typeof(CustomAudioSource))]
    public sealed class CustomAudioSourceEditor : UnityEditor.Editor
    {
        private SerializedProperty _obstructionProp;
        private SerializedProperty _obstructionModelProp;
        private SerializedProperty _enabledProp;

        private SerializedProperty _diffractionProp;
        private SerializedProperty _diffractionModelProp;
        private SerializedProperty _diffractionEnabledProp;

        private bool _obstructionFoldout = true;
        private bool _diffractionFoldout = true;

        private void OnEnable()
        {
            _obstructionProp = serializedObject.FindProperty("_obstruction");
            _obstructionModelProp = _obstructionProp.FindPropertyRelative("_model");
            _enabledProp = _obstructionModelProp.FindPropertyRelative("enabled");

            _diffractionProp = serializedObject.FindProperty("_diffraction");
            _diffractionModelProp = _diffractionProp.FindPropertyRelative("_model");
            _diffractionEnabledProp = _diffractionModelProp.FindPropertyRelative("enabled");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawObstructionModule();
            DrawDiffractionModule();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawObstructionModule()
        {
            EditorGUILayout.Space(4);

            // Foldout header with toggle
            EditorGUILayout.BeginHorizontal();

            _obstructionFoldout = EditorGUILayout.Foldout(_obstructionFoldout, "Obstruction", true, EditorStyles.foldoutHeader);

            // Enable toggle on the right
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(_enabledProp.boolValue, GUILayout.Width(16));
            if (EditorGUI.EndChangeCheck())
                _enabledProp.boolValue = enabled;

            EditorGUILayout.EndHorizontal();

            if (!_obstructionFoldout) return;

            EditorGUI.indentLevel++;

            // Skip "enabled" since it's already drawn as toggle
            DrawObstructionProperty("obstructionLayerMask", "Layer Mask");
            DrawObstructionProperty("useMultiRay", "Use Multi Ray");

            var useMultiRay = _obstructionModelProp.FindPropertyRelative("useMultiRay");
            if (useMultiRay != null && useMultiRay.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawObstructionProperty("rayCount", "Ray Count");
                DrawObstructionProperty("raySpreadRadius", "Ray Spread Radius");
                EditorGUI.indentLevel--;
            }

            DrawObstructionProperty("updateInterval", "Update Interval");
            DrawObstructionProperty("smoothSpeed", "Smooth Speed");
            DrawObstructionProperty("minCutoffFrequency", "Min Cutoff (Hz)");
            DrawObstructionProperty("maxCutoffFrequency", "Max Cutoff (Hz)");
            DrawObstructionProperty("dbPerMeter", "Attenuation (dB/m)");
            DrawObstructionProperty("maxAttenuationDb", "Max Attenuation (dB)");
            DrawObstructionProperty("minVolumeScale", "Min Volume Scale");
            DrawObstructionProperty("maxDbChangePerSecond", "Max dB Change/sec");

            EditorGUI.indentLevel--;
        }

        private void DrawDiffractionModule()
        {
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            _diffractionFoldout = EditorGUILayout.Foldout(_diffractionFoldout, "Diffraction", true, EditorStyles.foldoutHeader);

            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(_diffractionEnabledProp.boolValue, GUILayout.Width(16));
            if (EditorGUI.EndChangeCheck())
                _diffractionEnabledProp.boolValue = enabled;

            EditorGUILayout.EndHorizontal();

            if (!_diffractionFoldout) return;

            EditorGUI.indentLevel++;

            DrawDiffractionProperty("diffractionLayerMask", "Layer Mask");
            DrawDiffractionProperty("horizontalProbeCount", "Horizontal Probes");
            DrawDiffractionProperty("verticalProbeCount", "Vertical Probes");
            DrawDiffractionProperty("maxProbeAngle", "Max Probe Angle");
            DrawDiffractionProperty("maxDiffractionOrder", "Max Diffraction Order");
            DrawDiffractionProperty("updateInterval", "Update Interval");
            DrawDiffractionProperty("smoothSpeed", "Smooth Speed");
            DrawDiffractionProperty("attenuationPerRadian", "Attenuation (dB/rad)");
            DrawDiffractionProperty("minCutoffFrequency", "Min Cutoff (Hz)");
            DrawDiffractionProperty("maxCutoffFrequency", "Max Cutoff (Hz)");
            DrawDiffractionProperty("minVolumeScale", "Min Volume Scale");
            DrawDiffractionProperty("redirectSpatialization", "Redirect Spatialization");

            EditorGUI.indentLevel--;
        }

        private void DrawObstructionProperty(string propertyName, string label)
        {
            var prop = _obstructionModelProp.FindPropertyRelative(propertyName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }

        private void DrawDiffractionProperty(string propertyName, string label)
        {
            var prop = _diffractionModelProp.FindPropertyRelative(propertyName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }
    }
}
