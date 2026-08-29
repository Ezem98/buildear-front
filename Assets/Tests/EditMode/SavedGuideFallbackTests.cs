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

        [Test]
        public void MissingSavedGuideLookup_ContinuesWithGenerationWhenSessionIsValid()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts", "APIController.cs")
            );
            int failureHandler = source.IndexOf("HandleSavedGuideLookupFailure");
            int sessionCheck = source.IndexOf(
                "if (!UIController.Instance.HasValidSession())",
                failureHandler
            );
            int generationRequest = source.IndexOf(
                "RequestGeneratedGuide(modelId, model);",
                sessionCheck
            );

            Assert.That(failureHandler, Is.GreaterThanOrEqualTo(0));
            Assert.That(sessionCheck, Is.GreaterThan(failureHandler));
            Assert.That(generationRequest, Is.GreaterThan(sessionCheck));
            Assert.That(
                source,
                Does.Not.Contain(
                    "ErrorMessage(error, \"No se pudo consultar la guía guardada.\")"
                )
            );
        }

        [Test]
        public void InvalidSavedGuide_IsTreatedAsMissingSoItCanBeRegenerated()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts", "APIController.cs")
            );

            Assert.That(source, Does.Contain("catch (JsonException)"));
            Assert.That(source, Does.Contain("apiResponse.data.guideObject = null;"));
        }

        [Test]
        public void FailedGeneration_RecoversGuidePersistedBeforeResponseWasLost()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts", "APIController.cs")
            );
            int handler = source.IndexOf("private void HandleGuideGenerationFailure");
            int lookup = source.IndexOf("GetUserModel(", handler);
            int savedGuideCheck = source.IndexOf("bool guideWasSaved", lookup);
            int showGuide = source.IndexOf(
                "ShowGuide(modelId, userModelData.guideObject, savedStep);",
                savedGuideCheck
            );

            Assert.That(handler, Is.GreaterThanOrEqualTo(0));
            Assert.That(lookup, Is.GreaterThan(handler));
            Assert.That(savedGuideCheck, Is.GreaterThan(lookup));
            Assert.That(showGuide, Is.GreaterThan(savedGuideCheck));
        }

        [Test]
        public void NetworkFailure_ProducesSpecificMessageInsteadOfEmptyFallback()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts", "APIController.cs")
            );

            Assert.That(source, Does.Contain("RequestErrorPayload(webRequest)"));
            Assert.That(
                source,
                Does.Contain("No se pudo conectar con el servidor. Revisá tu conexión")
            );
            Assert.That(source, Does.Contain("webRequest.timeout = 120;"));
        }
    }
}
