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
    public bool isPlaying => currentAnimation != null;
    [ContextMenu("FadeIn")]
    public void FadeIn(){
        if(currentAnimation == null)
            currentAnimation=StartCoroutine(FadeInCoroutine(startingPoint, finalPoint));
    }
    [ContextMenu("FadeOut")]
    public void FadeOut(){
        if(currentAnimation == null)
            currentAnimation=StartCoroutine(FadeInCoroutine(finalPoint, startingPoint));
    }
    IEnumerator FadeInCoroutine(Vector2 a, Vector2 b){
        Vector2 startingPoint = a;
        Vector2 finalPoint = b;
        float elapsed = 0;
        while (elapsed <= delay){
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0;
        while (elapsed <= duration){
            float percentage = elapsed/duration;
            elapsed += Time.deltaTime;
            Vector2 currentPosition = Vector2.Lerp(startingPoint, finalPoint, percentage);
            target.anchoredPosition = currentPosition;
            yield return null;
        }

        target.anchoredPosition = finalPoint;
        currentAnimation = null;

    }
}
