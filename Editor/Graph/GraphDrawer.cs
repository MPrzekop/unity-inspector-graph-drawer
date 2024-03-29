﻿using System.Linq;
using UnityEditor;
using UnityEngine;

namespace mprzekop.unityinspectorgraph.graph
{
    public static class GraphDrawer
    {
        private const float FontSize = 14;
        private const float DividersDrawLimit = 150000;

        private static void RemapSet(ref Vector3[] data, Vector2 oldMin, Vector2 oldMax, Vector2 newMin, Vector2 newMax)
        {
            float maxY = oldMax.y;
            float minY = oldMin.y;
            float maxX = oldMax.x;
            float minX = oldMin.x;

            for (int i = 0; i < data.Length; i++)
            {
                data[i].x = data[i].x.Remap(minX, maxX, newMin.x, newMax.x);
                data[i].y = newMax.y - data[i].y.Remap(minY, maxY, newMin.y, newMax.y);
            }
        }

        public static void DrawGraph(params Vector3[][] points)
        {
            var data = new LineData[points.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new LineData()
                {
                    color = Color.white,
                    points = points[i],
                    width = 2
                };
            }

            DrawGraph(200, 20, 20, Color.clear, false, data);
        }

        public static void DrawGraph(params LineFunctionData[] functions)
        {
            DrawGraph(200, 20, 20, false, functions);
        }

        public static void DrawGraph(int samples = 200, params LineFunctionData[] functions)
        {
            DrawGraph(samples, 20, 20, false, functions);
        }

        public static void DrawGraph(int samples = 200, float yRectPadding = 20, params LineFunctionData[] functions)
        {
            DrawGraph(samples, 20, yRectPadding, false, functions);
        }


        public static void DrawGraph(int samples = 200, float windowPadding = 20, float yRectPadding = 20,
            bool orthogonalLayout = false,
            params LineFunctionData[] functions)
        {
            var data = new LineData[functions.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new LineData()
                {
                    color = functions[i].LineColor,
                    points = LineFunctionSampler.SampleLineFunction(functions[i], samples),
                    width = functions[i].LineWidth
                };
            }

            DrawGraph(200, windowPadding, yRectPadding, Color.clear, orthogonalLayout, data);
        }

        public static void DrawGraph(
            params LineData[] data)
        {
            DrawGraph(200, 20, 20, Color.clear, false, data);
        }

        public static void DrawGraph(float yPadding = 20,
            params LineData[] data)
        {
            DrawGraph(200, 20, yPadding, Color.clear, false, data);
        }

        /// <summary>
        /// draw graph rect
        /// </summary>
        /// <param name="rectHeight"> max height of created rect</param>
        /// <param name="windowPadding"> padding between rect and graph </param>
        /// <param name="yPadding"> padding of line values to avoid clipping in the top/bottom of the graph window </param>
        /// <param name="backgroundColor"> color behind graph</param>
        /// <param name="orthogonalLayout"> should grid be of equal size on X and Y coords </param>
        /// <param name="data"> lines to draw </param>
        public static void DrawGraph(float rectHeight = 200, float windowPadding = 20, float yPadding = 20,
            Color backgroundColor = default, bool orthogonalLayout = false,
            params LineData[] data)
        {
            if (data == null || data.Length == 0) return;

            Color cache = GUI.color;
            Rect rect = GUILayoutUtility.GetRect(10, 1000, 200, rectHeight);
            if (backgroundColor == default)
            {
                backgroundColor = Color.clear;
            }

            EditorGUI.DrawRect(rect, backgroundColor);
            PreparePoints(windowPadding, yPadding, rect, orthogonalLayout, out var minY, out var maxY, out var minX,
                out var maxX,
                ref data);
            GUI.BeginGroup(rect);
            Handles.color = Color.gray;
            DrawDividers(minX, maxX, minY, maxY, windowPadding, rect);

            // Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1f, new Vector3(padding, rect.height / 2),
            //     new Vector3(rect.width - padding, rect.height / 2));
            foreach (var d in data)
            {
                Handles.color = d.color;
                Handles.DrawAAPolyLine(d.width, d.points);
            }

            GUI.EndGroup();
            GUI.color = cache;
        }

