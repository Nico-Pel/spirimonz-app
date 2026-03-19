using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class CatchableBook : CatchableObject
{
    [Header("Book Visuals")]
    public SkinnedMeshRenderer bookRenderer;
    public string openBlendShapeName = "Open";
    public float closedBlendshapeWeight = 0f; // Unity blendshape weight is usually 0..100
    public float openBlendshapeWeight = 100f;
    public float openCloseDuration = 0.25f;
    public Ease openCloseEase = Ease.OutCubic;

    [Header("Model Variations")]
    public GameObject[] modelVariants;
    public bool randomizeModelOnStart = true;
    public bool preserveMaterialsOnVariantSwap = true;

    [Header("Collider Settings")]
    public BoxCollider bookCollider;
    public Vector3 closedColliderCenter = Vector3.zero;
    public Vector3 closedColliderSize = Vector3.one;
    public Vector3 openColliderCenter = Vector3.zero;
    public Vector3 openColliderSize = Vector3.one;
    public bool drawColliderGizmos = true;
    public Color closedGizmoColor = new Color(0.2f, 0.9f, 1f, 0.25f);
    public Color openGizmoColor = new Color(1f, 0.6f, 0.2f, 0.25f);

    [Header("Throw Toggle Chances")]
    [Range(0f, 1f)] public float playerOpenOnThrowChance = 0.5f;
    [Range(0f, 1f)] public float playerCloseOnThrowChance = 0.2f;
    [Range(0f, 1f)] public float ghostOpenOnThrowChance = 0.5f;
    [Range(0f, 1f)] public float ghostCloseOnThrowChance = 0.2f;

    [Header("Manual Toggle Sounds")]
    public SoundParameters manualOpenSoundParameters;
    public SoundParameters manualCloseSoundParameters;

    [Header("Evidence Pages")]
    public EvidenceParameter[] evidenceParameters;
    [Range(0f, 1f)] public float evidencePagesChance = 0.1f;

    [Header("Material Variations")]
    public bool randomizeCoverMaterial = true;
    public Material[] coverMaterialOptions;
    public bool randomizePagesOffset = true;
    [Min(0)] public int pageMaterialIndex = 1;
    public float[] pageOffsetOptions = { 0f, 0.25f, 0.75f };
    public Vector2 atlasScale = new Vector2(1f, 0.25f);

    private readonly List<BlendshapeBinding> _blendshapeBindings = new();
    private bool _isOpen;
    private Tween _openTween;
    private float _lastSyncedWeight = float.NaN;
    private bool _materialsRandomized;
    private bool _hasEvidencePage;
    private Material _selectedEvidenceMaterial;
    private float _selectedEvidenceOffset;
    private MaterialPropertyBlock _pagePropertyBlock;
    private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

    private void Awake()
    {
        base.Awake();
        InitializeColliderDefaults();
        RefreshBlendshapeBindings();
        SyncStateFromBlendshape(applyCollider: false);
    }

    private void Start()
    {
        _hasEvidencePage = TrySelectEvidencePage(out _selectedEvidenceMaterial, out _selectedEvidenceOffset);
        if (!_hasEvidencePage)
            SelectModelVariant();

        RefreshBlendshapeBindings();
        SyncStateFromBlendshape(applyCollider: true);
        ApplyRandomizedMaterials();
    }

    private void LateUpdate()
    {
        if (_openTween != null && _openTween.IsActive() && _openTween.IsPlaying())
            return;

        float current = GetCurrentBlendshapeWeight();
        if (float.IsNaN(_lastSyncedWeight) || Mathf.Abs(current - _lastSyncedWeight) > 0.01f)
        {
            _lastSyncedWeight = current;
            _isOpen = IsWeightCloserToOpen(current);
            ApplyColliderForWeight(current);
        }
    }

    public override void SpecialActionInHandsOnClick()
    {
        if (!isGrabbed)
            return;

        ToggleOpenWithSound();
    }

    public override void OnThrow()
    {
        base.OnThrow();
        TryToggleOnThrow(playerOpenOnThrowChance, playerCloseOnThrowChance);
    }

    public void OnGhostThrow()
    {
        TryToggleOnThrow(ghostOpenOnThrowChance, ghostCloseOnThrowChance);
    }

    private void TryToggleOnThrow(float openChance, float closeChance)
    {
        bool currentlyOpen = GetCurrentOpenState();
        if (!currentlyOpen)
        {
            if (Random.value <= Mathf.Clamp01(openChance))
                SetOpen(true, instant: false);
        }
        else
        {
            if (Random.value <= Mathf.Clamp01(closeChance))
                SetOpen(false, instant: false);
        }
    }

    private void ToggleOpen()
    {
        bool currentlyOpen = GetCurrentOpenState();
        SetOpen(!currentlyOpen, instant: false);
    }

    private void ToggleOpenWithSound()
    {
        bool currentlyOpen = GetCurrentOpenState();
        bool open = !currentlyOpen;
        SetOpen(open, instant: false);

        if (open)
            manualOpenSoundParameters?.PlaySound(transform.position);
        else
            manualCloseSoundParameters?.PlaySound(transform.position);
    }

    private void SetOpen(bool open, bool instant)
    {
        _isOpen = open;
        AnimateBlendshapeToWeight(open ? openBlendshapeWeight : closedBlendshapeWeight, instant);
    }

    private void AnimateBlendshapeToWeight(float targetWeight, bool instant)
    {
        _openTween?.Kill();

        if (instant || openCloseDuration <= 0f)
        {
            SetBlendshapeWeight(targetWeight);
            ApplyColliderForWeight(targetWeight);
            _lastSyncedWeight = targetWeight;
            return;
        }

        _openTween = DOTween.To(
                () => GetCurrentBlendshapeWeight(),
                v =>
                {
                    SetBlendshapeWeight(v);
                    ApplyColliderForWeight(v);
                    _lastSyncedWeight = v;
                },
                targetWeight,
                openCloseDuration)
            .SetEase(openCloseEase);
    }

    private void SetBlendshapeWeight(float weight)
    {
        if (_blendshapeBindings.Count == 0)
            RefreshBlendshapeBindings();

        for (int i = 0; i < _blendshapeBindings.Count; i++)
        {
            BlendshapeBinding binding = _blendshapeBindings[i];
            if (binding.Renderer != null)
                binding.Renderer.SetBlendShapeWeight(binding.Index, weight);
        }
    }

    private void ApplyColliderForWeight(float weight)
    {
        if (bookCollider == null)
            return;

        float openAmount = GetOpenAmountFromWeight(weight);
        bookCollider.center = Vector3.Lerp(closedColliderCenter, openColliderCenter, openAmount);
        bookCollider.size = Vector3.Lerp(closedColliderSize, openColliderSize, openAmount);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawColliderGizmos || bookCollider == null)
            return;

        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = bookCollider.transform.localToWorldMatrix;

        Gizmos.color = closedGizmoColor;
        Gizmos.DrawWireCube(closedColliderCenter, closedColliderSize);

        Gizmos.color = openGizmoColor;
        Gizmos.DrawWireCube(openColliderCenter, openColliderSize);

        Gizmos.matrix = previous;
    }

    private void SyncStateFromBlendshape(bool applyCollider)
    {
        float current = GetCurrentBlendshapeWeight();
        _isOpen = IsWeightCloserToOpen(current);
        _lastSyncedWeight = current;

        if (applyCollider)
            ApplyColliderForWeight(current);
    }

    private bool GetCurrentOpenState()
    {
        SyncStateFromBlendshape(applyCollider: false);
        return _isOpen;
    }

    private float GetCurrentBlendshapeWeight()
    {
        if (_blendshapeBindings.Count == 0)
            RefreshBlendshapeBindings();

        if (_blendshapeBindings.Count == 0)
            return closedBlendshapeWeight;

        BlendshapeBinding binding = _blendshapeBindings[0];
        if (binding.Renderer == null)
            return closedBlendshapeWeight;

        return binding.Renderer.GetBlendShapeWeight(binding.Index);
    }

    private float GetOpenAmountFromWeight(float weight)
    {
        if (Mathf.Approximately(closedBlendshapeWeight, openBlendshapeWeight))
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(closedBlendshapeWeight, openBlendshapeWeight, weight));
    }

    private bool IsWeightCloserToOpen(float weight)
    {
        float closedDistance = Mathf.Abs(weight - closedBlendshapeWeight);
        float openDistance = Mathf.Abs(weight - openBlendshapeWeight);

        // Closed wins on equality to avoid accidental open.
        return openDistance < closedDistance;
    }

    private void InitializeColliderDefaults()
    {
        if (bookCollider == null)
            return;

        bool closedDefault = closedColliderCenter == Vector3.zero && closedColliderSize == Vector3.one;
        bool openDefault = openColliderCenter == Vector3.zero && openColliderSize == Vector3.one;

        if (closedDefault)
        {
            closedColliderCenter = bookCollider.center;
            closedColliderSize = bookCollider.size;
        }

        if (openDefault)
        {
            openColliderCenter = bookCollider.center;
            openColliderSize = bookCollider.size;
        }
    }

    private void ApplyRandomizedMaterials()
    {
        if (_materialsRandomized || bookRenderer == null)
            return;

        _materialsRandomized = true;

        Material[] materials = bookRenderer.materials;
        if (materials == null || materials.Length == 0)
            return;

        if (randomizeCoverMaterial && coverMaterialOptions != null && coverMaterialOptions.Length > 0)
        {
            int coverIndex = 0;
            if (coverIndex >= 0 && coverIndex < materials.Length)
            {
                Material chosenCover = coverMaterialOptions[Random.Range(0, coverMaterialOptions.Length)];
                if (chosenCover != null)
                    materials[coverIndex] = chosenCover;
            }
        }

        if (!randomizePagesOffset)
        {
            bookRenderer.materials = materials;
            return;
        }

        int materialIndex = pageMaterialIndex;
        if (materialIndex < 0 || materialIndex >= materials.Length)
            materialIndex = Mathf.Clamp(materialIndex, 0, materials.Length - 1);

        Material target = materials[materialIndex];
        if (target == null)
            return;

        if (_hasEvidencePage && _selectedEvidenceMaterial != null)
        {
            Material evidenceInstance = new Material(_selectedEvidenceMaterial);
            ApplyPageOffsetToMaterial(evidenceInstance, _selectedEvidenceOffset);
            materials[materialIndex] = evidenceInstance;
            bookRenderer.materials = materials;
            return;
        }

        float offsetY = GetRandomPageOffset();
        ApplyPageOffsetToMaterial(target, offsetY);
        materials[materialIndex] = target;
        bookRenderer.materials = materials;
    }

    private void ApplyPageOffsetToMaterial(Material material, float offsetY)
    {
        if (material == null)
            return;

        Vector2 scale = material.mainTextureScale;
        if (!Mathf.Approximately(scale.y, 1f))
            material.mainTextureScale = new Vector2(scale.x, 1f);

        Vector2 offset = material.mainTextureOffset;
        material.mainTextureOffset = new Vector2(offset.x, offsetY);
    }

    private float GetRandomPageOffset()
    {
        if (pageOffsetOptions == null || pageOffsetOptions.Length == 0)
            return 0f;

        int index = Random.Range(0, pageOffsetOptions.Length);
        return pageOffsetOptions[index];
    }

    private bool TrySelectEvidencePage(out Material evidenceMaterial, out float offsetY)
    {
        evidenceMaterial = null;
        offsetY = 0f;

        if (!randomizePagesOffset)
            return false;

        float chance = Mathf.Clamp01(evidencePagesChance);
        if (chance <= 0f || Random.value > chance)
            return false;

        return TryGetEvidencePage(out evidenceMaterial, out offsetY);
    }

    private bool TryGetEvidencePage(out Material evidenceMaterial, out float offsetY)
    {
        evidenceMaterial = null;
        offsetY = 0f;
        Ghost currentGhost = House.Instance != null ? House.Instance.currentGhost : null;
        GhostParameters ghostParameters = currentGhost != null ? currentGhost.ghostParameters : null;
        if (ghostParameters == null)
            return false;

        if (evidenceParameters == null || evidenceParameters.Length == 0)
            return false;

        List<EvidenceParameter> validEvidences = new List<EvidenceParameter>(evidenceParameters.Length);
        for (int i = 0; i < evidenceParameters.Length; i++)
        {
            EvidenceParameter evidence = evidenceParameters[i];
            if (evidence != null && evidence.linkedMaterial != null)
                validEvidences.Add(evidence);
        }

        if (validEvidences.Count == 0)
            return false;

        EvidenceParameter chosen = validEvidences[Random.Range(0, validEvidences.Count)];
        evidenceMaterial = chosen.linkedMaterial;
        bool hasEvidence = ghostParameters.HasEvidence(chosen.evidenceType);
        offsetY = hasEvidence ? chosen.offsetYYes : chosen.offsetYNo;
        return true;
    }

    private void SelectModelVariant()
    {
        if (!randomizeModelOnStart || modelVariants == null || modelVariants.Length == 0)
            return;

        int chosenIndex = Random.Range(0, modelVariants.Length);
        bool hasSceneVariants = false;
        for (int i = 0; i < modelVariants.Length; i++)
        {
            if (modelVariants[i] == null)
                continue;

            if (modelVariants[i].transform.IsChildOf(transform))
            {
                hasSceneVariants = true;
                break;
            }
        }

        if (hasSceneVariants)
        {
            for (int i = 0; i < modelVariants.Length; i++)
            {
                if (modelVariants[i] != null)
                    modelVariants[i].SetActive(i == chosenIndex);
            }

            if (modelVariants[chosenIndex] != null)
            {
                SkinnedMeshRenderer renderer = modelVariants[chosenIndex].GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (renderer != null)
                    bookRenderer = renderer;
            }

            RefreshBlendshapeBindings();
            return;
        }

        GameObject chosen = modelVariants[chosenIndex];
        if (chosen == null)
            return;

        SkinnedMeshRenderer sourceRenderer = chosen.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (sourceRenderer == null)
            return;

        SkinnedMeshRenderer targetRenderer = bookRenderer;
        if (targetRenderer == null || !targetRenderer.transform.IsChildOf(transform))
            targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);

        if (targetRenderer == null)
            return;

        Material[] preservedMaterials = targetRenderer.sharedMaterials;
        targetRenderer.sharedMesh = sourceRenderer.sharedMesh;

        if (!preserveMaterialsOnVariantSwap)
        {
            targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        }
        else
        {
            if (preservedMaterials == null || preservedMaterials.Length == 0)
                preservedMaterials = sourceRenderer.sharedMaterials;

            int needed = sourceRenderer.sharedMesh != null ? sourceRenderer.sharedMesh.subMeshCount : 0;
            if (preservedMaterials != null && preservedMaterials.Length > 0 && needed > 0)
            {
                if (preservedMaterials.Length != needed)
                {
                    Material[] resized = new Material[needed];
                    for (int i = 0; i < needed; i++)
                    {
                        if (i < preservedMaterials.Length)
                            resized[i] = preservedMaterials[i];
                        else
                            resized[i] = preservedMaterials[preservedMaterials.Length - 1];
                    }

                    targetRenderer.sharedMaterials = resized;
                }
                else
                {
                    targetRenderer.sharedMaterials = preservedMaterials;
                }
            }
        }

        bookRenderer = targetRenderer;
        RefreshBlendshapeBindings();
    }

    private void RefreshBlendshapeBindings()
    {
        _blendshapeBindings.Clear();

        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        string targetName = string.IsNullOrWhiteSpace(openBlendShapeName) ? null : openBlendShapeName.Trim();

        for (int r = 0; r < renderers.Length; r++)
        {
            SkinnedMeshRenderer renderer = renderers[r];
            Mesh mesh = renderer != null ? renderer.sharedMesh : null;
            if (mesh == null || mesh.blendShapeCount == 0)
                continue;

            int index = FindBlendshapeIndex(mesh, targetName, exact: true);
            if (index < 0)
                index = FindBlendshapeIndex(mesh, targetName, exact: false);

            if (index >= 0)
            {
                _blendshapeBindings.Add(new BlendshapeBinding(renderer, index));
                continue;
            }
        }

        if (_blendshapeBindings.Count > 0)
        {
            SkinnedMeshRenderer bindingRenderer = _blendshapeBindings[0].Renderer;
            if (bindingRenderer != null && (bookRenderer == null || !bookRenderer.transform.IsChildOf(transform)))
                bookRenderer = bindingRenderer;
        }

        if (_blendshapeBindings.Count > 0)
            return;

        for (int r = 0; r < renderers.Length; r++)
        {
            SkinnedMeshRenderer renderer = renderers[r];
            Mesh mesh = renderer != null ? renderer.sharedMesh : null;
            if (mesh == null || mesh.blendShapeCount == 0)
                continue;

            _blendshapeBindings.Add(new BlendshapeBinding(renderer, 0));
            return;
        }
    }

    private int FindBlendshapeIndex(Mesh mesh, string targetName, bool exact)
    {
        if (mesh == null || mesh.blendShapeCount == 0 || string.IsNullOrEmpty(targetName))
            return -1;

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string name = mesh.GetBlendShapeName(i);
            if (exact)
            {
                if (string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            else
            {
                if (name != null && name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
        }

        return -1;
    }

    private readonly struct BlendshapeBinding
    {
        public readonly SkinnedMeshRenderer Renderer;
        public readonly int Index;

        public BlendshapeBinding(SkinnedMeshRenderer renderer, int index)
        {
            Renderer = renderer;
            Index = index;
        }
    }
}
