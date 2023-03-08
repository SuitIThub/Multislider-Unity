using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static Multislider.MultisliderCore;

namespace Multislider
{
    public class MultisliderElement : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        public string id { get; private set; }

        internal MultisliderCore slider;
        private RectTransform rect;
        private bool isDragging = false;

        internal MultisliderElement limitLeft;
        internal MultisliderElement limitRight;

        public event SliderEvent OnStartDraggingSlider;
        public event SliderEvent<float> OnDraggingSlider;
        public event SliderEvent<float> OnSliderValueChange;
        public event SliderEvent OnStopDraggingSlider;
        public event SliderEvent<float> OnSliderWidthChange;

        private float width
        {
            get => rect.rect.width;
        }

        private float _value = 0;
        public float value
        {
            get => _value;
            private set
            {
                _value = clampPos(value);
                updateSliderPos();
            }
        }

        public float sliderPos
        {
            get
            {
                if (slider.minValue.difference(slider.maxValue) < slider.minDistance)
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
                OnSliderValueChange.Invoke(this, delta);
            }
        }

        public void clampPos(bool withLimiter = false)
        {
            _value = clampPos(value, withLimiter);
            updateSliderPos();
        }

        public void clampPosDir(bool isRightward = true, bool withLimiter = false)
        {
            _value = clampPosDir(value, isRightward, withLimiter);
            updateSliderPos();
        }

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

        public void updateWidth()
        {
            float width = slider.absoluteSliderWidth;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            updateSliderPos();
            slider.sliderWidthChange(this, width);
            OnSliderWidthChange.Invoke(this, width);
        }

        public void updateSliderPos()
        {
            float delta = rect.localPosition.x;
            rect.localPosition = new Vector3(sliderPos, rect.localPosition.y);
            delta = rect.localPosition.x - delta;
            if (delta != 0f)
            {
                slider.draggingSlider(this, delta);
                OnDraggingSlider.Invoke(this, delta);
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
            OnStartDraggingSlider.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            slider.updateSliderOrder();
            clampPos(true);
            slider.updateSliderPos();

            slider.stopDraggingSlider(this);
            OnStopDraggingSlider.Invoke(this);
        }

        public void Awake()
        {
            id = IdUtilities.id;
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
                if (rect.localPosition.x < (slider.bar.rect.width / -2) + (width / 2))
                    newLocal.x = (slider.bar.rect.width / -2) + (width / 2);
                if (rect.localPosition.x > (slider.bar.rect.width / 2) - (width / 2))
                    newLocal.x = (slider.bar.rect.width / 2) - (width / 2);

                float newValue = slider.minValue
                    + (newLocal.x + ((slider.bar.rect.width - width) / 2))
                    / (slider.bar.rect.width - width)
                    * (slider.maxValue - slider.minValue);

                newValue = slider.Round(newValue);

                moveElement(newValue, true);
                delta = rect.position.x - delta;
                if (delta != 0)
                {
                    slider.draggingSlider(this, delta);
                    OnDraggingSlider.Invoke(this, delta);
                }
            }
        }

        #endregion

        public override bool Equals(object other)
        {
            if (other.GetType() != this.GetType())
                return base.Equals(other);

            return id == ((MultisliderElement)other).id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }
}