        public static void DrawGraphAttribute(Rect position, int samples, params LineFunctionData[] functions)
        {
            var data = new LineData[functions.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new LineData()
                {
                    color = functions[i].LineColor,
                    points = LineFunctionSampler.SampleLineFunction(functions[i], (samples)),
                    width = functions[i].LineWidth
                };
            }

            DrawGraphAttribute(position, data);
        }

        public static void DrawGraphAttribute(Rect position, params LineData[] data)
        {
            if (data == null || data.Length == 0) return;
            Color cache = GUI.color;

            float windowPadding = 20;
            var rect = new Rect(position.position, new Vector2(position.width, 200));
            float yPadding = 20;
            PreparePoints(windowPadding, yPadding, rect, false, out var minY, out var maxY, out var minX, out var maxX,
                ref data);

            GUI.BeginGroup(rect);
            Handles.color = Color.gray;
            DrawDividers(minX, maxX, minY, maxY, windowPadding, rect);

            // Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1f, new Vector3(padding, rect.height / 2),
            //     new Vector3(rect.width - padding, rect.height / 2));
            foreach (var d in data)
            {
                Handles.color = d.color;
                Handles.DrawAAPolyLine(d.width, d.points);
            }

            GUI.EndGroup();
            GUI.color = cache;
        }

        public static void DrawGraph(Vector3[] reference, Vector3[] data)
        {
            DrawGraph(new LineData() {color = Color.gray, points = reference, width = 1.5f},
                new LineData() {color = Color.white, points = data, width = 2f});
        }

        static void PreparePoints(float windowPadding, float yPadding, Rect rect, bool orthogonalLayout, out float minY,
            out float maxY,
            out float minX, out float maxX, ref LineData[] data)
        {
            var allPoints = data.Select(x => x.points).ToArray();
            var concatPoints = ConcatArrays(allPoints);
            maxY = Mathf.CeilToInt(concatPoints.Max(x => x.y) * 2f) / 2f;

            minY = Mathf.FloorToInt(concatPoints.Min(x => x.y) * 2f) / 2f;
            float rectToUnitY = Mathf.Abs(maxY - minY) / (rect.height - windowPadding * 2);
            float unitToRectY = (rect.height - windowPadding * 2) / Mathf.Abs(maxY - minY);


            maxY += rectToUnitY * yPadding;
            minY -= rectToUnitY * yPadding;
            maxX = concatPoints.Max(x => x.x);
            minX = concatPoints.Min(x => x.x);
            float rectToUnitX = Mathf.Abs(maxX - minX) / (rect.width - windowPadding * 2);
            float unitToRectX = (rect.width - windowPadding * 2) / Mathf.Abs(maxX - minX);
            if (orthogonalLayout)
            {
                maxX = Mathf.Max(maxX, maxY);
                maxY = maxX;
                maxX *= unitToRectX * rectToUnitY;
                minX = Mathf.Min(minX, minY);
                minY = minX;
                minX *= unitToRectX * rectToUnitY;
            }

            for (var index = 0; index < data.Length; index++)
            {
                RemapSet(ref data[index].points, new Vector2(minX, minY), new Vector2(maxX, maxY),
                    new Vector2(windowPadding, windowPadding),
                    new Vector2(rect.width - windowPadding, rect.height - windowPadding));
            }
        }

