using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public float gain = 0.5F;

    private float amp = 0.0F;
    private float phase = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false;

    public List<SampleSequence> sequences;

    private Dictionary<int, int> progress;
    private HashSet<int> completed;

    private void Awake() 
    {
        Services.audioManager = this;
        sequences = new();
    }

    // Start is called before the first frame update
    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        running = true;
        progress = new();
        completed = new();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;

        if (!NetworkManager.Singleton.IsServer) return;

        int dataLen = data.Length / channels;
        List<SampleSequence> cache = new(sequences);

        var texture = Services.textureManager;
        if (texture == null) return;

        // int xReturn = 0;
        // for (int y = 0; y < texture.height; y++)
        // {
        //     for (int x = 0; x < texture.width; x++)
        //     {
        //         int i = x + y * texture.width;
        //         if (completed.Contains(i)) continue;
        //         completed.Add(i);
        //         Color c = texture.GetPixel(x, y);
        //         if (c.a == 0) continue;
        //         int xAnchor = x;
        //     }
        // }

        // Determine applicable progress
        List<List<int>> blobs = new();
        HashSet<int> firstPixels = new();
        int[] p = new int[texture.blobs.Count];
        for (int b = 0; b < texture.blobs.Count; b++)
        {
            blobs.Add(new(texture.blobs[b]));
            var blob = blobs[b];
            firstPixels.Add(blob[0]);
            int indexKey = -b - 1;
            // Check progress by first pixel first
            if (!progress.TryGetValue(blob[0], out int p1))
            {
                // Then check by blob index
                if (!progress.TryGetValue(indexKey, out int p2))
                {
                    p2 = 0;
                    progress[indexKey] = p2;
                }
                p1 = p2;
                progress[blob[0]] = p1;
            }
            p[b] = p1;
        }

        for (int i = 0; i < dataLen; i++)
        {
            float sample = 0f;
            for (int b = 0; b < blobs.Count; b++)
            {
                var blob = blobs[b];
                int indexKey = -b - 1;
                // Color color = texture.pixels[blob[p % blob.Count]];
                // sample += ColorToSample(color) * gain;
                int index = blob[p[b] % blob.Count];
                sample += texture.samples[index] * gain;
                int next = (p[b] + 1) % blob.Count;
                progress[blob[0]] = next;
                progress[indexKey] = next;
                p[b] = next;
            }

            sample = MathF.Tanh(sample);
            for (int c = 0; c < channels; c++) 
            {
                int s = i * channels + c;
                data[s] = sample;
            }
        }

        var keys = progress.Keys.ToArray();
        foreach (var key in keys)
        {
            if (!firstPixels.Contains(key) && key >= 0)
            {
                progress.Remove(key);
            }
        }
    }

    const float PHASE_PART = 2f * Mathf.PI / 3f;
    
    public static Color PhaseAmpToColor(float phase, float amplitude)
    {
        phase = Mathf.Repeat(phase, 2f * Mathf.PI);
        float f = phase / PHASE_PART;
        int i = Mathf.FloorToInt(f);
        f -= i;
        Color c = Color.black;
        c[i] = (1 - f) * amplitude;
        c[(i+1) % 3] = f * amplitude;
        return c;
        // Debug.Log(phase);
        // return Color.HSVToRGB(phase / (2 * Mathf.PI), amplitude, 1f);
    }

    public static float ColorToSample(Color c) 
    {
        if (c.a == 0) return 0f;
        // Color.RGBToHSV(c, out float H, out float S, out float V);
        // return Mathf.Sin(H * 2 * Mathf.PI) * S;
        float dcOffset = Mathf.Min(c.r, c.g, c.b);
        c.r -= dcOffset;
        c.g -= dcOffset;
        c.b -= dcOffset;
        float amplitude = c.r + c.g + c.b;
        if (amplitude == 0) return 0f;
        Color norm = c / amplitude;
        float phase = (norm.r * (norm.g == 0 ? 3f : 0f) + norm.g + norm.b * 2f) * PHASE_PART;
        return Mathf.Sin(phase) * amplitude * c.a;
    }   
}
