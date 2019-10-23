﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
    
    public enum DrawMode {
        NoiseMap,
        ColorMap,
        Mesh
    };

    public const int mapChunkSize = 241;
    // 1, 2, 4, 6, 8, 10, 12
    [Range(0,6)] // multiply by 2 to get 4, 6, 8, 10, 12
    public int levelOfDetail;

    public DrawMode drawMode;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public int seed;
    public Vector2 offset;
    public bool autoUpdate;

    public TerrainType[] regions;

    /// <summary>
    /// Generate a map with a perlin noise map.
    /// </summary>
    public void GenerateMap() {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);


        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight <= regions[i].height) {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        } else if (drawMode == DrawMode.ColorMap) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        } else if (drawMode == DrawMode.Mesh) {
            // Draw mesh takes the mesh data and texture data
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }
    }

    /// <summary>
    /// On validate is called whenever a value is changed in the editor.
    /// For validation reasoning, please check http://libnoise.sourceforge.net/glossary/#lacunarity
    /// </summary>
    void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }

        if (octaves < 0) {
            octaves = 1;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string Label;
    public float height;
    public Color color;
}
