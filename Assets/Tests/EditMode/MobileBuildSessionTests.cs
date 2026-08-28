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

        [Test]
        public void DeleteButton_IsBoundAtRuntimeAndDestroysAfterInteractionUpdate()
        {
            string source = ReadRuntimeSource("Scripts", "CanvasManager.cs");

            Assert.That(source, Does.Contain("BindDeleteButton();"));
            Assert.That(source, Does.Contain("button.onClick.AddListener(DestroyObject);"));
            Assert.That(source, Does.Contain("if (destroyRequested)"));
            Assert.That(source, Does.Contain("ResolveObjectToDestroy(metadata)"));
            Assert.That(source, Does.Contain("yield return null;"));
            Assert.That(source, Does.Not.Contain("Destroy(objectToDestroy, 0.1f);"));
        }

        [Test]
        public void ModelDetailBackButton_TargetsUiNavigationAndHasSafeFallback()
        {
            string uiScene = ReadRuntimeSource("Scenes", "UI.unity");
            int backButtonPosition = uiScene.IndexOf("--- !u!1 &1724267176");
            int nextObjectPosition = uiScene.IndexOf("--- !u!1 &", backButtonPosition + 1);
            string backButtonBlock = uiScene.Substring(
                backButtonPosition,
                nextObjectPosition - backButtonPosition
            );
            string controllerSource = ReadRuntimeSource("Scripts", "UIController.cs");

            Assert.That(backButtonPosition, Is.GreaterThanOrEqualTo(0));
            Assert.That(backButtonBlock, Does.Contain("m_Target: {fileID: 1638469108}"));
            Assert.That(
                backButtonBlock,
                Does.Contain("m_TargetAssemblyTypeName: UIController, Assembly-CSharp")
            );
            Assert.That(backButtonBlock, Does.Contain("m_MethodName: GoBack"));
            Assert.That(controllerSource, Does.Contain("return \"Home\";"));
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
