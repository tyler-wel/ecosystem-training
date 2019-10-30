using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    private Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisibleInView;

    // Dictionary for holding all the rendered chunks
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    // List of chunks that were rendered last frame
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDistance = detailLevels[detailLevels.Length - 1].lodRange;
        // Terrain chunks end up being 240x240
        // If we center our coordinates at 0,0, gotta be carful with indexing
        chunkSize = MapGenerator.mapChunkSize - 1;
        // Chunks visible in the player's view
        chunksVisibleInView = Mathf.RoundToInt(maxViewDistance / chunkSize);

        // Because we're not updating every frame anymore, manually update at start
        updateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        // We will be using this to only update terrain if player has moved past a certain threshhold
        if ( (viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            updateVisibleChunks();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void updateVisibleChunks()
    {

        // If chunk was rendered in the last frame, disable it
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        // Clear the list for this frame
        terrainChunksVisibleLastUpdate.Clear();

        // These values will end up being, say (1,0) one chunk to the right
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // For each chunk
        for (int yOffSet = -chunksVisibleInView; yOffSet <= chunksVisibleInView; yOffSet++)
        {
            for (int xOffSet = -chunksVisibleInView; xOffSet <= chunksVisibleInView; xOffSet++)
            {
                Vector2 chunkCoord = new Vector2(currentChunkCoordX + xOffSet, currentChunkCoordY + yOffSet);

                if (terrainChunkDictionary.ContainsKey(chunkCoord))
                {
                    terrainChunkDictionary[chunkCoord].UpdateTerrainChunk();
                    // If the current chunk is visible, add it to the visible chunk list
                    if (terrainChunkDictionary[chunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[chunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDictionary.Add(chunkCoord, new TerrainChunk(chunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    /// <summary>
    /// This class holds data for a single terrain chunk
    /// </summary>
    public class TerrainChunk
    {

        GameObject meshObject;
        Vector2 position;
        // Bounding Box
        Bounds bounds;

        MapData mapData;
        bool mapDataReceived;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int previousLODIndex = -1;

        /// <summary>
        /// Constructor for creating a new terrain chunk
        /// </summary>
        /// <param name="coord">Coordinate of terrain chunk</param>
        /// <param name="size">Size of terrain chunk</param>
        /// <param name="parent">Parent game object</param>
        /// <param name="material"></param>
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            // This takes our coord system in increments of 1 and translates it to world space chunk size
            //  aka, (1,2) = (240, 480)
            position = coord * size;
            // Setup the bounding box to surround this plane
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3;
            // Primite Plane is default 10 scale, so divide by 10 to get the correct value
            // meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            // Default state is invisible
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        /// <summary>
        /// Callback for when map data is received from thread
        /// </summary>
        /// <param name="mapData"></param>
        void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;
            
            // Set the texture based on our colormap and chunk sizes
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            // manually update chunks when map data is received
            UpdateTerrainChunk();
        }


        /// <summary>
        /// Find the point on the perimeter that is the closest to viewer position, find distance between that point and viewer
        ///   if that distance is less than max view dist, make sure mesh is enabled
        ///   if that distance is greater than max view, disable the mesh
        /// </summary>
        public void UpdateTerrainChunk()
        {
            if (mapDataReceived) {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

                if (visible) {
                    int lodIndex = 0;
                    
                    // We don't need to look at the last level of detail because visible won't be true at furthest range
                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].lodRange) {
                            lodIndex = i + 1;
                        } else {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh) {
                            previousLODIndex = lodIndex;
                            // Change the mesh to be of different quality
                            meshFilter.mesh = lodMesh.mesh;
                        } else if (!lodMesh.hasRequestedMesh) {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }

                SetVisible(visible);
            }
        }

        /// <summary>
        /// Sets the visibility of the terrain chunk
        /// </summary>
        /// <param name="visible">Is chunk visible?</param>
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        /// <summary>
        /// Is current mesh (plane) active?
        /// </summary>
        /// <returns>boolean</returns>
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    /// <summary>
    /// This class holds data for the level of detail mesh for a terrain chuink
    /// </summary>
    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;

        // Because we're not updating every frame, lets create a callback to the update function when mesh/map data is received
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        public void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            // When mesh is received, manually call update
            updateCallback();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        } 
    }

    /// <summary>
    /// Struct for holding LOD info
    /// </summary>
    [System.Serializable]
    public struct LODInfo { 
        public int lod;
        public float lodRange;
    }
}
