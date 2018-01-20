/*
* Velcro Physics:
* Copyright (c) 2017 Ian Qvist
* 
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org 
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
using System.Diagnostics;
using Duality;
using Jazz2.Actors;

namespace Jazz2.Game.Collisions
{
    /// <summary>
    /// The broad-phase is used for computing pairs and performing volume queries and ray casts.
    /// This broad-phase does not persist pairs. Instead, this reports potentially new pairs.
    /// It is up to the client to consume the new pairs and to track subsequent overlap.
    /// </summary>
    public class DynamicTreeBroadPhase
    {
        public delegate void BroadphaseHandler(ActorBase proxyA, ActorBase proxyB);

        private const int NullProxy = -1;
        private int[] moveBuffer;
        private int moveCapacity;
        private int moveCount;

        private Pair[] pairBuffer;
        private int pairCapacity;
        private int pairCount;
        private int proxyCount;
        private Func<ActorBase, bool> queryCallback;
        private int queryProxyId;
        private DynamicTree<ActorBase> tree = new DynamicTree<ActorBase>();

        /// <summary>
        /// Constructs a new broad phase based on the dynamic tree implementation
        /// </summary>
        public DynamicTreeBroadPhase()
        {
            queryCallback = QueryCallback;
            proxyCount = 0;

            pairCapacity = 16;
            pairCount = 0;
            pairBuffer = new Pair[pairCapacity];

            moveCapacity = 16;
            moveCount = 0;
            moveBuffer = new int[moveCapacity];
        }

        /// <summary>
        /// Get the tree quality based on the area of the tree.
        /// </summary>
        public float TreeQuality => tree.AreaRatio;

        /// <summary>
        /// Gets the height of the tree.
        /// </summary>
        public int TreeHeight => tree.Height;

        /// <summary>
        /// Get the number of proxies.
        /// </summary>
        /// <value>The proxy count.</value>
        public int ProxyCount => proxyCount;

        /// <summary>
        /// Create a proxy with an initial AABB. Pairs are not reported until
        /// UpdatePairs is called.
        /// </summary>
        /// <param name="proxy">The user data.</param>
        /// <returns></returns>
        public int AddProxy(ActorBase proxy)
        {
            Debug.Assert(proxy.ProxyId == -1);

            int proxyId = tree.AddProxy(ref proxy.AABB, proxy);

            proxy.ProxyId = proxyId;

            ++proxyCount;
            BufferMove(proxyId);
            return proxyId;
        }

        /// <summary>
        /// Destroy a proxy. It is up to the client to remove any pairs.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void RemoveProxy(ActorBase proxy)
        {
            //Debug.Assert(proxy.ProxyId != -1);
            if (proxy.ProxyId == -1) {
                return;
            }

            UnBufferMove(proxy.ProxyId);
            --proxyCount;
            tree.RemoveProxy(proxy.ProxyId);

            proxy.ProxyId = -1;
        }

        /// <summary>
        /// Call MoveProxy as many times as you like, then when you are done
        /// call UpdatePairs to finalized the proxy pairs (for your time step).
        /// </summary>
        public void MoveProxy(ActorBase proxy, ref AABB aabb, Vector2 displacement)
        {
            Debug.Assert(proxy.ProxyId != -1);

            bool buffer = tree.MoveProxy(proxy.ProxyId, ref aabb, displacement);
            if (buffer)
                BufferMove(proxy.ProxyId);
        }

        /// <summary>
        /// Call to trigger a re-processing of it's pairs on the next call to UpdatePairs.
        /// </summary>
        public void TouchProxy(ActorBase proxy)
        {
            Debug.Assert(proxy.ProxyId != -1);

            BufferMove(proxy.ProxyId);
        }

        /// <summary>
        /// Get the AABB for a proxy.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="aabb">The AABB.</param>
        public void GetFatAABB(ActorBase proxy, out AABB aabb)
        {
            Debug.Assert(proxy.ProxyId != -1);

            tree.GetFatAABB(proxy.ProxyId, out aabb);
        }

        /// <summary>
        /// Get user data from a proxy. Returns null if the id is invalid.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns></returns>
        /*public ActorBase GetProxy(int proxyId)
        {
            return tree.GetUserData(proxyId);
        }*/

        /// <summary>
        /// Test overlap of fat AABBs.
        /// </summary>
        /// <param name="proxyIdA">The proxy id A.</param>
        /// <param name="proxyIdB">The proxy id B.</param>
        /// <returns></returns>
        public bool TestOverlap(ActorBase proxyA, ActorBase proxyB)
        {
            tree.GetFatAABB(proxyA.ProxyId, out AABB aabbA);
            tree.GetFatAABB(proxyB.ProxyId, out AABB aabbB);
            return AABB.TestOverlap(ref aabbA, ref aabbB);
        }

        /// <summary>
        /// Update the pairs. This results in pair callbacks. This can only add pairs.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public void UpdatePairs(BroadphaseHandler callback)
        {
            // Reset pair buffer
            pairCount = 0;

            // Perform tree queries for all moving proxies.
            for (int j = 0; j < moveCount; ++j) {
                queryProxyId = moveBuffer[j];
                if (queryProxyId == NullProxy)
                    continue;

                // We have to query the tree with the fat AABB so that
                // we don't fail to create a pair that may touch later.
                tree.GetFatAABB(queryProxyId, out AABB fatAABB);

                // Query tree, create pairs and add them pair buffer.
                tree.Query(queryCallback, ref fatAABB);
            }

            // Reset move buffer
            moveCount = 0;

            // Sort the pair buffer to expose duplicates.
            Array.Sort(pairBuffer, 0, pairCount);

            // Send the pairs back to the client.
            int i = 0;
            while (i < pairCount) {
                Pair primaryPair = pairBuffer[i];
                ActorBase userDataA = tree.GetUserData(primaryPair.ProxyIdA);
                ActorBase userDataB = tree.GetUserData(primaryPair.ProxyIdB);

                callback(userDataA, userDataB);
                ++i;

                // Skip any duplicate pairs.
                while (i < pairCount) {
                    Pair pair = pairBuffer[i];
                    if (pair.ProxyIdA != primaryPair.ProxyIdA || pair.ProxyIdB != primaryPair.ProxyIdB)
                        break;

                    ++i;
                }
            }

            // Try to keep the tree balanced.
            //_tree.Rebalance(4);
        }

        /// <summary>
        /// Query an AABB for overlapping proxies. The callback class
        /// is called for each proxy that overlaps the supplied AABB.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="aabb">The AABB.</param>
        public void Query(Func<ActorBase, bool> callback, ref AABB aabb)
        {
            tree.Query(callback, ref aabb);
        }

        /// <summary>
        /// Ray-cast against the proxies in the tree. This relies on the callback
        /// to perform a exact ray-cast in the case were the proxy contains a shape.
        /// The callback also performs the any collision filtering. This has performance
        /// roughly equal to k * log(n), where k is the number of collisions and n is the
        /// number of proxies in the tree.
        /// </summary>
        /// <param name="callback">A callback class that is called for each proxy that is hit by the ray.</param>
        /// <param name="input">The ray-cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1).</param>
        /*public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
        {
            _tree.RayCast(callback, ref input);
        }*/

        /// <summary>
        /// Shift the world origin. Useful for large worlds.
        /// </summary>
        public void ShiftOrigin(Vector2 newOrigin)
        {
            tree.ShiftOrigin(newOrigin);
        }

        private void BufferMove(int proxyId)
        {
            if (moveCount == moveCapacity) {
                int[] oldBuffer = moveBuffer;
                moveCapacity *= 2;
                moveBuffer = new int[moveCapacity];
                Array.Copy(oldBuffer, moveBuffer, moveCount);
            }

            moveBuffer[moveCount] = proxyId;
            ++moveCount;
        }

        private void UnBufferMove(int proxyId)
        {
            for (int i = 0; i < moveCount; ++i) {
                if (moveBuffer[i] == proxyId)
                    moveBuffer[i] = NullProxy;
            }
        }

        /// <summary>
        /// This is called from DynamicTree.Query when we are gathering pairs.
        /// </summary>
        private bool QueryCallback(ActorBase proxy)
        {
            // A proxy cannot form a pair with itself.
            if (proxy.ProxyId == queryProxyId)
                return true;

            // Grow the pair buffer as needed.
            if (pairCount == pairCapacity) {
                Pair[] oldBuffer = pairBuffer;
                pairCapacity *= 2;
                pairBuffer = new Pair[pairCapacity];
                Array.Copy(oldBuffer, pairBuffer, pairCount);
            }

            pairBuffer[pairCount].ProxyIdA = Math.Min(proxy.ProxyId, queryProxyId);
            pairBuffer[pairCount].ProxyIdB = Math.Max(proxy.ProxyId, queryProxyId);
            ++pairCount;

            return true;
        }
    }
}