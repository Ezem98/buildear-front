using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Raycast_script : MonoBehaviour
{
    public GameObject spawn_prefab;
    List<GameObject> spawned_objects = new();
    ARRaycastManager arraymanager; // Devuelve una lista de las colisiones que detecto, el raycast funciona como un laser cuanto tocas que sale de donde esta la camara hasta la colision
    readonly List<ARRaycastHit> hits = new();


    // Start is called before the first frame update
    void Start()
    {
        arraymanager = GetComponent<ARRaycastManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0 && spawned_objects.Count < 1) //Spawneo
        {
            if (arraymanager.Raycast(Input.GetTouch(0).position, hits, TrackableType.PlaneWithinPolygon))
            {
                var hitpose = hits[0].pose;
                spawned_objects.Add(Instantiate(spawn_prefab, hitpose.position, hitpose.rotation));
            }
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Input.GetTouch(0).position,Input.GetTouch(0).position + new Vector2(0,5));
    }
}
