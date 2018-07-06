using UnityEngine;
using System.Collections;

public interface IPrimitive
{
    BVHBBox BBox { get; set; }
    void CalculateBBox();

    int PrimitivesCount { get; set; }

    int NodeID   { get; set; }
    int ParentID { get; set; }
    int LChildID { get; set; }
    int RChildID { get; set; }
    bool IsLeaf  { get; set; }

    bool IntersectRay(Ray ray, float tmin, float tmax, ref HitInfo hitInfo);
    bool IntersectBBox(Ray ray, float t0, float t1, ref HitInfo hitInfo);
}
