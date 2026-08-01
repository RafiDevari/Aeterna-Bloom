using UnityEngine;

namespace AeternaBloom.Effects.Common
{
    /// <summary>
    /// Handles the visual heat / overheat effect (pulsing warm aura and smooth fade-out).
    /// Can be applied to either a Room or a ContainmentUnit.
    /// </summary>
    public class HeatEffect : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime = 3.0f;
        private float timer = 0f;
        private Color baseHeatColor = new Color(1.0f, 0.35f, 0.1f, 0.75f); // Warm orange-red

        /// <summary>
        /// Initializes the heat effect with a sprite, sorting order, and optional lifetime.
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
            spriteRenderer.color = baseHeatColor;
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

            if (spriteRenderer != null)
            {
                // Pulsing intensity
                float pulse = 0.8f + Mathf.Sin(Time.time * 12f) * 0.2f;
                float overallAlpha = Mathf.Lerp(baseHeatColor.a, 0f, timer / lifetime);

                Color c = baseHeatColor;
                c.a = overallAlpha * pulse;
                spriteRenderer.color = c;
            }
        }
    }
}
