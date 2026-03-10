using UnityEngine;

public class ColoredMat : MonoBehaviour
{
    public Renderer targetRenderer;

    [Header("Colors")]
    public Color shirtColor = Color.white;
    public Color logoColor = Color.white;

    [Header("Logo Atlas")]
    public Texture2D logoAtlas;

    [Range(0, 15)]
    public int logoID;

    const int atlasSize = 4;

    MaterialPropertyBlock mpb;

    void Awake()
    {
        Apply();
    }

    void Apply()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetColor("_Color", shirtColor);
        mpb.SetColor("_DetailColor", logoColor);

        if (logoAtlas != null)
        {
            mpb.SetTexture("_DetailTex", logoAtlas);

            int x = logoID % atlasSize;
            int y = logoID / atlasSize;

            Vector2 tiling = new Vector2(0.2f, 0.2f);

            float offsetX = 0.02f + (0.25f * x);
            float offsetY = 0.76f - (0.25f * y);

            mpb.SetVector("_DetailTex_ST", new Vector4(tiling.x, tiling.y, offsetX, offsetY));
        }

        targetRenderer.SetPropertyBlock(mpb);
    }
}