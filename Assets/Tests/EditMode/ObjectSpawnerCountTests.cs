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
        private const string TestSnapGroup = "BuildeAR.Tests.Ceramic";
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
        public void ClearSpawnedObjects_RemovesPlacedModelsAndResetsCounts()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Model prefab"));

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 42 };
            spawner.spawnOptionId = 42;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Assert.That(spawner.TrySpawnObject(Vector3.forward, Vector3.up), Is.True);
            Assert.That(spawner.transform.childCount, Is.EqualTo(1));
            Assert.That(spawner.CountDictionary[42], Is.EqualTo(1));

            spawner.ClearSpawnedObjects();

            Assert.That(spawner.transform.childCount, Is.Zero);
            Assert.That(spawner.CountDictionary, Is.Empty);
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
        public void CeramicRotation_UsesSurfaceNormalAndKeepsModelFlat()
        {
            GameObject ceramic = Track(new GameObject("Ceramic"));
            SurfacePlacementOffset placementSettings =
                ceramic.AddComponent<SurfacePlacementOffset>();
            placementSettings.enableEdgeSnap = true;
            Vector3 surfaceNormal = ceramic.transform.forward;

            Vector3 rotationAxis = SurfacePlacementOffset.GetLocalRotationAxis(
                ceramic,
                true
            );
            ceramic.transform.Rotate(rotationAxis, 90f, Space.Self);

            Assert.That(rotationAxis, Is.EqualTo(Vector3.back));
            Assert.That(
                Vector3.Dot(surfaceNormal, ceramic.transform.forward),
                Is.EqualTo(1f).Within(0.0001f)
            );
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
        public void TrySpawnObject_WithSurfaceAlignment_MakesModelParallelToPlane()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Window prefab"));
            SurfacePlacementOffset placementSettings =
                prefab.AddComponent<SurfacePlacementOffset>();
            placementSettings.alignToSurfaceNormal = true;
            placementSettings.localSurfaceNormal = Vector3.right;

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 7 };
            spawner.spawnOptionId = 7;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Vector3 wallNormal = Vector3.forward;
            Assert.That(spawner.TrySpawnObject(Vector3.zero, wallNormal), Is.True);

            Transform spawnedWindow = spawner.transform.GetChild(0);
            Vector3 spawnedWindowNormal = spawnedWindow.rotation * Vector3.right;
            Assert.That(
                Vector3.Dot(spawnedWindowNormal, wallNormal),
                Is.EqualTo(1f).Within(0.0001f)
            );
            Assert.That(
                Vector3.Dot(spawnedWindow.up, Vector3.up),
                Is.EqualTo(1f).Within(0.0001f)
            );
        }

        [Test]
        public void TrySpawnObject_WithCameraFacingPlacement_FacesDoorTowardCameraSide()
        {
            ObjectSpawner spawner = CreateSpawner();
            spawner.cameraToFace.transform.position = new Vector3(0f, 0f, -5f);
            GameObject prefab = Track(new GameObject("Door prefab"));
            prefab.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            SurfacePlacementOffset placementSettings =
                prefab.AddComponent<SurfacePlacementOffset>();
            placementSettings.faceCameraOnSurface = true;

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 1 };
            spawner.spawnOptionId = 1;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Assert.That(spawner.TrySpawnObject(Vector3.zero, Vector3.up), Is.True);

            Transform spawnedDoor = spawner.transform.GetChild(0);
            Assert.That(
                Vector3.Dot(spawnedDoor.forward, Vector3.forward),
                Is.EqualTo(1f).Within(0.0001f)
            );
            Assert.That(
                Vector3.Dot(spawnedDoor.up, Vector3.up),
                Is.EqualTo(1f).Within(0.0001f)
            );
        }

        [Test]
        public void TrySpawnObject_OnVerticalWall_KeepsCameraFacingDoorUpright()
        {
            ObjectSpawner spawner = CreateSpawner();
            spawner.cameraToFace.transform.position = new Vector3(0f, 0f, -5f);
            GameObject prefab = Track(new GameObject("Door prefab"));
            prefab.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            SurfacePlacementOffset placementSettings =
                prefab.AddComponent<SurfacePlacementOffset>();
            placementSettings.faceCameraOnSurface = true;

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 1 };
            spawner.spawnOptionId = 1;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Assert.That(spawner.TrySpawnObject(Vector3.zero, Vector3.back), Is.True);

            Transform spawnedDoor = spawner.transform.GetChild(0);
            Assert.That(
                Vector3.Dot(spawnedDoor.forward, Vector3.forward),
                Is.EqualTo(1f).Within(0.0001f)
            );
            Assert.That(
                Vector3.Dot(spawnedDoor.up, Vector3.up),
                Is.EqualTo(1f).Within(0.0001f)
            );
        }

        [Test]
        public void TrySpawnObject_WithoutPlacementSettings_AppliesDefaultSurfaceOffset()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject prefab = Track(new GameObject("Wall model prefab"));

            spawner.objectPrefabs = new List<GameObject> { prefab };
            spawner.objectPrefabsIndex = new List<int> { 6 };
            spawner.spawnOptionId = 6;
            spawner.spawnAsChildren = true;
            spawner.onlySpawnInView = false;

            Vector3 spawnPoint = new(1f, 2f, 3f);
            bool spawned = spawner.TrySpawnObject(spawnPoint, Vector3.forward);

            Assert.That(spawned, Is.True);
            Assert.That(
                spawner.transform.GetChild(0).position,
                Is.EqualTo(spawnPoint + Vector3.forward * 0.005f)
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
        public void WindowPrefab_AlignsItsThinAxisToScannedWallNormal()
        {
            const string windowPath =
                "Assets/Prefabs/Ventanas/VentanaDobleAluminioModel.prefab";
            GameObject window = AssetDatabase.LoadAssetAtPath<GameObject>(windowPath);

            Assert.That(window, Is.Not.Null);
            SurfacePlacementOffset placementSettings =
                window.GetComponent<SurfacePlacementOffset>();
            Assert.That(placementSettings, Is.Not.Null);
            Assert.That(placementSettings.alignToSurfaceNormal, Is.True);
            Assert.That(placementSettings.localSurfaceNormal, Is.EqualTo(Vector3.right));
        }

        [Test]
        public void DoorPrefab_FacesItsControlsTowardCameraAtPlacement()
        {
            const string doorPath = "Assets/Prefabs/Puertas/PuertaMaderaRoja.prefab";
            GameObject door = AssetDatabase.LoadAssetAtPath<GameObject>(doorPath);

            Assert.That(door, Is.Not.Null);
            SurfacePlacementOffset placementSettings =
                door.GetComponent<SurfacePlacementOffset>();
            Assert.That(placementSettings, Is.Not.Null);
            Assert.That(placementSettings.faceCameraOnSurface, Is.True);
        }

        [Test]
        public void WallPrefab_HasOnlyOneEnabledCanvasManager()
        {
            const string wallPath = "Assets/Prefabs/Paredes/fence.prefab";
            GameObject wall = AssetDatabase.LoadAssetAtPath<GameObject>(wallPath);

            Assert.That(wall, Is.Not.Null);
            int enabledCanvasManagers = 0;
            foreach (MonoBehaviour behaviour in wall.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (
                    behaviour != null &&
                    behaviour.enabled &&
                    behaviour.GetType().Name == "CanvasManager"
                )
                {
                    enabledCanvasManagers++;
                }
            }

            Assert.That(enabledCanvasManagers, Is.EqualTo(1));
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
        public void SnapCopyToFreeEdge_PrefersSideAndAvoidsOccupiedEdge()
        {
            ObjectSpawner spawner = CreateSpawner();
            GameObject source = CreateSnappingCeramic("Source ceramic", Vector3.zero);
            GameObject firstCopy = CreateSnappingCeramic("First copy", Vector3.zero);
            GameObject secondCopy = CreateSnappingCeramic("Second copy", Vector3.zero);
            spawner.RegisterSpawnedObject(source);

            Assert.That(spawner.SnapCopyToFreeEdge(firstCopy, source), Is.True);
            Assert.That(firstCopy.transform.position.x, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(firstCopy.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
            spawner.RegisterSpawnedObject(firstCopy);

            Assert.That(spawner.SnapCopyToFreeEdge(secondCopy, source), Is.True);
            Assert.That(secondCopy.transform.position.x, Is.EqualTo(-0.8f).Within(0.0001f));
            Assert.That(secondCopy.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void CeramicPrefab_RuntimeColliderCoversCompleteMesh()
        {
            const string ceramicPath = "Assets/Prefabs/Pisos/Ceramic.prefab";
            GameObject ceramicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ceramicPath);
            GameObject ceramic = Track(Object.Instantiate(ceramicPrefab));
            MeshFilter meshFilter = ceramic.GetComponent<MeshFilter>();
            BoxCollider boxCollider = ceramic.GetComponent<BoxCollider>();
            SurfacePlacementOffset placementSettings =
                ceramic.GetComponent<SurfacePlacementOffset>();

            Assert.That(meshFilter, Is.Not.Null);
            Assert.That(boxCollider, Is.Not.Null);
            Assert.That(placementSettings, Is.Not.Null);

            placementSettings.FitBoxColliderToMesh();

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
            placementSettings.snapGroup = TestSnapGroup;
            placementSettings.activateCanvasOnSelect = true;

            Assert.That(grabInteractable.selectFilters.count, Is.EqualTo(1));
            placementSettings.ActivateSelectionMenu();

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

            secondCeramic.GetComponent<SurfacePlacementOffset>().ActivateSelectionMenu();

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
            spawner.applyRandomAngleAtSpawn = false;
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
            placementSettings.snapGroup = TestSnapGroup;
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
            placementSettings.snapGroup = TestSnapGroup;
            return ceramic;
        }

        private GameObject Track(GameObject target)
        {
            objectsToDestroy.Add(target);
            return target;
        }
    }

    public class CanvasActivationReceiver : MonoBehaviour, IModelCanvasController
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
