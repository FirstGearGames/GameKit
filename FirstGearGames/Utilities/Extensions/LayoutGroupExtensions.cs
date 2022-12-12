using UnityEngine;
using UnityEngine.UI;

public static class LayoutGroupExtensions
{    
    /// <summary>
    /// Returns how many entries can fit into a GridLayoutGroup
    /// </summary>
    /// <param name=""></param>
    public static int EntriesPerWidth(this GridLayoutGroup lg)
    {
        RectTransform rectTransform = lg.GetComponent<RectTransform>();
        return Mathf.CeilToInt(rectTransform.rect.width / lg.cellSize.x);
    }

}
