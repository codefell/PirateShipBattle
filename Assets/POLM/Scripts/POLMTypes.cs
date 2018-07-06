using UnityEngine;
using System.Collections;

// ------------------------------------------------------------------------------------------------------------------------------------------------------

public class HitInfo
{
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public float hitDistance;
    public Vector3 barycentricCoordinate;
    public Vector2 textureCoordinates;
    public int triangleIndex;

    public HitInfo()
    {
        hitDistance = float.MaxValue;
    }

    public void Clear()
    {
        hitDistance = float.MaxValue;
    }

    public HitInfo(Vector3 hitPoint,
                    Vector3 hitNormal,
                    float hitDistance,
                    Vector3 barycentricCoordinate,
                    Vector2 textureCoordinates,
                    int triangleIndex)
    {
        this.hitPoint = hitPoint;
        this.hitNormal = hitNormal;
        this.hitDistance = hitDistance;
        this.textureCoordinates = textureCoordinates;
        this.barycentricCoordinate = barycentricCoordinate;
        this.triangleIndex = triangleIndex;
    }
}

[System.Serializable]
public class POLMTriangle
{
    // Variables
    [SerializeField] public Vector3 p0;
    [SerializeField] public Vector3 p1;
    [SerializeField] public Vector3 p2;
    [SerializeField] public Vector3 n;
    [SerializeField] private int trID;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="_p0"></param>
    /// <param name="_p1"></param>
    /// <param name="_p2"></param>
    /// <param name="_trID"></param>
    public POLMTriangle(Vector3 _p0, Vector3 _p1, Vector3 _p2, int _trID)
    {
        p0 = _p0;
        p1 = _p1;
        p2 = _p2;
        n = Vector3.Cross((p1 - p0).normalized, (p2 - p0).normalized).normalized;
        trID = _trID;
    }
    /// <summary>
    /// 
    /// </summary>
    public Vector3[] Vertices
    {
        get
        {
            return new Vector3[] { p0, p1, p2 };
        }
    }

