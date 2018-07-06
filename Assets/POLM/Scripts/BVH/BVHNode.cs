using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

struct Node						// 32 bytes	
{
    // Bounds
    Vector3 vfMin;              // 12 bytes
    Vector3 vfMax;              // 12 bytes

    int nPrimitives;            // 4 bytes
    uint primitivesOffset;		// 4 bytes
};

struct LBVHNode					// 32 bytes
{
    // Bounds
    Vector3 vfMin;              // 12 bytes
    Vector3 vfMax;              // 12 bytes

    int iPrimCount;             // 4 bytes
    int iPrimPos;				// 4 bytes
};


[System.Serializable]
public class BVHNode : IPrimitive
{
    // Public Variables
    public BVHBBox bbox;
    public IPrimitive left;
    public IPrimitive right;

    int primitivesCount = 0;
    public int nodeID   = 0;
    public int parentID = 0;
    public int lChildID = 0;
    public int rChildID = 0;
    public bool isLeaf  = false;

    //uint splitAxis, firstPrimOffset, nPrimitives;

    public BVHNode(List<IPrimitive> list, float t0, float t1, ref List<IPrimitive> bvhNodes)
    {
        var axis = UnityEngine.Random.Range(0, 3);

        switch (axis)
        {
            case 0:
                list.Sort(CompareX);
                break;
            case 1:
                list.Sort(CompareY);
                break;
            case 2:
                list.Sort(CompareZ);
                break;
        }

        if (list.Count == 1) // LEAF
        {
            // The only left ptimitive is assigned to both left an right leaf nodes... but the primitivesCount for the one of the, (the right one) is se to 0
            // Later on we have to check if the primitives count is greated than 0 when trying to raycast against the triangle primitive
            left = right = list[0]; // Original code

            left.PrimitivesCount = right.PrimitivesCount = 1;
            left.IsLeaf = right.IsLeaf = true;

            // NOTE : NNED TO BE SURE THAT APPLYING IDs TO LEAFS IS DONE PROPERLY
             left.NodeID = bvhNodes.Count;
            right.NodeID = bvhNodes.Count;
        }
        else if (list.Count == 2) // LEAFS
        {
            left  = list[0];    // Original code
            right = list[1];    // Original code

            left.PrimitivesCount = right.PrimitivesCount = 1;
            left.IsLeaf = right.IsLeaf = true;

            // NOTE : NNED TO BE SURE THAT APPLYING IDs TO LEAFS IS DONE PROPERLY
             left.NodeID = bvhNodes.Count;
            right.NodeID = bvhNodes.Count + 1;
        }
        else // INNER
        {
            left  = new BVHNode(list.GetRange(0, list.Count / 2), t0, t1, ref bvhNodes);                                // Original code
            right = new BVHNode(list.GetRange(list.Count / 2, list.Count - (list.Count / 2)), t0, t1, ref bvhNodes);    // Original code
        }

        NodeID = bvhNodes.Count;

        BVHBBox boxLeft, boxRight;                  // Original code
        boxLeft  = left.BBox;                       // Original code
        boxRight = right.BBox;                      // Original code

        LChildID = bvhNodes.Count;
        rChildID = bvhNodes.Count + 1;

        left.ParentID  = NodeID;
        right.ParentID = NodeID;

        BBox = SurroundingBox(boxLeft, boxRight);

        //BBox.CreateBBoxGizmo("bbox");

        bvhNodes.Add(left);
        bvhNodes.Add(right);

        //BVHCreator.bvhNodes.Add(left);
        //BVHCreator.bvhNodes.Add(right);
    }

