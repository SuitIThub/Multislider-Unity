using System.Collections.Generic;
using UnityEngine;

namespace Multislider
{
    [RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Image))]
    public class MultisliderCore : MonoBehaviour
    {
        private class MultiSlideElementComparer : IComparer<MultisliderElement>
        {
            public int Compare(MultisliderElement x, MultisliderElement y)
            {
                if (x == null)
                    return -1;
                else
                {
                    if (y == null)
                        return 1;
                    else
                        return x.value.CompareTo(y.value);
                }
            }
        }

        internal RectTransform rect;
        internal RectTransform bar;
        internal List<MultisliderElement> sliderElements = new List<MultisliderElement>();

        internal Sprite sliderSprite;
        internal Color sliderColor;

        public delegate void Event<T>(T data);
        public delegate void Event<T, U>(T data1, U data2);
        public delegate void SliderEvent(MultisliderElement element);
        public delegate void SliderEvent<T>(MultisliderElement element, T data);
        public delegate void SliderBarEvent(MultisliderBar element);
        public delegate void SliderBarEvent<T>(MultisliderBar element, T data);
        public delegate void SliderBarEvent<T, U>(MultisliderBar element, T data1 , U data2);

        public event Event<float> OnSliderDistanceChange;
        public event Event<float, float> OnValueRangeChange;
        public event Event<float, float> OnLimitRangeChange;
        public event SliderEvent OnCreateSlider;
        public event SliderEvent OnDestroySlider;
        public event SliderEvent OnStartDraggingSlider;
        public event SliderEvent<float> OnDraggingSlider;
        public event SliderEvent<float> OnSliderValueChange;
        public event SliderEvent OnStopDraggingSlider;
        public event SliderEvent<float> OnSliderWidthChange;
        public event SliderBarEvent<Vector2> OnBarSizeChange;

        private float _minLimit = 0;
        public float minLimit
        {
            get => _minLimit;
            set
            {
                _minLimit = value;
                updateSliderPos();
                OnLimitRangeChange.Invoke(_minLimit, _maxLimit);
            }
        }
        private float _maxLimit = 100;
        public float maxLimit
        {
            get => _maxLimit;
            set
            {
                _maxLimit = value;
                updateSliderPos();
                OnLimitRangeChange.Invoke(_minLimit, _maxLimit);
            }
        }

        private float _minValue = 0;
        public float minValue
        {
            get => _minValue;
            set
            {
                _minValue = Round(value);
                updateSliderPos();
                OnValueRangeChange.Invoke(_minValue, _maxValue);
            }
        }
        private float _maxValue = 100;
        public float maxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = Round(value);
                updateSliderPos();
                OnValueRangeChange.Invoke(_minValue, _maxValue);
            }
        }

        private float _minWidth = 1;
        public float minWidth
        {
            get => _minWidth;
            set
            {
                _minWidth = Round(value, true);
                updateWidth();
            }
        }

        private float _minDistance = 1;
        public float minDistance
        {
            get => _minDistance;
            set
            {
                float min = value;
                if (min > maxValue)
                    min = maxValue;
                _minDistance = Round(min, true);
                updateWidth();
                OnSliderDistanceChange.Invoke(_minDistance);
            }
        }
        public float sliderMinWidth
        {
            get
            {
                float diff = maxLimit - minLimit;
                diff /= minDistance;
                if (diff >= 2)
                    return bar.rect.width / diff;
                else
                    return bar.rect.width / 2;
            }
        }
        public float absoluteSliderWidth
        {
            get => Round(minWidth);
            //get => Round((sliderMinWidth < minWidth) ? minWidth : sliderMinWidth);
        }

        private int _decimals = 0;
        public int decimals
        {
            get => _decimals;
            set
            {
                _decimals = value;
                if (_decimals < 0)
                    _decimals = 0;

                minValue = minValue;
                maxValue = maxValue;
                minWidth = minWidth;
                minDistance = minDistance;
            }
        }

        private int _multiples = 0;
        public int multiples
        {
            get => _multiples;
            set
            {
                _multiples = value;
                if (_multiples < 1)
                    _multiples = 1;

                minValue = minValue;
                maxValue = maxValue;
                minWidth = minWidth;
                minDistance = minDistance;
            }
        }

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            bar = transform.GetChild(0).GetComponent<RectTransform>();
        }

        public void removeSlider(MultisliderElement slider)
        {
            sliderElements.Remove(slider);
            if (Application.isPlaying)
                GameObject.Destroy(slider.gameObject);
            else
                GameObject.DestroyImmediate(slider.gameObject);

            OnDestroySlider.Invoke(slider);
        }

        public void addSlider()
        {
            MultisliderElement msc;
            if (Application.isPlaying)
                msc = GameObject.Instantiate(CentreBrain.data.Prefabs["WindowModuleMultiSliderSlider"],
                                                         bar.transform).GetComponent<MultisliderElement>();
            else
            {
                msc = GameObject.Instantiate(Resources.Load("Components/Multislider/Prefab/Slider") as GameObject,
                    bar.transform).GetComponent<MultisliderElement>();
                msc.Awake();
            }


            msc.slider = this;
            msc.updateWidth();

            if (minDistance > (maxValue - minValue) / (sliderElements.Count))
                minDistance = (maxValue - minValue) / (sliderElements.Count);

            sliderElements.Add(msc);
            updateSliderOrder();

            msc.moveElement(minValue, true);
            updateSliderPos();

            OnCreateSlider.Invoke(msc);
        }

        public void updateSliderColor(Color color)
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement mse = sliderElements[i];
                mse.GetComponent<UnityEngine.UI.Image>().color = color;
            }
        }

        public void updateSliderSprite(Sprite sprite)
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement mse = sliderElements[i];
                mse.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
            }
        }

        public void updateSliderOrder()
        {
            MultiSlideElementComparer msec = new MultiSlideElementComparer();
            sliderElements.Sort(msec);
            updateSliderLimits();
        }

        public void updateSliderLimits()
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement mse = sliderElements[i];
                mse.limitLeft = (i != 0) ? sliderElements[i - 1] : null;
                mse.limitRight = (i < sliderElements.Count - 1) ? sliderElements[i + 1] : null;
            }
        }

        public void updateSliderPos()
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement msc = sliderElements[i];
                msc.clampPosDir(true, true);
                msc.updateSliderPos();
            }

            for (int i = sliderElements.Count - 1; i >= 0; i--)
            {
                MultisliderElement msc = sliderElements[i];
                msc.clampPosDir(false, true);
                msc.updateSliderPos();
            }
        }

        public void updateSliderWidth()
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement msc = sliderElements[i];
                msc.updateWidth();
            }
        }

        public void updateWidth()
        {
            updateSliderWidth();
            updateSliderPos();
        }

        internal void startDraggingSlider(MultisliderElement mse)
        {
            OnStartDraggingSlider.Invoke(mse);
        }

        internal void stopDraggingSlider(MultisliderElement mse)
        {
            OnStopDraggingSlider.Invoke(mse);
        }

        internal void draggingSlider(MultisliderElement mse, float delta)
        {
            OnDraggingSlider.Invoke(mse, delta);
        }

        internal void movingSlider(MultisliderElement mse, float delta)
        {
            OnSliderValueChange.Invoke(mse, delta);
        }

        internal void sliderWidthChange(MultisliderElement mse, float width)
        {
            OnSliderWidthChange.Invoke(mse, width);
        }

        internal void barSizeChange(MultisliderBar bar, Vector2 sizeDelta)
        {
            OnBarSizeChange.Invoke(bar, sizeDelta);
        }

        public float Round(float value, bool noNull = false)
        {
            if (decimals == 0)
            {
                value = value.Multiples(multiples);
                if (noNull && value == 0)
                    value = multiples;
            }
            else
            {
                value = value.Round(decimals);
                if (noNull && value == 0)
                    value = Mathf.Pow(10, decimals);
            }

            return value;
        }

        public float Floor(float value, bool noNull = false)
        {
            if (decimals == 0)
            {
                value = value.FloorMultiples(multiples);
                if (noNull && value == 0)
                    value = multiples;
            }
            else
            {
                value = value.FloorRound(decimals);
                if (noNull && value == 0)
                    value = Mathf.Pow(10, decimals);
            }

            return value;
        }

        public float Ceil(float value, bool noNull = false)
        {
            if (decimals == 0)
            {
                value = value.CeilMultiples(multiples);
                if (noNull && value == 0)
                    value = multiples;
            }
            else
            {
                value = value.CeilRound(decimals);
                if (noNull && value == 0)
                    value = Mathf.Pow(10, decimals);
            }

            return value;
        }

        private void Update()
        {
            if (rect.hasChanged)
                updateSliderPos();
            rect.hasChanged = false;
        }
    }
}
