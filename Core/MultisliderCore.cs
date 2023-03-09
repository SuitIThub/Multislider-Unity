using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.Linq;
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
        internal Color sliderColor = Color.white;

        #region events

        public delegate void OnSliderDistanceChangeEvent(float delta);
        public delegate void OnValueRangeChangeEvent(float minLimit, float maxLimit);
        public delegate void OnLimitRangeChangeEvent(float minLimit, float maxLimit);
        public delegate void OnCreateSliderEvent(MultisliderElement element);
        public delegate void OnDestroySliderEvent(MultisliderElement element);
        public delegate void OnStartDraggingSliderEvent(MultisliderElement element);
        public delegate void OnStopDraggingSliderEvent(MultisliderElement element);
        public delegate void OnDraggingSliderEvent(MultisliderElement element, float delta);
        public delegate void OnSliderValueChangeEvent(MultisliderElement element, float delta);
        public delegate void OnSliderWidthChangeEvent(MultisliderElement element, float width);
        public delegate void OnBarSizeChangeEvent(MultisliderBar bar, Vector2 size);

        public event OnSliderDistanceChangeEvent OnSliderDistanceChange;
        public event OnValueRangeChangeEvent OnValueRangeChange;
        public event OnLimitRangeChangeEvent OnLimitRangeChange;
        public event OnCreateSliderEvent OnCreateSlider;
        public event OnDestroySliderEvent OnDestroySlider;
        public event OnStartDraggingSliderEvent OnStartDraggingSlider;
        public event OnStopDraggingSliderEvent OnStopDraggingSlider;
        public event OnDraggingSliderEvent OnDraggingSlider;
        public event OnSliderValueChangeEvent OnSliderValueChange;
        public event OnSliderWidthChangeEvent OnSliderWidthChange;
        public event OnBarSizeChangeEvent OnBarSizeChange;

        #endregion
        #region settings

        /// <summary>
        /// absolute minimum Limit of the slidebar-range
        /// </summary>
        public float minLimit
        {
            get => _minLimit;
            set
            {
                _minLimit = value;
                updateSliderPos();
                OnLimitRangeChange?.Invoke(_minLimit, _maxLimit);
            }
        }
        private float _minLimit = 0;

        /// <summary>
        /// absolute maximum Limit of the slidebar-range
        /// </summary>
        public float maxLimit
        {
            get => _maxLimit;
            set
            {
                _maxLimit = value;
                updateSliderPos();
                OnLimitRangeChange?.Invoke(_minLimit, _maxLimit);
            }
        }
        private float _maxLimit = 100;

        /// <summary>
        /// dynamic minimum Limit of the slidebar-range. Value gets fitted to decimal- or multiples-setting
        /// Must be in Range between <see cref="minLimit"/> and <see cref="maxLimit"/>
        /// </summary>
        public float minValue
        {
            get => _minValue;
            set
            {
                _minValue = Round(value);
                updateSliderPos();
                OnValueRangeChange?.Invoke(_minValue, _maxValue);
            }
        }
        private float _minValue = 0;

        /// <summary>
        /// Dynamic maximum Limit of the slidebar-range. Value gets fitted to decimal- or multiples-setting.
        /// Must be in Range between <see cref="minLimit"/> and <see cref="maxLimit"/>
        /// </summary>
        public float maxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = Round(value);
                updateSliderPos();
                OnValueRangeChange?.Invoke(_minValue, _maxValue);
            }
        }
        private float _maxValue = 100;

        /// <summary>
        /// Width of all the sliders
        /// </summary>
        public float sliderWidth
        {
            get => _sliderWidth;
            set
            {
                _sliderWidth = Round(value, true);
                updateWidth();
            }
        }
        private float _sliderWidth = 1;

        /// <summary>
        /// The minimum distance each slider has to each other
        /// </summary>
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
                OnSliderDistanceChange?.Invoke(_minDistance);
            }
        }
        private float _minDistance = 1;

        /// <summary>
        /// The smallest logical width of a slider calculated from the range between 
        /// <see cref="minValue"/> and <see cref="maxValue"/>, and <see cref="minDistance"/>.
        /// </summary>
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

        /// <summary>
        /// Sets the amount of decimals behind the point. Values with more decimals get rounded
        /// </summary>
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
                sliderWidth = sliderWidth;
                minDistance = minDistance;
            }
        }
        private int _decimals = 0;

        /// <summary>
        /// Sets the value which is the multiplicator of all other values.
        /// If a value is not dividable by <see cref="multiples"/> it gets rounded to the nearest.
        /// </summary>
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
                sliderWidth = sliderWidth;
                minDistance = minDistance;
            }
        }
        private int _multiples = 0;

        #endregion

        #region Slider

        /// <summary>
        /// get the values of all slider
        /// </summary>
        /// <returns>a float array of all slider values</returns>
        public float[] getSlider()
        {
            return sliderElements.Select(x => x.value).ToArray();
        }

        /// <summary>
        /// quick way for creating or modifiying the set of sliders in one step
        /// </summary>
        /// <param name="slider">a set of slider values for the multislider to set</param>
        public void setSlider(IEnumerable<float> slider)
        {
            while (slider.Count() > sliderElements.Count())
                addSlider();
            while (slider.Count() < sliderElements.Count())
                removeSlider(sliderElements.Last());

            int i = 0;
            foreach(float value in slider)
                sliderElements[i].setValueNoChange(value);

            updateSliderOrder();
            updateSliderPos();
        }

        /// <summary>
        /// Removes a slider from the slidebar
        /// </summary>
        /// <param name="slider">the slider to be removed</param>
        public void removeSlider(MultisliderElement slider)
        {
            sliderElements.Remove(slider);
            if (Application.isPlaying)
                GameObject.Destroy(slider.gameObject);
            else
                GameObject.DestroyImmediate(slider.gameObject);

            OnDestroySlider?.Invoke(slider);
        }

        /// <summary>
        /// Adds a new slider to the slidebar
        /// </summary>
        /// <param name="value">initial value of the new slider. Default is <see cref="minValue"/></param>
        public void addSlider(float value = float.NaN)
        {
            MultisliderElement msc = GameObject.Instantiate(Resources.Load("Multislider/Prefab/Slider") as GameObject,
                    bar.transform).GetComponent<MultisliderElement>();
            if (!Application.isPlaying)
                msc.Awake();

            msc.slider = this;
            msc.updateWidth();

            if (minDistance > (maxValue - minValue) / (sliderElements.Count))
                minDistance = (maxValue - minValue) / (sliderElements.Count);

            sliderElements.Add(msc);

            updateSliderOrder();

            if (float.IsNaN(value))
                value = minValue;
            msc.moveElement(value, true);
            updateSliderPos();

            OnCreateSlider?.Invoke(msc);
        }

        /// <summary>
        /// Updates the color of all sliders
        /// </summary>
        /// <param name="color">the color to be used</param>
        public void updateSliderColor(Color color)
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement mse = sliderElements[i];
                mse.GetComponent<UnityEngine.UI.Image>().color = color;
            }
        }

        /// <summary>
        /// updates the sprite of all sliders
        /// </summary>
        /// <param name="sprite">the sprite to be used</param>
        public void updateSliderSprite(Sprite sprite)
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement mse = sliderElements[i];
                mse.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
            }
        }

        /// <summary>
        /// sorts all slider depending on their positions
        /// </summary>
        public void updateSliderOrder()
        {
            MultiSlideElementComparer msec = new MultiSlideElementComparer();
            sliderElements.Sort(msec);
            updateSliderLimits();
        }

        /// <summary>
        /// updates all left and right neighbours for all sliders
        /// </summary>
        public void updateSliderLimits()
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement mse = sliderElements[i];
                mse.limitLeft = (i != 0) ? sliderElements[i - 1] : null;
                mse.limitRight = (i < sliderElements.Count - 1) ? sliderElements[i + 1] : null;
            }
        }

        /// <summary>
        /// recalculates the positions of all sliders depending on the value
        /// </summary>
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

        /// <summary>
        /// updates the width of all sliders
        /// </summary>
        public void updateSliderWidth()
        {
            for (int i = 0; i < sliderElements.Count; i++)
            {
                MultisliderElement msc = sliderElements[i];
                msc.updateWidth();
            }
        }

        /// <summary>
        /// updates the width of all sliders and then updates their positions to account for minimum distance
        /// </summary>
        public void updateWidth()
        {
            updateSliderWidth();
            updateSliderPos();
        }

        #endregion

        #region intern event-calls

        //-----------------------------------
        // use only for internal classes
        //-----------------------------------

        /// <summary>
        /// Event-Switch for triggering Events started by other classes
        /// </summary>
        /// <param name="mse"></param>
        internal void startDraggingSlider(MultisliderElement mse)
        {
            OnStartDraggingSlider?.Invoke(mse);
        }

        /// <summary>
        /// Event-Switch for triggering Events started by other classes
        /// </summary>
        /// <param name="mse"></param>
        internal void stopDraggingSlider(MultisliderElement mse)
        {
            OnStopDraggingSlider?.Invoke(mse);
        }

        /// <summary>
        /// Event-Switch for triggering Events started by other classes
        /// </summary>
        /// <param name="mse"></param>
        internal void draggingSlider(MultisliderElement mse, float delta)
        {
            OnDraggingSlider?.Invoke(mse, delta);
        }

        /// <summary>
        /// Event-Switch for triggering Events started by other classes
        /// </summary>
        /// <param name="mse"></param>
        internal void movingSlider(MultisliderElement mse, float delta)
        {
            OnSliderValueChange?.Invoke(mse, delta);
        }

        /// <summary>
        /// Event-Switch for triggering Events started by other classes
        /// </summary>
        /// <param name="mse"></param>
        internal void sliderWidthChange(MultisliderElement mse, float width)
        {
            OnSliderWidthChange?.Invoke(mse, width);
        }

        /// <summary>
        /// Event-Switch for triggering Events started by other classes
        /// </summary>
        /// <param name="mse"></param>
        internal void barSizeChange(MultisliderBar bar, Vector2 sizeDelta)
        {
            OnBarSizeChange?.Invoke(bar, sizeDelta);
        }

        #endregion

        #region math functions

        /// <summary>
        /// rounds the value to either the nearest decimal of <see cref="decimals"/> 
        /// or the nearest multiple of <see cref="multiples"/>
        /// </summary>
        /// <param name="value">the value to be rounded</param>
        /// <param name="noNull">if true, the <paramref name="value"/> 
        ///     will be rounded up to the next possible non-null value</param>
        /// <returns></returns>
        public float Round(float value, bool noNull = false)
        {
            if (decimals == 0)
            {
                value = Mathf.Round(value / multiples) * multiples;
                if (noNull && value == 0)
                    value = multiples;
            }
            else
            {
                value = (float)System.Math.Round((double)value, decimals);
                if (noNull && value == 0)
                    value = 1 / Mathf.Pow(10, decimals);
            }

            return value;
        }

        /// <summary>
        /// rounds the value to either the next lower decimal of <see cref="decimals"/> 
        /// or the next lower multiple of <see cref="multiples"/>
        /// </summary>
        /// <param name="value">the value to be rounded</param>
        /// <param name="noNull">if true, the <paramref name="value"/> 
        ///     will be rounded up to the next possible non-null value</param>
        /// <returns></returns>
        public float Floor(float value, bool noNull = false)
        {
            if (decimals == 0)
            {
                value = Mathf.Floor(value / multiples) * multiples;
                if (noNull && value == 0)
                    value = multiples;
            }
            else
            {
                value = Mathf.Floor(value * Mathf.Pow(10, decimals)) / Mathf.Pow(10, decimals);
                if (noNull && value == 0)
                    value = 1 / Mathf.Pow(10, decimals);
            }

            return value;
        }

        /// <summary>
        /// rounds the value to either the next higehr decimal of <see cref="decimals"/> 
        /// or the next higher multiple of <see cref="multiples"/>
        /// </summary>
        /// <param name="value">the value to be rounded</param>
        /// <param name="noNull">if true, the <paramref name="value"/> 
        ///     will be rounded up to the next possible non-null value</param>
        /// <returns></returns>
        public float Ceil(float value, bool noNull = false)
        {
            if (decimals == 0)
            {
                value = Mathf.Ceil(value / multiples) * multiples;
                if (noNull && value == 0)
                    value = multiples;
            }
            else
            {
                value = Mathf.Ceil(value * Mathf.Pow(10, decimals)) / Mathf.Pow(10, decimals);
                if (noNull && value == 0)
                    value = 1 / Mathf.Pow(10, decimals);
            }

            return value;
        }

        #endregion

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            bar = transform.GetChild(0).GetComponent<RectTransform>();
        }

    }
}
