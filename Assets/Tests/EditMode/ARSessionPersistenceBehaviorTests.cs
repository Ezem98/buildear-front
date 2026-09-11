using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using Object = UnityEngine.Object;

namespace BuildeAR.Tests.EditMode
{
    public class ARSessionPersistenceBehaviorTests
    {
        private static Type UIControllerType =>
            Type.GetType("UIController, Assembly-CSharp", throwOnError: true);

        private static Type BuildControllerType =>
            Type.GetType("BuildController, Assembly-CSharp", throwOnError: true);

        private readonly List<GameObject> trackedObjects = new();

        private GameObject Track(GameObject go)
        {
            trackedObjects.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in trackedObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            trackedObjects.Clear();

            // Clear static singleton references
            FieldInfo uiInstanceField = UIControllerType.GetField(
                "_instance",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            uiInstanceField?.SetValue(null, null);

            FieldInfo buildInstanceField = BuildControllerType.GetField(
                "_instance",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            buildInstanceField?.SetValue(null, null);
        }

        // =========================================================================
        // Test Group (a): Pure Spawn Cooldown in ARInteractorSpawnTrigger
        // =========================================================================

        [Test]
        public void ShouldSpawnAfterSelectAttempt_WhenPerformedBeforeEnable_ReturnsFalse()
        {
            bool everHadSelection = false;

            // Touch completed, but no performed event occurred since OnEnable
            bool shouldSpawn = ARInteractorSpawnTrigger.ShouldSpawnAfterSelectAttempt(
                hasSelection: false,
                everHadSelection: ref everHadSelection,
                hasObservedPerformedSinceEnable: false,
                timeSinceEnable: 1.0f,
                cooldownDuration: 0.3f
            );

            Assert.That(
                shouldSpawn,
                Is.False,
                "Must reject spawn if touch was already in progress before component was enabled."
            );
        }

        [Test]
        public void ShouldSpawnAfterSelectAttempt_WhenCompletedBeforeCooldown_ReturnsFalse()
        {
            bool everHadSelection = false;

            // Touch performed after enable, but within the cooldown duration (e.g. 0.1s < 0.3s)
            bool shouldSpawn = ARInteractorSpawnTrigger.ShouldSpawnAfterSelectAttempt(
                hasSelection: false,
                everHadSelection: ref everHadSelection,
                hasObservedPerformedSinceEnable: true,
                timeSinceEnable: 0.1f,
                cooldownDuration: 0.3f
            );

            Assert.That(
                shouldSpawn,
                Is.False,
                "Must reject spawn while within the reactivation cooldown window."
            );
        }

        [Test]
        public void ShouldSpawnAfterSelectAttempt_WhenPerformedAfterEnableAndCooldownExpired_ReturnsTrue()
        {
            bool everHadSelection = false;

            // Legitimate deliberate tap after enable and past cooldown window
            bool shouldSpawn = ARInteractorSpawnTrigger.ShouldSpawnAfterSelectAttempt(
                hasSelection: false,
                everHadSelection: ref everHadSelection,
                hasObservedPerformedSinceEnable: true,
                timeSinceEnable: 0.5f,
                cooldownDuration: 0.3f
            );

            Assert.That(
                shouldSpawn,
                Is.True,
                "Must allow spawn when tap was performed after enable and cooldown has expired."
            );
            Assert.That(everHadSelection, Is.False);
        }

        [Test]
        public void ShouldSpawnAfterSelectAttempt_WhenSelectionExisted_ReturnsFalseAndClearsFlag()
        {
            bool everHadSelection = true;

            bool shouldSpawn = ARInteractorSpawnTrigger.ShouldSpawnAfterSelectAttempt(
                hasSelection: false,
                everHadSelection: ref everHadSelection,
                hasObservedPerformedSinceEnable: true,
                timeSinceEnable: 0.5f,
                cooldownDuration: 0.3f
            );

            Assert.That(
                shouldSpawn,
                Is.False,
                "Must not spawn if the interactor previously held a selection."
            );
            Assert.That(
                everHadSelection,
                Is.False,
                "everHadSelection must be reset to false after evaluation."
            );
        }

        [Test]
        public void ShouldSpawnAfterSelectAttempt_OverloadForExistingCallers_PreservesCompatibility()
        {
            bool everHadSelection = false;

            bool shouldSpawn = ARInteractorSpawnTrigger.ShouldSpawnAfterSelectAttempt(
                hasSelection: false,
                ref everHadSelection
            );

            Assert.That(shouldSpawn, Is.True);
        }

        // =========================================================================
        // Test Group (b): UIController Build Mode Transitions (Pause, Do Not Deactivate)
        // =========================================================================

        [Test]
        public void DisableBuildMode_KeepsSessionObjectAndXRComponentActive_AndPausesARSession()
        {
            GameObject controllerGo = Track(new GameObject("UIController_Host"));
            Component controller = controllerGo.AddComponent(UIControllerType);

            GameObject arSessionGo = Track(new GameObject("ARSession_Object"));
            ARSession arSession = arSessionGo.AddComponent<ARSession>();
            arSession.enabled = true;

            GameObject xrComponentGo = Track(new GameObject("XRComponent_Object"));
            GameObject buildUiGo = Track(new GameObject("BuildUI_Object"));
            GameObject canvasGo = Track(new GameObject("Canvas_Object"));

            SetField(controller, "arSessionObject", arSessionGo);
            SetField(controller, "XRComponent", xrComponentGo);
            SetField(controller, "buildUI", buildUiGo);
            SetField(controller, "canvas", canvasGo);

            // 1. Enter build mode
            InvokeMethod(controller, "EnableBuildMode");
            Assert.That(arSessionGo.activeSelf, Is.True);
            Assert.That(xrComponentGo.activeSelf, Is.True);
            Assert.That(arSession.enabled, Is.True);
            Assert.That(buildUiGo.activeSelf, Is.True);
            Assert.That(canvasGo.activeSelf, Is.False);

            // 2. Exit build mode to catalogue
            InvokeMethod(controller, "DisableBuildMode");

            // GameObjects must remain active to avoid destroying subsystems/trackables!
            Assert.That(
                arSessionGo.activeSelf,
                Is.True,
                "arSessionObject must stay active to prevent native subsystem teardown."
            );
            Assert.That(
                xrComponentGo.activeSelf,
                Is.True,
                "XRComponent must stay active so ARPlaneManager retains existing trackables."
            );

            // ARSession component must be paused
            Assert.That(
                arSession.enabled,
                Is.False,
                "ARSession must be paused via enabled = false."
            );

            Assert.That(buildUiGo.activeSelf, Is.False);
            Assert.That(canvasGo.activeSelf, Is.True);

            // 3. Re-enter build mode
            InvokeMethod(controller, "EnableBuildMode");
            Assert.That(arSession.enabled, Is.True, "ARSession must resume when re-entering build mode.");
        }

        // =========================================================================
        // Test Group (c): Defensive Orphaned Plane Sanitation
        // =========================================================================

        [Test]
        public void SanitizeOrphanedPlanes_DestroysPlanesNotPresentInActiveTrackables()
        {
            GameObject originGo = Track(new GameObject("XR_Origin_Test"));
            ARPlaneManager planeManager = originGo.AddComponent<ARPlaneManager>();

            GameObject trackablesGo = Track(new GameObject("Trackables"));
            trackablesGo.transform.parent = originGo.transform;

            // Create an orphaned ARPlane under trackables
            GameObject orphanPlaneGo = Track(new GameObject("Orphan_Plane"));
            orphanPlaneGo.transform.parent = trackablesGo.transform;
            ARPlane orphanPlane = orphanPlaneGo.AddComponent<ARPlane>();

            MethodInfo sanitizeMethod = BuildControllerType.GetMethod(
                "SanitizeOrphanedPlanes",
                BindingFlags.Public | BindingFlags.Static
            );
            Assert.That(sanitizeMethod, Is.Not.Null, "SanitizeOrphanedPlanes method must exist on BuildController.");

            int destroyed = (int)sanitizeMethod.Invoke(null, new object[] { planeManager });

            Assert.That(destroyed, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                orphanPlaneGo == null || !orphanPlaneGo,
                Is.True,
                "Orphan plane GameObject must be destroyed during sanitation."
            );
        }

        // =========================================================================
        // Helpers
        // =========================================================================

        private static void SetField(Component target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            field?.SetValue(target, value);
        }

        private static void InvokeMethod(Component target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            method?.Invoke(target, args);
        }
    }
}
