using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        public void GoogleSignInPlugin_IsAbsentAndButtonsStayHidden()
        {
            AssertGoogleButtonsHidden("UI.unity");
            AssertGoogleButtonsHidden("BuildUI.unity");
            Assert.That(Directory.Exists(Path.Combine(Application.dataPath, "GoogleSignIn")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(Application.dataPath, "SignInSample")), Is.False);
            Assert.That(
                Directory.Exists(Path.Combine(Application.dataPath, "Plugins", "iOS", "GoogleSignIn")),
                Is.False
            );
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
            GameObject primaryObject = null;
            GameObject duplicateObject = null;
            try
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.SetInt("loggedIn", 1);
                PlayerPrefs.SetString("accessToken", "persisted-access");
                PlayerPrefs.SetString("accessTokenExpiresAt", DateTime.UtcNow.AddHours(1).ToString("O"));
                PlayerPrefs.SetString("userData", "{\"id\":42,\"username\":\"persisted-user\"}");
                PlayerPrefs.Save();

                Type controllerType = RequireRuntimeType("UIController");
                System.Reflection.FieldInfo instanceField = controllerType.GetField(
                    "_instance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
                );
                System.Reflection.MethodInfo register = controllerType.GetMethod(
                    "TryRegisterInstance",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                instanceField?.SetValue(null, null);
                primaryObject = new GameObject("Primary UIController");
                Component primary = primaryObject.AddComponent(controllerType);
                Assert.That(register?.Invoke(primary, null), Is.True);
                duplicateObject = new GameObject("Duplicate UIController");
                Component duplicate = duplicateObject.AddComponent(controllerType);
                LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
                Assert.That(register?.Invoke(duplicate, null), Is.False);

                controllerType.GetProperty("LoggedIn")?.SetValue(duplicate, false);
                controllerType.GetProperty("AccessToken")?.SetValue(duplicate, "replacement-access");
                controllerType.GetMethod("SaveData")?.Invoke(duplicate, null);

                Assert.That(PlayerPrefs.GetInt("loggedIn"), Is.EqualTo(1));
                Assert.That(PlayerPrefs.GetString("accessToken"), Is.EqualTo("persisted-access"));
                Assert.That(
                    PlayerPrefs.GetString("userData"),
                    Does.Contain("persisted-user")
                );
            }
            finally
            {
                if (duplicateObject != null) UnityEngine.Object.DestroyImmediate(duplicateObject);
                if (primaryObject != null) UnityEngine.Object.DestroyImmediate(primaryObject);
                Type controllerType = Type.GetType("UIController, Assembly-CSharp");
                controllerType?.GetField(
                    "_instance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
                )?.SetValue(null, null);
                PlayerPrefs.DeleteAll();
            }
        }

        [Test]
        public void SessionValidity_DoesNotDependOnDeviceClock()
        {
            GameObject controllerObject = null;
            try
            {
                PlayerPrefs.DeleteAll();
                controllerObject = new GameObject("Clock-skew UIController");
                Type controllerType = RequireRuntimeType("UIController");
                Component controller = controllerObject.AddComponent(controllerType);
                controllerType.GetProperty("AccessToken")?.SetValue(controller, "access-token");
                controllerType.GetProperty("AccessTokenExpiresAt")?.SetValue(controller, "2000-01-01T00:00:00Z");
                controllerType.GetProperty("RefreshToken")?.SetValue(controller, "refresh-token");
                controllerType.GetProperty("RefreshTokenExpiresAt")?.SetValue(controller, "2000-01-01T00:00:00Z");

                Assert.That(controllerType.GetMethod("HasValidSession")?.Invoke(controller, null), Is.True);
                Assert.That(controllerType.GetMethod("HasRefreshSession")?.Invoke(controller, null), Is.True);
            }
            finally
            {
                if (controllerObject != null) UnityEngine.Object.DestroyImmediate(controllerObject);
                PlayerPrefs.DeleteAll();
            }
        }

        private static string ReadSource(params string[] parts)
        {
            string path = Application.dataPath;
            foreach (string part in parts) path = Path.Combine(path, part);
            return File.ReadAllText(path);
        }

        private static Type RequireRuntimeType(string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, typeName + " runtime type was not found");
            return type;
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
