using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ResourceLoader
{
    private static readonly Dictionary<string, Sprite> spriteCache = new();

    /// <summary>
    /// Resources/UpgradeIcons.png 안에 잘라놓은 Sprite를 이름으로 로드
    /// </summary>
    public static Sprite LoadUpgradeIcon(string name)
    {
        // 최초 한 번만 LoadAll
        if (spriteCache.Count == 0)
        {
            var sprites = Resources.LoadAll<Sprite>("UpgradeIcons");
            foreach (var sprite in sprites)
            {
                spriteCache[sprite.name] = sprite;
            }
        }

        // 없으면 null
        if (spriteCache.TryGetValue(name, out var result))
            return result;

        Debug.LogWarning($"[ResourceLoader] 스프라이트 '{name}'을 찾을 수 없습니다.");
        return null;
    }
}
