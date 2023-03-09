using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static Multislider.MultisliderCore;

namespace Multislider
{
    public class MultisliderElement : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        internal MultisliderCore slider;
        private RectTransform rect;
        private bool isDragging = false;

        //neighbours
        internal MultisliderElement limitLeft;
        internal MultisliderElement limitRight;

        #region events

        public event OnStartDraggingSliderEvent OnStartDraggingSlider;
        public event OnStopDraggingSliderEvent OnStopDraggingSlider;
        public event OnDraggingSliderEvent OnDraggingSlider;
        public event OnSliderValueChangeEvent OnSliderValueChange;
        public event OnSliderWidthChangeEvent OnSliderWidthChange;

        #endregion

        #region runtime values

        /// <summary>
        /// he width of the slide bar RectTransform
        /// </summary>
        private float width
        {
            get => rect.rect.width;
        }

        /// <summary>
        /// the value of the slider in the range of the slide bar
        /// </summary>
        public float value
        {
            get => _value;
            private set
            {
                _value = clampPos(value);
                updateSliderPos();
            }
        }
        private float _value = 0;

        /// <summary>
        /// converts the <see cref="value"/> to the x position of the slider in the UI
        /// </summary>
        public float sliderPos
        {
            get
            {
                if (Mathf.Abs(slider.maxValue - slider.minValue) < slider.minDistance)
                    slider.maxValue = slider.minValue + slider.minDistance;

                float valPos = value;

                float diff = slider.maxValue - slider.minValue;
                valPos -= slider.minValue;
                valPos /= diff;
                float barWidth = slider.bar.rect.width - width;
                float pos = (barWidth * valPos)
                    - (slider.bar.rect.width / 2 - width / 2);
                return pos;
            }
        }

        #endregion

        /// <summary>
        /// <list type="table">
        ///     <item><description>
        ///         set the value of the slider without checking for limits or updating the 'InWorld'-´position.
        ///     </description></item>
        ///     <item><term>
        ///         Be careful when using the method as it has no control-mechanisms.
        ///     </term></item>
        ///     <item><term>
        ///         Value may be changed upon updating the slider.
        ///     </term></item>
        /// </list>
        /// </summary>
        internal void setValueNoChange(float value)
        {
            _value = value;
        }

        /// <summary>
        /// updates the value of the slider element
        /// <list type="bullet">
        ///     <item>
        ///         <term><paramref name="isAbsolute"/> is true</term>
        ///         <description><paramref name="distance"/> will overwrite the value of the slider</description>
        ///     </item>
        ///     <item>
        ///         <term><paramref name="isAbsolute"/> is false</term>
        ///         <description><paramref name="distance"/> will be added to the value of the slider</description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="distance">the value to manipulate the sliders value</param>
        /// <param name="isAbsolute">decides if <paramref name="distance"/> 
        ///     will overwrite or add to the sliders value</param>
        public void moveElement(float distance, bool isAbsolute = false)
        {
            float delta = value;
            if (isAbsolute)
                value = distance;
            else
                value = _value + distance;
            delta = value - delta;
            if (delta != 0)
            {
                slider.movingSlider(this, delta);
                OnSliderValueChange?.Invoke(this, delta);
            }
        }

        /// <summary>
        /// calls <see cref="clampPos(float, bool)"/> with the sliders value
        /// </summary>
        /// <param name="withLimiter">decides if the position will be limited to the area between its neighbours 
        ///     or to the range of the whole slidebar</param>
        public void clampPos(bool withLimiter = false)
        {
            _value = clampPos(value, withLimiter);
            updateSliderPos();
        }

        /// <summary>
        /// calls <see cref="clampPosDir(float, bool, bool)"/> with the sliders value
        /// </summary>
        /// <param name="isRightward">decides if the limit should be applied 
        ///     only to the rightward direction or not.</param>
        /// <param name="withLimiter">decides if the position will be limited to the area between its neighbours 
        ///     or to the range of the whole slidebar</param>
        public void clampPosDir(bool isRightward = true, bool withLimiter = false)
        {
            _value = clampPosDir(value, isRightward, withLimiter);
            updateSliderPos();
        }

