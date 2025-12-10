/*
===================================================================================
 Author          : Xiya Tao
 Institution     : Universidad Politécnica de Madrid (UPM)
 Modification    : Implements real-time temporal alignment for multimodal data streams.
                   - Collects Kinect, Emotion, and Sensor frames in independent buffers
                   - Aligns heterogeneous frame rates within a configurable time window
                   - Emits one fused JSON frame every defined interval (default 100 ms)
                   - Selects nearest timestamps for each modality to ensure synchronization
                   - Enables downstream fusion (e.g., DataFusionBuffer or UDP transmission)
                   - Handles missing modalities safely and returns null for empty channels
 Last Modified   : 2025-11-03
===================================================================================
*/



using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Kinect_Middleware.Core
{
    public class TemporalAligner
    {
        private readonly int windowMs;
        private long lastEmit = 0;

        private readonly List<Tuple<long, Dictionary<string, object>>> skeletonBuffer =
            new List<Tuple<long, Dictionary<string, object>>>();
        private readonly List<Tuple<long, Dictionary<string, object>>> emotionBuffer =
            new List<Tuple<long, Dictionary<string, object>>>();
        private readonly List<Tuple<long, Dictionary<string, object>>> sensorBuffer =
            new List<Tuple<long, Dictionary<string, object>>>();

        public TemporalAligner(int windowMs = 100)
        {
            this.windowMs = windowMs;
        }

        public void AddSkeleton(long ts, Dictionary<string, object> data)
        {
            skeletonBuffer.Add(Tuple.Create(ts, data));
        }

        public void AddEmotion(long ts, Dictionary<string, object> data)
        {
            emotionBuffer.Add(Tuple.Create(ts, data));
        }

        public void AddSensor(long ts, Dictionary<string, object> data)
        {
            sensorBuffer.Add(Tuple.Create(ts, data));
        }

        public string TryEmitAligned()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - lastEmit < windowMs) return null;
            lastEmit = now;

            var aligned = new Dictionary<string, object>();
            aligned["timestamp"] = now;
            aligned["skeleton"] = GetNearest(skeletonBuffer, now);
            aligned["emotion"] = GetNearest(emotionBuffer, now);
            aligned["sensor"] = GetNearest(sensorBuffer, now);

            return JsonConvert.SerializeObject(aligned, Formatting.None);
        }

        private static object GetNearest(List<Tuple<long, Dictionary<string, object>>> buffer, long t)
        {
            if (buffer.Count == 0) return null;
            var nearest = buffer.OrderBy(b => Math.Abs(b.Item1 - t)).First();
            return nearest.Item2;
        }
    }
}
