/*
===================================================================================
 Author          : Xiya Tao
 Institution     : Universidad Politécnica de Madrid (UPM)
 Module          : DataFusionBuffer.cs
 Modification    : Implements sliding-window numerical fusion for synchronized multimodal frames.
                   - Receives aligned Kinect, Emotion, and Sensor data from TemporalAligner
                   - Converts heterogeneous frame dictionaries into unified numeric feature vectors
                   - Maintains a fixed-length temporal window for model inference
                   - Exports data as [1, T, D] tensors directly usable by ONNX or deep models
                   - Supports configurable window length and step size
                   - Provides average frame-interval diagnostics and safe buffer clearing
 Last Modified   : 2025-11-03
===================================================================================
*/


using System;
using System.Collections.Generic;
using System.Linq;

namespace Kinect_Middleware.Fusion
{
    /// <summary>
    /// 实时多模态数据融合缓存。
    /// 功能：
    ///  1️ 接收 Kinect + Emotion + Sensor 三模态的最新帧
    ///  2️ 基于时间戳自动对齐
    ///  3️ 维持固定长度窗口
    ///  4️ 输出 [1, T, D] 张量（可直接输入ONNX模型）
    /// </summary>
    public class DataFusionBuffer
    {
        private readonly int windowSize; 
        private readonly int stepSize;
        private readonly List<FusedFrame> buffer;

        public DataFusionBuffer(int windowSize = 20, int stepSize = 5)
        {
            this.windowSize = windowSize;
            this.stepSize = stepSize;
            buffer = new List<FusedFrame>();
        }

        /// <summary>
        /// 向缓存中添加新的一帧（已融合的模态数据）
        /// </summary>
        public void AddFrame(Dictionary<string, object> frameData)
        {
            var frame = new FusedFrame
            {
                Timestamp = DateTime.Now,
                Features = new Dictionary<string, float>()
            };

            foreach (var kvp in frameData)
            {
                if (kvp.Value is float f)
                    frame.Features[kvp.Key] = f;
                else if (float.TryParse(kvp.Value?.ToString(), out float val))
                    frame.Features[kvp.Key] = val;
            }

            // 追加新帧
            buffer.Add(frame);

            // 超出窗口大小则滑动
            if (buffer.Count > windowSize)
                buffer.RemoveRange(0, stepSize);
        }

        /// <summary>
        /// 返回窗口内的 [1, T, D] 张量，用于模型推理
        /// </summary>
        public float[,,] GetTensor()
        {
            if (buffer.Count < windowSize)
                return null;

            int featureCount = buffer.Last().Features.Count;
            float[,,] tensor = new float[1, windowSize, featureCount];

            for (int t = 0; t < windowSize; t++)
            {
                var frame = buffer[t];
                int i = 0;
                foreach (var val in frame.Features.Values)
                    tensor[0, t, i++] = val;
            }

            return tensor;
        }

        /// <summary>
        /// 获取窗口内的平均时间间隔（调试用途）
        /// </summary>
        public double GetAverageDeltaTime()
        {
            if (buffer.Count < 2) return 0;
            var diffs = new List<double>();
            for (int i = 1; i < buffer.Count; i++)
                diffs.Add((buffer[i].Timestamp - buffer[i - 1].Timestamp).TotalMilliseconds);
            return diffs.Average();
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear() => buffer.Clear();
    }

    /// <summary>
    /// 单帧数据结构
    /// </summary>
    public class FusedFrame
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, float> Features { get; set; }
    }
}
