using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class ModelControlsTests
    {
        [Test]
        public void CanvasMenus_IterateAvailableButtonsInsteadOfUsingFixedIndexes()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts", "CanvasManager.cs")
            );

            Assert.That(source, Does.Not.Contain(".GetChild("));
            Assert.That(source, Does.Not.Contain("menu["));
            Assert.That(source, Does.Contain("ShowMenu(modelActions);"));
            Assert.That(source, Does.Contain("foreach (Transform action in actions.transform)"));
            Assert.That(source, Does.Contain("if (actions == null)"));
        }

        [Test]
        public void XRInteractionToolkit_IncludesScreenSpaceCanvasNullGuard()
        {
            string manifest = File.ReadAllText(
                Path.GetFullPath(
                    Path.Combine(Application.dataPath, "..", "Packages", "manifest.json")
                )
            );

            Assert.That(
                manifest,
                Does.Contain("\"com.unity.xr.interaction.toolkit\": \"3.0.8\"")
            );
        }

        [Test]
        public void ARSpawnInput_IsSeparatedFromObjectSelection()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildScene = File.ReadAllText(
                Path.Combine(projectRoot, "Assets", "Scenes", "Build.unity")
            );
            string buildUiScene = File.ReadAllText(
                Path.Combine(projectRoot, "Assets", "Scenes", "BuildUI.unity")
            );
            string inputActions = File.ReadAllText(
                Path.Combine(
                    projectRoot,
                    "Assets",
                    "Samples",
                    "XR Interaction Toolkit",
                    "3.0.4",
                    "Starter Assets",
                    "XRI Default Input Actions.inputactions"
                )
            );

            Assert.That(buildScene, Does.Contain("m_SpawnTriggerType: 1"));
            Assert.That(buildUiScene, Does.Contain("m_SpawnTriggerType: 1"));
            Assert.That(
                buildScene,
                Does.Contain("guid: c348712bda248c246b8c49b3db54643f")
            );
            Assert.That(
                buildUiScene,
                Does.Contain("guid: c348712bda248c246b8c49b3db54643f")
            );
            Assert.That(
                inputActions,
                Does.Contain("<TouchscreenGestureInputController>/tapStartPosition")
            );
            Assert.That(inputActions, Does.Contain("Tap(duration=0.5)"));
        }
    }
}
