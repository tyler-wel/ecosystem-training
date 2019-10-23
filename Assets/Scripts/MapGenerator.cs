using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

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

    Queue<MapThreadInfo<MapData>> mapDataTheradInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataTheradInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    /// <summary>
    /// Draw the generated map in the editor
    /// </summary>
    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData();
        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        } else if (drawMode == DrawMode.ColorMap) {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        } else if (drawMode == DrawMode.Mesh) {
            // Draw mesh takes the mesh data and texture data
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
    }

    /// <summary>
    /// Start a new thread to generate map data
    /// </summary>
    /// <param name="callback">Function to be called when thread finishes generating map data</param>
    public void RequestMapData(Action<MapData> callback) {
        // Start new thread with the delegate action
        ThreadStart threadStart = delegate {
            MapDataThread(callback);
        };

        new Thread(threadStart).Start();
    }
    
    /// <summary>
    /// Generate map data on a worker thread
    /// </summary>
    /// <param name="callback"></param>
    private void MapDataThread(Action<MapData> callback) {
        // Generate new map data
        MapData mapData = GenerateMapData();
        // Create a lock to stop race conditions
        lock (mapDataTheradInfoQueue) {
            // Enqueue the mapdata to be rendered
            mapDataTheradInfoQueue.Enqueue(new MapThreadInfo<MapData> (callback, mapData));
        }
    }

    /// <summary>
    /// Start a new thread to generate mesh data
    /// </summary>
    /// <param name="callback">Function to be called when thread finishes generating mesh data</param>
    public void RequestMeshData(MapData mapData, Action<MeshData> callback) {
        // Start new thread with the delegate action
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, callback);
        };

        new Thread(threadStart).Start();
    }
    
    /// <summary>
    /// Generate mesh data on a worker thread
    /// </summary>
    /// <param name="callback">Callback function to execute after thread finishes</param>
    private void MeshDataThread(MapData mapData, Action<MeshData> callback) {
        // Generate a terrain mesh based on previously generated mapdata and mesh parameters
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
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
    /// <returns>MapData struct</returns>
    private MapData GenerateMapData() {
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