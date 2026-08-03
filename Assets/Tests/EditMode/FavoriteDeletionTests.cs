using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class FavoriteDeletionTests
    {
        [Test]
        public void CreateFavorite_SendsOnlyFieldsAcceptedByBackend()
        {
            string controllerPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "APIController.cs"
            );
            string source = File.ReadAllText(controllerPath);
            Match createFavorite = Regex.Match(
                source,
                @"public\s+void\s+CreateFavorite[\s\S]*?public\s+void\s+DeleteFavorite"
            );

            Assert.That(createFavorite.Success, Is.True);
            Assert.That(createFavorite.Value, Does.Contain("user_id = favoriteData.user_id"));
            Assert.That(createFavorite.Value, Does.Contain("model_id = favoriteData.model_id"));
            Assert.That(createFavorite.Value, Does.Contain("JsonConvert.SerializeObject"));
            Assert.That(createFavorite.Value, Does.Contain("onSuccess?.Invoke()"));
        }

        [Test]
        public void DeleteFavorite_DoesNotDeserializeAnEmptyResponse()
        {
            string controllerPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "APIController.cs"
            );
            string source = File.ReadAllText(controllerPath);
            Match deleteFavorite = Regex.Match(
                source,
                @"public\s+void\s+DeleteFavorite[\s\S]*?public\s+void\s+IsFavorite"
            );

            Assert.That(deleteFavorite.Success, Is.True);
            Assert.That(deleteFavorite.Value, Does.Not.Contain("DeserializeObject"));
            Assert.That(deleteFavorite.Value, Does.Not.Contain("FromJson"));
            Assert.That(source, Does.Not.Contain("Delete request successful"));
        }

        [Test]
        public void ToggleFavorite_UpdatesStateOnlyAfterServerSuccess()
        {
            string managerPath = Path.Combine(Application.dataPath, "ModelManager.cs");
            string source = File.ReadAllText(managerPath);
            Match toggleFavorite = Regex.Match(
                source,
                @"public\s+void\s+ToggleFavorite[\s\S]*?private\s+void\s+IsFavorite"
            );

            Assert.That(toggleFavorite.Success, Is.True);
            Assert.That(
                toggleFavorite.Value,
                Does.Contain("onSuccess: () => SetFavoriteState(false)")
            );
            Assert.That(
                toggleFavorite.Value,
                Does.Contain("onSuccess: () => SetFavoriteState(true)")
            );
            Assert.That(toggleFavorite.Value, Does.Contain("onError: HandleFavoriteError"));
        }
    }
}
