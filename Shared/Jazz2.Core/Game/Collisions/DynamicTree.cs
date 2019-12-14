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
using System.Collections.Generic;
using System.Diagnostics;
using Duality;

namespace Jazz2.Game.Collisions
{
    /// <summary>
    /// A dynamic tree arranges data in a binary tree to accelerate
    /// queries such as volume queries and ray casts. Leafs are proxies
    /// with an AABB. In the tree we expand the proxy AABB by Settings.b2_fatAABBFactor
    /// so that the proxy AABB is bigger than the client object. This allows the client
    /// object to move by small amounts without triggering a tree update.
    /// Nodes are pooled and relocatable, so we use node indices rather than pointers.
    /// </summary>
    public class DynamicTree<T>
    {
        /// <summary>
        /// This is used to fatten AABBs in the dynamic tree. This allows proxies
        /// to move by a small amount without triggering a tree adjustment.
        /// This is in meters.
        /// </summary>
        public const float AABBExtension = 0.1f;

        /// <summary>
        /// This is used to fatten AABBs in the dynamic tree. This is used to predict
        /// the future position based on the current displacement.
        /// This is a dimensionless multiplier.
        /// </summary>
        public const float AABBMultiplier = 2.0f;


        internal const int NullNode = -1;
        private int freeList;
        private int nodeCapacity;
        private int nodeCount;
        private TreeNode<T>[] nodes;
        private Stack<int> queryStack = new Stack<int>(256);
        private Stack<int> raycastStack = new Stack<int>(256);
        private int root;

        /// <summary>
        /// Constructing the tree initializes the node pool.
        /// </summary>
        public DynamicTree()
        {
            root = NullNode;

            nodeCapacity = 16;
            nodeCount = 0;
            nodes = new TreeNode<T>[nodeCapacity];

            // Build a linked list for the free list.
            for (int i = 0; i < nodeCapacity - 1; ++i) {
                nodes[i] = new TreeNode<T>();
                nodes[i].ParentOrNext = i + 1;
                nodes[i].Height = 1;
            }
            nodes[nodeCapacity - 1] = new TreeNode<T>();
            nodes[nodeCapacity - 1].ParentOrNext = NullNode;
            nodes[nodeCapacity - 1].Height = 1;

            freeList = 0;
        }

        /// <summary>
        /// Compute the height of the binary tree in O(N) time. Should not be called often.
        /// </summary>
        public int Height
        {
            get
            {
                if (root == NullNode) {
                    return 0;
                }

                return nodes[root].Height;
            }
        }

        /// <summary>
        /// Get the ratio of the sum of the node areas to the root area.
        /// </summary>
        public float AreaRatio
        {
            get
            {
                if (this.root == NullNode) {
                    return 0.0f;
                }

                TreeNode<T> rootNode = nodes[root];
                float rootArea = rootNode.AABB.Perimeter;

                float totalArea = 0.0f;
                for (int i = 0; i < nodeCapacity; ++i) {
                    TreeNode<T> node = nodes[i];
                    if (node.Height < 0) {
                        // Free node in pool
                        continue;
                    }

                    totalArea += node.AABB.Perimeter;
                }

                return totalArea / rootArea;
            }
        }

        /// <summary>
        /// Get the maximum balance of an node in the tree. The balance is the difference
        /// in height of the two children of a node.
        /// </summary>
        public int MaxBalance
        {
            get
            {
                int maxBalance = 0;
                for (int i = 0; i < nodeCapacity; ++i) {
                    TreeNode<T> node = nodes[i];
                    if (node.Height <= 1) {
                        continue;
                    }

                    Debug.Assert(node.IsLeaf() == false);

                    int child1 = node.Child1;
                    int child2 = node.Child2;
                    int balance = Math.Abs(nodes[child2].Height - nodes[child1].Height);
                    maxBalance = Math.Max(maxBalance, balance);
                }

                return maxBalance;
            }
        }

