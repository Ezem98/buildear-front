using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuildeAR.Tests.EditMode
{
    public class ApiControllerRoutingTests
    {
        private const string ProductionBaseUrl =
            "https://buildear-backend-production.up.railway.app/api/v1";
        private const string MissingInstanceError =
            "No hay un ApiController configurado en la escena BuildUI. "
            + "La solicitud no se ejecutará.";
        private const string GuideMethodName = "GenerateBuildTutorial";

        private static Type ApiControllerType =>
            Type.GetType("ApiController, Assembly-CSharp", throwOnError: true);

        private static PropertyInfo InstanceProperty => ApiControllerType.GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.Static
        );

        private static readonly string[] ConcreteModelPrefabPaths =
        {
            "Assets/Prefabs/Pisos/Ceramic.prefab",
            "Assets/Prefabs/Puertas/PuertaMaderaRojaModel.prefab",
            "Assets/Prefabs/Ventanas/VentanaDobleAluminio.prefab",
            "Assets/Prefabs/Techos/techo02.prefab",
            "Assets/Prefabs/Paredes/fence.prefab",
        };

        [TearDown]
        public void TearDown()
        {
            foreach (Object candidate in Resources.FindObjectsOfTypeAll(ApiControllerType))
            {
                Component controller = (Component)candidate;
                if (!EditorUtility.IsPersistent(controller)
                    && controller.gameObject.scene.IsValid())
                {
                    Object.DestroyImmediate(controller.gameObject);
                }
            }
        }

        [Test]
        public void BackendEndpoint_IsOneProductionConstantAndIsNotSerializable()
        {
            FieldInfo endpoint = ApiControllerType.GetField(
                "BaseUrl",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            Assert.That(endpoint, Is.Not.Null);
            Assert.That(endpoint.IsLiteral, Is.True, "BaseUrl debe ser const.");
            Assert.That(endpoint.GetRawConstantValue(), Is.EqualTo(ProductionBaseUrl));
            Assert.That(
                ApiControllerType.GetField(
                    "baseUrl",
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static
                ),
                Is.Null,
                "No debe sobrevivir un campo configurable llamado baseUrl."
            );
        }

        [Test]
        public void DuplicateController_CannotReplaceOrClearRegisteredInstance()
        {
            GameObject sceneObject = new("Scene ApiController");
            Component sceneController = sceneObject.AddComponent(ApiControllerType);
            InvokeLifecycleMethod(sceneController, "Awake");
            GameObject duplicateObject = new("Prefab ApiController");
            Component duplicateController = duplicateObject.AddComponent(ApiControllerType);
            InvokeLifecycleMethod(duplicateController, "Awake");

            Assert.That(InstanceProperty.GetValue(null), Is.SameAs(sceneController));

            Object.DestroyImmediate(duplicateObject);
            Assert.That(InstanceProperty.GetValue(null), Is.SameAs(sceneController));

            Object.DestroyImmediate(sceneObject);
            AssertInstanceIsUnityNull();
        }

        [Test]
        public void DuplicateController_ResolvesRequestsToRegisteredInstance()
        {
            GameObject sceneObject = new("Scene ApiController");
            Component sceneController = sceneObject.AddComponent(ApiControllerType);
            InvokeLifecycleMethod(sceneController, "Awake");
            GameObject duplicateObject = new("Prefab ApiController");
            duplicateObject.SetActive(false);
            Component duplicateController = duplicateObject.AddComponent(ApiControllerType);
            MethodInfo resolver = ApiControllerType.GetMethod(
                "TryGetRequestExecutor",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            object[] arguments = { null };

            bool resolved = (bool)resolver.Invoke(duplicateController, arguments);

            Assert.That(resolved, Is.True);
            Assert.That(arguments[0], Is.SameAs(sceneController));
        }

        [Test]
        public void GenerateBuildTutorial_WithoutSceneInstanceLogsErrorAndStops()
        {
            GameObject duplicateObject = new("Orphan Prefab ApiController");
            duplicateObject.SetActive(false);
            Component duplicateController = duplicateObject.AddComponent(ApiControllerType);
            MethodInfo generate = ApiControllerType.GetMethod(
                GuideMethodName,
                BindingFlags.Public | BindingFlags.Instance
            );

            LogAssert.Expect(LogType.Error, MissingInstanceError);
            generate.Invoke(duplicateController, null);

            AssertInstanceIsUnityNull();
            Assert.That(
                duplicateObject.GetComponents(ApiControllerType),
                Has.Length.EqualTo(1),
                "No debe crearse un fallback con AddComponent."
            );
        }

        [Test]
        public void UnityAssets_DoNotSerializeBaseUrlOrReferenceLocalhost()
        {
            IEnumerable<string> assetPaths = FindAssetPaths("t:Prefab")
                .Concat(FindAssetPaths("t:Scene"))
                .Distinct();
            Regex serializedBaseUrl = new(
                @"^\s*(baseUrl\s*:|propertyPath\s*:\s*baseUrl\s*$)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            List<string> violations = new();

            foreach (string assetPath in assetPaths)
            {
                string contents = File.ReadAllText(assetPath);
                if (serializedBaseUrl.IsMatch(contents))
                {
                    violations.Add(assetPath + " serializa baseUrl");
                }
                if (contents.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    violations.Add(assetPath + " referencia localhost");
                }
            }

            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }

        [TestCaseSource(nameof(ConcreteModelPrefabPaths))]
        public void ConcreteGuideButton_TargetsApiControllerForwarder(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Button[] guideButtons = root.GetComponentsInChildren<Button>(true)
                    .Where(button => PersistentCallIndexes(
                        button.onClick,
                        GuideMethodName
                    ).Any())
                    .ToArray();

                Assert.That(
                    guideButtons,
                    Has.Length.EqualTo(1),
                    prefabPath + " debe tener exactamente un botón de guía."
                );

                Button guideButton = guideButtons[0];
                int[] guideCalls = PersistentCallIndexes(
                    guideButton.onClick,
                    GuideMethodName
                ).ToArray();
                Assert.That(guideCalls, Has.Length.EqualTo(1));
                Assert.That(
                    guideButton.onClick.GetPersistentTarget(guideCalls[0]),
                    Is.TypeOf(ApiControllerType),
                    prefabPath + " debe apuntar a un reenviador ApiController resoluble."
                );
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static IEnumerable<string> FindAssetPaths(string filter)
        {
            return AssetDatabase.FindAssets(filter, new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<int> PersistentCallIndexes(
            UnityEvent unityEvent,
            string methodName
        )
        {
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                if (unityEvent.GetPersistentMethodName(index) == methodName)
                {
                    yield return index;
                }
            }
        }

        private static void InvokeLifecycleMethod(Component controller, string methodName)
        {
            ApiControllerType.GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance
            ).Invoke(controller, null);
        }

        private static void AssertInstanceIsUnityNull()
        {
            Object instance = (Object)InstanceProperty.GetValue(null);
            Assert.That(instance == null, Is.True);
        }
    }
}
