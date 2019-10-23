﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
    
    public const float maxViewDistance = 450;
    public Transform viewer;

    public static Vector2 viewerPosition;

    int chunkSize;
    int chunksVisibleInView;
    
    // Dictionary for holding all the rendered chunks
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    // List of chunks that were rendered last frame
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start() {
        // Terrain chunks end up being 240x240
        // If we center our coordinates at 0,0, gotta be carful with indexing
        chunkSize = MapGenerator.mapChunkSize - 1;
        // Chunks visible in the player's view
        chunksVisibleInView = Mathf.RoundToInt(maxViewDistance / chunkSize); 
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        updateVisibleChunks();
    }

    /// <summary>
    /// 
    /// </summary>
    void updateVisibleChunks() {

        // If chunk was rendered in the last frame, disable it
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        // Clear the list for this frame
        terrainChunksVisibleLastUpdate.Clear();

        // These values will end up being, say (1,0) one chunk to the right
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // For each chunk
        for(int yOffSet = -chunksVisibleInView; yOffSet <= chunksVisibleInView; yOffSet++) {
            for(int xOffSet = -chunksVisibleInView; xOffSet <= chunksVisibleInView; xOffSet++) {
                Vector2 chunkCoord = new Vector2(currentChunkCoordX + xOffSet, currentChunkCoordY + yOffSet);

                if (terrainChunkDictionary.ContainsKey(chunkCoord)) {
                    terrainChunkDictionary[chunkCoord].UpdateTerrainChunk();
                    // If the current chunk is visible, add it to the visible chunk list
                    if (terrainChunkDictionary[chunkCoord].IsVisible()) {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[chunkCoord]);
                    }
                } else {
                    terrainChunkDictionary.Add(chunkCoord, new TerrainChunk(chunkCoord, chunkSize, transform));
                }
            }
        }  
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        // Bounding Box
        Bounds bounds;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="size"></param>
        /// <param name="parent"></param>
        public TerrainChunk(Vector2 coord, int size, Transform parent) {
            // This takes our coord system in increments of 1 and translates it to world space chunk size
            //  aka, (1,2) = (240, 480)
            position = coord * size;
            // Setup the bounding box to surround this plane
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            // Primite Plane is default 10 scale, so divide by 10 to get the correct value
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            // Default state is invisible
            SetVisible(false);
        }

        /// <summary>
        /// Find the point on the perimeter that is the closest to viewer position, find distance between that point and viewer
        ///   if that distance is less than max view dist, make sure mesh is enabled
        ///   if that distance is greater than max view, disable the mesh
        /// </summary>
        public void UpdateTerrainChunk() {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
            SetVisible(visible);
        }

        /// <summary>
        /// Sets the visibility of the terrain chunk
        /// </summary>
        /// <param name="visible">Is chunk visible?</param>
        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        /// <summary>
        /// Is current mesh (plane) active?
        /// </summary>
        /// <returns>boolean</returns>
        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }
}
