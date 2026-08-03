using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
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

        [Test]
        public void TrySpawnObject_NearMatchingEdge_SnapsModelsTogether()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Snapping floor prefab"));
            BoxCollider boxCollider = prefab.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.8f, 0.8f, 0.01f);
            SurfacePlacementOffset placementSettings =
                prefab.AddComponent<SurfacePlacementOffset>();
            placementSettings.enableEdgeSnap = true;
            placementSettings.snapDistance = 0.25f;
            placementSettings.snapGroup = "Ceramic";

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 8 };
            spawner.spawnOptionId = 8;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Assert.That(spawner.TrySpawnObject(Vector3.zero, Vector3.forward), Is.True);
            Assert.That(
                spawner.TrySpawnObject(new Vector3(0.72f, 0.03f, 0f), Vector3.forward),
                Is.True
            );

            Transform secondModel = spawner.transform.GetChild(1);
            Assert.That(secondModel.position.x, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(secondModel.position.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(secondModel.rotation, Is.EqualTo(spawner.transform.GetChild(0).rotation));
        }

        [Test]
        public void SnapNextToObject_PlacesCopyFlushAgainstSourceEdge()
        {
            GameObject source = CreateSnappingCeramic("Source ceramic", Vector3.zero);
            GameObject copy = CreateSnappingCeramic(
                "Copied ceramic",
                new Vector3(0f, -0.5f, 0f)
            );

            bool snapped = ObjectSpawner.SnapNextToObject(copy, source);

            Assert.That(snapped, Is.True);
            Assert.That(copy.transform.position.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(copy.transform.position.y, Is.EqualTo(-0.8f).Within(0.0001f));
            Assert.That(copy.transform.position.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(copy.transform.rotation, Is.EqualTo(source.transform.rotation));
        }

        [Test]
        public void CeramicPrefab_RuntimeColliderCoversCompleteMesh()
        {
            const string ceramicPath = "Assets/Prefabs/Pisos/Ceramic.prefab";
            GameObject ceramicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ceramicPath);
            GameObject ceramic = Track(Object.Instantiate(ceramicPrefab));
            MeshFilter meshFilter = ceramic.GetComponent<MeshFilter>();
            BoxCollider boxCollider = ceramic.GetComponent<BoxCollider>();

            Assert.That(meshFilter, Is.Not.Null);
            Assert.That(boxCollider, Is.Not.Null);

            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
            Assert.That(boxCollider.size.x, Is.EqualTo(Mathf.Max(meshSize.x, 0.01f)));
            Assert.That(boxCollider.size.y, Is.EqualTo(Mathf.Max(meshSize.y, 0.01f)));
            Assert.That(boxCollider.size.z, Is.EqualTo(Mathf.Max(meshSize.z, 0.01f)));
            Assert.That(boxCollider.center, Is.EqualTo(meshFilter.sharedMesh.bounds.center));
        }

        [Test]
        public void CeramicSelection_ActivatesCanvasOnFirstSelect()
        {
            GameObject ceramic = Track(new GameObject("Selectable ceramic"));
            ceramic.AddComponent<BoxCollider>();
            Rigidbody rigidbody = ceramic.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            XRGrabInteractable grabInteractable = ceramic.AddComponent<XRGrabInteractable>();
            grabInteractable.throwOnDetach = false;
            CanvasActivationReceiver receiver = ceramic.AddComponent<CanvasActivationReceiver>();
            SurfacePlacementOffset placementSettings =
                ceramic.AddComponent<SurfacePlacementOffset>();
            placementSettings.activateCanvasOnSelect = true;

            Assert.That(grabInteractable.selectFilters.count, Is.EqualTo(1));
            grabInteractable.selectEntered.Invoke(null);

            Assert.That(receiver.ActivationCount, Is.EqualTo(1));
        }

        [Test]
        public void CeramicSelection_ClosesOtherCeramicMenuBeforeOpeningSelectedOne()
        {
            GameObject firstCeramic = CreateSelectableCeramic("First ceramic");
            CanvasActivationReceiver firstReceiver =
                firstCeramic.GetComponent<CanvasActivationReceiver>();
            GameObject secondCeramic = CreateSelectableCeramic("Second ceramic");
            CanvasActivationReceiver secondReceiver =
                secondCeramic.GetComponent<CanvasActivationReceiver>();

            secondCeramic.GetComponent<XRGrabInteractable>().selectEntered.Invoke(null);

            Assert.That(firstReceiver.HideCount, Is.EqualTo(1));
            Assert.That(firstReceiver.ActivationCount, Is.Zero);
            Assert.That(secondReceiver.HideCount, Is.Zero);
            Assert.That(secondReceiver.ActivationCount, Is.EqualTo(1));
        }

        [Test]
        public void TrySpawnObject_CeramicClosesAllCeramicMenusWithoutOpeningOne()
        {
            GameObject existingCeramic = CreateSelectableCeramic("Existing ceramic");
            CanvasActivationReceiver existingReceiver =
                existingCeramic.GetComponent<CanvasActivationReceiver>();
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = CreateSelectableCeramic("Ceramic prefab");

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 8 };
            spawner.spawnOptionId = 8;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Assert.That(spawner.TrySpawnObject(Vector3.forward, Vector3.up), Is.True);

            CanvasActivationReceiver spawnedReceiver = spawner.transform
                .GetChild(0)
                .GetComponent<CanvasActivationReceiver>();
            Assert.That(existingReceiver.HideCount, Is.EqualTo(1));
            Assert.That(existingReceiver.ActivationCount, Is.Zero);
            Assert.That(spawnedReceiver.HideCount, Is.EqualTo(1));
            Assert.That(spawnedReceiver.ActivationCount, Is.Zero);
        }

        [Test]
        public void SelectAttempt_AfterSelection_DoesNotBlockFollowingClick()
        {
            bool everHadSelection = true;

            bool spawnFromSelectionClick = ARInteractorSpawnTrigger.ShouldSpawnAfterSelectAttempt(
                false,
                ref everHadSelection
            );
            bool spawnFromFollowingEmptyClick = ARInteractorSpawnTrigger.ShouldSpawnAfterSelectAttempt(
                false,
                ref everHadSelection
            );

            Assert.That(spawnFromSelectionClick, Is.False);
            Assert.That(spawnFromFollowingEmptyClick, Is.True);
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

        private GameObject CreateSelectableCeramic(string name)
        {
            GameObject ceramic = Track(new GameObject(name));
            ceramic.AddComponent<BoxCollider>();
            Rigidbody rigidbody = ceramic.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            XRGrabInteractable grabInteractable = ceramic.AddComponent<XRGrabInteractable>();
            grabInteractable.throwOnDetach = false;
            ceramic.AddComponent<CanvasActivationReceiver>();
            SurfacePlacementOffset placementSettings =
                ceramic.AddComponent<SurfacePlacementOffset>();
            placementSettings.snapGroup = "Ceramic";
            placementSettings.activateCanvasOnSelect = true;
            return ceramic;
        }

        private GameObject CreateSnappingCeramic(string name, Vector3 position)
        {
            GameObject ceramic = Track(new GameObject(name));
            ceramic.transform.position = position;
            BoxCollider boxCollider = ceramic.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.8f, 0.8f, 0.01f);
            SurfacePlacementOffset placementSettings =
                ceramic.AddComponent<SurfacePlacementOffset>();
            placementSettings.enableEdgeSnap = true;
            placementSettings.snapGroup = "Ceramic";
            return ceramic;
        }

        private GameObject Track(GameObject target)
        {
            objectsToDestroy.Add(target);
            return target;
        }
    }

    public class CanvasActivationReceiver : MonoBehaviour
    {
        public int ActivationCount { get; private set; }
        public int HideCount { get; private set; }

        public void ActivateModelCanvas()
        {
            ActivationCount++;
        }

        public void HideCanvas()
        {
            HideCount++;
        }
    }
}
