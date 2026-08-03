using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BuildeAR.Tests.EditMode
{
    public class KinematicGrabInteractableTests
    {
        [Test]
        public void KinematicGrabInteractables_DoNotThrowOnDetach()
        {
            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/Prefabs", "Assets/Models" }
            );
            List<string> invalidPrefabs = new();

            foreach (string prefabGuid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                foreach (
                    XRGrabInteractable grabInteractable in
                    prefab.GetComponentsInChildren<XRGrabInteractable>(true)
                )
                {
                    Rigidbody rigidbody = grabInteractable.GetComponent<Rigidbody>();
                    if (
                        rigidbody != null &&
                        rigidbody.isKinematic &&
                        grabInteractable.throwOnDetach
                    )
                    {
                        invalidPrefabs.Add($"{path} ({grabInteractable.name})");
                    }
                }
            }

            Assert.That(
                invalidPrefabs,
                Is.Empty,
                "Kinematic grab interactables must disable Throw On Detach:\n" +
                string.Join("\n", invalidPrefabs)
            );
        }
    }
}