    // ==============================================================================================================
    // IPrimitive interface methods
    public int PrimitivesCount { get { return primitivesCount; } set { primitivesCount = value; } }
    public int NodeID   { get { return   nodeID; } set {   nodeID = value; } }
    public int ParentID { get { return parentID; } set { parentID = value; } }
    public int LChildID { get { return lChildID; } set { lChildID = value; } }
    public int RChildID { get { return rChildID; } set { rChildID = value; } }
    public bool IsLeaf  { get { return   isLeaf; } set {   isLeaf = value; } }
    public void CalculateBBox() { }
    public bool IntersectRay(Ray r, float _tMin, float _tMax, ref HitInfo hitInfo)
    {
        float tmin, tmax, tymin, tymax, tzmin, tzmax;
        float flag = 1f;
        Vector3 m1 = bbox.min, m2 = bbox.max;
        if (r.direction.x >= 0)
        {
            tmin = (m1.x - r.origin.x) / r.direction.x;
            tmax = (m2.x - r.origin.x) / r.direction.x;
        }
        else
        {
            tmin = (m2.x - r.origin.x) / r.direction.x;
            tmax = (m1.x - r.origin.x) / r.direction.x;
        }
        if (r.direction.y >= 0)
        {
            tymin = (m1.y - r.origin.y) / r.direction.y;
            tymax = (m2.y - r.origin.y) / r.direction.y;
        }
        else
        {
            tymin = (m2.y - r.origin.y) / r.direction.y;
            tymax = (m1.y - r.origin.y) / r.direction.y;
        }

        if ((tmin > tymax) || (tymin > tmax)) flag = -1f;
        if (tymin > tmin) tmin = tymin;
        if (tymax < tmax) tmax = tymax;

        if (r.direction.z >= 0)
        {
            tzmin = (m1.z - r.origin.z) / r.direction.z;
            tzmax = (m2.z - r.origin.z) / r.direction.z;
        }
        else
        {
            tzmin = (m2.z - r.origin.z) / r.direction.z;
            tzmax = (m1.z - r.origin.z) / r.direction.z;
        }
        if ((tmin > tzmax) || (tzmin > tmax)) flag = -1f;
        if (tzmin > tmin) tmin = tzmin;
        if (tzmax < tmax) tmax = tzmax;

        hitInfo = new HitInfo();
        hitInfo.hitDistance = tmin;
        return (flag > 0f);
    }
    public bool IntersectBBox(Ray ray, float tmin, float tmax, ref HitInfo hitInfo)
    {
        if (bbox.IntersectRay(ray, tmin, tmax))
        {
            //float leftRec = float.MaxValue, rightRec = float.MaxValue;
            HitInfo leftRec = new HitInfo(), rightRec = new HitInfo();
            var hitLeft  = left.IntersectBBox(ray, tmin, tmax, ref leftRec);
            var hitRight = right.IntersectBBox(ray, tmin, tmax, ref rightRec);

            if (hitRight && hitLeft)
            {
                hitInfo = leftRec.hitDistance < rightRec.hitDistance ? leftRec : rightRec;
                return true;
            }
            else if (hitLeft)
            {
                hitInfo = leftRec;
                return true;
            }
            else if (hitRight)
            {
                hitInfo = rightRec;
                return true;
            }
        }

        hitInfo = null;
        return false;
    }


