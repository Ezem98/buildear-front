using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Behavior with an API for spawning objects from a given set of prefabs.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        const float k_DefaultSurfaceOffset = 0.005f;

        [SerializeField]
        [Tooltip("The camera that objects will face when spawned. If not set, defaults to the main camera.")]
        Camera m_CameraToFace;

        /// <summary>
        /// The camera that objects will face when spawned. If not set, defaults to the <see cref="Camera.main"/> camera.
        /// </summary>
        public Camera cameraToFace
        {
            get
            {
                EnsureFacingCamera();
                return m_CameraToFace;
            }
            set => m_CameraToFace = value;
        }

        [SerializeField]
        [Tooltip("The list of prefabs available to spawn.")]
        List<GameObject> m_ObjectPrefabs = new();

        /// <summary>
        /// The list of prefabs available to spawn.
        /// </summary>
        public List<GameObject> objectPrefabs
        {
            get => m_ObjectPrefabs;
            set => m_ObjectPrefabs = value;
        }

        [SerializeField]
        [Tooltip("The list of prefabs available to spawn.")]
        List<int> m_ObjectPrefabsIndex = new();

        private Dictionary<int, int> countDictionary = new();
        readonly List<GameObject> m_SpawnedObjects = new();
        public Dictionary<int, int> CountDictionary { get => countDictionary; set => countDictionary = value; }

        /// <summary>
        /// The list of prefabs available to spawn.
        /// </summary>
        public List<int> objectPrefabsIndex
        {
            get => m_ObjectPrefabsIndex;
            set => m_ObjectPrefabsIndex = value;
        }

        [SerializeField]
        [Tooltip("Optional prefab to spawn for each spawned object. Use a prefab with the Destroy Self component to make " +
            "sure the visualization only lives temporarily.")]
        GameObject m_SpawnVisualizationPrefab;

        /// <summary>
        /// Optional prefab to spawn for each spawned object.
        /// </summary>
        /// <remarks>Use a prefab with <see cref="DestroySelf"/> to make sure the visualization only lives temporarily.</remarks>
        public GameObject spawnVisualizationPrefab
        {
            get => m_SpawnVisualizationPrefab;
            set => m_SpawnVisualizationPrefab = value;
        }

        [SerializeField]
        [Tooltip("The backend model ID associated with the prefab to spawn. Use a negative value to select a random model.")]
        int m_SpawnOptionId = -1;

        /// <summary>
        /// The backend model ID associated with the prefab to spawn. A negative value selects a random model.
        /// </summary>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public int spawnOptionId
        {
            get => m_SpawnOptionId;
            set => m_SpawnOptionId = value;
        }

        /// <summary>
        /// Whether this behavior will select a random configured model each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionId"/>
        /// <seealso cref="RandomizeSpawnOption"/>
        public bool isSpawnOptionRandomized => m_SpawnOptionId < 0;

        [SerializeField]
        [Tooltip("Whether to only spawn an object if the spawn point is within view of the camera.")]
        bool m_OnlySpawnInView = true;

        /// <summary>
        /// Whether to only spawn an object if the spawn point is within view of the <see cref="cameraToFace"/>.
        /// </summary>
        public bool onlySpawnInView
        {
            get => m_OnlySpawnInView;
            set => m_OnlySpawnInView = value;
        }

        [SerializeField]
        [Tooltip("The size, in viewport units, of the periphery inside the viewport that will not be considered in view.")]
        float m_ViewportPeriphery = 0.15f;

        /// <summary>
        /// The size, in viewport units, of the periphery inside the viewport that will not be considered in view.
        /// </summary>
        public float viewportPeriphery
        {
            get => m_ViewportPeriphery;
            set => m_ViewportPeriphery = value;
        }

        [SerializeField]
        [Tooltip("When enabled, the object will be rotated about the y-axis when spawned by Spawn Angle Range, " +
            "in relation to the direction of the spawn point to the camera.")]
        bool m_ApplyRandomAngleAtSpawn = true;

        /// <summary>
        /// When enabled, the object will be rotated about the y-axis when spawned by <see cref="spawnAngleRange"/>
        /// in relation to the direction of the spawn point to the camera.
        /// </summary>
        public bool applyRandomAngleAtSpawn
        {
            get => m_ApplyRandomAngleAtSpawn;
            set => m_ApplyRandomAngleAtSpawn = value;
        }

        [SerializeField]
        [Tooltip("The range in degrees that the object will randomly be rotated about the y axis when spawned, " +
            "in relation to the direction of the spawn point to the camera.")]
        float m_SpawnAngleRange = 45f;

        /// <summary>
        /// The range in degrees that the object will randomly be rotated about the y axis when spawned, in relation
        /// to the direction of the spawn point to the camera.
        /// </summary>
        public float spawnAngleRange
        {
            get => m_SpawnAngleRange;
            set => m_SpawnAngleRange = value;
        }

        [SerializeField]
        [Tooltip("Whether to spawn each object as a child of this object.")]
        bool m_SpawnAsChildren;

        /// <summary>
        /// Whether to spawn each object as a child of this object.
        /// </summary>
        public bool spawnAsChildren
        {
            get => m_SpawnAsChildren;
            set => m_SpawnAsChildren = value;
        }

        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        /// <seealso cref="TrySpawnObject"/>
        public event Action<GameObject> objectSpawned;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Awake()
        {
            EnsureFacingCamera();
        }

        void EnsureFacingCamera()
        {
            if (m_CameraToFace == null)
                m_CameraToFace = Camera.main;
        }

        /// <summary>
        /// Sets this behavior to select a random object from <see cref="objectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionId"/>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public void RandomizeSpawnOption()
        {
            m_SpawnOptionId = -1;
        }

        /// <summary>
        /// Attempts to spawn an object from <see cref="objectPrefabs"/> at the given position. The object will have a
        /// yaw rotation that faces <see cref="cameraToFace"/>, plus or minus a random angle within <see cref="spawnAngleRange"/>.
        /// </summary>
        /// <param name="spawnPoint">The world space position at which to spawn the object.</param>
        /// <param name="spawnNormal">The world space normal of the spawn surface.</param>
        /// <returns>Returns <see langword="true"/> if the spawner successfully spawned an object. Otherwise returns
        /// <see langword="false"/>, for instance if the spawn point is out of view of the camera.</returns>
        /// <remarks>
        /// The object selected to spawn is resolved by matching <see cref="spawnOptionId"/> against
        /// <see cref="objectPrefabsIndex"/>. A negative ID selects a random configured model; an unknown positive
        /// ID is rejected so that the wrong model cannot be placed or counted.
        /// </remarks>
        /// <seealso cref="objectSpawned"/>
        public bool TrySpawnObject(Vector3 spawnPoint, Vector3 spawnNormal)
        {
            if (m_OnlySpawnInView)
            {
                var inViewMin = m_ViewportPeriphery;
                var inViewMax = 1f - m_ViewportPeriphery;
                var pointInViewportSpace = cameraToFace.WorldToViewportPoint(spawnPoint);
                if (pointInViewportSpace.z < 0f || pointInViewportSpace.x > inViewMax || pointInViewportSpace.x < inViewMin ||
                    pointInViewportSpace.y > inViewMax || pointInViewportSpace.y < inViewMin)
                {
                    return false;
                }
            }

            if (m_ObjectPrefabs == null || m_ObjectPrefabsIndex == null || m_ObjectPrefabs.Count == 0)
            {
                Debug.LogError("ObjectSpawner has no configured prefabs.", this);
                return false;
            }

            int objectIndex;
            int modelId;
            if (isSpawnOptionRandomized)
            {
                var availableOptions = Math.Min(m_ObjectPrefabs.Count, m_ObjectPrefabsIndex.Count);
                if (availableOptions == 0)
                {
                    Debug.LogError("ObjectSpawner has no model IDs associated with its prefabs.", this);
                    return false;
                }

                objectIndex = Random.Range(0, availableOptions);
                modelId = m_ObjectPrefabsIndex[objectIndex];
            }
            else
            {
                modelId = m_SpawnOptionId;
                objectIndex = m_ObjectPrefabsIndex.IndexOf(modelId);
                if (objectIndex < 0 || objectIndex >= m_ObjectPrefabs.Count)
                {
                    Debug.LogError($"No prefab is configured for model ID {modelId}.", this);
                    return false;
                }
            }

            if (m_ObjectPrefabs[objectIndex] == null)
            {
                Debug.LogError($"The prefab configured for model ID {modelId} is null.", this);
                return false;
            }

            var newObject = Instantiate(m_ObjectPrefabs[objectIndex]);
            DisableThrowForKinematicGrabInteractables(newObject);
            var metadata = newObject.GetComponent<SpawnedModelMetadata>();
            if (metadata == null)
                metadata = newObject.AddComponent<SpawnedModelMetadata>();
            metadata.Initialize(modelId);

            if (m_SpawnAsChildren)
                newObject.transform.parent = transform;

            newObject.transform.position = GetSpawnPosition(newObject, spawnPoint, spawnNormal);
            newObject.transform.rotation = GetSpawnRotation(
                newObject,
                spawnPoint,
                spawnNormal
            );
            SnapToNearbyObject(newObject);
            RegisterSpawnedObject(newObject);
            ClosePlacementMenus(newObject);
            EnsureFacingCamera();

            var facePosition = m_CameraToFace.transform.position;
            var forward = spawnPoint - facePosition;
            BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
            // newObject.transform.rotation = Quaternion.LookRotation(projectedForward, spawnNormal);

            if (m_ApplyRandomAngleAtSpawn)
            {
                var randomRotation = Random.Range(-m_SpawnAngleRange, m_SpawnAngleRange);
                newObject.transform.Rotate(Vector3.up, randomRotation);
            }

            if (m_SpawnVisualizationPrefab != null)
            {
                var visualizationTrans = Instantiate(m_SpawnVisualizationPrefab).transform;
                visualizationTrans.position = spawnPoint;
                visualizationTrans.rotation = newObject.transform.rotation;
            }

            IncrementCount(modelId);
            objectSpawned?.Invoke(newObject);
            return true;
        }

        static Vector3 GetSpawnPosition(GameObject spawnedObject, Vector3 spawnPoint, Vector3 spawnNormal)
        {
            var placementOffset = spawnedObject.GetComponent<SurfacePlacementOffset>();
            if (spawnNormal.sqrMagnitude <= Mathf.Epsilon)
                return spawnPoint;

            float offset = placementOffset != null
                ? placementOffset.offset
                : k_DefaultSurfaceOffset;
            return spawnPoint + spawnNormal.normalized * offset;
        }

        Quaternion GetSpawnRotation(
            GameObject spawnedObject,
            Vector3 spawnPoint,
            Vector3 spawnNormal
        )
        {
            var placementSettings = spawnedObject.GetComponent<SurfacePlacementOffset>();
            Quaternion currentRotation = spawnedObject.transform.rotation;
            if (placementSettings == null)
                return currentRotation;

            if (
                placementSettings.faceCameraOnSurface &&
                spawnNormal.sqrMagnitude > Mathf.Epsilon
            )
            {
                EnsureFacingCamera();
                if (m_CameraToFace != null)
                {
                    Vector3 normalizedSurfaceNormal = spawnNormal.normalized;
                    Vector3 cameraToSpawn = Vector3.ProjectOnPlane(
                        spawnPoint - m_CameraToFace.transform.position,
                        normalizedSurfaceNormal
                    );

                    // A door can be configured for a detected floor or wall.
                    // On a wall, keep it upright and choose the normal that points
                    // away from the camera so its controls stay on the visible side.
                    if (Mathf.Abs(Vector3.Dot(normalizedSurfaceNormal, Vector3.up)) < 0.5f)
                    {
                        Vector3 facingNormal = normalizedSurfaceNormal;
                        Vector3 cameraToObject =
                            spawnPoint - m_CameraToFace.transform.position;
                        if (Vector3.Dot(facingNormal, cameraToObject) < 0f)
                            facingNormal = -facingNormal;

                        return Quaternion.LookRotation(facingNormal, Vector3.up);
                    }

                    if (cameraToSpawn.sqrMagnitude > Mathf.Epsilon)
                    {
                        return Quaternion.LookRotation(
                            cameraToSpawn.normalized,
                            normalizedSurfaceNormal
                        );
                    }
                }
            }

            return placementSettings.GetSurfaceAlignedRotation(
                currentRotation,
                spawnNormal
            );
        }

        static void ClosePlacementMenus(GameObject spawnedObject)
        {
            var placementSettings = spawnedObject.GetComponent<SurfacePlacementOffset>();
            if (placementSettings != null && placementSettings.activateCanvasOnSelect)
                placementSettings.HideMenusInSnapGroup(true);
        }

        void SnapToNearbyObject(GameObject spawnedObject)
        {
            var snapSettings = spawnedObject.GetComponent<SurfacePlacementOffset>();
            var spawnedCollider = spawnedObject.GetComponent<BoxCollider>();
            if (snapSettings == null || !snapSettings.enableEdgeSnap || spawnedCollider == null)
                return;

            Vector3 desiredPosition = spawnedObject.transform.position;
            Vector3 snappedPosition = desiredPosition;
            Quaternion snappedRotation = spawnedObject.transform.rotation;
            float closestDistance = snapSettings.snapDistance;

            for (int index = m_SpawnedObjects.Count - 1; index >= 0; index--)
            {
                GameObject otherObject = m_SpawnedObjects[index];
                if (otherObject == null)
                {
                    m_SpawnedObjects.RemoveAt(index);
                    continue;
                }

                var otherSettings = otherObject.GetComponent<SurfacePlacementOffset>();
                if (otherSettings == null)
                    continue;

                if (!TryGetClosestAdjacentPose(
                    spawnedObject,
                    spawnedCollider,
                    otherSettings,
                    desiredPosition,
                    out Vector3 candidatePosition,
                    out Quaternion candidateRotation,
                    out float candidateDistance
                ))
                    continue;

                if (candidateDistance <= closestDistance)
                {
                    closestDistance = candidateDistance;
                    snappedPosition = candidatePosition;
                    snappedRotation = candidateRotation;
                }
            }

            spawnedObject.transform.SetPositionAndRotation(snappedPosition, snappedRotation);
        }

        public void RegisterSpawnedObject(GameObject spawnedObject)
        {
            if (spawnedObject != null && !m_SpawnedObjects.Contains(spawnedObject))
                m_SpawnedObjects.Add(spawnedObject);
        }

        public void UnregisterSpawnedObject(GameObject spawnedObject)
        {
            if (spawnedObject != null)
                m_SpawnedObjects.Remove(spawnedObject);
        }

        public static bool SnapNextToObject(GameObject objectToSnap, GameObject referenceObject)
        {
            if (objectToSnap == null || referenceObject == null)
                return false;

            var snapSettings = objectToSnap.GetComponent<SurfacePlacementOffset>();
            var referenceSettings = referenceObject.GetComponent<SurfacePlacementOffset>();
            var objectCollider = objectToSnap.GetComponent<BoxCollider>();
            if (snapSettings == null || referenceSettings == null || objectCollider == null ||
                !snapSettings.enableEdgeSnap || !referenceSettings.enableEdgeSnap ||
                snapSettings.snapGroup != referenceSettings.snapGroup)
            {
                return false;
            }

            if (!TryGetClosestAdjacentPose(
                objectToSnap,
                objectCollider,
                referenceSettings,
                objectToSnap.transform.position,
                out Vector3 snappedPosition,
                out Quaternion snappedRotation,
                out _
            ))
            {
                return false;
            }

            objectToSnap.transform.SetPositionAndRotation(snappedPosition, snappedRotation);
            return true;
        }

        public bool SnapCopyToFreeEdge(GameObject objectToSnap, GameObject referenceObject)
        {
            if (objectToSnap == null || referenceObject == null)
                return false;

            var snapSettings = objectToSnap.GetComponent<SurfacePlacementOffset>();
            var referenceSettings = referenceObject.GetComponent<SurfacePlacementOffset>();
            var objectCollider = objectToSnap.GetComponent<BoxCollider>();
            if (snapSettings == null || referenceSettings == null || objectCollider == null ||
                !snapSettings.enableEdgeSnap || !referenceSettings.enableEdgeSnap ||
                snapSettings.snapGroup != referenceSettings.snapGroup ||
                !TryGetAdjacentPoses(
                    objectToSnap,
                    objectCollider,
                    referenceSettings,
                    out Vector3[] candidatePositions,
                    out Quaternion snappedRotation
                ))
            {
                return false;
            }

            foreach (Vector3 candidatePosition in candidatePositions)
            {
                if (!IsPlacementOccupied(
                    objectToSnap,
                    objectCollider,
                    referenceObject,
                    candidatePosition,
                    snappedRotation
                ))
                {
                    objectToSnap.transform.SetPositionAndRotation(
                        candidatePosition,
                        snappedRotation
                    );
                    return true;
                }
            }

            objectToSnap.transform.SetPositionAndRotation(
                candidatePositions[0],
                snappedRotation
            );
            return true;
        }

        bool IsPlacementOccupied(
            GameObject objectToPlace,
            BoxCollider objectCollider,
            GameObject referenceObject,
            Vector3 candidatePosition,
            Quaternion candidateRotation
        )
        {
            const float edgeTolerance = 0.001f;
            Vector3 axisX = candidateRotation * Vector3.right;
            Vector3 axisY = candidateRotation * Vector3.up;
            float objectHalfX = GetScaledHalfSize(objectCollider, 0);
            float objectHalfY = GetScaledHalfSize(objectCollider, 1);
            Vector3 objectCenter = candidatePosition + candidateRotation * Vector3.Scale(
                objectCollider.center,
                objectToPlace.transform.lossyScale
            );

            for (int index = m_SpawnedObjects.Count - 1; index >= 0; index--)
            {
                GameObject otherObject = m_SpawnedObjects[index];
                if (otherObject == null)
                {
                    m_SpawnedObjects.RemoveAt(index);
                    continue;
                }

                if (otherObject == objectToPlace || otherObject == referenceObject)
                    continue;

                var otherCollider = otherObject.GetComponent<BoxCollider>();
                var otherPlacement = otherObject.GetComponent<SurfacePlacementOffset>();
                if (otherCollider == null || otherPlacement == null)
                    continue;

                Vector3 centerDelta = otherCollider.transform.TransformPoint(otherCollider.center) -
                    objectCenter;
                float combinedHalfX = objectHalfX + GetScaledHalfSize(otherCollider, 0);
                float combinedHalfY = objectHalfY + GetScaledHalfSize(otherCollider, 1);
                if (Mathf.Abs(Vector3.Dot(centerDelta, axisX)) < combinedHalfX - edgeTolerance &&
                    Mathf.Abs(Vector3.Dot(centerDelta, axisY)) < combinedHalfY - edgeTolerance)
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryGetClosestAdjacentPose(
            GameObject objectToSnap,
            BoxCollider objectCollider,
            SurfacePlacementOffset referenceSettings,
            Vector3 desiredPosition,
            out Vector3 snappedPosition,
            out Quaternion snappedRotation,
            out float snappedDistance
        )
        {
            if (!TryGetAdjacentPoses(
                objectToSnap,
                objectCollider,
                referenceSettings,
                out Vector3[] candidatePositions,
                out snappedRotation
            ))
            {
                snappedPosition = default;
                snappedDistance = default;
                return false;
            }

            snappedPosition = default;
            snappedDistance = float.MaxValue;
            foreach (Vector3 candidatePosition in candidatePositions)
            {
                float candidateDistance = Vector3.Distance(desiredPosition, candidatePosition);
                if (candidateDistance < snappedDistance)
                {
                    snappedDistance = candidateDistance;
                    snappedPosition = candidatePosition;
                }
            }

            return true;
        }

        static bool TryGetAdjacentPoses(
            GameObject objectToSnap,
            BoxCollider objectCollider,
            SurfacePlacementOffset referenceSettings,
            out Vector3[] candidatePositions,
            out Quaternion snappedRotation
        )
        {
            var referenceCollider = referenceSettings.GetComponent<BoxCollider>();
            if (referenceCollider == null)
            {
                candidatePositions = default;
                snappedRotation = default;
                return false;
            }

            Transform referenceTransform = referenceSettings.transform;
            Vector3 axisX = referenceTransform.right.normalized;
            Vector3 axisY = referenceTransform.up.normalized;
            float combinedHalfX = GetScaledHalfSize(referenceCollider, 0) +
                GetScaledHalfSize(objectCollider, 0);
            float combinedHalfY = GetScaledHalfSize(referenceCollider, 1) +
                GetScaledHalfSize(objectCollider, 1);
            Vector3 referenceCenter = referenceTransform.TransformPoint(referenceCollider.center);
            snappedRotation = referenceTransform.rotation;
            Vector3 objectCenterOffset = snappedRotation * Vector3.Scale(
                objectCollider.center,
                objectToSnap.transform.lossyScale
            );

            Vector3[] candidateCenters =
            {
                referenceCenter + axisX * combinedHalfX,
                referenceCenter - axisX * combinedHalfX,
                referenceCenter + axisY * combinedHalfY,
                referenceCenter - axisY * combinedHalfY,
            };

            candidatePositions = new Vector3[candidateCenters.Length];
            for (int index = 0; index < candidateCenters.Length; index++)
            {
                candidatePositions[index] = candidateCenters[index] - objectCenterOffset;
            }

            return true;
        }

        static float GetScaledHalfSize(BoxCollider boxCollider, int axis)
        {
            return boxCollider.size[axis] * Mathf.Abs(boxCollider.transform.lossyScale[axis]) * 0.5f;
        }

        static void DisableThrowForKinematicGrabInteractables(GameObject root)
        {
            foreach (var grabInteractable in root.GetComponentsInChildren<XRGrabInteractable>(true))
            {
                var rigidbody = grabInteractable.GetComponent<Rigidbody>();
                if (rigidbody != null && rigidbody.isKinematic)
                    grabInteractable.throwOnDetach = false;
            }
        }

        public void IncrementCount(int modelId)
        {
            if (countDictionary.TryGetValue(modelId, out var currentCount))
                countDictionary[modelId] = currentCount + 1;
            else
                countDictionary.Add(modelId, 1);
        }

        public void ReduceCount(int modelId)
        {
            if (countDictionary.TryGetValue(modelId, out var currentCount))
            {
                if (currentCount <= 1)
                    countDictionary.Remove(modelId);
                else
                    countDictionary[modelId] = currentCount - 1;
            }
        }
    }
}
