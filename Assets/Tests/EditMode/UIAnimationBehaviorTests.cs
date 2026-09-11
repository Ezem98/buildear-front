using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace BuildeAR.Tests.EditMode
{
    public class UIAnimationBehaviorTests
    {
        private static Type UIAnimationType =>
            Type.GetType("UIAnimation, Assembly-CSharp", throwOnError: true);

        private static Type ToolBarToggleType =>
            Type.GetType("ToolBarToggle, Assembly-CSharp", throwOnError: true);

        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            createdObjects.Clear();

            FieldInfo instanceField = UIAnimationType.GetField(
                "_instance",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            instanceField?.SetValue(null, null);
        }

        private UIAnimationTestDriver CreateDriver(
            Vector2 startingPoint,
            Vector2 finalPoint,
            float duration = 0.3f
        )
        {
            GameObject go = new("UIAnimation_TestTarget");
            createdObjects.Add(go);
            return new UIAnimationTestDriver(go, startingPoint, finalPoint, duration);
        }

        [Test]
        public void Open_InvokedTwice_DoesNotRestartOrRelaunchAnimation()
        {
            Vector2 closedPos = new(-110f, -755f);
            Vector2 openPos = new(-110f, 465f);
            UIAnimationTestDriver driver = CreateDriver(closedPos, openPos, 0.3f);

            Assert.That(driver.IsOpen, Is.False);

            driver.Open();
            Assert.That(driver.IsOpen, Is.True);
            object coroutine1 = driver.CurrentAnimation;
            Assert.That(coroutine1, Is.Not.Null, "First Open() must launch a coroutine.");

            // Second Open() call while open/opening
            driver.Open();
            Assert.That(driver.IsOpen, Is.True);
            object coroutine2 = driver.CurrentAnimation;

            Assert.That(
                coroutine2,
                Is.SameAs(coroutine1),
                "Second Open() must be idempotent and not replace or restart the current animation."
            );
        }

        [Test]
        public void Close_WhenAlreadyClosed_DoesNotMoveAnchoredPositionAndDoesNotPlay()
        {
            Vector2 closedPos = new(-110f, -755f);
            Vector2 openPos = new(-110f, 465f);
            UIAnimationTestDriver driver = CreateDriver(closedPos, openPos, 0.3f);

            Assert.That(driver.IsOpen, Is.False);
            Assert.That(driver.IsPlaying, Is.False);

            driver.Close();

            Assert.That(driver.AnchoredPosition, Is.EqualTo(closedPos));
            Assert.That(driver.IsPlaying, Is.False);
            Assert.That(driver.CurrentAnimation, Is.Null);
            Assert.That(driver.IsOpen, Is.False);
        }

        [Test]
        public void OnDisable_MidAnimation_ResetsIsPlayingAndSnapsToStartingPoint()
        {
            Vector2 closedPos = new(-110f, -755f);
            Vector2 openPos = new(-110f, 465f);
            UIAnimationTestDriver driver = CreateDriver(closedPos, openPos, 0.3f);

            driver.Open();
            Assert.That(driver.IsPlaying, Is.True);

            // Simulate intermediate position during animation
            driver.Target.anchoredPosition = new Vector2(-110f, -100f);

            // Deactivate GameObject to trigger OnDisable
            driver.GameObject.SetActive(false);

            Assert.That(driver.IsPlaying, Is.False, "IsPlaying must be false after OnDisable.");
            Assert.That(driver.CurrentAnimation, Is.Null, "currentAnimation must be null after OnDisable.");
            Assert.That(
                driver.AnchoredPosition,
                Is.EqualTo(closedPos),
                "Bar must snap to closed startingPoint on disable so it does not flash upon reactivating."
            );
            Assert.That(driver.IsOpen, Is.False);
        }

        [Test]
        public void Interruption_CloseWhileOpening_ReversesDirectionTowardsStartingPoint()
        {
            Vector2 closedPos = new(-110f, -755f);
            Vector2 openPos = new(-110f, 465f);
            UIAnimationTestDriver driver = CreateDriver(closedPos, openPos, 0.3f);

            driver.Open();
            Assert.That(driver.IsOpen, Is.True);
            object openCoroutine = driver.CurrentAnimation;

            // Simulate toolbar moved halfway
            driver.Target.anchoredPosition = new Vector2(-110f, -145f);

            // Invoke Close() mid-flight
            driver.Close();
            Assert.That(driver.IsOpen, Is.False, "Logical state must switch to closed/closing.");
            object closeCoroutine = driver.CurrentAnimation;

            Assert.That(
                closeCoroutine,
                Is.Not.SameAs(openCoroutine),
                "Previous coroutine must be stopped and a new close coroutine started from current position."
            );
            Assert.That(driver.IsPlaying, Is.True);
        }

        [Test]
        public void Awake_DuplicateInstance_DoesNotDestroyGameObject()
        {
            GameObject go1 = new("UIAnim_Primary");
            createdObjects.Add(go1);
            Component anim1 = go1.AddComponent(UIAnimationType);

            GameObject go2 = new("UIAnim_Duplicate");
            createdObjects.Add(go2);
            Component anim2 = go2.AddComponent(UIAnimationType);

            Assert.That(go2 != null, Is.True, "Second GameObject must not be destroyed.");
            Assert.That(go2.activeInHierarchy, Is.True);
            Assert.That(anim2 != null, Is.True);

            PropertyInfo instanceProp = UIAnimationType.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static
            );
            Assert.That(instanceProp?.GetValue(null), Is.SameAs(anim1));
        }

        [Test]
        public void ToolBarToggle_DelegatesToUIAnimation_AndTogglesStateReliably()
        {
            Vector2 closedPos = new(-110f, -755f);
            Vector2 openPos = new(-110f, 465f);
            UIAnimationTestDriver driver = CreateDriver(closedPos, openPos, 0.3f);

            Component toggle = driver.GameObject.AddComponent(ToolBarToggleType);
            PropertyInfo uiAnimProp = ToolBarToggleType.GetProperty("UIAnimation");
            uiAnimProp?.SetValue(toggle, driver.Component);

            PropertyInfo turnedOnProp = ToolBarToggleType.GetProperty("TurnedOn");
            PropertyInfo turnedOffProp = ToolBarToggleType.GetProperty("TurnedOff");
            UnityEvent turnedOnEvent = (UnityEvent)turnedOnProp?.GetValue(toggle);
            UnityEvent turnedOffEvent = (UnityEvent)turnedOffProp?.GetValue(toggle);

            int turnedOnCount = 0;
            int turnedOffCount = 0;
            turnedOnEvent?.AddListener(() => turnedOnCount++);
            turnedOffEvent?.AddListener(() => turnedOffCount++);

            MethodInfo toggleStateMethod = ToolBarToggleType.GetMethod("ToggleState");

            // Tap 1: Opens
            toggleStateMethod?.Invoke(toggle, null);
            Assert.That(driver.IsOpen, Is.True);
            Assert.That(turnedOnCount, Is.EqualTo(1));
            Assert.That(turnedOffCount, Is.EqualTo(0));

            // Tap 2: Closes
            toggleStateMethod?.Invoke(toggle, null);
            Assert.That(driver.IsOpen, Is.False);
            Assert.That(turnedOnCount, Is.EqualTo(1));
            Assert.That(turnedOffCount, Is.EqualTo(1));
        }

        private class UIAnimationTestDriver
        {
            public GameObject GameObject { get; }
            public RectTransform Target { get; }
            public Component Component { get; }

            public UIAnimationTestDriver(
                GameObject gameObject,
                Vector2 startingPoint,
                Vector2 finalPoint,
                float duration
            )
            {
                GameObject = gameObject;
                Target = GameObject.AddComponent<RectTransform>();
                Target.anchoredPosition = startingPoint;

                Component = GameObject.AddComponent(UIAnimationType);

                SetField("target", Target);
                SetField("startingPoint", startingPoint);
                SetField("finalPoint", finalPoint);
                SetField("duration", duration);

                InvokeMethod("SyncStateFromPosition");
            }

            public bool IsOpen => (bool)GetProperty("IsOpen");
            public bool IsPlaying => (bool)GetProperty("isPlaying");
            public Vector2 AnchoredPosition => Target.anchoredPosition;
            public object CurrentAnimation => GetField("currentAnimation");

            public void Open() => InvokeMethod("Open");
            public void Close() => InvokeMethod("Close");
            public void Toggle() => InvokeMethod("Toggle");

            public object GetField(string fieldName)
            {
                FieldInfo field = UIAnimationType.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                return field?.GetValue(Component);
            }

            public void SetField(string fieldName, object value)
            {
                FieldInfo field = UIAnimationType.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                field?.SetValue(Component, value);
            }

            public object GetProperty(string propertyName)
            {
                PropertyInfo prop = UIAnimationType.GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                return prop?.GetValue(Component);
            }

            public void InvokeMethod(string methodName, params object[] args)
            {
                MethodInfo method = UIAnimationType.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                method?.Invoke(Component, args);
            }
        }
    }
}
