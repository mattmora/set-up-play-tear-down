using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample
{
    public Vector2Int position;
    public Color color;
    public Sample next;
    public Sample up;
    public Sample down;
    public Sample left;
    public Sample right;

    public Sample(Vector2Int position, Color color)
    {
        this.position = position;
        this.color = color;
    }
}
