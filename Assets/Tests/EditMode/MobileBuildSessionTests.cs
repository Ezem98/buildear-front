using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

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
        public void SingleBuildUiScene_StartsWithArDisabledAndClosedChat()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildScene = File.ReadAllText(
                Path.Combine(projectRoot, "Assets", "Scenes", "BuildUI.unity")
            ).Replace("\r\n", "\n");

            Assert.That(buildScene, Does.Contain("m_Name: Chat\n"));
            Assert.That(buildScene, Does.Contain("ChatCloseButton: {fileID: 1114486724}"));
            Assert.That(buildScene, Does.Contain("ChatInputField: {fileID: 1753163606}"));
            Assert.That(buildScene, Does.Contain("ChatModal: {fileID: 1094948951}"));

            int chatPosition = buildScene.IndexOf("m_Name: Chat\n");
            int chatStatePosition = buildScene.IndexOf("m_IsActive: 0", chatPosition);
            Assert.That(chatStatePosition, Is.InRange(chatPosition, chatPosition + 250));

            int arSessionPosition = buildScene.IndexOf("m_Name: AR Session\n");
            int arSessionStatePosition = buildScene.IndexOf("m_IsActive: 0", arSessionPosition);
            Assert.That(arSessionStatePosition, Is.InRange(arSessionPosition, arSessionPosition + 250));
            Assert.That(
                buildScene,
                Does.Contain(
                    "propertyPath: m_Name\n" +
                    "      value: XR Origin (AR Rig)\n" +
                    "      objectReference: {fileID: 0}\n" +
                    "    - target: {fileID: 2512387470528047719"
                )
            );
            Assert.That(
                buildScene,
                Does.Contain("propertyPath: m_IsActive\n      value: 0")
            );
        }

        [Test]
        public void ArCamera_RequestsDepthOcclusionWithSafePlatformFallback()
        {
            string arRigPrefab = ReadRuntimeSource(
                "Samples",
                "XR Interaction Toolkit",
                "3.0.4",
                "AR Starter Assets",
                "Prefabs",
                "XR Origin (AR Rig).prefab"
            );

            Assert.That(
                arRigPrefab,
                Does.Contain("guid: b15f82cc229284894964d2d30806969d")
            );
            Assert.That(arRigPrefab, Does.Contain("m_EnvironmentDepthMode: 2"));
            Assert.That(
                arRigPrefab,
                Does.Contain("m_EnvironmentDepthTemporalSmoothing: 1")
            );
            Assert.That(arRigPrefab, Does.Contain("m_HumanSegmentationStencilMode: 3"));
            Assert.That(arRigPrefab, Does.Contain("m_HumanSegmentationDepthMode: 2"));
        }

        [Test]
        public void BuildController_ExitsBuildModeWithoutLoadingAnotherScene()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts", "BuildController.cs")
            );
            string uiControllerSource = ReadRuntimeSource("Scripts", "UIController.cs");

            Assert.That(source, Does.Not.Contain("DontDestroyOnLoad(gameObject)"));
            Assert.That(source, Does.Contain("UIController.Instance.GoBack();"));
            Assert.That(source, Does.Not.Contain("SceneManager.LoadScene"));
            Assert.That(uiControllerSource, Does.Not.Contain("SceneManager.LoadScene"));
            Assert.That(uiControllerSource, Does.Contain("arSession.Reset();"));
            Assert.That(uiControllerSource, Does.Contain("XRComponent?.SetActive(false);"));
        }

        [Test]
        public void BuildSettings_ContainsOnlyBuildUiScene()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildSettings = File.ReadAllText(
                Path.Combine(projectRoot, "ProjectSettings", "EditorBuildSettings.asset")
            );

            Assert.That(buildSettings, Does.Contain("path: Assets/Scenes/BuildUI.unity"));
            Assert.That(buildSettings, Does.Not.Contain("path: Assets/Scenes/UI.unity"));
            Assert.That(buildSettings, Does.Not.Contain("path: Assets/Scenes/Build.unity"));
        }

        [Test]
        public void BuildUiScene_UsesOneApiControllerAndOneBuildEntryAction()
        {
            string buildScene = ReadRuntimeSource("Scenes", "BuildUI.unity");

            Assert.That(
                CountOccurrences(buildScene, "guid: 43b40aadd52f18643ba398acb9a632ee"),
                Is.EqualTo(1)
            );
            Assert.That(
                CountOccurrences(buildScene, "m_MethodName: EnableBuildMode"),
                Is.EqualTo(1)
            );
            Assert.That(buildScene, Does.Not.Contain("m_MethodName: SceneHandler"));
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
        public void FloorPlacement_DoesNotForceAWorldSpaceHeight()
        {
            string source = ReadRuntimeSource("Scripts", "UIController.cs");
            int handlerPosition = source.IndexOf("public void OnObjectSpawned");
            int nextMethodPosition = source.IndexOf("public void SceneHandler", handlerPosition);
            string handler = source.Substring(
                handlerPosition,
                nextMethodPosition - handlerPosition
            );

            Assert.That(handler, Does.Not.Contain("transform.position ="));
            Assert.That(handler, Does.Not.Contain("0.01f"));
        }

        [Test]
        public void FloorPlacement_RejectsElevatedHorizontalPlane()
        {
            Assert.That(
                ARInteractorSpawnTrigger.IsWithinLowestHorizontalSurface(0.04f, 0f, 0.15f),
                Is.True
            );
            Assert.That(
                ARInteractorSpawnTrigger.IsWithinLowestHorizontalSurface(0.75f, 0f, 0.15f),
                Is.False
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
            string uiScene = ReadRuntimeSource("Scenes", "BuildUI.unity");
            int backButtonPosition = uiScene.IndexOf("--- !u!1 &290691207");
            int nextObjectPosition = uiScene.IndexOf("--- !u!1 &", backButtonPosition + 1);
            string backButtonBlock = uiScene.Substring(
                backButtonPosition,
                nextObjectPosition - backButtonPosition
            );
            string controllerSource = ReadRuntimeSource("Scripts", "UIController.cs");

            Assert.That(backButtonPosition, Is.GreaterThanOrEqualTo(0));
            Assert.That(backButtonBlock, Does.Contain("m_Target: {fileID: 414502328}"));
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

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int position = 0;
            while ((position = source.IndexOf(value, position)) >= 0)
            {
                count++;
                position += value.Length;
            }

            return count;
        }
    }
}
