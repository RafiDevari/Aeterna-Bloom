using UnityEngine;

namespace AeternaBloom.Effects.Room
{
    /// <summary>
    /// Handles the visual representation, fade-out behavior, and lifetime of temporary room effects.
    /// </summary>
    public class RoomEffect : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime = 3.0f;
        private float timer = 0f;

        /// <summary>
        /// Initializes the room effect with the specified sprite and sorting order.
        /// </summary>
        public void Init(Sprite sprite, int sortingOrder)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Smooth fade out effect
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
        }
    }
}
