using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.TerrainTools;
using System.Runtime.InteropServices;
using UnityEngine.UIElements;

namespace Multislider
{
    [CustomEditor(typeof(MultisliderCore))]
    [RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Image))]
    public class MultisliderEditor : Editor
    {
        private const float SLIDER_TIMEOUT = 1f;
        private bool sliderFoldout = true;
        private float slider_drag_timer = float.NaN;

        public override void OnInspectorGUI()
        {
            MultisliderCore script = (MultisliderCore)(object)target;

            OnRectComponent("Multislider", script.rect, false);
            if (script.rect == null)
                script.rect = script.GetComponent<RectTransform>();

            OnRectComponent("Slider Bar", script.bar);

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
                int newDecimals = script.decimals;
                OnSlider("Decimals", 0, 10, ref newDecimals);
                if (newDecimals != script.decimals)
                    script.decimals = newDecimals;

                EditorGUI.BeginDisabledGroup(script.decimals != 0);
                int newMultiples = script.multiples;
                OnSlider("Multiples", 1, 10, ref newMultiples);
                if (newMultiples != script.multiples)
                    script.multiples = newMultiples;
                EditorGUI.EndDisabledGroup();

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

                if (float.IsNaN(script.minWidth))
                    script.minWidth = script.Ceil(0, true);
                float newSliderWidth = script.minWidth;
                float sliderMin = script.sliderMinWidth;
                float sliderMax = script.Floor(script.bar.rect.width / 2);
                if (float.IsInfinity(sliderMin) || sliderMin > sliderMax)
                    sliderMin = sliderMax;
                OnSlider("Slider Min Width", script.Ceil(sliderMin, true), sliderMax, ref newSliderWidth);
                if (newSliderWidth != script.minWidth)
                {
                    script.minWidth = newSliderWidth;
                    script.updateWidth();
                }

                if (float.IsNaN(script.minValue))
                    script.minValue = script.minLimit;
                if (float.IsNaN(script.maxValue))
                    script.maxValue = script.maxLimit;
                float minValue = script.Ceil(script.minValue);
                float maxValue = script.Floor(script.maxValue);
                OnMinMaxSlider("Slider Bar Range", script.Ceil(script.minLimit), script.Floor(script.maxLimit), ref minValue, ref maxValue);
                if (minValue != script.minValue || maxValue != script.maxValue)
                {
                    if (minValue.difference(maxValue) < script.minDistance)
                        maxValue = minValue + script.minDistance;
                    script.minValue = minValue;
                    script.maxValue = maxValue;
                    script.updateSliderPos();
                }

                if (!float.IsNaN(slider_drag_timer))
                    slider_drag_timer -= Time.deltaTime;

                sliderFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(sliderFoldout, "Slider", EditorStyles.foldoutHeader);
                if (sliderFoldout)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    if (/*Application.isPlaying && */GUILayout.Button("+"))
                        script.addSlider();
                    for (int i = 0; i < script.sliderElements.Count; i++)
                    {
                        MultisliderElement msc = script.sliderElements[i];
                        EditorGUILayout.BeginHorizontal();

                        float newVal = msc.value;
                        OnSlider("", script.minValue, script.maxValue, ref newVal);
                        if (newVal != msc.value)
                        {
                            msc.moveElement(newVal, true);
                            slider_drag_timer = SLIDER_TIMEOUT;
                        }

                        if (GUILayout.Button("-"))
                            script.removeSlider(msc);

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                if (!float.IsNaN(slider_drag_timer) && slider_drag_timer <= 0)
                {
                    script.updateSliderOrder();
                    script.updateSliderPos();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Components missing");
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

        public void OnRectComponent(string title, RectTransform rect, ref Color color, ref Sprite sprite, bool rectIsChangeable = true)
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

            c = EditorGUILayout.ColorField(c);
            EditorGUILayout.EndVertical();
            s = (Sprite)EditorGUILayout.ObjectField(s, typeof(Sprite), true, GUILayout.Width(60), GUILayout.Height(60));
            color = c;
            sprite = s;
            EditorGUILayout.EndHorizontal();
        }

        public void OnRectComponent(string title, RectTransform rect, bool rectIsChangeable = true)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title);
            EditorGUI.BeginDisabledGroup(!rectIsChangeable);
            rect = (RectTransform)EditorGUILayout.ObjectField(rect, typeof(RectTransform), true);
            EditorGUI.EndDisabledGroup();

            Color c = Color.white;
            Sprite s = null;
            UnityEngine.UI.Image image = null;
            if (rect != null)
            {
                image = rect.GetComponent<UnityEngine.UI.Image>();
                c = image.color;
                s = image.sprite;
            }
            EditorGUI.BeginDisabledGroup(image == null);
            c = EditorGUILayout.ColorField(c);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            EditorGUI.BeginDisabledGroup(image == null);
            s = (Sprite)EditorGUILayout.ObjectField(s, typeof(Sprite), true, GUILayout.Width(60), GUILayout.Height(60));
            EditorGUI.EndDisabledGroup();
            if (image != null)
            {
                image.color = c;
                image.sprite = s;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}