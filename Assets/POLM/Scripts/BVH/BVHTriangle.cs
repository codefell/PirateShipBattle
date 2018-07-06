using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BVHTriangle : IPrimitive
{
    public BVHBBox bbox;
    public Vector3 v0, v1, v2, n;
    public int id;// this is triangleID

    public int primitivesCount = 0; // this is the position id in the list of objects when flatten down
    public int nodeID   = 0;
    public int parentID = 0;
    public int lChildID = 0;
    public int rChildID = 0;
    public bool isLeaf  = false;

    public Vector3 centroid = new Vector3();

    public BVHBBox BBox
    {
        get { return bbox; }
        set { bbox = value; }
    }

    public BVHTriangle( Vector3 _v0, Vector3 _v1, Vector3 _v2, int _id)
    {
        v0 = _v0; v1 = _v1; v2 = _v2;
        n = Vector3.Cross((v1 - v0).normalized, (v2 - v0).normalized).normalized;
        id = _id;
        CalculateBBox();
    }

    public int PrimitivesCount { get { return primitivesCount; } set { primitivesCount = value; } }
    public int NodeID   { get { return   nodeID; } set { nodeID   = value; } }
    public int ParentID { get { return parentID; } set { parentID = value; } }
    public int LChildID { get { return lChildID; } set { lChildID = value; } }
    public int RChildID { get { return rChildID; } set { rChildID = value; } }
    public bool IsLeaf  { get { return   isLeaf; } set { isLeaf   = value; } }

    public void CalculateBBox()
    {
        bbox = new BVHBBox();
        bbox.min = new Vector3(Mathf.Min(bbox.min.x, v0.x), Mathf.Min(bbox.min.y, v0.y), Mathf.Min(bbox.min.z, v0.z));
        bbox.max = new Vector3(Mathf.Max(bbox.max.x, v0.x), Mathf.Max(bbox.max.y, v0.y), Mathf.Max(bbox.max.z, v0.z));

        bbox.min = new Vector3(Mathf.Min(bbox.min.x, v1.x), Mathf.Min(bbox.min.y, v1.y), Mathf.Min(bbox.min.z, v1.z));
        bbox.max = new Vector3(Mathf.Max(bbox.max.x, v1.x), Mathf.Max(bbox.max.y, v1.y), Mathf.Max(bbox.max.z, v1.z));
                                                                                                               
        bbox.min = new Vector3(Mathf.Min(bbox.min.x, v2.x), Mathf.Min(bbox.min.y, v2.y), Mathf.Min(bbox.min.z, v2.z));
        bbox.max = new Vector3(Mathf.Max(bbox.max.x, v2.x), Mathf.Max(bbox.max.y, v2.y), Mathf.Max(bbox.max.z, v2.z));

        Vector3 center = (bbox.min + bbox.max) * 0.5f;
        Vector3 size = new Vector3((bbox.min.x + bbox.max.x) * 0.5f, (bbox.min.y + bbox.max.y) * 0.5f, (bbox.min.z + bbox.max.z) * 0.5f);
        bbox.bounds = new Bounds(center, size);
        bbox.bounds.min = bbox.min;
        bbox.bounds.max = bbox.max;
    }

    // used with 2D BVH trees - like UVs
    public bool IntersectRay2D(Ray ray, float tmin, float tmax, ref HitInfo hitInfo)
    {
        hitInfo = new HitInfo();
        hitInfo.hitPoint = ray.origin;
        hitInfo.hitDistance = Vector3.Distance(ray.origin, ray.origin);
        hitInfo.hitNormal = n;
        hitInfo.triangleIndex = 0;
        hitInfo.barycentricCoordinate = Barycentric(v0, v1, v2, ray.origin);
        hitInfo.triangleIndex = id;

        if (hitInfo.barycentricCoordinate.x < 0f) return false;
        if (hitInfo.barycentricCoordinate.y < 0f) return false;
        if (hitInfo.barycentricCoordinate.z < 0f) return false;

        return true;
    }

    public bool IntersectRay(Ray ray, float tmin, float tmax, ref HitInfo hitInfo)
    {
        hitInfo = new HitInfo();

        Vector3 rayO = ray.origin, rayD = ray.direction;

        Vector3 o = ray.origin; o.z = 0f;
        // This is needed in case a uv triangle is flipped
        //Debug.DrawRay(o, n * 0.1f, Color.green, 10f);

        //Vector3 edge0 = v0 - rayO;
        //Vector3 edge1 = v1 - rayO;
        //Vector3 edge2 = v2 - rayO;
        //Vector3 cross0 = Vector3.Cross(edge0, edge1);
        //Vector3 cross1 = Vector3.Cross(edge1, edge2);
        //Vector3 cross2 = Vector3.Cross(edge2, edge0);
        //float angle1 = Vector3.Dot(rayD, cross0);
        //float angle2 = Vector3.Dot(rayD, cross1);
        //float angle3 = Vector3.Dot(rayD, cross2);

        double edge0x = v0.x - rayO.x, edge0y = v0.y - rayO.y, edge0z = v0.z - rayO.z;
        double edge1x = v1.x - rayO.x, edge1y = v1.y - rayO.y, edge1z = v1.z - rayO.z;
        double edge2x = v2.x - rayO.x, edge2y = v2.y - rayO.y, edge2z = v2.z - rayO.z;
        double cross11 = edge0y * edge1z - edge0z * edge1y;
        double cross12 = edge0z * edge1x - edge0x * edge1z;
        double cross13 = edge0x * edge1y - edge0y * edge1x;
        double cross21 = edge1y * edge2z - edge1z * edge2y;
        double cross22 = edge1z * edge2x - edge1x * edge2z;
        double cross23 = edge1x * edge2y - edge1y * edge2x;
        double cross31 = edge2y * edge0z - edge2z * edge0y;
        double cross32 = edge2z * edge0x - edge2x * edge0z;
        double cross33 = edge2x * edge0y - edge2y * edge0x;
        double angle1 = rayD.x * cross11 + rayD.y * cross12 + rayD.z * cross13;
        double angle2 = rayD.x * cross21 + rayD.y * cross22 + rayD.z * cross23;
        double angle3 = rayD.x * cross31 + rayD.y * cross32 + rayD.z * cross33;

        if (angle1 < 0f && angle2 < 0f && angle3 < 0f)
        {
            float r, a, b;
            Vector3 w0 = rayO - v0;
            a = -Vector3.Dot(n, w0);
            b =  Vector3.Dot(n, rayD);
            r = a / b;
            Vector3 I = rayO + rayD * r;
            if (a < 0f)// if the triangle is not behind us
            {
                hitInfo.hitPoint = I;
                hitInfo.hitDistance = Vector3.Distance(rayO, I);
                hitInfo.hitNormal = n;
                hitInfo.triangleIndex = 0;
                hitInfo.barycentricCoordinate = Barycentric(v0, v1, v2, I);
                hitInfo.triangleIndex = id;
                return true;
            }
        }
        return false;
    }

    public static Vector3 Barycentric(Vector3 a, Vector3 b, Vector3 c, Vector3 I)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = I - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        Vector3 bary = Vector3.zero;
        bary.y = (d11 * d20 - d01 * d21) / denom;
        bary.z = (d00 * d21 - d01 * d20) / denom;
        bary.x = 1.0f - bary.z - bary.y;
        return bary;
    }

    /// <summary>
    /// NOTE : might be possible to remove the bbox testing and doing triangle testing directly
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="t0"></param>
    /// <param name="t1"></param>
    /// <param name="hitInfo"></param>
    /// <returns></returns>
    public bool IntersectBBox(Ray ray, float t0, float t1, ref HitInfo hitInfo)
    {
        //float hDist = float.MaxValue;
        //if (bbox.IntersectRay(ray, t0, t1, out hDist))
        //{
        //    if (hDist <= t1)
        //    {
                if (!IntersectRay(ray, t0, t1, ref hitInfo))
                    return false;

                return true;
        //    }
        //    return false;
        //}
        //return false;
    }
}