using System.Collections.Generic;
using UnityEngine;

namespace GameCamp.Game.Path
{
    public sealed class PathCurveEvaluator
    {
        private readonly List<Vector2> runtimeWorldPoints = new();
        private readonly List<Vector2> sampledPoints = new();
        private readonly List<float> cumulativeLengths = new();

        private int linearSubdivisions = 4;
        private int bezierSubdivisions = 8;
        private bool useBezierSmoothing = true;

        public float TotalLength { get; private set; }
        public bool IsValid => sampledPoints.Count >= 2;

        public void SetSamplingQuality(int linear, int bezier)
        {
            linearSubdivisions = Mathf.Max(1, linear);
            bezierSubdivisions = Mathf.Max(2, bezier);

            if (runtimeWorldPoints.Count >= 2)
            {
                Rebuild();
            }
        }

        public void SetUseBezierSmoothing(bool useBezier)
        {
            useBezierSmoothing = useBezier;
            if (runtimeWorldPoints.Count >= 2)
            {
                Rebuild();
            }
        }

        public void SetWorldPath(IReadOnlyList<Vector2> worldPoints)
        {
            runtimeWorldPoints.Clear();
            if (worldPoints != null)
            {
                for (int i = 0; i < worldPoints.Count; i++)
                {
                    runtimeWorldPoints.Add(worldPoints[i]);
                }
            }

            Rebuild();
        }

        public bool Evaluate(float distance, out Vector2 position, out Vector2 tangent)
        {
            position = Vector2.zero;
            tangent = Vector2.up;

            if (!IsValid)
            {
                return false;
            }

            float d = Mathf.Clamp(distance, 0f, TotalLength);
            int segmentIndex = FindSegmentIndex(d);

            Vector2 a = sampledPoints[segmentIndex];
            Vector2 b = sampledPoints[segmentIndex + 1];

            float start = cumulativeLengths[segmentIndex];
            float end = cumulativeLengths[segmentIndex + 1];
            float length = Mathf.Max(0.0001f, end - start);
            float t = Mathf.Clamp01((d - start) / length);

            position = Vector2.Lerp(a, b, t);
            tangent = (b - a).normalized;
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector2.up;
            }

            return true;
        }

        private void Rebuild()
        {
            sampledPoints.Clear();
            cumulativeLengths.Clear();
            TotalLength = 0f;

            if (runtimeWorldPoints.Count < 2)
            {
                return;
            }

            if (runtimeWorldPoints.Count == 2)
            {
                AddLinear(runtimeWorldPoints[0], runtimeWorldPoints[1], linearSubdivisions);
            }
            else if (!useBezierSmoothing)
            {
                BuildLinearPath(runtimeWorldPoints);
            }
            else
            {
                BuildSmoothedPath(runtimeWorldPoints);
            }

            RebuildLengthLut();
        }

        private void BuildSmoothedPath(List<Vector2> src)
        {
            int n = src.Count;

            sampledPoints.Add(src[0]);

            Vector2 firstMid = Vector2.Lerp(src[0], src[1], 0.5f);
            AddLinear(src[0], firstMid, linearSubdivisions);

            for (int i = 1; i <= n - 2; i++)
            {
                Vector2 prev = src[i - 1];
                Vector2 curr = src[i];
                Vector2 next = src[i + 1];

                Vector2 start = Vector2.Lerp(prev, curr, 0.5f);
                Vector2 end = Vector2.Lerp(curr, next, 0.5f);

                AddQuadraticBezier(start, curr, end, bezierSubdivisions);
            }

            Vector2 lastMid = Vector2.Lerp(src[n - 2], src[n - 1], 0.5f);
            AddLinear(lastMid, src[n - 1], linearSubdivisions);
        }

        private void BuildLinearPath(List<Vector2> src)
        {
            sampledPoints.Add(src[0]);
            for (int i = 1; i < src.Count; i++)
            {
                AddLinear(src[i - 1], src[i], linearSubdivisions);
            }
        }

        private void AddLinear(Vector2 a, Vector2 b, int subdivisions)
        {
            for (int i = 1; i <= subdivisions; i++)
            {
                float t = i / (float)subdivisions;
                AddSamplePoint(Vector2.Lerp(a, b, t));
            }
        }

        private void AddQuadraticBezier(Vector2 a, Vector2 control, Vector2 b, int subdivisions)
        {
            for (int i = 1; i <= subdivisions; i++)
            {
                float t = i / (float)subdivisions;
                float oneMinusT = 1f - t;
                Vector2 p =
                    (oneMinusT * oneMinusT * a) +
                    (2f * oneMinusT * t * control) +
                    (t * t * b);

                AddSamplePoint(p);
            }
        }

        private void AddSamplePoint(Vector2 p)
        {
            if (sampledPoints.Count == 0)
            {
                sampledPoints.Add(p);
                return;
            }

            Vector2 last = sampledPoints[sampledPoints.Count - 1];
            if ((p - last).sqrMagnitude < 0.000001f)
            {
                return;
            }

            sampledPoints.Add(p);
        }

        private void RebuildLengthLut()
        {
            cumulativeLengths.Clear();
            TotalLength = 0f;

            if (sampledPoints.Count == 0)
            {
                return;
            }

            cumulativeLengths.Add(0f);
            for (int i = 1; i < sampledPoints.Count; i++)
            {
                float segment = Vector2.Distance(sampledPoints[i - 1], sampledPoints[i]);
                TotalLength += segment;
                cumulativeLengths.Add(TotalLength);
            }
        }

        private int FindSegmentIndex(float distance)
        {
            if (distance <= 0f)
            {
                return 0;
            }

            for (int i = 0; i < cumulativeLengths.Count - 1; i++)
            {
                if (distance <= cumulativeLengths[i + 1])
                {
                    return i;
                }
            }

            return cumulativeLengths.Count - 2;
        }
    }
}
