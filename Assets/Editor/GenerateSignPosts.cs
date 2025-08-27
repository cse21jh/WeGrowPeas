#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GenerateSignPosts
{
    [MenuItem("Tools/Shop/Generate SignPost 6")]
    public static void Generate()
    {
        string folder = "Assets/Resources/Data/SignPosts";
        System.IO.Directory.CreateDirectory(folder);

        Make("SignPost_Aging", "ÆÖ¸»: ÀÚ¿¬»ç", WaveType.Aging, 5, 1000, 2, 0.75f, 4, folder);
        Make("SignPost_Wind", "ÆÖ¸»: ¹Ù¶÷", WaveType.Wind, 5, 1000, 2, 0.75f, 4, folder);
        Make("SignPost_Flood", "ÆÖ¸»: È«¼ö", WaveType.Flood, 10, 1000, 2, 0.75f, 4, folder);
        Make("SignPost_Pest", "ÆÖ¸»: ÇØÃæ", WaveType.Pest, 15, 1000, 2, 0.75f, 4, folder);
        Make("SignPost_Cold", "ÆÖ¸»: ÃßÀ§", WaveType.Cold, 20, 1000, 2, 0.75f, 4, folder);
        Make("SignPost_HeavyRain", "ÆÖ¸»: Æø¿ì", WaveType.HeavyRain, 25, 1000, 2, 0.75f, 4, folder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SignPost 6°³ »ý¼º ¿Ï·á");
    }

    private static void Make(string id, string name, WaveType wave, int unlockDay, int price, int weight, float reducePercent, int duration, string folder)
    {
        var so = ScriptableObject.CreateInstance<ItemData_SignPost>();
        so.name = id;                // Unity asset ÀÌ¸§
        so.DisplayName = name;
        so.Price = price;
        so.targetWave = wave;
        so.unlockStageDay = unlockDay;
        so.rotationWeight = weight;
        so.reducePercent = reducePercent;
        so.durationDays = duration;

        AssetDatabase.CreateAsset(so, $"{folder}/{id}.asset");
    }
}
#endif
