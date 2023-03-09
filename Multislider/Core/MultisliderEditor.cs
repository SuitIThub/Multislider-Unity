using UnityEngine;
using UnityEditor;

namespace Multislider
{
    [CustomEditor(typeof(MultisliderCore))]
    [RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Image))]
    public class MultisliderEditor : Editor
    {
        private bool sliderFoldout = true;

        public override void OnInspectorGUI()
        {
            MultisliderCore script = (MultisliderCore)(object)target;

            //Setting of Multislider RectTransform
            OnRectComponent("Multislider", script.rect, false);
            if (script.rect == null)
                script.rect = script.GetComponent<RectTransform>();

            //Setting of Multislider Slidebar

            if (script.bar == null && script.transform.childCount > 0)
            {
                RectTransform barRect = script.transform.GetChild(0).GetComponent<RectTransform>();
                if (barRect.GetComponent<MultisliderBar>() != null)
                    script.bar = barRect;
            }
            RectTransform newRect = OnRectComponent("Slider Bar", script.bar);
            if (script.bar != newRect && (newRect == null || newRect.GetComponent<MultisliderBar>() != null))
                script.bar = newRect;

            //Setting of all slider-handles
            Color sliderColor = script.sliderColor;
            Sprite sliderSprite = script.sliderSprite;
            OnRectComponent("Slider-Handle", null, ref sliderColor, ref sliderSprite, false);
            if (sliderColor != script.sliderColor)
            {
                script.updateSliderColor(sliderColor);
                script.sliderColor = sliderColor;
            }
            if (sliderSprite != script.sliderSprite)
            {
                script.updateSliderSprite(sliderSprite);
                script.sliderSprite = sliderSprite;
            }

            if (script.rect != null && script.bar != null)
            {
                //Setting of decimals
                int newDecimals = script.decimals;
                OnSlider("Decimals", 0, 10, ref newDecimals);
                if (newDecimals != script.decimals)
                    script.decimals = newDecimals;

                //Setting of multiples
                EditorGUI.BeginDisabledGroup(script.decimals != 0);
                int newMultiples = script.multiples;
                OnSlider("Multiples", 1, 10, ref newMultiples);
                if (newMultiples != script.multiples)
                    script.multiples = newMultiples;
                EditorGUI.EndDisabledGroup();

                //Setting of minimum distance between sliders
                if (float.IsNaN(script.minDistance))
                    script.minDistance = 0;
                float newSliderDist = script.minDistance;
                float maxDist = script.Floor(Mathf.Abs((script.maxValue - script.minValue) / (script.sliderElements.Count - 1)));
                if (float.IsInfinity(maxDist))
                    maxDist = script.maxValue - script.minValue;
                OnSlider("Slider Distance",
                    script.Ceil(0f, true),
                    maxDist,
                    ref newSliderDist);
                if (newSliderDist != script.minDistance)
                {
                    script.minDistance = newSliderDist;
                    script.updateSliderPos();
                }

                //Setting of slider-handle width
                if (float.IsNaN(script.sliderWidth))
                    script.sliderWidth = script.Ceil(0, true);
                float newSliderWidth = script.sliderWidth;
                float sliderMin = script.sliderMinWidth;
                float sliderMax = script.Floor(script.bar.rect.width / 2);
                if (float.IsInfinity(sliderMin) || sliderMin > sliderMax)
                    sliderMin = sliderMax;
                OnSlider("Slider Min Width", script.Ceil(sliderMin, true), sliderMax, ref newSliderWidth);
                if (newSliderWidth != script.sliderWidth)
                {
                    script.sliderWidth = newSliderWidth;
                    script.updateWidth();
                }

                //Setting of slidebar-range
                if (float.IsNaN(script.minValue))
                    script.minValue = script.minLimit;
                if (float.IsNaN(script.maxValue))
                    script.maxValue = script.maxLimit;
                float minValue = script.Ceil(script.minValue);
                float maxValue = script.Floor(script.maxValue);
                OnMinMaxSlider("Slider Bar Range", script.Ceil(script.minLimit), script.Floor(script.maxLimit), ref minValue, ref maxValue);
                if (minValue != script.minValue || maxValue != script.maxValue)
                {
                    if (Mathf.Abs(maxValue - minValue) < script.minDistance)
                        maxValue = minValue + script.minDistance;
                    script.minValue = minValue;
                    script.maxValue = maxValue;
                    script.updateSliderPos();
                }

                //List of all sliders
                sliderFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(sliderFoldout, "Slider", EditorStyles.foldoutHeader);
                if (sliderFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    if (GUILayout.Button("+"))
                        script.addSlider();
                    for (int i = 0; i < script.sliderElements.Count; i++)
                    {
                        MultisliderElement msc = script.sliderElements[i];
                        EditorGUILayout.BeginHorizontal();

                        float newVal = msc.value;
                        OnSlider("", script.minValue, script.maxValue, ref newVal);
                        if (newVal != msc.value)
                        {
                            newVal = script.Round(newVal);
                            msc.moveElement(newVal, true);
                            script.updateSliderOrder();
                            script.updateSliderPos();
                        }

                        if (GUILayout.Button("-"))
                            script.removeSlider(msc);

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("Components missing", MessageType.Error);
            }
        }

        public void OnMinMaxSlider(string title, float minLimit, float maxLimit, ref float minValue, ref float maxValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.Width(100));
            minValue = EditorGUILayout.DelayedFloatField(minValue, GUILayout.Width(50));
            EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit);
            maxValue = EditorGUILayout.DelayedFloatField(maxValue, GUILayout.Width(50));
            if (minValue < minLimit)
                minValue = minLimit;
            if (maxValue > maxLimit)
                maxValue = maxLimit;
            if (minValue > maxValue)
            {
                float temp = minValue;
                minValue = maxValue;
                maxValue = temp;
            }
            EditorGUILayout.EndHorizontal();
        }

        public void OnSlider(string title, float min, float max, ref float value)
        {
            EditorGUILayout.BeginHorizontal();
            if (title.Length != 0)
                EditorGUILayout.LabelField(title, GUILayout.Width(100));
            value = EditorGUILayout.Slider(value, min, max);
            EditorGUILayout.EndHorizontal();
        }


        public void OnSlider(string title, int min, int max, ref int value)
        {
            EditorGUILayout.BeginHorizontal();
            if (title.Length != 0)
                EditorGUILayout.LabelField(title, GUILayout.Width(100));
            value = (int)EditorGUILayout.Slider(value, min, max);
            EditorGUILayout.EndHorizontal();
        }

        public RectTransform OnRectComponent(string title, RectTransform rect, ref Color color, ref Sprite sprite,
                                    bool rectIsChangeable = true, bool rectSwitch = false)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title);
            if (rectIsChangeable || rect != null)
            {
                EditorGUI.BeginDisabledGroup(!rectIsChangeable);
                rect = (RectTransform)EditorGUILayout.ObjectField(rect, typeof(RectTransform), true);

                EditorGUI.EndDisabledGroup();
            }

            Color c = color;
            Sprite s = sprite;

            if (!rectSwitch || rect != null)
                c = EditorGUILayout.ColorField(c);

            EditorGUILayout.EndVertical();

            if (!rectSwitch || rect != null)
                s = (Sprite)EditorGUILayout.ObjectField(s, typeof(Sprite), true, GUILayout.Width(60),
                                                        GUILayout.Height(60));
            color = c;
            sprite = s;
            EditorGUILayout.EndHorizontal();

            return rect;
        }

        public RectTransform OnRectComponent(string title, RectTransform rect, bool rectIsChangeable = true)
        {
            Color c = Color.white;
            Sprite s = null;
            UnityEngine.UI.Image image = null;
            if (rect != null)
            {
                image = rect.GetComponent<UnityEngine.UI.Image>();
                c = image.color;
                s = image.sprite;
            }

            rect = OnRectComponent(title, rect, ref c, ref s, rectIsChangeable, true);

            if (image != null)
            {
                image.color = c;
                image.sprite = s;
            }

            return rect;
        }
    }
}