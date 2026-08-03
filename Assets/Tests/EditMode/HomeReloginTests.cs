using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class HomeReloginTests
    {
        [Test]
        public void HomeOnEnable_RefreshesAuthenticatedUserModels()
        {
            string homePath = Path.Combine(Application.dataPath, "HomeManager.cs");
            string source = File.ReadAllText(homePath);
            Match onEnable = Regex.Match(
                source,
                @"private\s+void\s+OnEnable[\s\S]*?private\s+void\s+OnDisable"
            );
            Match refresh = Regex.Match(
                source,
                @"private\s+void\s+RefreshModels[\s\S]*?private\s+void\s+CreateButtons"
            );

            Assert.That(onEnable.Success, Is.True);
            Assert.That(onEnable.Value, Does.Contain("RefreshModels();"));
            Assert.That(refresh.Success, Is.True);
            Assert.That(refresh.Value, Does.Contain("GetModelsByUserId(user.id"));
            Assert.That(refresh.Value, Does.Contain("CreateButtons(models);"));
        }

        [Test]
        public void Login_ClearsPreviousModelCacheBeforeOpeningHome()
        {
            string apiPath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "APIController.cs"
            );
            string source = File.ReadAllText(apiPath);
            Match login = Regex.Match(
                source,
                @"public\s+void\s+Login[\s\S]*?public\s+void\s+Logout"
            );

            Assert.That(login.Success, Is.True);
            Assert.That(login.Value, Does.Contain("GuestUser = false"));
            Assert.That(login.Value, Does.Contain("MyModelsData = null"));
            Assert.That(login.Value, Does.Contain("ScreenHandler(\"Home\")"));
        }
    }
}
