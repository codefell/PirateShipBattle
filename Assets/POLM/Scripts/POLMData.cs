using UnityEngine;
using System.Collections;

// NOTE: do not uncomment the line below. ====================================================
//[CreateAssetMenu(fileName = "POLMData", menuName = "Create POLM Data Container", order = 1)]
// ===========================================================================================
public class POLMData : ScriptableObject
{
    public string tDir = "/POLM/SavedAssets/Textures";
    public string mDir = "/POLM/SavedAssets/Meshes";
    public Color textureBGColor = Color.white;
}
