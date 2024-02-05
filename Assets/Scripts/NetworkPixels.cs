using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class NetworkPixels : NetworkVariableBase
{
    /// Managed list of class instances
    public Color[] pixels;

    public NetworkPixels(int size, NetworkBehaviour behaviour)
    {
        pixels = new Color[size];
        Initialize(behaviour);
    }

    /// <summary>
    /// Writes the complete state of the variable to the writer
    /// </summary>
    /// <param name="writer">The stream to write the state to</param>
    public override void WriteField(FastBufferWriter writer)
    {
        // Serialize the data we need to synchronize
        writer.WriteValueSafe(pixels.Length);
        foreach (var color in pixels)
        {
            writer.WriteValueSafe(color);
        }
    }

    /// <summary>
    /// Reads the complete state from the reader and applies it
    /// </summary>
    /// <param name="reader">The stream to read the state from</param>
    public override void ReadField(FastBufferReader reader)
    {
        // De-Serialize the data being synchronized
        var itemsToUpdate = (int)0;
        reader.ReadValueSafe(out itemsToUpdate);
        for (int i = 0; i < itemsToUpdate; i++)
        {
            reader.ReadValueSafe(out Color color);
            pixels[i] = color;
        }
    }

    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
        // Don'thing for this example
    }

    public override void WriteDelta(FastBufferWriter writer)
    {
        // Don'thing for this example
    }
}    