        /// <summary>
        /// Create a proxy in the tree as a leaf node. We return the index
        /// of the node instead of a pointer so that we can grow
        /// the node pool.
        /// </summary>
        /// <param name="aabb">The AABB.</param>
        /// <param name="userData">The user data.</param>
        /// <returns>Index of the created proxy</returns>
        public int AddProxy(ref AABB aabb, T userData)
        {
            int proxyId = AllocateNode();

            // Fatten the AABB.
            Vector2 r = new Vector2(AABBExtension, AABBExtension);
            nodes[proxyId].AABB.LowerBound = aabb.LowerBound - r;
            nodes[proxyId].AABB.UpperBound = aabb.UpperBound + r;
            nodes[proxyId].UserData = userData;
            nodes[proxyId].Height = 0;

            InsertLeaf(proxyId);

            return proxyId;
        }

        /// <summary>
        /// Destroy a proxy. This asserts if the id is invalid.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void RemoveProxy(int proxyId)
        {
            Debug.Assert(0 <= proxyId && proxyId < nodeCapacity);
            Debug.Assert(nodes[proxyId].IsLeaf());

            RemoveLeaf(proxyId);
            FreeNode(proxyId);
        }

        /// <summary>
        /// Move a proxy with a swepted AABB. If the proxy has moved outside of its fattened AABB,
        /// then the proxy is removed from the tree and re-inserted. Otherwise
        /// the function returns immediately.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="aabb">The AABB.</param>
        /// <param name="displacement">The displacement.</param>
        /// <returns>true if the proxy was re-inserted.</returns>
        public bool MoveProxy(int proxyId, ref AABB aabb, Vector2 displacement)
        {
            Debug.Assert(0 <= proxyId && proxyId < nodeCapacity);
            Debug.Assert(nodes[proxyId].IsLeaf());

            if (nodes[proxyId].AABB.Contains(ref aabb)) {
                return false;
            }

            RemoveLeaf(proxyId);

            // Extend AABB.
            AABB b = aabb;
            Vector2 r = new Vector2(AABBExtension, AABBExtension);
            b.LowerBound = b.LowerBound - r;
            b.UpperBound = b.UpperBound + r;

            // Predict AABB displacement.
            Vector2 d = AABBMultiplier * displacement;

            if (d.X < 0.0f) {
                b.LowerBound.X += d.X;
            } else {
                b.UpperBound.X += d.X;
            }

            if (d.Y < 0.0f) {
                b.LowerBound.Y += d.Y;
            } else {
                b.UpperBound.Y += d.Y;
            }

            nodes[proxyId].AABB = b;

            InsertLeaf(proxyId);
            return true;
        }

        /// <summary>
        /// Get proxy user data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns>the proxy user data or 0 if the id is invalid.</returns>
        public T GetUserData(int proxyId)
        {
            Debug.Assert(0 <= proxyId && proxyId < nodeCapacity);

            return nodes[proxyId].UserData;
        }

        /// <summary>
        /// Get the fat AABB for a proxy.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="fatAABB">The fat AABB.</param>
        public void GetFatAABB(int proxyId, out AABB fatAABB)
        {
            Debug.Assert(0 <= proxyId && proxyId < nodeCapacity);

            fatAABB = nodes[proxyId].AABB;
        }

        /// <summary>
        /// Query an AABB for overlapping proxies. The callback class
        /// is called for each proxy that overlaps the supplied AABB.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="aabb">The AABB.</param>
        public void Query(Func<T, bool> callback, ref AABB aabb)
        {
            queryStack.Clear();
            queryStack.Push(root);

            while (queryStack.Count > 0) {
                int nodeId = queryStack.Pop();
                if (nodeId == NullNode) {
                    continue;
                }

                TreeNode<T> node = nodes[nodeId];

                if (AABB.TestOverlap(ref node.AABB, ref aabb)) {
                    if (node.IsLeaf()) {
                        bool proceed = callback(nodes[nodeId].UserData);
                        if (!proceed) {
                            return;
                        }
                    } else {
                        queryStack.Push(node.Child1);
                        queryStack.Push(node.Child2);
                    }
                }
            }
        }

