using DisplayScreen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Multislider
{
    [RequireComponent(typeof(RectTransform))]
    public class MultisliderBar : MonoBehaviour
    {
        public MultisliderCore slider;

        [ExecuteInEditMode]
        private void OnRectTransformDimensionsChange()
        {
            slider.updateWidth();
        }
    }
}