using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ------------------------------------------------------------------------------------------------------------------------------------------------------

[System.Serializable]
public class BVHBBox
{
    public Vector3 min = Vector3.one * float.MaxValue;
    public Vector3 max = Vector3.one * -float.MaxValue;
    public Bounds bounds;

    // add a point to the bounding box, possibly expanding it. If the point is inside the current box, nothing happens.
    // if it is outside, the box grows just enough so that in encompasses the new point.
    public BVHBBox()
    {
        min = Vector3.one *  float.MaxValue;
        max = Vector3.one * -float.MaxValue;
    }

    /// <summary>
    /// CONSTRUCTOR WITH INITIALIZATION
    /// </summary>
    /// <param name="_min"></param>
    /// <param name="_max"></param>
    public BVHBBox(Vector3 _min, Vector3 _max)
    {
        min = _min;
        max = _max;
        bounds = new Bounds((_min + _max) * 0.5f,
                            new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f));
        bounds.min = _min;
        bounds.max = _max;
    }

    /// <summary>
    /// 
    /// </summary>
    public Vector3[] Vertices
    {
        get
        {
            return new Vector3[] { min, max };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tris"></param>
    public void CalculateBBox(List<POLMTriangle> tris) // original name Add
    {
        foreach (POLMTriangle tr in tris)
        {
            min = new Vector3(Mathf.Min(min.x, tr.p0.x), Mathf.Min(min.y, tr.p0.y), Mathf.Min(min.z, tr.p0.z));
            max = new Vector3(Mathf.Max(max.x, tr.p0.x), Mathf.Max(max.y, tr.p0.y), Mathf.Max(max.z, tr.p0.z));

            min = new Vector3(Mathf.Min(min.x, tr.p1.x), Mathf.Min(min.y, tr.p1.y), Mathf.Min(min.z, tr.p1.z));
            max = new Vector3(Mathf.Max(max.x, tr.p1.x), Mathf.Max(max.y, tr.p1.y), Mathf.Max(max.z, tr.p1.z));

            min = new Vector3(Mathf.Min(min.x, tr.p2.x), Mathf.Min(min.y, tr.p2.y), Mathf.Min(min.z, tr.p2.z));
            max = new Vector3(Mathf.Max(max.x, tr.p2.x), Mathf.Max(max.y, tr.p2.y), Mathf.Max(max.z, tr.p2.z));
        }

        //Vector3 center = (min + max) * 0.5f;
        //Vector3 size = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f);
        //bounds = new Bounds(center, size);
        //bounds.min = min;
        //bounds.max = max;

        bounds = new Bounds(); bounds.min = min; bounds.max = max;
        //Vector3 center = bounds.center;
        //Vector3 size = bounds.size;
    }

    /// <summary>
    /// Checks if a point is inside the bounding box (borders-inclusive)
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
	bool Inside(Vector3 v)
    {
        return bounds.Contains(v);
    }

    /// <summary>
    /// Calculate the BBox longest axis
    /// </summary>
    /// <returns></returns>
    public int BBoxLongestAxis()
    {
        float xLength = Mathf.Abs(bounds.min.x - bounds.max.x);
        if (xLength < 0.000001f) xLength = 0f;
        float yLength = Mathf.Abs(bounds.min.y - bounds.max.y);
        if (yLength < 0.000001f) yLength = 0f;
        float zLength = Mathf.Abs(bounds.min.z - bounds.max.z);
        if (zLength < 0.000001f) zLength = 0f;
        float[] sides = new float[] { xLength, yLength, zLength };

        float l = Mathf.Max(sides);

        for (int i = 0; i < sides.Length; i++)
        {
            if (l == sides[i])
            {
                return i;
            }
        }
        Debug.LogError("NOTE:BBox longest side is not calculated properly!");
        return 0;
    }

    /// <summary>
    /// Get the longest box axis and the float value of the middle of the axis
    /// </summary>
    /// <returns></returns>
    public float[] GetLongestAxisAndValue()
    {
        float xLength = Mathf.Abs(bounds.min.x - bounds.max.x);
        if (xLength < 0.000001f) xLength = 0f;
        float yLength = Mathf.Abs(bounds.min.y - bounds.max.y);
        if (yLength < 0.000001f) yLength = 0f;
        float zLength = Mathf.Abs(bounds.min.z - bounds.max.z);
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
                return new float[] { i, bounds.center[i] };
            }
        }
        Debug.LogError("NOTE:BBox longest side is not calculated properly!");
        return new float[] { 0f, 0f };
    }

    public bool IntersectRay(Ray r, out float hitDist)
    {
        float t_min, t_max, t_xmin, t_xmax, t_ymin, t_ymax, t_zmin, t_zmax;
        float x_a = 1f / r.direction.x, y_a = 1f / r.direction.y, z_a = 1f / r.direction.z;
        float x_e = r.origin.x, y_e = r.origin.y, z_e = r.origin.z;

        // calculate t interval in x-axis
        if (x_a >= 0)
        {
            t_xmin = (bounds.min.x - x_e) * x_a;
            t_xmax = (bounds.max.x - x_e) * x_a;
        }
        else
        {
            t_xmin = (bounds.max.x - x_e) * x_a;
            t_xmax = (bounds.min.x - x_e) * x_a;
        }

        // calculate t interval in y-axis
        if (y_a >= 0)
        {
            t_ymin = (bounds.min.y - y_e) * y_a;
            t_ymax = (bounds.max.y - y_e) * y_a;
        }
        else
        {
            t_ymin = (bounds.max.y - y_e) * y_a;
            t_ymax = (bounds.min.y - y_e) * y_a;
        }

        // calculate t interval in z-axis
        if (z_a >= 0)
        {
            t_zmin = (bounds.min.z - z_e) * z_a;
            t_zmax = (bounds.max.z - z_e) * z_a;
        }
        else
        {
            t_zmin = (bounds.max.z - z_e) * z_a;
            t_zmax = (bounds.min.z - z_e) * z_a;
        }

        // find if there an intersection among three t intervals
        t_min = Mathf.Max(t_xmin, Mathf.Max(t_ymin, t_zmin));
        t_max = Mathf.Min(t_xmax, Mathf.Min(t_ymax, t_zmax));

        hitDist = t_min;
        return (t_min <= t_max);
    }
    public bool IntersectRay(Ray r, ref HitInfo hitInfo)
    {
        float t_min, t_max, t_xmin, t_xmax, t_ymin, t_ymax, t_zmin, t_zmax;
        float x_a = 1f / r.direction.x, y_a = 1f / r.direction.y, z_a = 1f / r.direction.z;
        float x_e = r.origin.x, y_e = r.origin.y, z_e = r.origin.z;

        // calculate t interval in x-axis
        if (x_a >= 0)
        {
            t_xmin = (bounds.min.x - x_e) * x_a;
            t_xmax = (bounds.max.x - x_e) * x_a;
        }
        else
        {
            t_xmin = (bounds.max.x - x_e) * x_a;
            t_xmax = (bounds.min.x - x_e) * x_a;
        }

        // calculate t interval in y-axis
        if (y_a >= 0)
        {
            t_ymin = (bounds.min.y - y_e) * y_a;
            t_ymax = (bounds.max.y - y_e) * y_a;
        }
        else
        {
            t_ymin = (bounds.max.y - y_e) * y_a;
            t_ymax = (bounds.min.y - y_e) * y_a;
        }

        // calculate t interval in z-axis
        if (z_a >= 0)
        {
            t_zmin = (bounds.min.z - z_e) * z_a;
            t_zmax = (bounds.max.z - z_e) * z_a;
        }
        else
        {
            t_zmin = (bounds.max.z - z_e) * z_a;
            t_zmax = (bounds.min.z - z_e) * z_a;
        }

        // find if there an intersection among three t intervals
        t_min = Mathf.Max(t_xmin, Mathf.Max(t_ymin, t_zmin));
        t_max = Mathf.Min(t_xmax, Mathf.Min(t_ymax, t_zmax));

        hitInfo.hitDistance = t_min;
        return (t_min <= t_max);
    }
    public bool IntersectRay(Ray r, out float[] tminmax)
    {
        float t_min, t_max, t_xmin, t_xmax, t_ymin, t_ymax, t_zmin, t_zmax;
        float x_a = 1f / r.direction.x, y_a = 1f / r.direction.y, z_a = 1f / r.direction.z;
        float x_e = r.origin.x, y_e = r.origin.y, z_e = r.origin.z;

        // calculate t interval in x-axis
        if (x_a >= 0)
        {
            t_xmin = (bounds.min.x - x_e) * x_a;
            t_xmax = (bounds.max.x - x_e) * x_a;
        }
        else
        {
            t_xmin = (bounds.max.x - x_e) * x_a;
            t_xmax = (bounds.min.x - x_e) * x_a;
        }

        // calculate t interval in y-axis
        if (y_a >= 0)
        {
            t_ymin = (bounds.min.y - y_e) * y_a;
            t_ymax = (bounds.max.y - y_e) * y_a;
        }
        else
        {
            t_ymin = (bounds.max.y - y_e) * y_a;
            t_ymax = (bounds.min.y - y_e) * y_a;
        }

        // calculate t interval in z-axis
        if (z_a >= 0)
        {
            t_zmin = (bounds.min.z - z_e) * z_a;
            t_zmax = (bounds.max.z - z_e) * z_a;
        }
        else
        {
            t_zmin = (bounds.max.z - z_e) * z_a;
            t_zmax = (bounds.min.z - z_e) * z_a;
        }

        // find if there an intersection among three t intervals
        t_min = Mathf.Max(t_xmin, Mathf.Max(t_ymin, t_zmin));
        t_max = Mathf.Min(t_xmax, Mathf.Min(t_ymax, t_zmax));

        tminmax = new float[2] { t_min, t_max };
        return (t_min <= t_max);
    }
    public bool IntersectRay(Ray r, float tmin, float tmax)
    {
        float t_min, t_max, t_xmin, t_xmax, t_ymin, t_ymax, t_zmin, t_zmax;
        float x_a = 1f / r.direction.x, y_a = 1f / r.direction.y, z_a = 1f / r.direction.z;
        float x_e = r.origin.x, y_e = r.origin.y, z_e = r.origin.z;

        // calculate t interval in x-axis
        if (x_a >= 0)
        {
            t_xmin = (bounds.min.x - x_e) * x_a;
            t_xmax = (bounds.max.x - x_e) * x_a;
        }
        else
        {
            t_xmin = (bounds.max.x - x_e) * x_a;
            t_xmax = (bounds.min.x - x_e) * x_a;
        }

        // calculate t interval in y-axis
        if (y_a >= 0)
        {
            t_ymin = (bounds.min.y - y_e) * y_a;
            t_ymax = (bounds.max.y - y_e) * y_a;
        }
        else
        {
            t_ymin = (bounds.max.y - y_e) * y_a;
            t_ymax = (bounds.min.y - y_e) * y_a;
        }

        // calculate t interval in z-axis
        if (z_a >= 0)
        {
            t_zmin = (bounds.min.z - z_e) * z_a;
            t_zmax = (bounds.max.z - z_e) * z_a;
        }
        else
        {
            t_zmin = (bounds.max.z - z_e) * z_a;
            t_zmax = (bounds.min.z - z_e) * z_a;
        }

        // find if there an intersection among three t intervals
        t_min = Mathf.Max(t_xmin, Mathf.Max(t_ymin, t_zmin));
        t_max = Mathf.Min(t_xmax, Mathf.Min(t_ymax, t_zmax));

        tmin = t_min; tmax = t_max;

        return (t_min <= t_max);
    }
    public bool IntersectRay(Ray r, float _mitDist, float _maxDist, out float _dist)
    {
        float t_min, t_max, t_xmin, t_xmax, t_ymin, t_ymax, t_zmin, t_zmax;
        float x_a = 1f / r.direction.x, y_a = 1f / r.direction.y, z_a = 1f / r.direction.z;
        float x_e = r.origin.x, y_e = r.origin.y, z_e = r.origin.z;

        // calculate t interval in x-axis
        if (x_a >= 0)
        {
            t_xmin = (bounds.min.x - x_e) * x_a;
            t_xmax = (bounds.max.x - x_e) * x_a;
        }
        else
        {
            t_xmin = (bounds.max.x - x_e) * x_a;
            t_xmax = (bounds.min.x - x_e) * x_a;
        }

        // calculate t interval in y-axis
        if (y_a >= 0)
        {
            t_ymin = (bounds.min.y - y_e) * y_a;
            t_ymax = (bounds.max.y - y_e) * y_a;
        }
        else
        {
            t_ymin = (bounds.max.y - y_e) * y_a;
            t_ymax = (bounds.min.y - y_e) * y_a;
        }

        // calculate t interval in z-axis
        if (z_a >= 0)
        {
            t_zmin = (bounds.min.z - z_e) * z_a;
            t_zmax = (bounds.max.z - z_e) * z_a;
        }
        else
        {
            t_zmin = (bounds.max.z - z_e) * z_a;
            t_zmax = (bounds.min.z - z_e) * z_a;
        }

        // find if there an intersection among three t intervals
        t_min = Mathf.Max(t_xmin, Mathf.Max(t_ymin, t_zmin));
        t_max = Mathf.Min(t_xmax, Mathf.Min(t_ymax, t_zmax));
        _dist = t_min;
        return (t_min <= t_max && (t_min < _maxDist && t_min > _mitDist));
    }
    public bool BBoxIntersectRay(Ray ray)
    {
        return bounds.IntersectRay(ray);
    }
    public bool BBoxIntersectRay(Ray ray, float distance)
    {
        return bounds.IntersectRay(ray, out distance);
    }
    public bool BBoxIntersectRay(Ray ray, float distance, Vector3[] tMinMax)
    {
        if (tMinMax == null)
            tMinMax = new Vector3[2];

        return bounds.IntersectRay(ray, out distance);
    }
    public bool BBoxIntersectRay(Ray ray, float distance, ref HitInfo hitInfo, float[] tMinMax)
    {
        if (tMinMax == null)
            tMinMax = new float[2];

        //-------------------------------------------------------------------------------------------
        Vector3 dirFrac = new Vector3(1.0f / ray.direction.x, 1.0f / ray.direction.y, 1.0f / ray.direction.z);
        float t1 = (min.x - ray.origin.x) * dirFrac.x;
        float t2 = (max.x - ray.origin.x) * dirFrac.x;
        float t3 = (min.y - ray.origin.y) * dirFrac.y;
        float t4 = (max.y - ray.origin.y) * dirFrac.y;
        float t5 = (min.z - ray.origin.z) * dirFrac.z;
        float t6 = (max.z - ray.origin.z) * dirFrac.z;
        float tmin = Mathf.Max(Mathf.Max(Mathf.Min(t1, t2), Mathf.Min(t3, t4)), Mathf.Min(t5, t6));
        float tmax = Mathf.Min(Mathf.Min(Mathf.Max(t1, t2), Mathf.Max(t3, t4)), Mathf.Max(t5, t6));

        // Finding the normal vector;
        //Vector3 normal = new Vector3();
        //if (tmin == t1) normal = Vector3.left;
        //else if (tmin == t2) normal = Vector3.right;
        //else if (tmin == t3) normal = Vector3.down;
        //else if (tmin == t4) normal = Vector3.up;
        //else if (tmin == t5) normal = Vector3.back;
        //else if (tmin == t6) normal = Vector3.forward;

        Debug.Log("Tmin is: " + tmin);

        if (tmax < 0) return false;
        if (tmin > tmax) return false;

        Vector3 hitPoint = ray.origin + ray.direction * tmin;
        float hitDist = Vector3.Distance(ray.origin, hitPoint);

        tMinMax[0] = tmin;
        tMinMax[1] = tmax;

        if (hitDist <= distance)
        {
            return true;
        }
        return false;
    }
    public bool Get_T_MinMax(BVHBBox b, Ray r, out float[] tMinMax)
    {
        tMinMax = new float[2] { float.MaxValue, float.MaxValue };

        float tmin = (b.min.x - r.origin.x) / r.direction.x;
        float tmax = (b.max.x - r.origin.x) / r.direction.x;

        if (tmin > tmax) swap(ref tmin, ref tmax);

        float tymin = (b.min.y - r.origin.y) / r.direction.y;
        float tymax = (b.max.y - r.origin.y) / r.direction.y;

        if (tymin > tymax) swap(ref tymin, ref tymax);

        if ((tmin > tymax) || (tymin > tmax))
            return false;

        if (tymin > tmin)
            tmin = tymin;

        if (tymax < tmax)
            tmax = tymax;

        float tzmin = (b.min.z - r.origin.z) / r.direction.z;
        float tzmax = (b.max.z - r.origin.z) / r.direction.z;

        if (tzmin > tzmax) swap(ref tzmin, ref tzmax);

        if ((tmin > tzmax) || (tzmin > tmax))
            return false;

        if (tzmin > tmin)
            tmin = tzmin;

        if (tzmax < tmax)
            tmax = tzmax;

        tMinMax[0] = tmin; tMinMax[1] = tmax;

        return true;
    }
    public bool Get_T_MinMax(Ray ray, out float[] tMinMax)
    {
        //-------------------------------------------------------------------------------------------
        Vector3 dirFrac = new Vector3(1.0f / ray.direction.x, 1.0f / ray.direction.y, 1.0f / ray.direction.z);
        float t1 = (min.x - ray.origin.x) * dirFrac.x;
        float t2 = (max.x - ray.origin.x) * dirFrac.x;
        float t3 = (min.y - ray.origin.y) * dirFrac.y;
        float t4 = (max.y - ray.origin.y) * dirFrac.y;
        float t5 = (min.z - ray.origin.z) * dirFrac.z;
        float t6 = (max.z - ray.origin.z) * dirFrac.z;
        float tmin = Mathf.Max(Mathf.Max(Mathf.Min(t1, t2), Mathf.Min(t3, t4)), Mathf.Min(t5, t6));
        float tmax = Mathf.Min(Mathf.Min(Mathf.Max(t1, t2), Mathf.Max(t3, t4)), Mathf.Max(t5, t6));

        tMinMax = new float[2] { tmin, tmax };

        if (tmax < 0f)
        {
            return false;
        }
        if (tmin > tmax)
        {
            return false;
        }
        return true;
    }
    public bool Get_T_MinMax(BVHBBox bounds, Ray ray)
    {
        //Vector3[] _bounds = new Vector3[2] { min, max };
        //Vector3 invDir = new Vector3 (1f / ray.direction.x, 1 / ray.direction.y, 1f / ray.direction.z);
        //int[] dirIsNeg = new int[3] { invDir.x < 0f ? 1 : 0, invDir.y < 0f ? 1 : 0, invDir.z < 0f ? 1 : 0 };
        //// Check for ray intersection against $x$ and $y$ slabs
        //float tmin  = (_bounds[    dirIsNeg[0]].x - ray.origin.x) * invDir.x;
        //float tmax  = (_bounds[1 - dirIsNeg[0]].x - ray.origin.x) * invDir.x;
        //float tymin = (_bounds[    dirIsNeg[1]].y - ray.origin.y) * invDir.y;
        //float tymax = (_bounds[1 - dirIsNeg[1]].y - ray.origin.y) * invDir.y;

        //if ((tmin > tymax) || (tymin > tmax))
        //    return false;
        //if (tymin > tmin) tmin = tymin;
        //if (tymax < tmax) tmax = tymax;

        //// Check for ray intersection against $z$ slab
        //float tzmin = (_bounds[    dirIsNeg[2]].z - ray.origin.z) * invDir.z;
        //float tzmax = (_bounds[1 - dirIsNeg[2]].z - ray.origin.z) * invDir.z;
        //if ((tmin > tzmax) || (tzmin > tmax))
        //    return false;

        //if (tzmin > tmin)
        //    tmin = tzmin;
        //if (tzmax < tmax)
        //    tmax = tzmax;

        //return (tmin < ray.maxt) && (tmax > ray.mint);
        return false;
    }

    public void swap(ref float tmin, ref float tmax)
    {
        float tmp = tmin;
        tmin = tmax;
        tmax = tmp;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float VectorLength(Vector3 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }

    /// <summary>
    /// Check whether the box intersects a triangle (all three cases)
    /// </summary>
    /// <param name="triangle"></param>
    /// <returns></returns>
    public bool BBoxIntersectTriangle(POLMTriangle triangle)
    {
        // Case 1a
        if (bounds.Contains(triangle.p0))
            return true;
        if (bounds.Contains(triangle.p1))
            return true;
        if (bounds.Contains(triangle.p2))
            return true;

        Ray ray;
        //Case 2
        float dist = float.MaxValue;
        Vector3 ray1Dir = triangle.p1 - triangle.p0;
        ray = new Ray(triangle.p0, ray1Dir.normalized);
        if (bounds.IntersectRay(ray, out dist))
            if (dist <= ray1Dir.magnitude)
                return true;

        dist = float.MaxValue;
        Vector3 ray2Dir = triangle.p2 - triangle.p1;
        ray = new Ray(triangle.p1, ray2Dir.normalized);
        if (bounds.IntersectRay(ray, out dist))
            if (dist <= ray2Dir.magnitude)
                return true;

        dist = float.MaxValue;
        Vector3 ray3Dir = triangle.p0 - triangle.p2;
        ray = new Ray(triangle.p2, ray3Dir.normalized);
        if (bounds.IntersectRay(ray, out dist))
            if (dist <= ray3Dir.magnitude)
                return true;

        // CASE 3 - The box itself intersects the triangle(i.e.some of the box edges intersects the triangle)
        Vector3 trEdgeOne = triangle.p1 - triangle.p0;
        Vector3 trEdgeTwo = triangle.p2 - triangle.p0;
        Vector3 trNormal = Vector3.Cross(trEdgeOne, trEdgeTwo);
        float D = Vector3.Dot(triangle.p0, trNormal);
        for (int mask = 0; mask < 7; mask++)
        {
            for (int j = 0; j < 3; j++)
            {
                if ((mask & (1 << j)) > 0) continue;
                Vector3 rayStart = new Vector3((mask & 1) == 1 ? max.x : min.x, (mask & 2) == 2 ? max.y : min.y, (mask & 4) == 4 ? max.z : min.z);
                Vector3 rayEnd = rayStart;
                rayEnd[j] = max[j];
                if (Mathf.Sign(Vector3.Dot(rayStart, trNormal) - D) != Mathf.Sign(Vector3.Dot(rayEnd, trNormal) - D))
                {
                    Vector3 rayDir = rayEnd - rayStart;
                    ray = new Ray(rayStart, rayDir.normalized);
                    if (POLMTriangle.TriangleIntersectRay(triangle, ray, rayDir.magnitude))
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="points"></param>
    /// <param name="axis"></param>
    /// <param name="_min_"></param>
    /// <param name="_max_"></param>
    void Project(Vector3[] points, Vector3 axis, out float _min_, out float _max_)
    {
        _min_ = float.PositiveInfinity;
        _max_ = float.NegativeInfinity;
        foreach (var p in points)
        {
            float val = Vector3.Dot(axis, p);
            if (val < _min_) _min_ = val;
            if (val > _max_) _max_ = val;
        }
    }

    // DEBUGGING ------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bboxName"></param>
#if UNITY_EDITOR
    public void CreateBBoxGizmo(string bboxName)
    {
        //Debug.Log("create bbox gizmo");

        Vector3 p0 = min;
        Vector3 p1 = new Vector3(max.x, min.y, min.z);
        Vector3 p2 = new Vector3(max.x, min.y, max.z);
        Vector3 p3 = new Vector3(min.x, min.y, max.z);
        Vector3 p4 = new Vector3(min.x, max.y, min.z);
        Vector3 p5 = new Vector3(max.x, max.y, min.z);
        Vector3 p6 = max;
        Vector3 p7 = new Vector3(min.x, max.y, max.z);
        Vector3[] verts = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };
        int[] tris = new int[] { 0, 1, 2, 2, 3, 0, 0, 3, 7, 7, 4, 0, 4, 7, 5, 5, 7, 6, 5, 6, 2, 2, 1, 5, 0, 4, 5, 5, 1, 0, 6, 7, 3, 3, 2, 6 };
        Mesh newMesh = new Mesh();
        newMesh.Clear();
        newMesh.vertices = verts;
        newMesh.triangles = tris;

        Vector2[] newUV = new Vector2[newMesh.vertices.Length];
        for (int v = 0; v < newMesh.uv.Length; v++)
        {
            newMesh.uv[v] = new Vector2((float)Random.value, (float)Random.value);
        }
        for (int v = 0; v < newMesh.uv.Length; v++)
        {
            newMesh.uv[v] = new Vector2((float)Random.value, (float)Random.value);
        }
        newMesh.uv = newUV;
        GameObject newBox = new GameObject();
        newBox.name = "bbox_" + bboxName;
        newBox.AddComponent<MeshFilter>().mesh = newMesh;
        newBox.AddComponent<MeshRenderer>();
        Material newMat = new Material(Shader.Find("Transparent/Diffuse"));
        newMat.color = new Color(Random.value, Random.value, Random.value, 0.15f);
        newBox.GetComponent<MeshRenderer>().material = newMat;
        //newBox.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
    }
#endif
    // ----------------------------------------------------------------------------------------------------------------------------------------------------
}
