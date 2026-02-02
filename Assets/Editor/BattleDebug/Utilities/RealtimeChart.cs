using System.Collections.Generic;
using UnityEngine;

namespace Editor.BattleDebug
{
    /// <summary>
    /// 实时图表 - 用于显示性能数据曲线
    /// </summary>
    public class RealtimeChart
    {
        private readonly Queue<float> _values;
        private readonly int _maxSamples;
        private readonly string _label;
        private readonly Color _lineColor;
        private readonly Color _backgroundColor;
        
        private float _minValue;
        private float _maxValue;
        private float _currentValue;
        
        public float CurrentValue => _currentValue;
        public float MinValue => _minValue;
        public float MaxValue => _maxValue;
        public float AverageValue { get; private set; }
        
        public RealtimeChart(string label, int maxSamples = 100, Color? lineColor = null)
        {
            _label = label;
            _maxSamples = maxSamples;
            _values = new Queue<float>(maxSamples);
            _lineColor = lineColor ?? BattleDebugStyles.ChartLineColor;
            _backgroundColor = BattleDebugStyles.ChartBackgroundColor;
            
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
        }
        
        /// <summary>
        /// 添加数据点
        /// </summary>
        public void AddSample(float value)
        {
            _currentValue = value;
            _values.Enqueue(value);
            
            while (_values.Count > _maxSamples)
                _values.Dequeue();
            
            // 更新统计
            UpdateStats();
        }
        
        /// <summary>
        /// 清空数据
        /// </summary>
        public void Clear()
        {
            _values.Clear();
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
            _currentValue = 0;
            AverageValue = 0;
        }
        
        private void UpdateStats()
        {
            if (_values.Count == 0) return;
            
            float sum = 0;
            float min = float.MaxValue;
            float max = float.MinValue;
            
            foreach (var v in _values)
            {
                sum += v;
                if (v < min) min = v;
                if (v > max) max = v;
            }
            
            _minValue = min;
            _maxValue = max;
            AverageValue = sum / _values.Count;
        }
        
        /// <summary>
        /// 绘制图表
        /// </summary>
        public void Draw(float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height);
            Draw(rect);
        }
        
        /// <summary>
        /// 在指定区域绘制图表
        /// </summary>
        public void Draw(Rect rect)
        {
            // 绘制背景
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, _backgroundColor, 0, 0);
            
            if (_values.Count < 2) return;
            
            // 计算范围（留10%边距）
            float range = _maxValue - _minValue;
            if (range < 0.001f) range = 1f;
            float padding = range * 0.1f;
            float displayMin = _minValue - padding;
            float displayMax = _maxValue + padding;
            float displayRange = displayMax - displayMin;
            
            // 绘制网格线
            DrawGrid(rect, displayMin, displayMax, 4);
            
            // 绘制曲线
            var values = _values.ToArray();
            float xStep = rect.width / (values.Length - 1);
            
            Vector3 prevPoint = Vector3.zero;
            for (int i = 0; i < values.Length; i++)
            {
                float x = rect.x + i * xStep;
                float normalizedY = (values[i] - displayMin) / displayRange;
                float y = rect.y + rect.height - normalizedY * rect.height;
                
                var currentPoint = new Vector3(x, y, 0);
                
                if (i > 0)
                {
                    // 使用Handles绘制线条
                    DrawLine(prevPoint, currentPoint, _lineColor, 2f);
                }
                
                prevPoint = currentPoint;
            }
            
            // 绘制标签和当前值
            var labelRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, 16);
            GUI.Label(labelRect, $"{_label}: {_currentValue:F2}", BattleDebugStyles.SubHeaderStyle);
            
            var statsRect = new Rect(rect.x + 5, rect.y + rect.height - 18, rect.width - 10, 16);
            GUI.Label(statsRect, $"Min:{_minValue:F1} Max:{_maxValue:F1} Avg:{AverageValue:F1}", BattleDebugStyles.DisabledStyle);
        }
        
        private void DrawGrid(Rect rect, float minValue, float maxValue, int lines)
        {
            var gridColor = BattleDebugStyles.ChartGridColor;
            float step = rect.height / (lines + 1);
            
            for (int i = 1; i <= lines; i++)
            {
                float y = rect.y + i * step;
                DrawLine(
                    new Vector3(rect.x, y, 0),
                    new Vector3(rect.x + rect.width, y, 0),
                    gridColor,
                    1f
                );
            }
        }
        
        private void DrawLine(Vector3 from, Vector3 to, Color color, float width)
        {
            // 使用GL绘制（在OnGUI中工作）
            if (Event.current.type != EventType.Repaint) return;
            
            GL.PushMatrix();
            
            var mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            mat.SetPass(0);
            
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex(from);
            GL.Vertex(to);
            GL.End();
            
            GL.PopMatrix();
        }
    }
}
