using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteManager : MonoBehaviour
{
    public List<Color> colors;

    private void Awake() {
        Services.paletteManager = this;
    }
}