        private static void DrawDividers(float minX, float maxX, float minY, float maxY, float padding, Rect parent)
        {
            Handles.color = Color.gray;
            float unitToRect = (parent.width - padding * 2) / Mathf.Abs(maxX - minX);


            float offset = -minX;
            int startInt = Mathf.CeilToInt(minX);
            float unitToRectY = (parent.height - padding * 2) / Mathf.Abs(maxY - minY);
            int startY = Mathf.FloorToInt(minY);
            float offsetY = -minY;
            int xStep = (int) Mathf.CeilToInt(Mathf.Pow(10,
                Mathf.FloorToInt(Mathf.Max(Mathf.Log10(Mathf.Abs(maxX - minX)), 0))));
            if (Mathf.Abs(maxX - minX) / xStep < 2.5)
            {
                xStep = Mathf.CeilToInt(xStep / 4f);
            }
            else if (Mathf.Abs(maxX - minX) / xStep < 5)
            {
                xStep = Mathf.CeilToInt(xStep / 2f);
            }

            int yStep = (int) Mathf.CeilToInt(Mathf.Pow(10,
                Mathf.FloorToInt(Mathf.Max(Mathf.Log10(Mathf.Abs(maxY - minY)), 0))));
            if (Mathf.Abs(maxY - minY) / yStep < 2.5)
            {
                yStep = Mathf.CeilToInt(yStep / 4f);
            }
            else if (Mathf.Abs(maxY - minY) / yStep < 5)
            {
                yStep = Mathf.CeilToInt(yStep / 2f);
            }

            int xStart = Mathf.FloorToInt(minX / xStep) * xStep;
            int yStart = Mathf.FloorToInt(minY / yStep) * yStep;
            for (int i = (int) Mathf.Max(xStart, -DividersDrawLimit);
                i < (int) Mathf.Min(Mathf.CeilToInt(maxX), DividersDrawLimit);
                i += xStep)
            {
                Handles.color = Color.gray * 0.75f;
                var currentWidth = (i + offset) * unitToRect + padding;
                Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1f,
                    new Vector3(currentWidth, 0),
                    new Vector3(currentWidth, parent.height - padding));
                var textBox = new Rect(new Vector2((i + offset) * unitToRect + padding / 2, parent.height - padding),
                    new Vector2(120, FontSize));
                GUI.color = Color.white * 0.75f;

                GUI.Label(textBox, i.ToString());
                float divs = 10;
                float step = xStep / (divs);
                Handles.color = Color.gray * 0.55f;
                for (int j = 1; j < divs; j++)
                {
                    var divH = currentWidth + step * j * unitToRect;

                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1f,
                        new Vector3(divH, 0),
                        new Vector3(divH, parent.height - padding));
                }
            }

            for (int i = (int) Mathf.Max(yStart, -DividersDrawLimit);
                i <= Mathf.Min(Mathf.CeilToInt(maxY), DividersDrawLimit);
                i += yStep)
            {
                Handles.color = Color.gray * 0.75f;

                float h = parent.height - padding * 2;
                var currentHeight = (h - ((i + offsetY) * unitToRectY));
                if (currentHeight < (parent.height - padding))
                {
                    Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1.5f,
                        new Vector3(padding, currentHeight),
                        new Vector3(parent.width - padding, currentHeight));
                    var textBox = new Rect(new Vector2(0, currentHeight - FontSize / 2f),
                        new Vector2(120, FontSize));
                    GUI.color = Color.white * 0.75f;

                    GUI.Label(textBox, i.ToString());

                    float divs = 10;
                    float step = yStep / (divs);
                    Handles.color = Color.gray * 0.55f;

                    for (int j = 1; j < divs; j++)
                    {
                        var divH = currentHeight + step * j * unitToRectY;
                        if (divH < (parent.height - padding))
                        {
                            Handles.DrawAAPolyLine(Texture2D.whiteTexture, 1.5f,
                                new Vector3(padding, divH),
                                new Vector3(parent.width - padding, divH));
                        }
                    }
                }
            }
        }

//source https://gist.github.com/divayht/4317555
        public static T[] ConcatArrays<T>(params T[][] list)
        {
            var result = new T[list.Sum(a => a.Length)];
            int offset = 0;
            for (int x = 0; x < list.Length; x++)
            {
                list[x].CopyTo(result, offset);
                offset += list[x].Length;
            }

            return result;
        }
    }
}