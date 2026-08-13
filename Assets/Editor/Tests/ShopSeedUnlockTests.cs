using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopSeedUnlockTests
{
    [Test]
    public void ShopCarouselSwipeHandler_CanBeAttachedAndInitialized()
    {
        GameObject cardGo = new GameObject("CardFrame", typeof(RectTransform), typeof(Image));
        ShopCarouselSwipeHandler swipeHandler = cardGo.AddComponent<ShopCarouselSwipeHandler>();

        Assert.IsNotNull(swipeHandler);
        Assert.IsFalse(swipeHandler.IsDragging);
        Assert.IsTrue(swipeHandler.CanDrag);

        Object.DestroyImmediate(cardGo);
    }

    [Test]
    public void ShopCarouselSwipeHandler_TriggersSwipeEventsCorrectly()
    {
        GameObject cardGo = new GameObject("CardFrame", typeof(RectTransform), typeof(Image));
        ShopCarouselSwipeHandler swipeHandler = cardGo.AddComponent<ShopCarouselSwipeHandler>();

        bool swipedLeft = false;
        bool swipedRight = false;

        swipeHandler.OnSwipeLeft += () => swipedLeft = true;
        swipeHandler.OnSwipeRight += () => swipedRight = true;

        PointerEventData eventData = new PointerEventData(EventSystem.current);

        // Simulate swipe left (drag from x=500 to x=100 -> deltaX = -400)
        eventData.position = new Vector2(500, 0);
        swipeHandler.OnBeginDrag(eventData);

        eventData.position = new Vector2(100, 0);
        swipeHandler.OnDrag(eventData);
        swipeHandler.OnEndDrag(eventData);

        Assert.IsTrue(swipedLeft, "Expected OnSwipeLeft to be triggered when dragging left past threshold");
        Assert.IsFalse(swipedRight, "Did not expect OnSwipeRight");

        // Reset
        swipedLeft = false;

        // Simulate swipe right (drag from x=100 to x=500 -> deltaX = +400)
        eventData.position = new Vector2(100, 0);
        swipeHandler.OnBeginDrag(eventData);

        eventData.position = new Vector2(500, 0);
        swipeHandler.OnDrag(eventData);
        swipeHandler.OnEndDrag(eventData);

        Assert.IsTrue(swipedRight, "Expected OnSwipeRight to be triggered when dragging right past threshold");
        Assert.IsFalse(swipedLeft, "Did not expect OnSwipeLeft");

        Object.DestroyImmediate(cardGo);
    }

    [Test]
    public void ShopCarouselSwipeHandler_DoesNotTriggerEventBelowThreshold()
    {
        GameObject cardGo = new GameObject("CardFrame", typeof(RectTransform), typeof(Image));
        ShopCarouselSwipeHandler swipeHandler = cardGo.AddComponent<ShopCarouselSwipeHandler>();

        bool swipedLeft = false;
        bool swipedRight = false;

        swipeHandler.OnSwipeLeft += () => swipedLeft = true;
        swipeHandler.OnSwipeRight += () => swipedRight = true;

        PointerEventData eventData = new PointerEventData(EventSystem.current);

        // Drag small distance (10px) below threshold (80px)
        eventData.position = new Vector2(100, 0);
        swipeHandler.OnBeginDrag(eventData);

        eventData.position = new Vector2(90, 0);
        swipeHandler.OnDrag(eventData);
        swipeHandler.OnEndDrag(eventData);

        Assert.IsFalse(swipedLeft);
        Assert.IsFalse(swipedRight);

        Object.DestroyImmediate(cardGo);
    }
}