        /// <summary>
        /// limits the <paramref name="pos"/> to a specific range defined by <paramref name="withLimiter"/> to both sides.
        /// if <paramref name="pos"/> oversteps a limit in either direction it will be reset to the nearest possible position.
        /// </summary>
        /// <param name="pos">the position to be capped. positions refers to the value of the slider on the slidebar</param>
        /// <param name="withLimiter">decides if the position will be limited to the area between its neighbours 
        ///     or to the range of the whole slidebar</param>
        /// <returns>the limited value</returns>
        public float clampPos(float pos, bool withLimiter = false)
        {
            float leftValueLimit = slider.minValue;
            float rightValueLimit = slider.maxValue;
            if (withLimiter)
            {
                if (limitLeft != null && limitLeft.value + slider.minDistance >= slider.minValue)
                    leftValueLimit = limitLeft.value + slider.minDistance;
                if (limitRight != null && limitRight.value - slider.minDistance <= slider.maxValue)
                    rightValueLimit = limitRight.value - slider.minDistance;
            }

            if (pos < leftValueLimit)
                pos = leftValueLimit;
            if (pos > rightValueLimit)
                pos = rightValueLimit;

            return pos;
        }

        /// <summary>
        /// limits the <paramref name="pos"/> to a specific range defined by <paramref name="withLimiter"/> to a specific side
        /// if <paramref name="pos"/> oversteps a limit in either direction it will be reset to the nearest possible position.
        /// </summary>
        /// <param name="pos">the position to be capped. positions refers to the value of the slider on the slidebar</param>
        /// <param name="isRightward">decides if <paramref name="pos"/> should be limited only to the right or to the left</param>
        /// <param name="withLimiter">decides if the position will be limited to the area between its neighbours 
        ///     or to the range of the whole slidebar</param>
        /// <returns>the limited value</returns>
        public float clampPosDir(float pos, bool isRightward = true, bool withLimiter = false)
        {
            float leftValueLimit = slider.minValue;
            float rightValueLimit = slider.maxValue;
            if (withLimiter)
            {
                if (limitLeft != null && limitLeft.value + slider.minDistance >= slider.minValue)
                    leftValueLimit = limitLeft.value + slider.minDistance;
                if (limitRight != null && limitRight.value - slider.minDistance <= slider.maxValue)
                    rightValueLimit = limitRight.value - slider.minDistance;
            }

            if (pos < leftValueLimit && isRightward)
                pos = leftValueLimit;
            if (pos > rightValueLimit && !isRightward)
                pos = rightValueLimit;

            return pos;
        }

        /// <summary>
        /// updates the width of the slider using current values
        /// </summary>
        public void updateWidth()
        {
            float width = slider.sliderWidth;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            updateSliderPos();
            slider.sliderWidthChange(this, width);
            OnSliderWidthChange?.Invoke(this, width);
        }

        /// <summary>
        /// recalculates the x position of the slider in the UI
        /// </summary>
        public void updateSliderPos()
        {
            float delta = rect.localPosition.x;
            rect.localPosition = new Vector3(sliderPos, rect.localPosition.y);
            delta = rect.localPosition.x - delta;
            if (delta != 0f)
            {
                slider.draggingSlider(this, delta);
                OnDraggingSlider?.Invoke(this, delta);
            }    
        }

        #region Unity-Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                slider.removeSlider(this);
            else
                isDragging = true;

            slider.startDraggingSlider(this);
            OnStartDraggingSlider?.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            slider.updateSliderOrder();
            clampPos(true);
            slider.updateSliderPos();

            slider.stopDraggingSlider(this);
            OnStopDraggingSlider?.Invoke(this);
        }

        public void Awake()
        {
            rect = GetComponent<RectTransform>();
            rect.hideFlags = HideFlags.NotEditable;
        }

        private void Update()
        {
            if (isDragging)
            {
                float delta = rect.position.x;
                rect.position = new Vector3(Input.mousePosition.x, rect.position.y, rect.position.z);
                Vector2 newLocal = new Vector2(rect.localPosition.x, rect.localPosition.y);

                //clamps the position of the slider to the bounds of the slide bar
                if (rect.localPosition.x < (slider.bar.rect.width / -2) + (width / 2))
                    newLocal.x = (slider.bar.rect.width / -2) + (width / 2);
                if (rect.localPosition.x > (slider.bar.rect.width / 2) - (width / 2))
                    newLocal.x = (slider.bar.rect.width / 2) - (width / 2);

                //converts the local position of the slider in the UI to the value of the slide bar range
                float newValue = slider.Round(
                    slider.minValue
                    + (newLocal.x + ((slider.bar.rect.width - width) / 2))
                    / (slider.bar.rect.width - width)
                    * (slider.maxValue - slider.minValue));

                moveElement(newValue, true);
                delta = rect.position.x - delta;
                if (delta != 0)
                {
                    slider.draggingSlider(this, delta);
                    OnDraggingSlider?.Invoke(this, delta);
                }
            }
        }

        #endregion
    }
}