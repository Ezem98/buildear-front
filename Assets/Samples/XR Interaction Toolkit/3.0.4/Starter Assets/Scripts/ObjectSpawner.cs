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

            newObject.transform.position = spawnPoint;
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
