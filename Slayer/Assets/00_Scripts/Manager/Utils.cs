using UnityEngine;
using UnityEngine.U2D;

public class Utils 
{
    public static SpriteAtlas atlas = Resources.Load<SpriteAtlas>("Atlas");

    public static Sprite GetAtlas(string path)
    {
        return atlas.GetSprite(path);
    }
}
