using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class POLMObject : MonoBehaviour
{
    public Mesh o_Mesh;
    public Mesh b_Mesh;
    public bool b_MeshUsed = false;

    Renderer rend;

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        ApplyMesh();
    }

    void OnDestroy()
    {
        Debug.Log("component removed");
    }

    public void Init()
    {
        if (!rend)
            rend = GetComponent<Renderer>();

        if (!o_Mesh)
        {
            if (rend as MeshRenderer)
            {
                o_Mesh = GetComponent<MeshFilter>().sharedMesh;
            }
            else if (rend as SkinnedMeshRenderer)
            {
                o_Mesh = (rend as SkinnedMeshRenderer).sharedMesh;
            }
        }
    }

    public void PreviewBakedMesh()
    {
        ApplyMesh();
        b_MeshUsed = true;
    }

    /// <summary>
    /// 
    /// </summary>
    void ApplyMesh()
    {
        rend = GetComponent<Renderer>();
        if (rend as MeshRenderer)
        {
            if (b_Mesh)
            {
                GetComponent<MeshFilter>().mesh = b_Mesh;
            }
        }
        else if (rend as SkinnedMeshRenderer)
        {
            if (b_Mesh)
            {
                (rend as SkinnedMeshRenderer).sharedMesh = b_Mesh;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void RevertMesh()
    {
        rend = GetComponent<Renderer>();
        if (rend as MeshRenderer)
        {
            GetComponent<MeshFilter>().mesh = o_Mesh;
        }
        else if (rend as SkinnedMeshRenderer)
        {
            (rend as SkinnedMeshRenderer).sharedMesh = o_Mesh;
        }
    }

    /// <summary>
    /// Toggle baked mesh preview
    /// </summary>
    public void ToggleBakedMeshPreview()
    {
        if (!b_MeshUsed)
        {
            b_MeshUsed = true;
            ApplyMesh();
        }
        else
        {
            b_MeshUsed = false;
            RevertMesh();
        }
    }

#if UNITY_EDITOR

    /// <summary>
    /// Called from the editor script when the RemoveComponent button is pressed
    /// </summary>
    public void ApplyMeshAndRemoveComponent()
    {
        ApplyMesh();
    }

    /// <summary>
    /// Called from the editor script when the RemoveComponent button is pressed
    /// </summary>
    public void RemoveComponent()
    {
        RevertMesh();
    }

#endif

    /// <summary>
    /// Get/Set the baked mesh
    /// </summary>
    public Mesh BMesh
    {
        get { return b_Mesh; }
        set { b_Mesh = value; }
    }
}
