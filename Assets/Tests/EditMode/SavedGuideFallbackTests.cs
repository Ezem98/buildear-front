using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class SavedGuideReuseTests
    {
        [Test]
        public void ExistingGuide_IsReusedWithItsSavedStepWithoutRegeneration()
        {
            string source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "APIController.cs"));

            Assert.That(source, Does.Contain("if (hasSavedGuide)"));
            Assert.That(source, Does.Contain("int savedStep = userModelData.current_step > 0"));
            Assert.That(source, Does.Contain("ShowGuide(modelId, userModelData.guideObject, savedStep);"));
            Assert.That(source, Does.Not.Contain("hasCostEstimate"));
            Assert.That(source, Does.Not.Contain("hasCurrentPrompt"));
        }
    }
}
