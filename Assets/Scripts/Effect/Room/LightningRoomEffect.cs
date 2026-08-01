using UnityEngine;

namespace AeternaBloom.Effects.Room
{
    /// <summary>
    /// Handles the specific visual logic, animations, and lifetime for the Dandelectric Lightning shock effect.
    /// </summary>
    public class LightningRoomEffect : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime = 3.0f;
        private float timer = 0f;

        /// <summary>
        /// Initializes the lightning effect with a sprite and sorting order.
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

            // Smooth fade out - can be customized with flash or frame animations later
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
