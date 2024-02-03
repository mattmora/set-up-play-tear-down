using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample
{
    public float value;
    public Color color;

    public Sample(Color color)
    {
        this.color = color;
        value = AudioManager.ColorToSample(color);
    }
}
