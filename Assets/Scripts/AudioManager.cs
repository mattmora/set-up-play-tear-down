using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public float gain = 0.5F;

    public int canvasDownsample = 2;
    public int playersDownsample = 8;

    private float previous = 0f;

    private bool running = false;

    public List<SampleSequence> sequences;

    private int canvasProgress;
    private int playersProgress;
    private HashSet<int> completed;

    private float mute = 1f;

    private void Awake() 
    {
        Services.audioManager = this;
        sequences = new();
    }

    // Start is called before the first frame update
    void Start()
    {
        running = true;
        completed = new();
    }

    private void Update() 
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            mute = 1f - mute;
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;

        int dataLen = data.Length / channels;
        List<SampleSequence> cache = new(sequences);

        var texture = Services.textureManager;
        if (texture == null) return;

        var canvasBlob = Services.textureManager.canvasBlob.ToArray();
        var playersBlob = Services.textureManager.playersBlob.ToArray();
        for (int i = 0; i < dataLen; i++)
        {
            float sample = 0f;

            if (canvasBlob.Length > 0)
            {
                canvasProgress %= canvasBlob.Length;
                int canvasIndex = canvasBlob[canvasProgress++];
                Color playerPixel = texture.playersPixels[canvasIndex];
                float s = playerPixel.a > 0.8f ? texture.playersSamples[canvasIndex] : texture.canvasSamples[canvasIndex];
                sample += s * 0.5f;
            }

            if (playersBlob.Length > 0)
            {
                playersProgress %= playersBlob.Length * playersDownsample;
                int playersIndex = playersBlob[playersProgress / playersDownsample];
                playersProgress++;
                sample += texture.playersSamples[playersIndex] * 0.5f;
            }

            sample = MathF.Tanh(sample * gain * mute); 
            for (int c = 0; c < channels; c++) 
            {
                int s = i * channels + c;
                data[s] = sample * 0.5f + previous * 0.5f;
            }
            previous = sample;
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
        Color.RGBToHSV(c, out float H, out float S, out float V);
        return Mathf.Cos(H * 2 * Mathf.PI) * S;
        // float dcOffset = Mathf.Min(c.r, c.g, c.b);
        // c.r -= dcOffset;
        // c.g -= dcOffset;
        // c.b -= dcOffset;
        // float amplitude = c.r + c.g + c.b;
        // if (amplitude == 0) return 0f;
        // Color norm = c / amplitude;
        // float phase = (norm.r * (norm.g == 0 ? 3f : 0f) + norm.g + norm.b * 2f) * PHASE_PART;
        // return Mathf.Sin(phase) * amplitude * c.a;
    }   
}
