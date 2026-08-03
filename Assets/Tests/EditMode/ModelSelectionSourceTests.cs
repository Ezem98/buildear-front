using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class ModelSelectionSourceTests
    {
        [Test]
        public void ModelSelection_UsesPreviousScreenBeforeSearchResults()
        {
            string managerPath = Path.Combine(Application.dataPath, "ModelManager.cs");
            string source = File.ReadAllText(managerPath);
            Match resolver = Regex.Match(
                source,
                @"private\s+ModelData\s+ResolveSelectedModel[\s\S]*?public\s+void\s+ToggleFavorite"
            );

            Assert.That(resolver.Success, Is.True);
            Assert.That(resolver.Value, Does.Contain("switch (ui.PreviousScreen)"));
            Assert.That(resolver.Value, Does.Contain("case \"Home\":"));
            Assert.That(resolver.Value, Does.Contain("ui.MyModelsData?.Find"));
            Assert.That(resolver.Value, Does.Contain("case \"Favorites\":"));
            Assert.That(resolver.Value, Does.Contain("ui.FavoritesModelsData?.Find"));
            Assert.That(resolver.Value, Does.Contain("case \"Models\":"));
            Assert.That(resolver.Value, Does.Contain("ui.ComesFromSearch"));
            Assert.That(resolver.Value, Does.Contain("ui.SearchModelsData?.Find"));
            Assert.That(resolver.Value, Does.Contain("ui.ModelsData?.Find"));
        }

        [Test]
        public void ReturningToModels_KeepsSearchContextActive()
        {
            string managerPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "ModelsManager.cs"
            );
            string source = File.ReadAllText(managerPath);
            Match searchBranch = Regex.Match(
                source,
                @"if\s*\(UIController\.Instance\.ComesFromSearch\)[\s\S]*?else"
            );

            Assert.That(searchBranch.Success, Is.True);
            Assert.That(
                searchBranch.Value,
                Does.Contain("CreateButtons(UIController.Instance.SearchModelsData)")
            );
            Assert.That(
                searchBranch.Value,
                Does.Not.Contain("ComesFromSearch = false")
            );
        }

        [Test]
        public void NormalCatalogue_ClearsPreviousSearchResults()
        {
            string managerPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "ModelsManager.cs"
            );
            string source = File.ReadAllText(managerPath);
            Match onEnable = Regex.Match(
                source,
                @"private\s+void\s+OnEnable[\s\S]*?private\s+void\s+OnDisable"
            );

            Assert.That(onEnable.Success, Is.True);
            Assert.That(
                onEnable.Value,
                Does.Contain("SearchModelsData = null")
            );
            Assert.That(
                onEnable.Value.IndexOf("SearchModelsData = null"),
                Is.LessThan(onEnable.Value.IndexOf("GetModelsByCategoryId"))
            );
        }

        [Test]
        public void CategoryChangeAndLogout_ClearSearchContext()
        {
            string controllerPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "UIController.cs"
            );
            string source = File.ReadAllText(controllerPath);
            Match changeCategory = Regex.Match(
                source,
                @"public\s+void\s+ChangeCategory[\s\S]*?public\s+void\s+JoinAsGuest"
            );
            Match clearSession = Regex.Match(
                source,
                @"public\s+void\s+ClearSession[\s\S]*?private\s+void\s+OnDisable"
            );

            Assert.That(changeCategory.Value, Does.Contain("ComesFromSearch = false"));
            Assert.That(changeCategory.Value, Does.Contain("SearchModelsData = null"));
            Assert.That(clearSession.Value, Does.Contain("ComesFromSearch = false"));
            Assert.That(clearSession.Value, Does.Contain("SearchModelsData = null"));
        }
    }
}
