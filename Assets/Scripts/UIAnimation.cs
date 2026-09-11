using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAnimation : MonoBehaviour
{
    [SerializeField] float duration = 0.3f;
    [SerializeField] float delay = 0f;
    [SerializeField] AnimationCurve animationCurve;
    [SerializeField] RectTransform target;
    [SerializeField] Vector2 startingPoint;
    [SerializeField] Vector2 finalPoint;

    private Coroutine currentAnimation;
    private bool isOpen;
    private static UIAnimation _instance;

    public bool IsOpen => isOpen;
    public bool isPlaying => currentAnimation != null;
    public bool IsPlaying => isPlaying;

    public RectTransform Target
    {
        get => target;
        set => target = value;
    }

    public Vector2 StartingPoint
    {
        get => startingPoint;
        set => startingPoint = value;
    }

    public Vector2 FinalPoint
    {
        get => finalPoint;
        set => finalPoint = value;
    }

    public float Duration
    {
        get => duration;
        set => duration = value;
    }

    public float Delay
    {
        get => delay;
        set => delay = value;
    }

    public AnimationCurve Curve
    {
        get => animationCurve;
        set => animationCurve = value;
    }

    [ContextMenu("FadeIn")]
    public void FadeIn() => Open();

    [ContextMenu("FadeOut")]
    public void FadeOut() => Close();

    public void Open()
    {
        // Idempotente: si ya está abierta o abriéndose, no relanzar
        if (isOpen && (currentAnimation != null || IsAtPosition(finalPoint)))
        {
            return;
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        isOpen = true;

        if (target == null)
        {
            return;
        }

        if (duration <= 0f)
        {
            target.anchoredPosition = finalPoint;
            return;
        }

        currentAnimation = StartCoroutine(AnimateToCoroutine(finalPoint));
    }

    public void Close()
    {
        // Idempotente: si ya está cerrada o cerrándose, no relanzar
        if (!isOpen && (currentAnimation != null || IsAtPosition(startingPoint)))
        {
            return;
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        isOpen = false;

        if (target == null)
        {
            return;
        }

        if (duration <= 0f)
        {
            target.anchoredPosition = startingPoint;
            return;
        }

        currentAnimation = StartCoroutine(AnimateToCoroutine(startingPoint));
    }

    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        // Si ya hay otra instancia, no destruimos el GameObject para no romper otros componentes.

        SyncStateFromPosition();
    }

    private void OnDisable()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        if (target != null)
        {
            target.anchoredPosition = startingPoint;
        }

        isOpen = false;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void SyncStateFromPosition()
    {
        if (target != null)
        {
            float distToStart = Vector2.Distance(target.anchoredPosition, startingPoint);
            float distToEnd = Vector2.Distance(target.anchoredPosition, finalPoint);
            isOpen = distToEnd < distToStart;
        }
    }

    private bool IsAtPosition(Vector2 position)
    {
        if (target == null) return false;
        return Vector2.Distance(target.anchoredPosition, position) <= 0.001f;
    }

    private IEnumerator AnimateToCoroutine(Vector2 destination)
    {
        if (delay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < delay)
            {
                delayElapsed += Time.deltaTime;
                yield return null;
            }
        }

        Vector2 startPosition = target.anchoredPosition;
        float totalDistance = Vector2.Distance(startingPoint, finalPoint);
        float remainingDistance = Vector2.Distance(startPosition, destination);

        float effectiveDuration = totalDistance > 0.001f
            ? duration * (remainingDistance / totalDistance)
            : duration;

        if (effectiveDuration <= 0.001f)
        {
            target.anchoredPosition = destination;
            currentAnimation = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < effectiveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / effectiveDuration);

            if (animationCurve != null && animationCurve.length > 0)
            {
                t = animationCurve.Evaluate(t);
            }

            target.anchoredPosition = Vector2.Lerp(startPosition, destination, t);
            yield return null;
        }

        target.anchoredPosition = destination;
        currentAnimation = null;
    }

    public static UIAnimation Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIAnimation>();
                if (_instance == null)
                {
                    Debug.LogError("No hay una instancia de UIAnimation en la escena.");
                }
            }
            return _instance;
        }
    }
}
