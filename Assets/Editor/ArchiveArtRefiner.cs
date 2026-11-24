using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WeGrowPeas.UI.Book;
using Object = UnityEngine.Object;

/// <summary>Updates only the archive-owned art; existing book layouts are never rebuilt.</summary>
public static class ArchiveArtRefiner
{
    private const string Art = "Assets/Resource/UI/Archive/";
    private static readonly string[] Categories = { "Item", "Plant", "Curse", "Bug" };
    private static readonly Color[] Faces = {
        new Color(.983653724f,.528902829f,.415215015f), new Color(.718147635f,.891345978f,.475670218f),
        new Color(.989423037f,.579382956f,.579382956f), new Color(.604751289f,.278975517f,.677884638f) };
    private static readonly Color[] Depths = {
        new Color(.841346145f,.457711071f,.219235197f), new Color(.582541645f,.802884638f,.274061561f),
        new Color(.939422905f,.452548683f,.452548683f), new Color(.505430162f,.201763868f,.589422941f) };
    private static readonly Color[] Outlines = {
        new Color(.537499964f,.243313536f,.106983222f), new Color(.294118613f,.552884638f,.082401074f),
        new Color(.835576892f,.206483990f,.206483990f), new Color(.393137246f,.090196081f,.494117647f) };

    [MenuItem("Tools/Archive/Update Figma Artwork")]
    public static void Apply()
    {
        RequireEditMode();
        ImportArt();
        var root = PrefabUtility.LoadPrefabContents(ArchiveBookBuilder.PrefabPath);
        try
        {
            ConfigureTabFeedback(root);
            var book = root.GetComponentInChildren<BookLayoutController>(true);
            var tabTitle = book.transform.parent.Find("ArchiveNavigation/CurrentTabTitle").GetComponent<TMP_Text>();
            tabTitle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,190);
            tabTitle.fontSizeMin = 20;
            var left = book.Layouts[3].leftContent;
            var right = book.Layouts[3].rightContent;
            var view = left.GetComponent<ArchiveCatalogView>();
            var viewSo = new SerializedObject(view);
            var buttons = viewSo.FindProperty("categoryButtons");
            for (int i = 0; i < 4; i++)
            {
                var button = (Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                // Preserve any palette/offset edits made after the layered version was installed.
                if (button.GetComponent<ArchiveBookmark>() == null) BuildBookmark(button, i);
            }
            var oldFrame = right.Find("IconFrame");
            if (right.Find("FigmaFrameDecoration") == null)
            {
                oldFrame.gameObject.SetActive(false);
                var splitFrame = right.Find("FigmaIconFrame");
                if (splitFrame != null) Object.DestroyImmediate(splitFrame.gameObject);
                // One native export preserves every original stroke and gap at the same 0.6 scale.
                var frame = Picture(right,"FigmaFrameDecoration",71.142f,175,407.7163f,231.1951f,"CodexFrameDecoration");
                frame.transform.SetSiblingIndex(oldFrame.GetSiblingIndex());
                var icon = (RectTransform)right.Find("DetailIcon");
                icon.anchoredPosition = new Vector2(205,-214); icon.sizeDelta = new Vector2(142,142);
            }
            if (right.Find("ItemPrice") == null)
            {
                var price = Rect("ItemPrice",right,185,414,220,45);
                Picture(price,"Coin",0,1,43,43,"Coin");
                var label = MakeText(price,"Value",48,0,162,45,32);
                label.color = new Color32(255,215,118,255);
                label.fontSharedMaterial = FontMaterial(label.font,"ArchivePrice",new Color32(141,46,18,255),.25f);
                Ref(viewSo,"priceRoot",price.gameObject); Ref(viewSo,"detailPrice",label);
                var divider = (RectTransform)right.Find("Divider"); divider.anchoredPosition = new Vector2(45,-468);
                var description = (RectTransform)right.Find("Description");
                description.anchoredPosition = new Vector2(45,-482); description.sizeDelta = new Vector2(475,153);
            }
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            var fallback = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resource/Sprites/UI/Icons/땅문서.png");
            foreach (var catalog in root.GetComponentsInChildren<ArchiveCatalogView>(true))
            {
                var so = new SerializedObject(catalog);
                Ref(so,"achievementFallbackIcon",fallback);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var light = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Maplestory Light SDF.asset");
            for (int i = 3; i < 5; i++)
            {
                var page = book.Layouts[i].rightContent;
                var heading = page.Find("DetailTitle").GetComponent<TMP_Text>();
                heading.margin = new Vector4(8,0,i == 4 ? 85 : 8,0);
                heading.textWrappingMode = TextWrappingModes.Normal;
                heading.overflowMode = TextOverflowModes.Ellipsis;
                var description = page.Find("Description");
                var image = description.GetComponent<Image>();
                image.sprite = Sprite(i == 3 ? "CodexDescription" : "AchievementDescription");
                image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 3.2f;
                image.color = Color.white;
                var body = description.Find("Viewport/Content/Text").GetComponent<TMP_Text>();
                body.font = light; body.fontSharedMaterial = light.material; body.fontSize = 19;
                var rail = (RectTransform)description.Find("Scrollbar");
                rail.anchoredPosition = new Vector2(456,-13); rail.sizeDelta = new Vector2(8,((RectTransform)description).rect.height - 26);
            }
            var caption = book.Layouts[4].rightContent.Find("ProgressLabel").GetComponent<TMP_Text>();
            caption.fontSharedMaterial = FontMaterial(caption.font,"ArchiveProgress",new Color32(141,46,18,255),.25f);
            caption.enableAutoSizing = true; caption.fontSizeMin = 18; caption.fontSizeMax = 29;
            caption.margin = new Vector4(0,0,6,0);
            var progressView = new SerializedObject(book.Layouts[4].leftContent.GetComponent<ArchiveCatalogView>());
            ConfigureProgressTrack(book.Layouts[4].rightContent.Find("ProgressTrack").GetComponent<Image>(),progressView,book);
            progressView.ApplyModifiedPropertiesWithoutUndo();
            foreach (var row in root.GetComponentsInChildren<ArchiveEntryRow>(true))
                row.GetComponent<Image>().pixelsPerUnitMultiplier = 3.2f;
            PrefabUtility.SaveAsPrefabAsset(root, ArchiveBookBuilder.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("[Archive] Applied Figma PNG artwork and independently tintable bookmark layers.");
    }

    [MenuItem("Tools/Archive/Update Tab Feedback")]
    public static void ApplyTabFeedback()
    {
        RequireEditMode();
        var root = PrefabUtility.LoadPrefabContents(ArchiveBookBuilder.PrefabPath);
        try
        {
            ConfigureTabFeedback(root);
            PrefabUtility.SaveAsPrefabAsset(root,ArchiveBookBuilder.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void RequireEditMode()
    {
        const string allowed = "C:/Users/pingguin1234/Seungbu/00. Work Directory/01. Game Development/03. Fork/WeGrowPeas";
        if (!string.Equals(Path.GetFullPath(Application.dataPath + "/..").Replace('\\','/'),allowed,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Archive authoring is restricted to WeGrowPeas.");
        if (Application.isPlaying) throw new InvalidOperationException("Apply archive art outside Play Mode.");
    }

    private static void ConfigureTabFeedback(GameObject root)
    {
        var book = root.GetComponentInChildren<BookLayoutController>(true);
        var header = book.transform.parent.Find("ArchiveNavigation");
        var hit = header.Find("CurrentTabHit");
        if (hit == null)
        {
            hit = Rect("CurrentTabHit",header,10,10,365,260);
            hit.SetAsFirstSibling();
            var image = hit.gameObject.AddComponent<Image>();
            image.color = Color.clear; image.raycastTarget = true;
            var button = hit.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        }
        foreach (var button in header.GetComponentsInChildren<Button>(true))
        {
            button.transition = Selectable.Transition.None;
            if (button.GetComponent<ArchiveTabPointer>() == null) button.gameObject.AddComponent<ArchiveTabPointer>();
        }
        var settings = new SerializedObject(root.GetComponent<ArchiveBookController>());
        Ref(settings,"currentButton",hit.GetComponent<Button>());
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ConfigureProgressTrack(Image track, SerializedObject view, BookLayoutController book)
    {
        var bar = track.GetComponent<ArchiveProgressBar>();
        if (bar == null)
        {
            for (int i = track.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(track.transform.GetChild(i).gameObject);
            track.rectTransform.sizeDelta = new Vector2(365,26.25f);
            track.sprite = Sprite("ProgressTrack"); track.color = Color.white;
            track.type = Image.Type.Sliced; track.pixelsPerUnitMultiplier = 6.4f;
            var rail = Picture(track.transform,"Rail",3.125f,3.125f,358.75f,20,"ProgressRail");
            rail.type = Image.Type.Sliced; rail.preserveAspect = false; rail.pixelsPerUnitMultiplier = 6.4f;
            var fill = Picture(track.transform,"Fill",3.125f,3.125f,358.75f,20,"ProgressFill");
            fill.type = Image.Type.Sliced; fill.preserveAspect = false; fill.pixelsPerUnitMultiplier = 6.4f;
            fill.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var stripes = Rect("ScrollingStripes",fill.transform,0,0,358.75f,20).gameObject.AddComponent<RawImage>();
            stripes.rectTransform.anchorMin = Vector2.zero;
            stripes.rectTransform.anchorMax = Vector2.one;
            stripes.rectTransform.offsetMin = stripes.rectTransform.offsetMax = Vector2.zero;
            stripes.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"ProgressStripeTile.png");
            stripes.raycastTarget = false;
            bar = track.gameObject.AddComponent<ArchiveProgressBar>();
            var settings = new SerializedObject(bar);
            Ref(settings,"rail",rail.rectTransform); Ref(settings,"fill",fill); Ref(settings,"stripes",stripes);
            Ref(settings,"pageTurner",book.Turner); settings.ApplyModifiedPropertiesWithoutUndo();
        }
        // The original white rail is baked over the brown track in Figma's compositing space.
        // Retain its RectTransform as the fill-width reference without compositing it twice in linear Unity UI.
        track.transform.Find("Rail").GetComponent<Image>().enabled = false;
        Ref(view,"progressBar",bar);
    }

    public static void BuildBookmark(Button button,int index)
    {
        var root = (RectTransform)button.transform;
        for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
        root.anchoredPosition = new Vector2(30 + index * 92,7); root.sizeDelta = new Vector2(78,125);
        var hit = button.GetComponent<Image>(); hit.sprite = null; hit.color = Color.clear;
        hit.raycastTarget = true; button.transition = Selectable.Transition.None;
        var bookmark = root.gameObject.AddComponent<ArchiveBookmark>();
        var so = new SerializedObject(bookmark);
        so.FindProperty("faceColor").colorValue = Faces[index];
        so.FindProperty("depthColor").colorValue = Depths[index];
        so.FindProperty("outlineColor").colorValue = Outlines[index];
        so.FindProperty("iconInkColor").colorValue = Outlines[index];
        so.FindProperty("selected").boolValue = index == 0;
        Image[] faces = new Image[2], depths = new Image[2], outlines = new Image[2], shadows = new Image[2];
        for (int state = 0; state < 2; state++)
        {
            bool selected = state == 1;
            string prefix = selected ? "BookmarkLong" : "BookmarkShort";
            var shape = Rect(selected ? "SelectedShape" : "NormalShape",root,0,0,78,selected ? 125 : 95);
            shadows[state] = Picture(shape,"Shadow",1.2f,4.8f,77.6818f,selected ? 120.4494f : 89.7411f,prefix+"Outline");
            outlines[state] = Picture(shape,"Outline",0,0,77.6818f,selected ? 120.4494f : 89.7411f,prefix+"Outline");
            depths[state] = Picture(shape,"Depth",3,3,71.6818f,selected ? 114.4437f : 83.7362f,prefix+"Depth");
            faces[state] = Picture(shape,"Face",3,3,71.6818f,selected ? 103.1015f : 75.5658f,prefix+"Face");
            Ref(so,selected ? "selectedShape" : "normalShape",shape.gameObject);
            shape.gameObject.SetActive(selected == (index == 0));
        }
        Images(so,"faces",faces); Images(so,"depths",depths); Images(so,"outlines",outlines); Images(so,"shadows",shadows);
        Vector4[] iconBounds = { new Vector4(13.874f,14.618f,47.407f,37.056f),new Vector4(22.381f,15.350f,31.281f,39.962f),
            new Vector4(21.479f,11.372f,36.389f,46.157f),new Vector4(16.090f,15.058f,45.892f,39.954f) };
        var b = iconBounds[index];
        Ref(so,"iconFill",Picture(root,"IconFill",b.x,b.y,b.z,b.w,Categories[index]+"IconFill"));
        Ref(so,"iconInk",Picture(root,"IconInk",b.x,b.y,b.z,b.w,Categories[index]+"IconInk"));
        var label = MakeText(root,"Label",-5,66,88,34,28);
        label.text = new[] { "아이템","식물","저주","벌레" }[index];
        label.fontSharedMaterial = FontMaterial(label.font,"ArchiveBookmark"+Categories[index],Outlines[index],.25f);
        label.gameObject.AddComponent<LocalizedText>().Link("archive."+Categories[index].ToLowerInvariant(),label.text);
        Ref(so,"label",label);
        so.ApplyModifiedPropertiesWithoutUndo();
        bookmark.ApplyPalette(); bookmark.SetSelected(index == 0);
    }

    private static void ImportArt()
    {
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Art,"*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            if (path.Contains("Description")) importer.spriteBorder = new Vector4(50,50,50,50);
            if (path.Contains("EntryBackground")) importer.spriteBorder = new Vector4(44,44,44,44);
            if (path.Contains("ProgressTrack")) importer.spriteBorder = new Vector4(84,84,84,84);
            if (path.Contains("ProgressRail") || path.Contains("ProgressFill")) importer.spriteBorder = new Vector4(64,64,64,64);
            if (path.Contains("ProgressStripeTile"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapModeU = TextureWrapMode.Repeat; importer.wrapModeV = TextureWrapMode.Clamp;
            }
            importer.SaveAndReimport();
        }
    }
    private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var t = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        t.gameObject.layer=5; t.SetParent(parent,false); t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);
        t.anchoredPosition=new Vector2(x,-y); t.sizeDelta=new Vector2(w,h); return t;
    }
    private static Image Picture(Transform parent,string name,float x,float y,float w,float h,string art)
    {
        var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>(); image.sprite=Sprite(art);
        image.preserveAspect=true; image.raycastTarget=false; return image;
    }
    private static TMP_Text MakeText(Transform parent,string name,float x,float y,float w,float h,float size)
    {
        var label=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
        label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Tenada/Tenada SDF.asset");
        label.fontSize=size; label.textWrappingMode=TextWrappingModes.NoWrap; label.alignment=TextAlignmentOptions.Center;
        label.raycastTarget=false; return label;
    }
    private static Material FontMaterial(TMP_FontAsset font,string name,Color color,float width)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(Art+name+".mat");
        if(material!=null)return material;
        material=new Material(font.material){name=name}; material.EnableKeyword("OUTLINE_ON");
        material.SetColor("_OutlineColor",color); material.SetFloat("_OutlineWidth",width);
        AssetDatabase.CreateAsset(material,Art+name+".mat"); return material;
    }
    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png") ?? throw new InvalidOperationException("Missing Figma PNG: "+name);
    private static void Ref(SerializedObject so,string field,Object value) => so.FindProperty(field).objectReferenceValue=value;
    private static void Images(SerializedObject so,string field,Image[] values)
    {
        var p=so.FindProperty(field);p.arraySize=values.Length;
        for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
    }
}
