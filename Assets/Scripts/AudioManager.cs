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

        double sample = AudioSettings.dspTime * sampleRate;
        int dataLen = data.Length / channels;
        List<SampleSequence> cache = new(sequences);

        for (int i = 0; i < dataLen; i++)
        {
            for (int c = 0; c < channels; c++) 
            {
                int s = i * channels + c;
                // int n = (i + 1) * channels + c;
                data[s] = 0f;
                foreach (SampleSequence sequence in cache) 
                {
                    Color color = Services.textureManager.flat[sequence.Read()];
                    data[s] += ColorToSample(color) * gain;
                }
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
}
