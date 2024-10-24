using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GyroscopeManager : MonoBehaviour
{
    private Gyroscope gyro;
    public TextMeshProUGUI pitchText; // Eje X (Pitch)
    public TextMeshProUGUI rollText; // Eje Z (Roll)
    public TextMeshProUGUI yawText; // Eje Y (Yaw)
    [SerializeField] private Slider pitchSlider;
    [SerializeField] private Slider rollSlider;
    [SerializeField] private Slider yawSlider;
    public float tolerance = 2.0f; // Tolerancia para considerar que el teléfono está "nivelado"

    private Vector3 calibratedRotation = Vector3.zero;
    private bool isCalibrated = false;
    // Start is called before the first frame update
    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            gyro = Input.gyro;
            gyro.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.gyro.enabled)
        {
            Vector3 currentRotation = Input.gyro.attitude.eulerAngles;

            float pitch = NormalizeAngle(currentRotation.y);
            float roll = NormalizeAngle(currentRotation.x);
            float yaw = NormalizeAngle(currentRotation.z);
            pitchText.text = $"{pitch:F1}°";
            rollText.text = $"{roll:F1}°";
            yawText.text = $"{yaw:F1}°";
            pitchSlider.value = pitch;
            rollSlider.value = roll;
            yawSlider.value = yaw;
        }
    }

    // Función para ajustar los ángulos entre -180 y 180
    float NormalizeAngle(float angle)
    {
        if (angle > 180)
            angle -= 360;
        return angle;
    }
}
