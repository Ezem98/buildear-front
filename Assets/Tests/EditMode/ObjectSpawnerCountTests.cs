using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace BuildeAR.Tests.EditMode
{
    public class ObjectSpawnerCountTests
    {
        private readonly List<GameObject> objectsToDestroy = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject target in objectsToDestroy)
            {
                if (target != null)
                    Object.DestroyImmediate(target);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void IncrementAndReduceCount_UsesModelIdAsKey()
        {
            ObjectSpawner spawner = CreateSpawner();

            spawner.IncrementCount(42);
            spawner.IncrementCount(42);
            spawner.IncrementCount(7);

            Assert.That(spawner.CountDictionary[42], Is.EqualTo(2));
            Assert.That(spawner.CountDictionary[7], Is.EqualTo(1));

            spawner.ReduceCount(42);
            Assert.That(spawner.CountDictionary[42], Is.EqualTo(1));

            spawner.ReduceCount(42);
            Assert.That(spawner.CountDictionary.ContainsKey(42), Is.False);
            Assert.That(spawner.CountDictionary[7], Is.EqualTo(1));
        }

        [Test]
        public void TrySpawnObject_StoresBackendModelIdInCountAndMetadata()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Model prefab"));

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 42 };
            spawner.spawnOptionId = 42;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            bool spawned = spawner.TrySpawnObject(Vector3.forward, Vector3.up);

            Assert.That(spawned, Is.True);
            Assert.That(spawner.CountDictionary.ContainsKey(0), Is.False);
            Assert.That(spawner.CountDictionary[42], Is.EqualTo(1));
            Assert.That(spawner.transform.childCount, Is.EqualTo(1));

            SpawnedModelMetadata metadata = spawner.transform.GetChild(0).GetComponent<SpawnedModelMetadata>();
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.ModelId, Is.EqualTo(42));
        }

        [Test]
        public void TrySpawnObject_WithUnknownModelId_ReturnsFalseWithoutChangingCounts()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Model prefab"));

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 42 };
            spawner.spawnOptionId = 999;
            spawner.onlySpawnInView = false;

            LogAssert.Expect(LogType.Error, "No prefab is configured for model ID 999.");
            bool spawned = spawner.TrySpawnObject(Vector3.zero, Vector3.up);

            Assert.That(spawned, Is.False);
            Assert.That(spawner.CountDictionary, Is.Empty);
        }

        [Test]
        public void TrySpawnObject_DisablesThrowForKinematicGrabInteractable()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Kinematic model prefab"));
            Rigidbody rigidbody = prefab.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            XRGrabInteractable grabInteractable = prefab.AddComponent<XRGrabInteractable>();
            grabInteractable.throwOnDetach = true;

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 42 };
            spawner.spawnOptionId = 42;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            bool spawned = spawner.TrySpawnObject(Vector3.forward, Vector3.up);

            Assert.That(spawned, Is.True);
            XRGrabInteractable spawnedGrab = spawner.transform.GetChild(0).GetComponent<XRGrabInteractable>();
            Assert.That(spawnedGrab.throwOnDetach, Is.False);
        }

        [Test]
        public void TrySpawnObject_WithSurfaceOffset_MovesModelAlongSurfaceNormal()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Floor model prefab"));
            SurfacePlacementOffset placementOffset = prefab.AddComponent<SurfacePlacementOffset>();
            placementOffset.offset = 0.005f;

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 8 };
            spawner.spawnOptionId = 8;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Vector3 spawnPoint = new(1f, 2f, 3f);
            bool spawned = spawner.TrySpawnObject(spawnPoint, Vector3.up);

            Assert.That(spawned, Is.True);
            Assert.That(
                spawner.transform.GetChild(0).position,
                Is.EqualTo(spawnPoint + Vector3.up * 0.005f)
            );
        }

        [Test]
        public void CeramicPrefab_HasSurfaceOffsetToAvoidPlaneZFighting()
        {
            const string ceramicPath = "Assets/Prefabs/Pisos/Ceramic.prefab";
            GameObject ceramic = AssetDatabase.LoadAssetAtPath<GameObject>(ceramicPath);

            Assert.That(ceramic, Is.Not.Null);
            SurfacePlacementOffset placementOffset = ceramic.GetComponent<SurfacePlacementOffset>();
            Assert.That(placementOffset, Is.Not.Null);
            Assert.That(placementOffset.offset, Is.EqualTo(0.005f));
        }

        private ObjectSpawner CreateSpawner()
        {
            GameObject cameraObject = Track(new GameObject("Main Camera"));
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject spawnerObject = Track(new GameObject("Object Spawner"));
            ObjectSpawner spawner = spawnerObject.AddComponent<ObjectSpawner>();
            spawner.cameraToFace = camera;
            return spawner;
        }

        private GameObject Track(GameObject target)
        {
            objectsToDestroy.Add(target);
            return target;
        }
    }
}