        /// <summary>
        /// Ray-cast against the proxies in the tree. This relies on the callback
        /// to perform a exact ray-cast in the case were the proxy contains a Shape.
        /// The callback also performs the any collision filtering. This has performance
        /// roughly equal to k * log(n), where k is the number of collisions and n is the
        /// number of proxies in the tree.
        /// </summary>
        /// <param name="callback">A callback class that is called for each proxy that is hit by the ray.</param>
        /// <param name="input">The ray-cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1).</param>
        /*public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
        {
            Vector2 p1 = input.Point1;
            Vector2 p2 = input.Point2;
            Vector2 r = p2 - p1;
            Debug.Assert(r.LengthSquared > 0.0f);
            r.Normalize();

            // v is perpendicular to the segment.
            Vector2 absV = new Vector2(MathF.Abs(r.Y), MathF.Abs(r.X));

            // Separating axis for segment (Gino, p80).
            // |dot(v, p1 - c)| > dot(|v|, h)

            float maxFraction = input.MaxFraction;

            // Build a bounding box for the segment.
            AABB segmentAABB = new AABB();
            {
                Vector2 t = p1 + maxFraction * (p2 - p1);
                Vector2.Min(ref p1, ref t, out segmentAABB.LowerBound);
                Vector2.Max(ref p1, ref t, out segmentAABB.UpperBound);
            }

            raycastStack.Clear();
            raycastStack.Push(root);

            while (raycastStack.Count > 0) {
                int nodeId = raycastStack.Pop();
                if (nodeId == NullNode) {
                    continue;
                }

                TreeNode<T> node = _nodes[nodeId];

                if (AABB.TestOverlap(ref node.AABB, ref segmentAABB) == false) {
                    continue;
                }

                // Separating axis for segment (Gino, p80).
                // |dot(v, p1 - c)| > dot(|v|, h)
                Vector2 c = node.AABB.Center;
                Vector2 h = node.AABB.Extents;
                float separation = Math.Abs(Vector2.Dot(new Vector2(-r.Y, r.X), p1 - c)) - Vector2.Dot(absV, h);
                if (separation > 0.0f) {
                    continue;
                }

                if (node.IsLeaf()) {
                    RayCastInput subInput;
                    subInput.Point1 = input.Point1;
                    subInput.Point2 = input.Point2;
                    subInput.MaxFraction = maxFraction;

                    float value = callback(subInput, nodeId);

                    if (value == 0.0f) {
                        // the client has terminated the raycast.
                        return;
                    }

                    if (value > 0.0f) {
                        // Update segment bounding box.
                        maxFraction = value;
                        Vector2 t = p1 + maxFraction * (p2 - p1);
                        segmentAABB.LowerBound = Vector2.Min(p1, t);
                        segmentAABB.UpperBound = Vector2.Max(p1, t);
                    }
                } else {
                    raycastStack.Push(node.Child1);
                    raycastStack.Push(node.Child2);
                }
            }
        }*/

        private int AllocateNode()
        {
            // Expand the node pool as needed.
            if (freeList == NullNode) {
                Debug.Assert(nodeCount == nodeCapacity);

                // The free list is empty. Rebuild a bigger pool.
                TreeNode<T>[] oldNodes = nodes;
                nodeCapacity *= 2;
                nodes = new TreeNode<T>[nodeCapacity];
                Array.Copy(oldNodes, nodes, nodeCount);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                for (int i = nodeCount; i < nodeCapacity - 1; ++i) {
                    nodes[i] = new TreeNode<T>();
                    nodes[i].ParentOrNext = i + 1;
                    nodes[i].Height = -1;
                }
                nodes[nodeCapacity - 1] = new TreeNode<T>();
                nodes[nodeCapacity - 1].ParentOrNext = NullNode;
                nodes[nodeCapacity - 1].Height = -1;

                freeList = nodeCount;
            }

            // Peel a node off the free list.
            int nodeId = freeList;
            freeList = nodes[nodeId].ParentOrNext;

            nodes[nodeId].ParentOrNext = NullNode;
            nodes[nodeId].Child1 = NullNode;
            nodes[nodeId].Child2 = NullNode;
            nodes[nodeId].Height = 0;
            nodes[nodeId].UserData = default(T);

            ++nodeCount;
            return nodeId;
        }

