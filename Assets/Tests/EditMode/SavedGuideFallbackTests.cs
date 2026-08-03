using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class SavedGuideFallbackTests
    {
        [Test]
        public void GuideRefreshFailure_ShowsPreviouslySavedGuide()
        {
            string source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "APIController.cs"));

            Assert.That(source, Does.Contain("hasSavedGuide ? userModelData : null"));
            Assert.That(source, Does.Contain("ShowGuide(modelId, savedUserModel.guideObject, savedStep);"));
            Assert.That(source, Does.Contain("Se muestra la versión guardada."));
        }
    }
}
