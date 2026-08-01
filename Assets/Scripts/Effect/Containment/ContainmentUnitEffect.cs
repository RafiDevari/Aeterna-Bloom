using UnityEngine;

namespace AeternaBloom.Effects.Containment
{
    /// <summary>
    /// Handles visual representation, fade-out behavior, and lifetime for generic temporary Containment Unit effects.
    /// </summary>
    public class ContainmentUnitEffect : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime = 3.0f;
        private float timer = 0f;

        /// <summary>
        /// Initializes the containment unit effect with the specified sprite, sorting order, and lifetime.
        /// </summary>
        public void Init(Sprite sprite, int sortingOrder, float customLifetime = 3.0f)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            this.lifetime = customLifetime;
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
