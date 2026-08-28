using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class MobileBuildSessionTests
    {
        [Test]
        public void ConfigureSpawnerForSelectedModel_UsesExactBackendModelId()
        {
            string source = ReadRuntimeSource("Scripts", "BuildController.cs");
            int methodPosition = source.IndexOf("ConfigureSpawnerForSelectedModel");
            int assignmentPosition = source.IndexOf("spawner.spawnOptionId = modelId;", methodPosition);

            Assert.That(methodPosition, Is.GreaterThanOrEqualTo(0));
            Assert.That(assignmentPosition, Is.GreaterThan(methodPosition));
        }

        [Test]
        public void ConfigureSpawnerForSelectedModel_RejectsUnknownIdWithoutRandomizingSelection()
        {
            string source = ReadRuntimeSource("Scripts", "BuildController.cs");
            int validationPosition = source.IndexOf("!spawner.objectPrefabsIndex.Contains(modelId)");
            int assignmentPosition = source.IndexOf("spawner.spawnOptionId = modelId;");

            Assert.That(validationPosition, Is.GreaterThanOrEqualTo(0));
            Assert.That(assignmentPosition, Is.GreaterThan(validationPosition));
        }

        [Test]
        public void IsHitOnInteractable_AcceptsColliderOnModelChild()
        {
            string source = ReadRuntimeSource(
                "MobileARTemplateAssets",
                "Scripts",
                "ARTemplateMenuManager.cs"
            );

            Assert.That(
                source,
                Does.Contain("hitTransform.IsChildOf(interactableRoot.transform)")
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

        private static string ReadRuntimeSource(params string[] relativePath)
        {
            string[] pathParts = new string[relativePath.Length + 1];
            pathParts[0] = Application.dataPath;
            relativePath.CopyTo(pathParts, 1);
            return File.ReadAllText(Path.Combine(pathParts));
        }
    }
}
