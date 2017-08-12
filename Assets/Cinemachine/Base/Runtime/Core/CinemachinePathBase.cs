using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>Abstract base class for a world-space path,
    /// suitable for a camera dolly track.</summary>
    public abstract class CinemachinePathBase : MonoBehaviour
    {
        /// <summary>The minimum value for the path position</summary>
        public abstract float MinPos { get; }

        /// <summary>The maximum value for the path position</summary>
        public abstract float MaxPos { get; }

        /// <summary>True if the path ends are joined to form a continuous loop</summary>
        public abstract bool Looped { get; }

        /// <summary>Get a normalized path position, taking spins into account if looped</summary>
        /// <param name="pos">Position along the path</param>
        /// <returns>Normalized position, between MinPos and MaxPos</returns>
        public virtual float NormalizePos(float pos)
        {
            if (MaxPos == 0)
                return 0;
            if (Looped)
            {
                pos = pos % MaxPos;
                if (pos < 0)
                    pos += MaxPos;
                return pos;
            }
            return Mathf.Clamp(pos, 0, MaxPos);
        }

        /// <summary>Get a worldspace position of a point along the path</summary>
        /// <param name="pos">Postion along the path.  Need not be normalized.</param>
        /// <returns>World-space position of the point along at path at pos</returns>
        public abstract Vector3 EvaluatePosition(float pos);

        /// <summary>Get the tangent of the curve at a point along the path.</summary>
        /// <param name="pos">Postion along the path.  Need not be normalized.</param>
        /// <returns>World-space direction of the path tangent.
        /// Length of the vector represents the tangent strength</returns>
        public abstract Vector3 EvaluateTangent(float pos);

        /// <summary>Get the orientation the curve at a point along the path.</summary>
        /// <param name="pos">Postion along the path.  Need not be normalized.</param>
        /// <returns>World-space orientation of the path</returns>
        public abstract Quaternion EvaluateOrientation(float pos);

        /// <summary>Find the closest point on the path to a given worldspace target point.</summary>
        /// <remarks>Performance could be improved by checking the bounding polygon of each segment,
        /// and only entering the best segment(s)</remarks>
        /// <param name="p">Worldspace target that we want to approach</param>
        /// <param name="startSegment">In what segment of the path to start the search</param>
        /// <param name="searchRadius">How many segments on either side of the startSegment
        /// to search.  -1 means no limit, i.e. search the entire path</param>
        /// <param name="stepsPerSegment">We search a segment by dividing it into this many
        /// straight pieces.  The higher the number, the more accurate the result, but performance
        /// is proportionally slower for higher numbers</param>
        /// <returns>The position along the path that is closest to the target point</returns>
        public virtual float FindClosestPoint(
            Vector3 p, int startSegment, int searchRadius, int stepsPerSegment)
        {
            float start = MinPos;
            float end = MaxPos;
            if (searchRadius >= 0)
            {
                float r = Mathf.Min(searchRadius, (end - start) / 2f);
                start = startSegment - r;
                end = startSegment + r + 1;
            }
            stepsPerSegment = Mathf.RoundToInt(Mathf.Clamp(stepsPerSegment, 1f, 100f));
            float stepSize = 1f / stepsPerSegment;
            float bestPos = startSegment;
            float bestDistance = float.MaxValue;
            int iterations = (stepsPerSegment == 1) ? 1 : 3;
            for (int i = 0; i < iterations; ++i)
            {
                Vector3 v0 = EvaluatePosition(start);
                for (float f = start + stepSize; f <= end; f += stepSize)
                {
                    Vector3 v = EvaluatePosition(f);
                    float t = p.ClosestPointOnSegment(v0, v);
                    float d = Vector3.SqrMagnitude(p - Vector3.Lerp(v0, v, t));
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        bestPos = f - (1 - t) * stepSize;
                    }
                    v0 = v;
                }
                start = bestPos - stepSize;
                end = bestPos + stepSize;
                stepSize /= stepsPerSegment;
            }
            return bestPos;
        }
    }
}
