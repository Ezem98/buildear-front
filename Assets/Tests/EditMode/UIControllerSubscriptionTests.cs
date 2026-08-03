using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class UIControllerSubscriptionTests
    {
        [Test]
        public void ObjectSpawnedSubscription_IsManagedByEnableAndDisable()
        {
            string controllerPath = Path.Combine(Application.dataPath, "Scripts", "UIController.cs");
            string source = File.ReadAllText(controllerPath);

            Assert.That(source, Does.Contain("private void OnEnable()"));
            Assert.That(source, Does.Contain("SubscribeToObjectSpawner();"));
            Assert.That(source, Does.Contain("UnsubscribeFromObjectSpawner();"));
            Assert.That(
                Regex.Matches(source, @"objectSpawned\s*\+=\s*OnObjectSpawned").Count,
                Is.EqualTo(1),
                "The spawn callback must only be subscribed from the lifecycle helper.");
            Assert.That(
                Regex.IsMatch(source, @"void\s+Update\s*\(\s*\)[\s\S]*?objectSpawned\s*\+="),
                Is.False,
                "The spawn callback must never be subscribed once per frame.");
        }
    }
}
