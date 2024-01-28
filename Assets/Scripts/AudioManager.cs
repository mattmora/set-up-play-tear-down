using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    private void Awake() 
    {
        Services.audioManager = this;
        sequences = new();
        mergedComponentProgress = new Dictionary<long, int>();
    }

    // Start is called before the first frame update
    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        running = true;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;

        ComputeConnectedComponents();

        Dictionary<long, int> cache = new(mergedComponentProgress);
        int dataLen = data.Length / channels;
        for (int i = 0; i < dataLen; i++)
        {
            // for (int c = 0; c < channels; c++) 
            // {
            //     int s = i * channels + c;
                float sample = 0f;
                foreach (long label in cache.Keys)
                {
                    List<long> process = new();
                    int size = 0;
                    foreach (long key in components.Keys)
                    {
                        if ((label & key) > 0)
                        {
                            size += components[key].Count;
                            process.Add(key);
                        }
                    }
                    if (size == 0) continue;
                    int progress = cache[label] % size;
                    int offset = 0;
                    foreach (long key in process)
                    {
                        if (progress < components[key].Count + offset) sample += ColorToSample(components[key][progress - offset]) * gain;
                        else offset += components[key].Count;
                        components.Remove(key);
                    }
                    mergedComponentProgress[label]++;
                }
                for (int c = 0; c < channels; c++) 
                {
                    data[i * channels + c] = sample;
                }
            // }
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
        return Mathf.Sin(phase) * amplitude;
    }   

    private Dictionary<long, int> mergedComponentProgress;
    private Dictionary<long, List<Color>> components;
    private void ComputeConnectedComponents()
    {
        Dictionary<long, int> copy = new(mergedComponentProgress);
        mergedComponentProgress = new Dictionary<long, int>();
        components = new();
        Color[] arr = Services.textureManager.flat;
        int width = Services.textureManager.width;
        long[] labels = new long[arr.Length];
        long key = 1;
        for (int i = 0; i < arr.Length; i++)
        {
            Color c = arr[i];
            if (c.a == 0) continue;
            int x = i % width;
            int y = i / width;
            long left = 0;
            long up = 0;
            if (x > 0) left = labels[x - 1 + y * width];
            if (y > 0) up = labels[x + (y - 1) * width];
            if (left > 0) 
            {
                // Both are colored
                if (up > 0)
                {
                    // List<Color> merge = dict[up];
                    // merge.AddRange(dict[left]);
                    // merge.Add(c);
                    // dict[left | up] = merge;
                    // dict.Remove(left);
                    // dict.Remove(up);
                    long merge = left | up;
                    components[merge] = new List<Color>() { c };
                    mergedComponentProgress.Remove(left);
                    mergedComponentProgress.Remove(up);
                    mergedComponentProgress[merge] = 0;
                }
                // only left is colored
                else 
                {
                    components[left].Add(c);
                    labels[i] = left;
                }
            }
            // only up is colored
            else if (up > 0) 
            {
                components[up].Add(c);
                labels[i] = up;
            }
            // neither is colored
            else 
            {
                components[key] = new List<Color>() { c };
                labels[i] = key;
                mergedComponentProgress[key] = 0;
                key *= 2;
            }
            foreach (long label in copy.Keys)
            {
                if (mergedComponentProgress.ContainsKey(label))
                {
                    mergedComponentProgress[label] = copy[label];
                }
            }
        }
    }
}