        private void FreeNode(int nodeId)
        {
            Debug.Assert(0 <= nodeId && nodeId < nodeCapacity);
            Debug.Assert(0 < nodeCount);

            nodes[nodeId].ParentOrNext = freeList;
            nodes[nodeId].Height = -1;
            freeList = nodeId;
            --nodeCount;
        }

        private void InsertLeaf(int leaf)
        {
            if (root == NullNode) {
                root = leaf;
                nodes[root].ParentOrNext = NullNode;
                return;
            }

            // Find the best sibling for this node
            AABB leafAABB = nodes[leaf].AABB;
            int index = root;
            while (nodes[index].IsLeaf() == false) {
                int child1 = nodes[index].Child1;
                int child2 = nodes[index].Child2;

                float area = nodes[index].AABB.Perimeter;

                AABB combinedAABB = new AABB();
                combinedAABB.Combine(ref nodes[index].AABB, ref leafAABB);
                float combinedArea = combinedAABB.Perimeter;

                // Cost of creating a new parent for this node and the new leaf
                float cost = 2.0f * combinedArea;

                // Minimum cost of pushing the leaf further down the tree
                float inheritanceCost = 2.0f * (combinedArea - area);

                // Cost of descending into child1
                float cost1;
                if (nodes[child1].IsLeaf()) {
                    AABB aabb = new AABB();
                    aabb.Combine(ref leafAABB, ref nodes[child1].AABB);
                    cost1 = aabb.Perimeter + inheritanceCost;
                } else {
                    AABB aabb = new AABB();
                    aabb.Combine(ref leafAABB, ref nodes[child1].AABB);
                    float oldArea = nodes[child1].AABB.Perimeter;
                    float newArea = aabb.Perimeter;
                    cost1 = (newArea - oldArea) + inheritanceCost;
                }

                // Cost of descending into child2
                float cost2;
                if (nodes[child2].IsLeaf()) {
                    AABB aabb = new AABB();
                    aabb.Combine(ref leafAABB, ref nodes[child2].AABB);
                    cost2 = aabb.Perimeter + inheritanceCost;
                } else {
                    AABB aabb = new AABB();
                    aabb.Combine(ref leafAABB, ref nodes[child2].AABB);
                    float oldArea = nodes[child2].AABB.Perimeter;
                    float newArea = aabb.Perimeter;
                    cost2 = newArea - oldArea + inheritanceCost;
                }

                // Descend according to the minimum cost.
                if (cost < cost1 && cost1 < cost2) {
                    break;
                }

                // Descend
                if (cost1 < cost2) {
                    index = child1;
                } else {
                    index = child2;
                }
            }

            int sibling = index;

            // Create a new parent.
            int oldParent = nodes[sibling].ParentOrNext;
            int newParent = AllocateNode();
            nodes[newParent].ParentOrNext = oldParent;
            nodes[newParent].UserData = default(T);
            nodes[newParent].AABB.Combine(ref leafAABB, ref nodes[sibling].AABB);
            nodes[newParent].Height = nodes[sibling].Height + 1;

            if (oldParent != NullNode) {
                // The sibling was not the root.
                if (nodes[oldParent].Child1 == sibling) {
                    nodes[oldParent].Child1 = newParent;
                } else {
                    nodes[oldParent].Child2 = newParent;
                }

                nodes[newParent].Child1 = sibling;
                nodes[newParent].Child2 = leaf;
                nodes[sibling].ParentOrNext = newParent;
                nodes[leaf].ParentOrNext = newParent;
            } else {
                // The sibling was the root.
                nodes[newParent].Child1 = sibling;
                nodes[newParent].Child2 = leaf;
                nodes[sibling].ParentOrNext = newParent;
                nodes[leaf].ParentOrNext = newParent;
                root = newParent;
            }

            // Walk back up the tree fixing heights and AABBs
            index = nodes[leaf].ParentOrNext;
            while (index != NullNode) {
                index = Balance(index);

                int child1 = nodes[index].Child1;
                int child2 = nodes[index].Child2;

                Debug.Assert(child1 != NullNode);
                Debug.Assert(child2 != NullNode);

                nodes[index].Height = 1 + Math.Max(nodes[child1].Height, nodes[child2].Height);
                nodes[index].AABB.Combine(ref nodes[child1].AABB, ref nodes[child2].AABB);

                index = nodes[index].ParentOrNext;
            }

            //Validate();
        }

