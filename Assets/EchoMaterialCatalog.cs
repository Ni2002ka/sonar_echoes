using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EchoMaterialCatalog", menuName = "Echo/Material Catalog")]
public class EchoMaterialCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public Material echoMaterial;
        public Material originalMaterial;
    }

    public Entry[] entries = Array.Empty<Entry>();

    public bool TryGetOriginal(Material echoMaterial, out Material originalMaterial)
    {
        if (echoMaterial == null)
        {
            originalMaterial = null;
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].echoMaterial == echoMaterial)
            {
                originalMaterial = entries[i].originalMaterial;
                return originalMaterial != null;
            }
        }

        originalMaterial = null;
        return false;
    }
}
