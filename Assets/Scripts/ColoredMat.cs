using UnityEngine;
using UnityEditor;

public class ColoredMat : MonoBehaviour
{
    public Renderer targetRenderer;

    [Header("Colors")]
    public Color shirtColor = Color.white;
    public Color logoColor = Color.white;

    [Header("Texture")]
    public Texture2D mainTexture;
    public Texture2D atlasTexture;

    [Header("Atlas Settings")]
    public int columns = 4;
    public int rows = 4;

    [Range(0, 100)]
    public int imageID;

    [Header("Atlas Offset Steps")]
    public float stepX = 0.25f;
    public float stepY = 0.25f;

    [Header("Base Offset")]
    public float baseOffsetX = 0.02f;
    public float baseOffsetY = 0.76f;

    [Header("Tiling")]
    public Vector2 tiling = new Vector2(0.2f, 0.2f);

    MaterialPropertyBlock mpb;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetColor("_Color", shirtColor);
        mpb.SetColor("_DetailColor", logoColor);

        if (mainTexture != null)
            mpb.SetTexture("_MainTex", mainTexture);

        if (atlasTexture != null)
        {
            mpb.SetTexture("_DetailTex", atlasTexture);

            int maxImages = columns * rows;
            imageID = Mathf.Clamp(imageID, 0, maxImages - 1);

            int x = imageID % columns;
            int y = imageID / columns;

            float offsetX = baseOffsetX + (stepX * x);
            float offsetY = baseOffsetY - (stepY * y);

            mpb.SetVector("_DetailTex_ST",
                new Vector4(tiling.x, tiling.y, offsetX, offsetY));
        }

        targetRenderer.SetPropertyBlock(mpb);
    }
}

[CustomEditor(typeof(ColoredMat))]
public class ColoredMatEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ColoredMat matScript = (ColoredMat)target;

        if (GUILayout.Button("Test"))
        {
            matScript.Apply();
            SceneView.RepaintAll();
        }
    }
}