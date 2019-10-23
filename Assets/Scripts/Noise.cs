using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {
    
    /// <summary>
    /// Generates a perlin noise map.
    /// </summary>
    /// <param name="mapChunkSize">Width of the map (X).</param>
    /// <param name="mapChunkSize">Height of the map (Z).</param>
    /// <param name="seed">Custom seed for random values.</param>
    /// <param name="scale">Scale of the noise (smaller vs bigger).</param>
    /// <param name="octaves">Octave to control amount of detail of the Perlin Noise.</param>
    /// <param name="persistance">How quickly the amplitude diminishes per octave.</param>
    /// <param name="lacunarity">How quickly the frequency increases per octave.</param>
    /// <param name = "offset">Custom offset for scrolling through noise.</param>
    /// <returns>Float[] NoiseMap</returns>
    public static float [,] GenerateNoiseMap(int width, int height,  int seed, float scale, int octaves, 
                                                float persistance, float lacunarity, Vector2 offset) {
        
        float [,] noiseMap = new float[width,height];
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        // Psuedo Random Number Generator
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++) {
            // Generate random offsets per octave
            float offsetX = prng.Next(-100000,100000) + offset.x;
            float offsetY = prng.Next(-100000,100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if(scale <= 0) {
            scale = 0.0001f;
        }

        // Half values for scaling (zooming) from the center
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // Loop through all "points" of our map
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                // Loop through different "octaves", remember Mountain vs Hill vs Stone
                // The greater the octave, the less effect it has on the overall noise
                for (int i = 0; i < octaves; i++) { 
                    // Frequency used for number of cycles per unit length
                    // Multiply values by frequency, will "flatten/exaggerate" out the noise
                    // Offset the values per octave for more randomness
                    //  - Added halfHeight and halfWidth for scaling off the center
                    float sampleX = ((x - halfHeight) / scale) * frequency + octaveOffsets[i].x;
                    float sampleY = ((y - halfWidth) / scale) * frequency + octaveOffsets[i].y;

                    // Perlin generates between 0 ~ 1
                    // But lets generate a perlin value between -1 ~ 1 to add more 'height'
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    // Multiply by the amplitude, effecting the overall maximum height
                    noiseHeight += perlinValue * amplitude;
                    // Range 0 - 1
                    amplitude *= persistance;
                    // Range increases 
                    frequency *= lacunarity;
                }
                if (noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                } else if (noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                // Normalize the noiseMap using InverseLerp
                // InverseLerp returns a value between 0 ~ 1
                // If noiseMap == minNoiseHeight it will return 0
                // If noiseMap == maxNoiseHeight, it will return 1
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x,y]);
            }
        }


        return noiseMap;
    }

}