        private void RemoveLeaf(int leaf)
        {
            if (leaf == root) {
                root = NullNode;
                return;
            }

            int parent = nodes[leaf].ParentOrNext;
            int grandParent = nodes[parent].ParentOrNext;
            int sibling;
            if (nodes[parent].Child1 == leaf) {
                sibling = nodes[parent].Child2;
            } else {
                sibling = nodes[parent].Child1;
            }

            if (grandParent != NullNode) {
                // Destroy parent and connect sibling to grandParent.
                if (nodes[grandParent].Child1 == parent) {
                    nodes[grandParent].Child1 = sibling;
                } else {
                    nodes[grandParent].Child2 = sibling;
                }
                nodes[sibling].ParentOrNext = grandParent;
                FreeNode(parent);

                // Adjust ancestor bounds.
                int index = grandParent;
                while (index != NullNode) {
                    index = Balance(index);

                    int child1 = nodes[index].Child1;
                    int child2 = nodes[index].Child2;

                    nodes[index].AABB.Combine(ref nodes[child1].AABB, ref nodes[child2].AABB);
                    nodes[index].Height = 1 + Math.Max(nodes[child1].Height, nodes[child2].Height);

                    index = nodes[index].ParentOrNext;
                }
            } else {
                root = sibling;
                nodes[sibling].ParentOrNext = NullNode;
                FreeNode(parent);
            }

            //Validate();
        }

