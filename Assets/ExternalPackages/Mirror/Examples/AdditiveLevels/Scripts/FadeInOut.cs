using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.AdditiveLevels
{
    public class FadeInOut : MonoBehaviour
    {
        [Header("Components")] [SerializeField]
        private Image panelImage;

        [Header("Settings")] [SerializeField] [Range(1, 10)] [Tooltip("Time in seconds to fade in")]
        private byte fadeInTime = 2;

        [SerializeField] [Range(1, 10)] [Tooltip("Time in seconds to fade out")]
        private byte fadeOutTime = 2;

        private bool isFading;

        private void OnValidate()
        {
            if (panelImage == null)
                panelImage = GetComponentInChildren<Image>();

            fadeInTime = (byte)Mathf.Max(fadeInTime, 1);
            fadeOutTime = (byte)Mathf.Max(fadeOutTime, 1);
        }

        public float GetFadeInTime()
        {
            return fadeInTime + Time.fixedDeltaTime;
        }

        public IEnumerator FadeIn()
        {
            //Debug.Log($"FadeIn {isFading}");
            yield return FadeImage(0f, 1f, fadeInTime);
        }

        public float GetFadeOutTime()
        {
            return fadeOutTime + Time.fixedDeltaTime;
        }

        public IEnumerator FadeOut()
        {
            //Debug.Log($"FadeOut {isFading}");
            yield return FadeImage(1f, 0f, fadeOutTime);
        }

        private IEnumerator FadeImage(float startAlpha, float endAlpha, float duration)
        {
            if (panelImage == null) yield break;

            if (isFading) yield break;

            // Short circuit if the alpha is already at endAlpha
            var color = panelImage.color;
            if (Mathf.Approximately(color.a, endAlpha)) yield break;

            isFading = true;

            var elapsedTime = 0f;
            var fixedDeltaTime = Time.fixedDeltaTime;

            while (elapsedTime < duration)
            {
                elapsedTime += fixedDeltaTime;
                var alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                panelImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return new WaitForFixedUpdate();
            }

            // Ensure the final alpha value is set
            panelImage.color = new Color(color.r, color.g, color.b, endAlpha);

            isFading = false;
        }
    }
}