    //IPrimitive GetNode(int nID)
    //{
    //    return BVHCreator.bvhNodes[nID];
    //}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="hitInfo"></param>
    /// <returns></returns>
    public bool TraverseWithStack(Ray ray, ref List<IPrimitive> bvhNodes, ref HitInfo hitInfo)
    {
        int [] stack = new int[64];
        int stackPos = 0, node = bvhNodes.Count - 1;

        bool intersect = false;

        IPrimitive cellLeft, cellRight;
        IPrimitive current = bvhNodes[node];

        while (true)
        {
            if (current.IsLeaf)
            {
                //Debug.Log(hitInfo.hitDistance);

                intersect = current.IntersectBBox(ray, 0, 100f, ref hitInfo);

                if (intersect)
                {
                    //Debug.Log(intersect + " | " + hitInfo.hitDistance);
                    return true;
                }

                if (stackPos > 0)
                {
                    node = stack[--stackPos];
                    current = bvhNodes[node];
                }
                else
                    return false;
            }
            else
            {
                //Debug.Log("inner");

                int leftNode  =  current.LChildID;
                int rightNode =  current.RChildID;
                float lMin, rMin;

                cellLeft  = bvhNodes[leftNode];
                cellRight = bvhNodes[rightNode];

                bool wantLeft  =  cellLeft.BBox.bounds.IntersectRay(ray, out lMin);
                bool wantRight = cellRight.BBox.bounds.IntersectRay(ray, out rMin);
                
                if (wantLeft && wantRight)
                {
                    bool firstLeft = lMin < rMin;
                    if (firstLeft)
                    {
                        current = cellLeft;
                        node = leftNode;
                        stack[stackPos++] = rightNode;
                    }
                    else
                    {
                        current = cellRight;
                        node = rightNode;
                        stack[stackPos++] = leftNode;
                    }
                }
                else if (wantRight)
                {
                    current = cellRight;
                    node = rightNode;
                }
                else if (wantLeft)
                {
                    current = cellLeft;
                    node = leftNode;
                }
                else
                {
                    if (stackPos > 0)
                    {
                        node = stack[--stackPos];
                        current = bvhNodes[node];
                    }
                    else return false;
                }
            }
        }
    }

    public bool TraverseStackless(Ray ray, ref List<IPrimitive> bvhNodes, ref HitInfo hitInfo)
    {
        IPrimitive root = this;
        IPrimitive last = root;
        IPrimitive current = this;
        IPrimitive near, far;

        int c = nearChild(ray, root, ref bvhNodes);
        if (c == -1) { Debug.Log(c + " | " + "Current children nodes missed"); return false; }// quick way to check if both children are not intersected
        current = bvhNodes[c];

        int iters = 0;

        //while (true)
        while (iters < 10)
        {
            //iters++;

            // 0 ----------------------------------------------------------------------------------------------------

            int nChild = nearChild(ray, current, ref bvhNodes);
            if (nChild == -1) { Debug.Log(nChild + " | " + "Children nodes missed"); return false; }// quick way to check if both children are not intersected

            int n = nChild;
            int f = nChild == LChildID ? RChildID : LChildID;

            near = bvhNodes[n];
            far  = bvhNodes[f];

            //BVHCreator.bvhNodes[n].BBox.CreateBBoxGizmo(n.ToString());
            //BVHCreator.bvhNodes[f].BBox.CreateBBoxGizmo(f.ToString());

            // 1 ----------------------------------------------------------------------------------------------------

            if (last == far)
            {
                last = current;
                current = bvhNodes[current.ParentID];
                Debug.Log("last equal to current");
                continue;
            }

            // 2 ----------------------------------------------------------------------------------------------------

            Debug.Log(last.ParentID + " | " + current.ParentID);// BVHCreator.bvhNodes[current.ParentID].ParentID);
            IPrimitive tryChild = (last == bvhNodes[current.ParentID]) ? near : far;
            
            if (current.BBox.bounds.IntersectRay(ray))
            {
                Debug.Log("intersects current bbox");
                last    = current;
                current = tryChild;
            }
            else
            {
                Debug.Log("No intersects current bbox");
                if (tryChild == near)
                {
                    last = near;
                }
                else
                {
                    last = current;
                    current = bvhNodes[current.ParentID];
                }
            }

            //last.BBox.CreateBBoxGizmo("last leaf");
            //current.BBox.CreateBBoxGizmo("current leaf");
            //tryChild.BBox.CreateBBoxGizmo("tryChild leaf");
            //return true;

            // 3 ----------------------------------------------------------------------------------------------------

            if (current == root)
            {
                Debug.Log("Current is the root");
                return false;
            }
        }
        return true;
    }

