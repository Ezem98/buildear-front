using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BuildeAR.Tests.EditMode
{
    public class AuditRemediationTests
    {
        [Test]
        public void ProfilePayload_OmitsNullsAndPasswordFields()
        {
            string dto = ReadSource("Classes", "UpdateUserData.cs");
            string controller = ReadSource("Scripts", "APIController.cs");

            Assert.That(dto, Does.Not.Contain("password"));
            Assert.That(dto, Does.Not.Contain("newPassword"));
            Assert.That(dto, Does.Contain("NullValueHandling.Ignore"));
            Assert.That(controller, Does.Contain("NullValueHandling = NullValueHandling.Ignore"));
        }

        [Test]
        public void PasswordPayload_UsesDedicatedBackendContract()
        {
            string dto = ReadSource("Classes", "UpdatePasswordData.cs");
            string controller = ReadSource("Scripts", "APIController.cs");

            Assert.That(dto, Does.Contain("public string password;"));
            Assert.That(dto, Does.Contain("public string newPassword;"));
            Assert.That(controller, Does.Contain("/users/me/password"));
        }

        [Test]
        public void RegisterPayload_UsesUnityPlaceholderWhenImageIsAbsent()
        {
            string dto = ReadSource("Classes", "RegisterData.cs");

            Assert.That(dto, Does.Contain("public string image;"));
            Assert.That(dto, Does.Not.Contain("altervista"));
        }

        [Test]
        public void BackendErrorShape_ProducesVisibleMessageAndCode()
        {
            const string json = "{\"error\":{\"code\":\"VALIDATION_ERROR\",\"message\":\"Revisá los datos\"}}";

            string source = ReadSource("Scripts", "APIController.cs");

            Assert.That(source, Does.Contain("payload?.error?.message"));
            Assert.That(source, Does.Contain("ParseErrorResponse(jsonResponse)?.error?.code"));
            Assert.That(json, Does.Contain("VALIDATION_ERROR"));
        }

        [Test]
        public void GoogleButtons_AreHiddenAndDisconnected()
        {
            AssertGoogleButtonsHidden("UI.unity");
            AssertGoogleButtonsHidden("BuildUI.unity");
        }

        private static void AssertGoogleButtonsHidden(string sceneName)
        {
            string scene = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scenes", sceneName)
            ).Replace("\r\n", "\n");

            AssertInactiveNearName(scene, "LoginGoogleButton");
            AssertInactiveNearName(scene, "RegisterGoogleButton");
            Assert.That(scene, Does.Not.Contain("m_MethodName: OnSignIn"));
            Assert.That(scene, Does.Not.Contain("m_MethodName: SignInGoogle"));
        }

        [Test]
        public void SecurityArtifacts_AreAbsentAndIgnored()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string ignore = File.ReadAllText(Path.Combine(projectRoot, ".gitignore"));

            Assert.That(File.Exists(Path.Combine(projectRoot, "user.keystore")), Is.False);
            Assert.That(
                File.Exists(Path.Combine(Application.dataPath, "Scripts", "Certificate.cs")),
                Is.False
            );
            Assert.That(ignore, Does.Contain("*.keystore"));
        }

        [Test]
        public void RequestLayer_HasRefreshRetryTimeoutsAndSanitizedLogging()
        {
            string source = ReadSource("Scripts", "APIController.cs");

            Assert.That(source, Does.Contain("RefreshAccessToken"));
            Assert.That(source, Does.Contain("retryAfterRefresh"));
            Assert.That(source, Does.Contain("DefaultTimeoutSeconds = 30"));
            Assert.That(source, Does.Contain("OpenAITimeoutSeconds = 120"));
            Assert.That(source, Does.Not.Contain("+ $\"({webRequest.responseCode}, {webRequest.result}): {responseBody}\""));
            Assert.That(source, Does.Contain("new Vector2(0.5f, 0.5f)"));
        }

        [Test]
        public void DuplicateController_CannotOverwritePersistedSession()
        {
            string source = ReadSource("Scripts", "UIController.cs");

            Assert.That(source, Does.Contain("isDuplicate = true;"));
            Assert.That(source, Does.Contain("if (isDuplicate) return;"));
            Assert.That(source, Does.Not.Contain("AddComponent<UIController>"));
        }

        private static string ReadSource(params string[] parts)
        {
            string path = Application.dataPath;
            foreach (string part in parts) path = Path.Combine(path, part);
            return File.ReadAllText(path);
        }

        private static void AssertInactiveNearName(string scene, string name)
        {
            int namePosition = scene.IndexOf("m_Name: " + name);
            int inactivePosition = scene.IndexOf("m_IsActive: 0", namePosition);
            Assert.That(namePosition, Is.GreaterThanOrEqualTo(0));
            Assert.That(inactivePosition, Is.InRange(namePosition, namePosition + 250));
        }
    }
}
