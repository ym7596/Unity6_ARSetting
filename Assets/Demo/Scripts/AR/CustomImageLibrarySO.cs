using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomImageLibrarySO", menuName = "XRData/CustomImageLibrarySO", order = 1)]
public class CustomImageLibrarySO : ScriptableObject
{
    public float width = 1f;
    
    public List<CustomData> _customDatas;

    public Dictionary<string, CustomData> dicTrackingData = new Dictionary<string, CustomData>();

    public void Init()
    {
        foreach (CustomData i in _customDatas)
        {
            dicTrackingData.Add(i.id,i);
        }
    }

    public List<Texture2D> GetImages()
    {
        List<Texture2D> imgs = new List<Texture2D>();
        
        foreach (KeyValuePair<string, CustomData> data in dicTrackingData)
            imgs.Add(data.Value.texture);

        return imgs;
    }
}
