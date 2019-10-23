using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    Remember that a mesh is made up of a bunch of triangles with individual vertices
    To give height to our actual plane, we can set the height of individual vertices of the triangles.
 */
public static class MeshGenerator {
    
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {
        // We create a new animation curve so each thread will have its own animation curve object to calculate heights with
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        //     CENTERING     //
        // To find the center (for centering the mesh) we must have the center x be zero as below
        //   which we calculate from below
        //  X X X
        // -1 0 1
        // x = (w-1) / -2
        // width of 3, (3 - 1) / -2 = -1, so we must remember to subtract 1 from the width to get the center value
        // X X X X X
        // width of 5, (5 - 1) / -2 = -2, so we must subtract 2 from the width to get the center, 
        // AKA this is finding how to offset everything so its centered
        // X is positive to the right
        float topLeftX = (width - 1) / -2f;
        // Z value is positive going down
        float topLeftZ = (height - 1) / 2f;
        // 0 1 2
        // 3 4 5 
        // 6 7 8


        //     VERTEX RESOLUTION     //
        // vertex resolution, skip certain values in loop or add more to add extra veticies
        // resolution i must be a factor of (w-1), using an example of w = 9, i = 2, we know we should hit 5 vertices
        // Using this above we come to the formula vertex = (w-1)/i + 1
        // Lets also make a fixed square size for mesh chunks
        // When i = 1, v = w * h, v = w^2
        // Unity has a limit of vertices of 65025, and we know
        // w <= 255, so lets do w = 241,  because i must be a factor of w-1 and w-1 = 240, so i = 2, 4, 6, 8, 10, 12
        int meshSimplificationIncrement = levelOfDetail == 0 ? 1 : (levelOfDetail * 2);
        // (w-1)/i + 1 as seen in the vertex resolution notes
        int verticesPerLine = ( (width - 1) / meshSimplificationIncrement ) + 1;

        // Our mesh data is now only squares, so w = h
        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        

        // meshSimplificationIncrement is the 'levelOfDetail' adjustment, skipping steps depending on resolution
        for (int y = 0; y < height; y += meshSimplificationIncrement) {
            for (int x = 0; x < width; x += meshSimplificationIncrement) {

                // For all coordinates x and y, create a new vector 3 vertex with height from our generated height map
                // See note above for centering map so 0 is in the center instead of top left
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x,y]) * heightMultiplier, topLeftZ - y);

                //     HEIGHT CURVE (ANIMATION CURVE)     //
                // Height Curve - maps specific X values to the Y value animation curve
                // evaluate will take the X value from heightMap and map it to the correct Y value from the curve

                //     UV     //
                // Tell each vertex where it is in relation to the rest of the map, as a percentage of X and Y axis
                // Percent between 0 ~ 1
                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                
                //     VERTEX MAPPING TO 1-DIMENSIONAL ARRAY     //
                // Notive below the first row is just normal i, i+1
                // The following row is i+w and so on
                // So the vertices for the first triangle is (i, i+w+1, i+w)
                // And the vertices for the second triangle is (i+w+1, i, i+1)
                // i     i+1
                //
                // i+w   i+w+1

                // We don't not need to worry about setting heights for triangles on far right or bottom edge
                if (x < width - 1 && y < height - 1) { 
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        // In unity can't return a mesh from within a thread, so we return the meshdata to then later create mesh from
        return meshData;

    }
}

public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight) {
        // Array of all the vertices for the mesh (remember mesh are made of up triangles)
        // Length (number of all vertices) is mapChunkSize * mapChunkSize
        vertices = new Vector3[meshWidth * meshHeight];
        // Length (number of all triangles) is :
        // (number of squares) * (number of vertices per square, aka 3 * 2) or
        //(width - 1) * (height - 1) * 6 
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];

        // UVs are needed for wrapping 2D image around a 3D mesh
        uvs = new Vector2[meshWidth * meshHeight];

        triangleIndex = 0;
    }

    public void AddTriangle(int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;

        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}