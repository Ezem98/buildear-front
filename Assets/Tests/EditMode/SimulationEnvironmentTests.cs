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

        [Test]
        public void SimulationEnvironmentManager_UsesExpandedEnvironment()
        {
            Object manager = AssetDatabase.LoadMainAssetAtPath(EnvironmentManagerPath);
            Assert.That(manager, Is.Not.Null);

            SerializedObject serializedManager = new(manager);
            SerializedProperty paths = serializedManager.FindProperty("m_EnvironmentPrefabPaths");

            Assert.That(paths, Is.Not.Null);
            Assert.That(paths.arraySize, Is.EqualTo(1));
            Assert.That(paths.GetArrayElementAtIndex(0).stringValue, Is.EqualTo(EnvironmentPath));
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
    }
}
