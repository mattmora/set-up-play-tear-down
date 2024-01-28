using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class SampleSequence : MonoBehaviour
{
    private int[] positions;
    public LineRenderer line;

    public int readIndex;

    public int Length {
        get => positions.Length;
    }

    private void Awake() 
    {
        Services.audioManager.sequences.Add(this);
    }

    public void Initialize(int size) 
    {
        line = GetComponent<LineRenderer>();
        positions = new int[size];
        line.positionCount = size;
        readIndex = 0;
    }

    public int Read()
    {
        readIndex %= positions.Length;
        return positions[readIndex++];
    }

    public int this[int key]
    {
        get => positions[key];
        set => positions[key] = value;
    }
}
