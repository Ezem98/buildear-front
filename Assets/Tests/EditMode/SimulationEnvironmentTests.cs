using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class SimulationEnvironmentTests
    {
        private const string EnvironmentPath =
            "Assets/XR/UserSimulationSettings/LargeSimulationEnvironment.prefab";
        private const string EnvironmentManagerPath =
            "Assets/XR/UserSimulationSettings/SimulationEnvironmentAssetsManager.asset";
        private const string RuntimePreferencesPath =
            "Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset";

        [Test]
        public void SimulationEnvironmentManager_UsesExpandedEnvironment()
        {
            Object manager = AssetDatabase.LoadMainAssetAtPath(EnvironmentManagerPath);
            Assert.That(manager, Is.Not.Null);

            SerializedObject serializedManager = new(manager);
            SerializedProperty paths = serializedManager.FindProperty("m_EnvironmentPrefabPaths");

            Assert.That(paths, Is.Not.Null);
            bool containsExpandedEnvironment = false;
            for (int index = 0; index < paths.arraySize; index++)
            {
                if (paths.GetArrayElementAtIndex(index).stringValue == EnvironmentPath)
                {
                    containsExpandedEnvironment = true;
                    break;
                }
            }

            Assert.That(containsExpandedEnvironment, Is.True);
            Assert.That(
                serializedManager.FindProperty("m_FallbackAtEndOfList").boolValue,
                Is.False
            );
        }

        [Test]
        public void RuntimeSimulationPreferences_SelectExpandedEnvironment()
        {
            Object preferences = AssetDatabase.LoadMainAssetAtPath(RuntimePreferencesPath);
            GameObject environment = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath);

            Assert.That(preferences, Is.Not.Null);
            Assert.That(environment, Is.Not.Null);

            SerializedObject serializedPreferences = new(preferences);
            Assert.That(
                serializedPreferences.FindProperty("m_EnvironmentPrefab").objectReferenceValue,
                Is.EqualTo(environment)
            );
            Assert.That(
                serializedPreferences.FindProperty("m_FallbackEnvironmentPrefab").objectReferenceValue,
                Is.EqualTo(environment)
            );
        }

        [Test]
        public void LargeSimulationEnvironment_HasExpandedFloorAndWall()
        {
            GameObject environment = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath);

            Assert.That(environment, Is.Not.Null);

            Transform floor = FindChild(environment.transform, "Floor");
            Transform wall = FindChild(environment.transform, "Wall");

            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.localScale.x, Is.EqualTo(10f));
            Assert.That(floor.localScale.z, Is.EqualTo(10f));
            Assert.That(wall, Is.Not.Null);
            Assert.That(wall.localPosition.x, Is.EqualTo(-5f));
            Assert.That(wall.localScale.z, Is.EqualTo(10f));

            bool hasMovementExtents = TryGetVector3PropertyOnRootComponents(
                environment,
                "m_CameraMovementBounds",
                "m_Extent",
                out Vector3 movementExtents
            );
            Assert.That(hasMovementExtents, Is.True);
            Assert.That(movementExtents.x, Is.EqualTo(5f));
            Assert.That(movementExtents.z, Is.EqualTo(5f));
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private static bool TryGetVector3PropertyOnRootComponents(
            GameObject root,
            string propertyName,
            string relativePropertyName,
            out Vector3 value
        )
        {
            foreach (Component component in root.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                SerializedObject serializedComponent = new(component);
                SerializedProperty property = serializedComponent.FindProperty(propertyName);
                if (property != null)
                {
                    value = property.FindPropertyRelative(relativePropertyName).vector3Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