        /// <summary>
        /// Perform a left or right rotation if node A is imbalanced.
        /// </summary>
        /// <param name="iA"></param>
        /// <returns>the new root index.</returns>
        private int Balance(int iA)
        {
            Debug.Assert(iA != NullNode);

            TreeNode<T> A = nodes[iA];
            if (A.IsLeaf() || A.Height < 2) {
                return iA;
            }

            int iB = A.Child1;
            int iC = A.Child2;
            Debug.Assert(0 <= iB && iB < nodeCapacity);
            Debug.Assert(0 <= iC && iC < nodeCapacity);

            TreeNode<T> B = nodes[iB];
            TreeNode<T> C = nodes[iC];

            int balance = C.Height - B.Height;

            // Rotate C up
            if (balance > 1) {
                int iF = C.Child1;
                int iG = C.Child2;
                TreeNode<T> F = nodes[iF];
                TreeNode<T> G = nodes[iG];
                Debug.Assert(0 <= iF && iF < nodeCapacity);
                Debug.Assert(0 <= iG && iG < nodeCapacity);

                // Swap A and C
                C.Child1 = iA;
                C.ParentOrNext = A.ParentOrNext;
                A.ParentOrNext = iC;

                // A's old parent should point to C
                if (C.ParentOrNext != NullNode) {
                    if (nodes[C.ParentOrNext].Child1 == iA) {
                        nodes[C.ParentOrNext].Child1 = iC;
                    } else {
                        Debug.Assert(nodes[C.ParentOrNext].Child2 == iA);
                        nodes[C.ParentOrNext].Child2 = iC;
                    }
                } else {
                    root = iC;
                }

                // Rotate
                if (F.Height > G.Height) {
                    C.Child2 = iF;
                    A.Child2 = iG;
                    G.ParentOrNext = iA;
                    A.AABB.Combine(ref B.AABB, ref G.AABB);
                    C.AABB.Combine(ref A.AABB, ref F.AABB);

                    A.Height = 1 + Math.Max(B.Height, G.Height);
                    C.Height = 1 + Math.Max(A.Height, F.Height);
                } else {
                    C.Child2 = iG;
                    A.Child2 = iF;
                    F.ParentOrNext = iA;
                    A.AABB.Combine(ref B.AABB, ref F.AABB);
                    C.AABB.Combine(ref A.AABB, ref G.AABB);

                    A.Height = 1 + Math.Max(B.Height, F.Height);
                    C.Height = 1 + Math.Max(A.Height, G.Height);
                }

                return iC;
            }

            // Rotate B up
            if (balance < -1) {
                int iD = B.Child1;
                int iE = B.Child2;
                TreeNode<T> D = nodes[iD];
                TreeNode<T> E = nodes[iE];
                Debug.Assert(0 <= iD && iD < nodeCapacity);
                Debug.Assert(0 <= iE && iE < nodeCapacity);

                // Swap A and B
                B.Child1 = iA;
                B.ParentOrNext = A.ParentOrNext;
                A.ParentOrNext = iB;

                // A's old parent should point to B
                if (B.ParentOrNext != NullNode) {
                    if (nodes[B.ParentOrNext].Child1 == iA) {
                        nodes[B.ParentOrNext].Child1 = iB;
                    } else {
                        Debug.Assert(nodes[B.ParentOrNext].Child2 == iA);
                        nodes[B.ParentOrNext].Child2 = iB;
                    }
                } else {
                    root = iB;
                }

                // Rotate
                if (D.Height > E.Height) {
                    B.Child2 = iD;
                    A.Child1 = iE;
                    E.ParentOrNext = iA;
                    A.AABB.Combine(ref C.AABB, ref E.AABB);
                    B.AABB.Combine(ref A.AABB, ref D.AABB);

                    A.Height = 1 + Math.Max(C.Height, E.Height);
                    B.Height = 1 + Math.Max(A.Height, D.Height);
                } else {
                    B.Child2 = iE;
                    A.Child1 = iD;
                    D.ParentOrNext = iA;
                    A.AABB.Combine(ref C.AABB, ref D.AABB);
                    B.AABB.Combine(ref A.AABB, ref E.AABB);

                    A.Height = 1 + Math.Max(C.Height, D.Height);
                    B.Height = 1 + Math.Max(A.Height, E.Height);
                }

                return iB;
            }

            return iA;
        }

        /// <summary>
        /// Compute the height of a sub-tree.
        /// </summary>
        /// <param name="nodeId">The node id to use as parent.</param>
        /// <returns>The height of the tree.</returns>
        public int ComputeHeight(int nodeId)
        {
            Debug.Assert(0 <= nodeId && nodeId < nodeCapacity);
            TreeNode<T> node = nodes[nodeId];

            if (node.IsLeaf()) {
                return 0;
            }

            int height1 = ComputeHeight(node.Child1);
            int height2 = ComputeHeight(node.Child2);
            return 1 + Math.Max(height1, height2);
        }

        /// <summary>
        /// Compute the height of the entire tree.
        /// </summary>
        /// <returns>The height of the tree.</returns>
        public int ComputeHeight()
        {
            int height = ComputeHeight(root);
            return height;
        }

        public void ValidateStructure(int index)
        {
            if (index == NullNode) {
                return;
            }

            if (index == root) {
                Debug.Assert(nodes[index].ParentOrNext == NullNode);
            }

            TreeNode<T> node = nodes[index];

            int child1 = node.Child1;
            int child2 = node.Child2;

            if (node.IsLeaf()) {
                Debug.Assert(child1 == NullNode);
                Debug.Assert(child2 == NullNode);
                Debug.Assert(node.Height == 0);
                return;
            }

            Debug.Assert(0 <= child1 && child1 < nodeCapacity);
            Debug.Assert(0 <= child2 && child2 < nodeCapacity);

            Debug.Assert(nodes[child1].ParentOrNext == index);
            Debug.Assert(nodes[child2].ParentOrNext == index);

            ValidateStructure(child1);
            ValidateStructure(child2);
        }

