using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace BuildeAR.Tests.EditMode
{
    public class MobileBuildSessionTests
    {
        private readonly List<GameObject> objectsToDestroy = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in objectsToDestroy)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void ConfigureSpawnerForSelectedModel_UsesExactBackendModelId()
        {
            ObjectSpawner spawner = CreateSpawner();
            spawner.objectPrefabsIndex = new List<int> { 3, 1, 7, 9 };

            bool configured = BuildController.ConfigureSpawnerForSelectedModel(spawner, 7);

            Assert.That(configured, Is.True);
            Assert.That(spawner.spawnOptionId, Is.EqualTo(7));
            Assert.That(spawner.isSpawnOptionRandomized, Is.False);
        }

        [Test]
        public void ConfigureSpawnerForSelectedModel_RejectsUnknownIdWithoutRandomizingSelection()
        {
            ObjectSpawner spawner = CreateSpawner();
            spawner.objectPrefabsIndex = new List<int> { 1, 7, 9 };
            spawner.spawnOptionId = 1;

            bool configured = BuildController.ConfigureSpawnerForSelectedModel(spawner, 99);

            Assert.That(configured, Is.False);
            Assert.That(spawner.spawnOptionId, Is.EqualTo(1));
        }

        [Test]
        public void IsHitOnInteractable_AcceptsColliderOnModelChild()
        {
            GameObject modelRoot = Track(new GameObject("Door"));
            GameObject meshChild = new GameObject("Door mesh");
            meshChild.transform.SetParent(modelRoot.transform);

            Assert.That(
                ARTemplateMenuManager.IsHitOnInteractable(meshChild.transform, modelRoot),
                Is.True
            );
        }

        [Test]
        public void AndroidBuildScene_StartsWithClosedAndConnectedChat()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildScene = File.ReadAllText(
                Path.Combine(projectRoot, "Assets", "Scenes", "Build.unity")
            ).Replace("\r\n", "\n");

            Assert.That(buildScene, Does.Contain("m_Name: Chat\n"));
            Assert.That(buildScene, Does.Contain("ChatCloseButton: {fileID: 1485165205}"));
            Assert.That(buildScene, Does.Contain("ChatInputField: {fileID: 761309916}"));
            Assert.That(buildScene, Does.Contain("ChatModal: {fileID: 793547324}"));

            int chatPosition = buildScene.IndexOf("m_Name: Chat\n");
            int chatStatePosition = buildScene.IndexOf("m_IsActive: 0", chatPosition);
            Assert.That(chatStatePosition, Is.InRange(chatPosition, chatPosition + 250));
        }

        [Test]
        public void BuildController_IsSceneLocalAndDoesNotDisableNewSceneObjectsManually()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts", "BuildController.cs")
            );

            Assert.That(source, Does.Not.Contain("DontDestroyOnLoad(gameObject)"));
            Assert.That(source, Does.Not.Contain("GameObject.Find(\"XR Origin (AR Rig)\")"));
            Assert.That(source, Does.Contain("UIController.Instance.objectSpawner = null;"));
        }

        [Test]
        public void ArMenuSelection_MapsPrefabPositionToBackendModelId()
        {
            string source = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "MobileARTemplateAssets",
                    "Scripts",
                    "ARTemplateMenuManager.cs"
                )
            );

            Assert.That(
                source,
                Does.Contain(
                    "m_ObjectSpawner.spawnOptionId = " +
                    "m_ObjectSpawner.objectPrefabsIndex[objectIndex];"
                )
            );
        }

        private ObjectSpawner CreateSpawner()
        {
            GameObject spawnerObject = Track(new GameObject("Object Spawner"));
            return spawnerObject.AddComponent<ObjectSpawner>();
        }

        private GameObject Track(GameObject gameObject)
        {
            objectsToDestroy.Add(gameObject);
            return gameObject;
        }
    }
}
