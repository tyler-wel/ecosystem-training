﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
    
    public enum DrawMode {
        NoiseMap,
        ColorMap,
        Mesh,
        FalloffMap
    };

    public const int mapChunkSize = 120;
    // 1, 2, 4, 6, 8, 10, 12
    [Range(0,6)] // multiply by 2 to get 4, 6, 8, 10, 12
    public int editorLOD;

    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public int seed;
    public Vector2 offset;

    public bool useFalloff;
    public bool autoUpdate;

    public TerrainType[] regions;

    public float[,] falloffMap; 

    Queue<MapThreadInfo<MapData>> mapDataTheradInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataTheradInfoQueue = new Queue<MapThreadInfo<MeshData>>();


    void Awake() {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    /// <summary>
    /// Draw the generated map in the editor
    /// </summary>
    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        } else if (drawMode == DrawMode.ColorMap) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        } else if (drawMode == DrawMode.Mesh) {
            // Draw mesh takes the mesh data and texture data
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorLOD), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        } else if (drawMode == DrawMode.FalloffMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    /// <summary>
    /// Start a new thread to generate map data
    /// </summary>
    /// <param name="center"></param>
    /// <param name="callback">Function to be called when thread finishes generating map data</param>
    public void RequestMapData(Vector2 center, Action<MapData> callback) {
        // Start new thread with the delegate action
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }
    
    /// <summary>
    /// Generate map data on a worker thread
    /// </summary>
    /// <param name="center"></param>
    /// <param name="callback"></param>
    private void MapDataThread(Vector2 center, Action<MapData> callback) {
        // Generate new map data
        MapData mapData = GenerateMapData(center);
        // Create a lock to stop race conditions
        lock (mapDataTheradInfoQueue) {
            // Enqueue the mapdata to be rendered
            mapDataTheradInfoQueue.Enqueue(new MapThreadInfo<MapData> (callback, mapData));
        }
    }

    /// <summary>
    /// Start a new thread to generate mesh data
    /// </summary>
    /// <param name="mapData">Map data object</param>
    /// <param name="levelOfDetail">LOD for mesh</param>
    /// <param name="callback">Function to be called when thread finishes generating mesh data</param>
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        // Start new thread with the delegate action
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }
    
    /// <summary>
    /// Generate mesh data on a worker thread
    /// </summary>
    /// <param name="mapData">Map data object</param>
    /// <param name="levelOfDetail">LOD for mesh</param>
    /// <param name="callback">Function to be called when thread finishes generating mesh data</param>
    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        // Generate a terrain mesh based on previously generated mapdata and mesh parameters
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        // Use lock to stop race conditions
        lock (meshDataTheradInfoQueue) {
            // Queue the mesh to be rendered
            meshDataTheradInfoQueue.Enqueue(new MapThreadInfo<MeshData> (callback, meshData));
        }
    }
 
    private void Update() {
        if (mapDataTheradInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataTheradInfoQueue.Count; i++) {
                // If there is data in the queue, pop it off and render (using the callback function)
                MapThreadInfo<MapData> threadInfo = mapDataTheradInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataTheradInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataTheradInfoQueue.Count; i++) {
                // If there is data in the queue, pop it off and render (using the callback function)
                MapThreadInfo<MeshData> threadInfo = meshDataTheradInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    /// <summary>
    /// Generate a map with a perlin noise map.
    /// </summary>
    /// <param name="center"></param>
    /// <returns>MapData struct</returns>
    private MapData GenerateMapData(Vector2 center) {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                if (useFalloff) {
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight >= regions[i].height) {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                    } else {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colorMap);
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

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string Label;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}