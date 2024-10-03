using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resize : MonoBehaviour
{
    public Vector3 resizeDirection = Vector3.right; // Dirección en la que la pared se redimensionará
    public Transform anchorPoint; // Punto de anclaje, puede ser uno de los bordes de la pared

    private Vector3 initialScale;
    private Vector3 initialPosition;

    void Start()
    {
        // Guarda la escala y posición inicial del objeto
        initialScale = transform.localScale;
        initialPosition = transform.position;
    }

    public void ResizeOnDirection(float scaleAmount)
    {
        // Calcula el cambio de escala en función de la dirección deseada
        Vector3 scaleChange = resizeDirection * scaleAmount;
        Vector3 newScale = initialScale + scaleChange;

        // Actualiza la posición del objeto para simular el anclaje en el punto deseado
        Vector3 positionChange = resizeDirection / 2 * scaleAmount;
        Vector3 newPosition = initialPosition + positionChange;

        // Aplica la nueva escala y posición
        transform.localScale = newScale;
        transform.position = newPosition;
    }
}
