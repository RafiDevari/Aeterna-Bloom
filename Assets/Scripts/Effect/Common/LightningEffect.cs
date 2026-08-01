using UnityEngine;

namespace AeternaBloom.Effects.Common
{
    /// <summary>
    /// Handles the visual representation, flickering/fade-out behavior, and lifetime of electric shock effects.
    /// Works on both Room and ContainmentUnit targets.
    /// </summary>
    public class LightningEffect : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime = 3.0f;
        private float timer = 0f;
        private float flickerInterval = 0.08f;
        private float flickerTimer = 0f;

        /// <summary>
        /// Initializes the lightning effect with a sprite and sorting order.
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

            // Rapid flickering electric shock visual effect
            flickerTimer += Time.deltaTime;
            if (flickerTimer >= flickerInterval)
            {
                flickerTimer = 0f;
                if (spriteRenderer != null)
                {
                    float alphaBase = Mathf.Lerp(1f, 0f, timer / lifetime);
                    float randomFlicker = Random.Range(0.4f, 1.0f);
                    Color color = spriteRenderer.color;
                    color.a = alphaBase * randomFlicker;
                    spriteRenderer.color = color;
                }
            }
        }
    }
}
