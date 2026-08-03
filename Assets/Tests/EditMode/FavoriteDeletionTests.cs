using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class FavoriteDeletionTests
    {
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
    }
}
