using UnityEngine;
using static Multislider.MultisliderCore;

namespace Multislider
{
    [RequireComponent(typeof(RectTransform))]
    public class MultisliderBar : MonoBehaviour
    {
        public MultisliderCore slider;
        public OnBarSizeChangeEvent OnBarSizeChangeEvent;

        [ExecuteInEditMode]
        private void OnRectTransformDimensionsChange()
        {
            slider.updateWidth();
            slider.barSizeChange(this, slider.bar.sizeDelta);
            OnBarSizeChangeEvent?.Invoke(this, slider.bar.sizeDelta);
        }
    }
}