        public void ValidateMetrics(int index)
        {
            if (index == NullNode) {
                return;
            }

            TreeNode<T> node = nodes[index];

            int child1 = node.Child1;
            int child2 = node.Child2;

            if (node.IsLeaf()) {
                Debug.Assert(child1 == NullNode);
                Debug.Assert(child2 == NullNode);
                Debug.Assert(node.Height == 0);
                return;
            }

            Debug.Assert(0 <= child1 && child1 < nodeCapacity);
            Debug.Assert(0 <= child2 && child2 < nodeCapacity);

            int height1 = nodes[child1].Height;
            int height2 = nodes[child2].Height;
            int height = 1 + Math.Max(height1, height2);
            Debug.Assert(node.Height == height);

            AABB AABB = new AABB();
            AABB.Combine(ref nodes[child1].AABB, ref nodes[child2].AABB);

            Debug.Assert(AABB.LowerBound == node.AABB.LowerBound);
            Debug.Assert(AABB.UpperBound == node.AABB.UpperBound);

            ValidateMetrics(child1);
            ValidateMetrics(child2);
        }

        /// <summary>
        /// Validate this tree. For testing.
        /// </summary>
        public void Validate()
        {
            ValidateStructure(root);
            ValidateMetrics(root);

            int freeCount = 0;
            int freeIndex = freeList;
            while (freeIndex != NullNode) {
                Debug.Assert(0 <= freeIndex && freeIndex < nodeCapacity);
                freeIndex = nodes[freeIndex].ParentOrNext;
                ++freeCount;
            }

            Debug.Assert(Height == ComputeHeight());

            Debug.Assert(nodeCount + freeCount == nodeCapacity);
        }

        /// <summary>
        /// Build an optimal tree. Very expensive. For testing.
        /// </summary>
        public void RebuildBottomUp()
        {
            int[] nodes = new int[nodeCount];
            int count = 0;

            // Build array of leaves. Free the rest.
            for (int i = 0; i < nodeCapacity; ++i) {
                if (this.nodes[i].Height < 0) {
                    // free node in pool
                    continue;
                }

                if (this.nodes[i].IsLeaf()) {
                    this.nodes[i].ParentOrNext = NullNode;
                    nodes[count] = i;
                    ++count;
                } else {
                    FreeNode(i);
                }
            }

            while (count > 1) {
                float minCost = float.MaxValue;
                int iMin = -1, jMin = -1;
                for (int i = 0; i < count; ++i) {
                    AABB AABBi = this.nodes[nodes[i]].AABB;

                    for (int j = i + 1; j < count; ++j) {
                        AABB AABBj = this.nodes[nodes[j]].AABB;
                        AABB b = new AABB();
                        b.Combine(ref AABBi, ref AABBj);
                        float cost = b.Perimeter;
                        if (cost < minCost) {
                            iMin = i;
                            jMin = j;
                            minCost = cost;
                        }
                    }
                }

                int index1 = nodes[iMin];
                int index2 = nodes[jMin];
                TreeNode<T> child1 = this.nodes[index1];
                TreeNode<T> child2 = this.nodes[index2];

                int parentIndex = AllocateNode();
                TreeNode<T> parent = this.nodes[parentIndex];
                parent.Child1 = index1;
                parent.Child2 = index2;
                parent.Height = 1 + Math.Max(child1.Height, child2.Height);
                parent.AABB.Combine(ref child1.AABB, ref child2.AABB);
                parent.ParentOrNext = NullNode;

                child1.ParentOrNext = parentIndex;
                child2.ParentOrNext = parentIndex;

                nodes[jMin] = nodes[count - 1];
                nodes[iMin] = parentIndex;
                --count;
            }

            root = nodes[0];

            Validate();
        }

        /// <summary>
        /// Shift the origin of the nodes
        /// </summary>
        /// <param name="newOrigin">The displacement to use.</param>
        public void ShiftOrigin(Vector2 newOrigin)
        {
            // Build array of leaves. Free the rest.
            for (int i = 0; i < nodeCapacity; ++i) {
                nodes[i].AABB.LowerBound -= newOrigin;
                nodes[i].AABB.UpperBound -= newOrigin;
            }
        }
    }
}