    /*
    near = nearChild(current);
    far  =  farChild(current);
    // alreadyreturned from farchild - traverse up
    if(last == far) 
    {
        last = current;
        current = parent(current);
        continue;
    }
    // if coming from parent, try near child , else far child
    tryChild = (last == parent(current)) ? near : far;
    if(boxtest(ray , current)
    {
        // i f box was h i t, de s c end
        last = current;
        current = tryChild;
    }
    else
    {
        // if missed
        if(tryChild == near)
        {
            // next is far
            last = near;
        }
        else
        {
            // goup instead
            last = current; current = parent(current);
        }
    }
    */

    int nearChild(Ray r, IPrimitive p, ref List<IPrimitive> bvhNodes)
    {
        float t1 = float.MaxValue, t2 = float.MaxValue;
        bool lHit, rHit;
        lHit = bvhNodes[p.LChildID].BBox.bounds.IntersectRay(r, out t1);
        rHit = bvhNodes[p.RChildID].BBox.bounds.IntersectRay(r, out t2);

        Debug.Log(t1 + " | " + t2);

        //if (t1 == float.MaxValue && t2 == float.MaxValue) return -1;

        //BVHCreator.bvhNodes[p.LChildID].BBox.CreateBBoxGizmo("l");
        //BVHCreator.bvhNodes[p.RChildID].BBox.CreateBBoxGizmo("r");

        if (lHit && rHit)
        {
            if (t1 <= t2) return p.LChildID;
            if (t1 >  t2) return p.RChildID;
        }
        else if(lHit)
        {
            return p.LChildID;
        }
        else if(rHit)
        {
            return p.RChildID;
        }

        return -1;
    }

    int farChild(Ray r, IPrimitive p)
    {
        return 0;
    }

    public BVHBBox BBox
    {
        get { return bbox; }
        set { bbox = value; }
    }
    // ==============================================================================================================

    public static BVHBBox SurroundingBox(BVHBBox box0, BVHBBox box1)
    {
        var bMin = new Vector3(Mathf.Min(box0.min.x, box1.min.x), Mathf.Min(box0.min.y, box1.min.y), Mathf.Min(box0.min.z, box1.min.z));
        var bMax = new Vector3(Mathf.Max(box0.max.x, box1.max.x), Mathf.Max(box0.max.y, box1.max.y), Mathf.Max(box0.max.z, box1.max.z));

        return new BVHBBox(bMin, bMax);
    }

    private static int CompareX(IPrimitive h1, IPrimitive h2)
    {
        if (h1.BBox.min.x - h2.BBox.min.x < 0)
        {
            return -1;
        }
        return 1;
    }

    private int CompareY(IPrimitive h1, IPrimitive h2)
    {
        if (h1.BBox.min.y - h2.BBox.min.y < 0)
        {
            return -1;
        }
        return 1;
    }

    private int CompareZ(IPrimitive h1, IPrimitive h2)
    {
        if (h1.BBox.min.z - h2.BBox.min.z < 0)
        {
            return -1;
        }
        return 1;
    }

    /// <summary>
    /// Get the longest box axis and the float value of the middle of the axis
    /// </summary>
    /// <returns></returns>
    public float[] GetLongestAxisAndValue()
    {
        float xLength = Mathf.Abs(bbox.min.x - bbox.max.x);
        if (xLength < 0.000001f) xLength = 0f;
        float yLength = Mathf.Abs(bbox.min.y - bbox.max.y);
        if (yLength < 0.000001f) yLength = 0f;
        float zLength = Mathf.Abs(bbox.min.z - bbox.max.z);
        if (zLength < 0.000001f) zLength = 0f;
        float[] sides = new float[] { xLength, yLength, zLength };

        float l = Mathf.Max(sides);

        for (int i = 0; i < sides.Length; i++)
        {
            if (l == sides[i])
            {
                // Return a new array with the first element the axis ID 
                // and the second element is the value which represents the half o
                // of the length of the longest axis
                return new float[] { i, bbox.bounds.center[i] };
            }
        }
        Debug.LogError("NOTE:BBox longest side is not calculated properly!");
        return new float[] { 0f, 0f };
    }
}
