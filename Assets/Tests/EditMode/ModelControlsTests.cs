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
    }
}
