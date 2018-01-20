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

namespace Jazz2.Game.Collisions
{
    internal struct Pair : IComparable<Pair>
    {
        public int ProxyIdA;
        public int ProxyIdB;

        #region IComparable<Pair> Members

        public int CompareTo(Pair other)
        {
            if (ProxyIdA < other.ProxyIdA) {
                return -1;
            }
            if (ProxyIdA == other.ProxyIdA) {
                if (ProxyIdB < other.ProxyIdB) {
                    return -1;
                }
                if (ProxyIdB == other.ProxyIdB) {
                    return 0;
                }
            }

            return 1;
        }

        #endregion
    }
}