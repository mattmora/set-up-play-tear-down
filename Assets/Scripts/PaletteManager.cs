using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteManager : MonoBehaviour
{
    public List<Color> colors;

    private void Awake() {
        Services.paletteManager = this;
    }

    private void Start() 
    {
        HashSet<Color> unique = new();
        foreach (Color c in Services.textureManager.image.GetPixels())
        {
            unique.Add(c);
        }
        int i = 0;
        foreach (Color c in unique)
        {
            colors[i] = c;
            i++;
        }
    }
}
