/*
* Velcro Physics:
* Copyright (c) 2017 Ian Qvist
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using Duality;

namespace Jazz2.Game.Collisions
{
    /// <summary>
    /// An axis aligned bounding box.
    /// </summary>
    public struct AABB
    {
        /// <summary>
        /// The lower vertex
        /// </summary>
        public Vector2 LowerBound;

        /// <summary>
        /// The upper vertex
        /// </summary>
        public Vector2 UpperBound;

        public AABB(float left, float top, float right, float bottom)
        {
            LowerBound = new Vector2(left, top);
            UpperBound = new Vector2(right, bottom);
        }

        public AABB(Vector2 min, Vector2 max)
            : this(ref min, ref max) { }

        public AABB(Vector2 center, float width, float height)
            : this(center - new Vector2(width / 2, height / 2), center + new Vector2(width / 2, height / 2))
        {
        }

        public AABB(ref Vector2 min, ref Vector2 max)
        {
            LowerBound = new Vector2(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y));
            UpperBound = new Vector2(Math.Max(min.X, max.X), Math.Max(min.Y, max.Y));
        }

        public float Width => UpperBound.X - LowerBound.X;

        public float Height => UpperBound.Y - LowerBound.Y;

        /// <summary>
        /// Get the center of the AABB.
        /// </summary>
        public Vector2 Center => 0.5f * (LowerBound + UpperBound);

        /// <summary>
        /// Get the extents of the AABB (half-widths).
        /// </summary>
        public Vector2 Extents => 0.5f * (UpperBound - LowerBound);

        /// <summary>
        /// Get the perimeter length
        /// </summary>
        public float Perimeter
        {
            get
            {
                float wx = UpperBound.X - LowerBound.X;
                float wy = UpperBound.Y - LowerBound.Y;
                return 2.0f * (wx + wy);
            }
        }

        /// <summary>
        /// Verify that the bounds are sorted. And the bounds are valid numbers (not NaN).
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid()
        {
            Vector2 d = UpperBound - LowerBound;
            bool valid = d.X >= 0.0f && d.Y >= 0.0f;
            return valid && !float.IsNaN(LowerBound.X) && !float.IsNaN(LowerBound.Y) && !float.IsNaN(UpperBound.X) && !float.IsNaN(UpperBound.Y);
        }

        /// <summary>
        /// Combine an AABB into this one.
        /// </summary>
        /// <param name="aabb">The AABB.</param>
        public void Combine(ref AABB aabb)
        {
            LowerBound = Vector2.Min(LowerBound, aabb.LowerBound);
            UpperBound = Vector2.Max(UpperBound, aabb.UpperBound);
        }

        /// <summary>
        /// Combine two AABBs into this one.
        /// </summary>
        /// <param name="aabb1">The aabb1.</param>
        /// <param name="aabb2">The aabb2.</param>
        public void Combine(ref AABB aabb1, ref AABB aabb2)
        {
            LowerBound = Vector2.Min(aabb1.LowerBound, aabb2.LowerBound);
            UpperBound = Vector2.Max(aabb1.UpperBound, aabb2.UpperBound);
        }

        /// <summary>
        /// Does this AABB contain the provided AABB.
        /// </summary>
        /// <param name="aabb">The AABB.</param>
        /// <returns>
        /// <c>true</c> if it contains the specified AABB; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(ref AABB aabb)
        {
            return LowerBound.X <= aabb.LowerBound.X
                 && LowerBound.Y <= aabb.LowerBound.Y
                 && aabb.UpperBound.X <= UpperBound.X
                 && aabb.UpperBound.Y <= UpperBound.Y;
        }

        /// <summary>
        /// Determines whether the AABB contains the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// <c>true</c> if it contains the specified point; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(ref Vector2 point)
        {
            //using epsilon to try and guard against float rounding errors.
            return (point.X > (LowerBound.X + float.Epsilon) && point.X < (UpperBound.X - float.Epsilon) &&
                    (point.Y > (LowerBound.Y + float.Epsilon) && point.Y < (UpperBound.Y - float.Epsilon)));
        }

        /// <summary>
        /// Test if the two AABBs overlap.
        /// </summary>
        /// <param name="a">The first AABB.</param>
        /// <param name="b">The second AABB.</param>
        /// <returns>True if they are overlapping.</returns>
        public static bool TestOverlap(ref AABB a, ref AABB b)
        {
            return a.LowerBound.X <= b.UpperBound.X
                 && a.LowerBound.Y <= b.UpperBound.Y
                 && a.UpperBound.X >= b.LowerBound.X
                 && a.UpperBound.Y >= b.LowerBound.Y;
        }

        public AABB Intersection(ref AABB a, ref AABB b)
        {
            if (!TestOverlap(ref a, ref b)) {
                return new AABB(0, 0, 0, 0);
            }

            return new AABB(
                Math.Max(a.LowerBound.X, b.LowerBound.X),
                Math.Max(a.LowerBound.Y, b.LowerBound.Y),
                Math.Min(a.UpperBound.X, b.UpperBound.X),
                Math.Min(a.UpperBound.Y, b.UpperBound.Y));
        }

        public AABB Extend(float left, float top, float right, float bottom)
        {
            AABB aabb = this;
            aabb.LowerBound.X -= left;
            aabb.LowerBound.Y -= top;
            aabb.UpperBound.X += right;
            aabb.UpperBound.Y += bottom;
            return aabb;
        }

        public AABB Extend(float x, float y)
        {
            AABB aabb = this;
            aabb.LowerBound.X -= x;
            aabb.LowerBound.Y -= y;
            aabb.UpperBound.X += x;
            aabb.UpperBound.Y += y;
            return aabb;
        }

        public static AABB operator +(AABB aabb, Vector2 vec2)
        {
            aabb.LowerBound += vec2;
            aabb.UpperBound += vec2;
            return aabb;
        }

        public static AABB operator -(AABB aabb, Vector2 vec2)
        {
            aabb.LowerBound -= vec2;
            aabb.UpperBound -= vec2;
            return aabb;
        }

        /// <summary>
        /// Raycast against this AABB using the specified points and maxfraction (found in input)
        /// </summary>
        /// <param name="output">The results of the raycast.</param>
        /// <param name="input">The parameters for the raycast.</param>
        /// <returns>True if the ray intersects the AABB</returns>
        /*public bool RayCast(out RayCastOutput output, ref RayCastInput input, bool doInteriorCheck = true)
        {
            // From Real-time Collision Detection, p179.

            output = new RayCastOutput();

            float tmin = -Settings.MaxFloat;
            float tmax = Settings.MaxFloat;

            Vector2 p = input.Point1;
            Vector2 d = input.Point2 - input.Point1;
            Vector2 absD = MathUtils.Abs(d);

            Vector2 normal = Vector2.Zero;

            for (int i = 0; i < 2; ++i) {
                float absD_i = i == 0 ? absD.X : absD.Y;
                float lowerBound_i = i == 0 ? LowerBound.X : LowerBound.Y;
                float upperBound_i = i == 0 ? UpperBound.X : UpperBound.Y;
                float p_i = i == 0 ? p.X : p.Y;

                if (absD_i < Settings.Epsilon) {
                    // Parallel.
                    if (p_i < lowerBound_i || upperBound_i < p_i) {
                        return false;
                    }
                } else {
                    float d_i = i == 0 ? d.X : d.Y;

                    float inv_d = 1.0f / d_i;
                    float t1 = (lowerBound_i - p_i) * inv_d;
                    float t2 = (upperBound_i - p_i) * inv_d;

                    // Sign of the normal vector.
                    float s = -1.0f;

                    if (t1 > t2) {
                        MathUtils.Swap(ref t1, ref t2);
                        s = 1.0f;
                    }

                    // Push the min up
                    if (t1 > tmin) {
                        if (i == 0) {
                            normal.X = s;
                        } else {
                            normal.Y = s;
                        }

                        tmin = t1;
                    }

                    // Pull the max down
                    tmax = Math.Min(tmax, t2);

                    if (tmin > tmax) {
                        return false;
                    }
                }
            }

            // Does the ray start inside the box?
            // Does the ray intersect beyond the max fraction?
            if (doInteriorCheck && (tmin < 0.0f || input.MaxFraction < tmin)) {
                return false;
            }

            // Intersection.
            output.Fraction = tmin;
            output.Normal = normal;
            return true;
        }*/
    }
}