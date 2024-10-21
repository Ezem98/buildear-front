using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAnimation : MonoBehaviour
{

    [SerializeField] float duration;
    [SerializeField] float delay;
    [SerializeField] AnimationCurve animationCurve;
    [SerializeField] RectTransform target;
    [SerializeField] Vector2 startingPoint;
    [SerializeField] Vector2 finalPoint;
    Coroutine currentAnimation;

    private static UIAnimation _instance;
    public bool isPlaying => currentAnimation != null;
    [ContextMenu("FadeIn")]
    public void FadeIn()
    {
        currentAnimation ??= StartCoroutine(FadeInCoroutine(startingPoint, finalPoint));
    }
    [ContextMenu("FadeOut")]
    public void FadeOut()
    {
        currentAnimation ??= StartCoroutine(FadeInCoroutine(finalPoint, startingPoint));
    }

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruir este objeto
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la instancia en todas las escenas
        }
    }

    IEnumerator FadeInCoroutine(Vector2 a, Vector2 b)
    {
        Vector2 startingPoint = a;
        Vector2 finalPoint = b;
        float elapsed = 0;
        while (elapsed <= delay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0;
        while (elapsed <= duration)
        {
            float percentage = elapsed / duration;
            elapsed += Time.deltaTime;
            Vector2 currentPosition = Vector2.Lerp(startingPoint, finalPoint, percentage);
            target.anchoredPosition = currentPosition;
            yield return null;
        }

        target.anchoredPosition = finalPoint;
        currentAnimation = null;

    }

    public static UIAnimation Instance
    {
        get
        {
            // Si no hay instancia, intentar encontrarla en la escena
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIAnimation>();

                // Si no se encuentra la instancia en la escena, crear una nueva
                if (_instance == null)
                {
                    GameObject singletonObject = new();
                    _instance = singletonObject.AddComponent<UIAnimation>();
                    singletonObject.name = typeof(UIAnimation).ToString() + " (Singleton)";
                    // Opcional: Evitar que sea destruido cuando se cambie de escena
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

}