    public static void Barycentric(Vector3 a, Vector3 b, Vector3 c, Vector3 I, out Vector3 barycentric)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = I - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        barycentric.y = (d11 * d20 - d01 * d21) / denom;
        barycentric.z = (d00 * d21 - d01 * d20) / denom;
        barycentric.x = 1.0f - barycentric.z - barycentric.y;
    }

    public static bool TriangleIntersectRay(POLMTriangle tr, Ray ray, float dist)
    {
        Vector3 rayO = ray.origin;
        Vector3 rayD = ray.direction;

        double rOx = rayO.x; double rOy = rayO.y; double rOz = rayO.z;
        double edge0x = tr.p0.x - rOx; double edge0y = tr.p0.y - rOy; double edge0z = tr.p0.z - rOz;
        double edge1x = tr.p1.x - rOx; double edge1y = tr.p1.y - rOy; double edge1z = tr.p1.z - rOz;
        double edge2x = tr.p2.x - rOx; double edge2y = tr.p2.y - rOy; double edge2z = tr.p2.z - rOz;
        double cross11 = edge0y * edge1z - edge0z * edge1y;
        double cross12 = edge0z * edge1x - edge0x * edge1z;
        double cross13 = edge0x * edge1y - edge0y * edge1x;
        double cross21 = edge1y * edge2z - edge1z * edge2y;
        double cross22 = edge1z * edge2x - edge1x * edge2z;
        double cross23 = edge1x * edge2y - edge1y * edge2x;
        double cross31 = edge2y * edge0z - edge2z * edge0y;
        double cross32 = edge2z * edge0x - edge2x * edge0z;
        double cross33 = edge2x * edge0y - edge2y * edge0x;
        double rayDirX = rayD.x; double rayDirY = rayD.y; double rayDirZ = rayD.z;
        double angle1 = rayDirX * cross11 + rayDirY * cross12 + rayDirZ * cross13;
        double angle2 = rayDirX * cross21 + rayDirY * cross22 + rayDirZ * cross23;
        double angle3 = rayDirX * cross31 + rayDirY * cross32 + rayDirZ * cross33;

        if (angle1 <= 0f && angle2 <= 0f && angle3 <= 0f)
        {
            float r, a, b;
            Vector3 w0 = rayO - tr.p0;
            a = -Vector3.Dot(tr.n, w0);
            b = Vector3.Dot(tr.n, rayD);
            r = a / b;
            Vector3 I = rayO + rayD * r;

            if (a < 0f)// if the triangle is not behind us
            {
                if (Vector3.Distance(I, rayO) < dist)
                    return true;
            }
        }
        return false;
    }
    public static bool TriangleIntersectRay(POLMTriangle tr, Ray ray, float dist, ref HitInfo hitInfo)
    {
        Vector3 rayO = ray.origin;
        Vector3 rayD = ray.direction;

        double rOx = rayO.x; double rOy = rayO.y; double rOz = rayO.z;
        double edge0x = tr.p0.x - rOx; double edge0y = tr.p0.y - rOy; double edge0z = tr.p0.z - rOz;
        double edge1x = tr.p1.x - rOx; double edge1y = tr.p1.y - rOy; double edge1z = tr.p1.z - rOz;
        double edge2x = tr.p2.x - rOx; double edge2y = tr.p2.y - rOy; double edge2z = tr.p2.z - rOz;
        double cross11 = edge0y * edge1z - edge0z * edge1y;
        double cross12 = edge0z * edge1x - edge0x * edge1z;
        double cross13 = edge0x * edge1y - edge0y * edge1x;
        double cross21 = edge1y * edge2z - edge1z * edge2y;
        double cross22 = edge1z * edge2x - edge1x * edge2z;
        double cross23 = edge1x * edge2y - edge1y * edge2x;
        double cross31 = edge2y * edge0z - edge2z * edge0y;
        double cross32 = edge2z * edge0x - edge2x * edge0z;
        double cross33 = edge2x * edge0y - edge2y * edge0x;
        double rayDirX = rayD.x; double rayDirY = rayD.y; double rayDirZ = rayD.z;
        double angle1 = rayDirX * cross11 + rayDirY * cross12 + rayDirZ * cross13;
        double angle2 = rayDirX * cross21 + rayDirY * cross22 + rayDirZ * cross23;
        double angle3 = rayDirX * cross31 + rayDirY * cross32 + rayDirZ * cross33;

        if (angle1 < 0f && angle2 < 0f && angle3 < 0f)
        {
            float r, a, b;
            Vector3 w0 = rayO - tr.p0;
            a = -Vector3.Dot(tr.n, w0);
            b = Vector3.Dot(tr.n, rayD);
            r = a / b;
            Vector3 I = rayO + rayD * r;
            if (a < 0f)// if the triangle is not behind us
            {
                float hDist = Vector3.Distance(I, rayO);
                if (hDist < hitInfo.hitDistance)
                {
                    hitInfo.hitPoint = I;
                    hitInfo.hitDistance = hDist;
                    hitInfo.hitNormal = tr.n;
                    hitInfo.triangleIndex = tr.trID;
                    Vector3 barycentric = new Vector3();
                    Barycentric(tr.p0, tr.p1, tr.p2, I, out barycentric);
                    hitInfo.barycentricCoordinate = barycentric;
                    return true;
                }
            }
        }
        //hitPoint = Vector3.zero;
        return false;
    }

    public bool TriangleIntersectRay(Ray ray, float dist, ref HitInfo hitInfo)
    {
        Vector3 rayO = ray.origin;
        Vector3 rayD = ray.direction;

        double rOx = rayO.x; double rOy = rayO.y; double rOz = rayO.z;
        double edge0x = p0.x - rOx; double edge0y = p0.y - rOy; double edge0z = p0.z - rOz;
        double edge1x = p1.x - rOx; double edge1y = p1.y - rOy; double edge1z = p1.z - rOz;
        double edge2x = p2.x - rOx; double edge2y = p2.y - rOy; double edge2z = p2.z - rOz;
        double cross11 = edge0y * edge1z - edge0z * edge1y;
        double cross12 = edge0z * edge1x - edge0x * edge1z;
        double cross13 = edge0x * edge1y - edge0y * edge1x;
        double cross21 = edge1y * edge2z - edge1z * edge2y;
        double cross22 = edge1z * edge2x - edge1x * edge2z;
        double cross23 = edge1x * edge2y - edge1y * edge2x;
        double cross31 = edge2y * edge0z - edge2z * edge0y;
        double cross32 = edge2z * edge0x - edge2x * edge0z;
        double cross33 = edge2x * edge0y - edge2y * edge0x;
        double rayDirX = rayD.x; double rayDirY = rayD.y; double rayDirZ = rayD.z;
        double angle1 = rayDirX * cross11 + rayDirY * cross12 + rayDirZ * cross13;
        double angle2 = rayDirX * cross21 + rayDirY * cross22 + rayDirZ * cross23;
        double angle3 = rayDirX * cross31 + rayDirY * cross32 + rayDirZ * cross33;

        //Vector3 edge0 = p0 - rayO;
        //Vector3 edge1 = p1 - rayO;
        //Vector3 edge2 = p2 - rayO;

        //Vector3 cross0 = Vector3.Cross(edge0, edge1);
        //Vector3 cross1 = Vector3.Cross(edge1, edge2);
        //Vector3 cross2 = Vector3.Cross(edge2, edge0);

        //float angle1 = Vector3.Dot(rayD, cross0);
        //float angle2 = Vector3.Dot(rayD, cross1);
        //float angle3 = Vector3.Dot(rayD, cross2);

        if (angle1 <= 0f && angle2 <= 0f && angle3 <= 0f)
        {
            float r, a, b;
            Vector3 w0 = rayO - p0;
            a = -Vector3.Dot(n, w0);
            b = Vector3.Dot(n, rayD);
            r = a / b;
            Vector3 I = rayO + rayD * r;
            if (a < 0f)// if the triangle is not behind us
            {
                float hDist = Vector3.Distance(I, rayO);

                if (hDist <= dist)
                {
                    hitInfo.hitPoint = I;
                    hitInfo.hitDistance = hDist;
                    hitInfo.hitNormal = n;
                    hitInfo.triangleIndex = trID;
                    Vector3 barycentric = new Vector3();
                    Barycentric(p0, p1, p2, I, out barycentric);
                    hitInfo.barycentricCoordinate = barycentric;

                    //Debug.Log(hDist);
                    //DRCKDSNode.CreateDbgSphere(ray.GetPoint(hitInfo.hitDistance), 0.015f, "hit point");

                    return true;
                }
            }
        }
        return false;
    }
}
