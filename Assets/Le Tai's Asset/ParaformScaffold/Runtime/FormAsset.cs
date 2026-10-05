// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System;
using LeTai.Common;
using UnityEngine;

namespace LeTai.Paraform
{
public partial class FormAsset : ScriptableObject
{
    public event Action changed;

    [DrawInline]
    public FormAssetGenerationParams generationParams = FormAssetGenerationParams.Default;

    public Sprite     FormSprite  => _formSprite;
    public Vector2Int ContentSize => _contentSize;
    public Vector2Int TextureSize => _textureSize;

    [HideInInspector]
    [SerializeField]
    Sprite _formSprite;

    [HideInInspector]
    [SerializeField]
    Vector2Int _contentSize;

    [HideInInspector]
    [SerializeField]
    Vector2Int _textureSize;

    internal void SetGenerated(Sprite formSprite, Vector2Int contentSize, Vector2Int textureSize)
    {
        _formSprite  = formSprite;
        _contentSize = contentSize;
        _textureSize = textureSize;
        changed?.Invoke();
    }

    void OnEnable()
    {
        MigrateLegacySize();
    }

    void MigrateLegacySize()
    {
        if (!_formSprite || _contentSize != Vector2Int.zero || _textureSize != Vector2Int.zero)
            return;

        _contentSize = _textureSize = Vector2Int.RoundToInt(_formSprite.rect.size);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    void OnValidate()
    {
        changed?.Invoke();
    }
}
}
