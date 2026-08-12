using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopCarouselSwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public event Action OnSwipeLeft;  // Triggered when swiping left -> Next item
    public event Action OnSwipeRight; // Triggered when swiping right -> Previous item

    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 80f;
    [SerializeField] private float returnSpeed = 15f;
    [SerializeField] private bool enableTilt = true;
    [SerializeField] private float maxTiltAngle = 6f;

    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    private bool canDrag = true;
    private Coroutine returnCoroutine;

    public bool IsDragging => isDragging;
    public bool CanDrag
    {
        get => canDrag;
        set => canDrag = value;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            initialPosition = rectTransform.anchoredPosition;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        isDragging = true;
        dragStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || !isDragging || rectTransform == null) return;

        float deltaX = eventData.position.x - dragStartPosition.x;
        rectTransform.anchoredPosition = initialPosition + new Vector2(deltaX, 0);

        if (enableTilt)
        {
            float tilt = Mathf.Clamp(-deltaX * 0.03f, -maxTiltAngle, maxTiltAngle);
            rectTransform.localRotation = Quaternion.Euler(0, 0, tilt);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag || !isDragging) return;

        isDragging = false;
        float deltaX = eventData.position.x - dragStartPosition.x;

        if (deltaX < -swipeThreshold)
        {
            OnSwipeLeft?.Invoke();
        }
        else if (deltaX > swipeThreshold)
        {
            OnSwipeRight?.Invoke();
        }
        else
        {
            ResetPositionSmooth();
        }
    }

    public void ResetPositionSmooth()
    {
        if (returnCoroutine != null) StopCoroutine(returnCoroutine);
        returnCoroutine = StartCoroutine(ReturnToInitialRoutine());
    }

    public void ResetPositionInstant()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = initialPosition;
            rectTransform.localRotation = Quaternion.identity;
        }
    }

    private IEnumerator ReturnToInitialRoutine()
    {
        if (rectTransform == null) yield break;

        while (Vector2.Distance(rectTransform.anchoredPosition, initialPosition) > 0.5f ||
               Quaternion.Angle(rectTransform.localRotation, Quaternion.identity) > 0.1f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, initialPosition, Time.deltaTime * returnSpeed);
            rectTransform.localRotation = Quaternion.Lerp(rectTransform.localRotation, Quaternion.identity, Time.deltaTime * returnSpeed);
            yield return null;
        }

        rectTransform.anchoredPosition = initialPosition;
        rectTransform.localRotation = Quaternion.identity;
        returnCoroutine = null;
    }
}
