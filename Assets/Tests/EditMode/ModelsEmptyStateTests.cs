using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class ModelsEmptyStateTests
    {
        [Test]
        public void EmptyCategory_HidesPreviousModelCount()
        {
            string managerPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "ModelsManager.cs"
            );
            string source = File.ReadAllText(managerPath);
            Match createButtons = Regex.Match(
                source,
                @"public\s+void\s+CreateButtons[\s\S]*?private\s+void\s+DestroyButtons"
            );

            Assert.That(createButtons.Success, Is.True);
            Assert.That(
                Regex.IsMatch(
                    createButtons.Value,
                    @"else\s*\{\s*ModelCountText\.SetActive\(false\);"
                ),
                Is.True
            );
            Assert.That(
                createButtons.Value,
                Does.Contain("LoadingText.text = \"Sin modelos disponibles.\"")
            );
        }

        [Test]
        public void LoadingNewCategory_ResetsCountBeforeResponse()
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
            Assert.That(onEnable.Value, Does.Contain("Cargando modelos..."));
            Assert.That(onEnable.Value, Does.Contain("ModelCountText.SetActive(false)"));
        }

        [Test]
        public void SingularCount_UsesModeloLabel()
        {
            string managerPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "ModelsManager.cs"
            );
            string source = File.ReadAllText(managerPath);

            Assert.That(
                source,
                Does.Contain("models.Count == 1 ? \"Modelo\" : \"Modelos\"")
            );
        }
    }
}
