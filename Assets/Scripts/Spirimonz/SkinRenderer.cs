using UnityEngine;

[DisallowMultipleComponent]
public class SkinRenderer : MonoBehaviour
{
    public Renderer targetRenderer;
    public string materialId;

    private Material[] _baseMaterials;
    private bool _cached;

    private void Awake()
    {
        CacheBaseMaterials();
    }

    public void ApplySkin(Material skinMat, bool useSkin)
    {
        CacheBaseMaterials();
        if (targetRenderer == null || _baseMaterials == null || _baseMaterials.Length == 0)
            return;

        bool replaceAllMaterials = string.IsNullOrWhiteSpace(materialId);
        bool hasMaterialIndexOverride = int.TryParse(materialId, out int materialIndex);
        string[] materialIds = hasMaterialIndexOverride || replaceAllMaterials
            ? null
            : SplitMaterialIds(materialId);

        Material[] appliedMaterials = new Material[_baseMaterials.Length];
        for (int i = 0; i < _baseMaterials.Length; i++)
        {
            Material baseMaterial = _baseMaterials[i];
            appliedMaterials[i] = baseMaterial;

            if (!useSkin || skinMat == null || baseMaterial == null)
                continue;

            bool shouldReplace = replaceAllMaterials ||
                                 (hasMaterialIndexOverride
                                     ? i == materialIndex
                                     : MatchesMaterialId(baseMaterial.name, materialIds));

            if (shouldReplace)
                appliedMaterials[i] = skinMat;
        }

        targetRenderer.sharedMaterials = appliedMaterials;
    }

    public static void ApplySkin(GameObject root, Material skinMat, bool useSkin)
    {
        if (root == null)
            return;

        SkinRenderer[] renderers = root.GetComponentsInChildren<SkinRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].ApplySkin(skinMat, useSkin);
    }

    private void CacheBaseMaterials()
    {
        if (_cached)
            return;

        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
            _baseMaterials = targetRenderer.sharedMaterials;

        _cached = true;
    }

    private static string NormalizeMaterialId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Replace(" (Instance)", string.Empty).Trim();
    }

    private static string[] SplitMaterialIds(string value)
    {
        return NormalizeMaterialId(value)
            .Split(new[] { ',', ';', '|' }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool MatchesMaterialId(string materialName, string[] materialIds)
    {
        if (materialIds == null || materialIds.Length == 0)
            return false;

        string normalizedName = NormalizeMaterialId(materialName);
        for (int i = 0; i < materialIds.Length; i++)
        {
            if (string.Equals(normalizedName, NormalizeMaterialId(materialIds[i]), System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
