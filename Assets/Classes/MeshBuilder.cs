﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder {

    static float wallThickness = 0.2f;
    static float wallHeight = 3f;

    public static Mesh GenerateFloorMesh(FloorGrid floorGrid) {

        float floorThickness = 0.13f;

        //Get how many Materials this grid has
        int[] materialIDS = floorGrid.GetUsedMaterialsIds();
        Mesh floorMesh = new Mesh(); floorMesh.name = "floor_mesh";
        floorMesh.subMeshCount = materialIDS.Length;

        //Initialize the Lists
        List<Vector3> vertex = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        int lastVertex = -1;

        //Every Material in this Mesh has to be one Submesh. Do the whole thing for every Material
        for (int m = 0; m < materialIDS.Length; m++) {

            //Reset triangle List because it is another SubMesh
            triangles = new List<int>();

            //Iterate throught the Grid
            for (int x = 0; x < floorGrid.GetLength(0); x++) {
                for (int z = 0; z < floorGrid.GetLength(1); z++) {

                    //Obtain the FloorInfo of this cell
                    FloorInfo fi = floorGrid[x, z];

                    //IF the cell has the material that we are currently building...
                    if(fi.materialId == materialIDS[m]) {
                        if (fi.present) {

                            //Añadir los vertices
                            vertex.Add(new Vector3(x, fi.height, z));          //Bottom Left
                            vertex.Add(new Vector3(x, fi.height, z + 1));      //Top Left
                            vertex.Add(new Vector3(x + 1f, fi.height, z));     //Bottom Right
                            vertex.Add(new Vector3(x + 1, fi.height, z + 1));  //Top Right

                            //Calcular los indices de los vertices para construir los tris
                            int bottomLeft = lastVertex + 1;
                            int topLeft = lastVertex + 2;
                            int bottomRight = lastVertex + 3;
                            int topRight = lastVertex + 4;

                            //Construir los dos triangulos usando los vertices en sentido de las agujas del reloj
                            triangles.Add(bottomLeft); triangles.Add(topLeft); triangles.Add(bottomRight);
                            triangles.Add(bottomRight); triangles.Add(topLeft); triangles.Add(topRight);
                            lastVertex += 4;

                            //Añadir las normales hacia arriba
                            normals.Add(Vector3.up); normals.Add(Vector3.up);
                            normals.Add(Vector3.up); normals.Add(Vector3.up);

                            //Añadir los mapas UV para las texturas
                            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, 1));
                            uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1));

                            bool needsThickness = false; Vector3 thickNormal = new Vector3();
                            Vector3 thickStart = new Vector3(); Vector3 thickEnd = new Vector3();

                            //Determinar si es necesario añadir grosor, y de donde a donde
                            for (int i = 0; i < 4; i++) {

                                needsThickness = false;

                                if (i == 0 && (z == 0 || floorGrid[x, z - 1].present == false)) {
                                    needsThickness = true;
                                    thickStart = new Vector3(x, fi.height, z);
                                    thickEnd = new Vector3(x + 1, fi.height, z);
                                    thickNormal = -Vector3.forward;
                                }
                                if (i == 1 && (x == floorGrid.GetLength(0) - 1 || floorGrid[x + 1, z].present == false)) {
                                    needsThickness = true;
                                    thickStart = new Vector3(x + 1, fi.height, z);
                                    thickEnd = new Vector3(x + 1, fi.height, z + 1);
                                    thickNormal = Vector3.right;
                                }

                                //Añadir efecto de grosor de ser necesario
                                if (needsThickness) {
                                    vertex.Add(thickStart);                                //Top Left
                                    vertex.Add(thickStart + Vector3.down * floorThickness);//Bottom Left
                                    vertex.Add(thickEnd);                                  //Top Right
                                    vertex.Add(thickEnd + Vector3.down * floorThickness);  //Bottom Right
                                    topLeft = lastVertex + 1;
                                    bottomLeft = lastVertex + 2;
                                    topRight = lastVertex + 3;
                                    bottomRight = lastVertex + 4;
                                    triangles.Add(bottomLeft); triangles.Add(topLeft); triangles.Add(bottomRight);
                                    triangles.Add(bottomRight); triangles.Add(topLeft); triangles.Add(topRight);
                                    lastVertex += 4;
                                    normals.Add(thickNormal); normals.Add(thickNormal);
                                    normals.Add(thickNormal); normals.Add(thickNormal);
                                    uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(0f, 1f - floorThickness));
                                    uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(1, 1f - floorThickness));
                                }
                            }

                            
                        }
                    }
                }
            }

            //Debug.Log("V:" + vertex.Count + ", T:" + triangles.Count + ", N:" + normals.Count + ", UV:" + uvs.Count);

            floorMesh.vertices = vertex.ToArray();
            floorMesh.SetTriangles(triangles.ToArray(), m);
            floorMesh.normals = normals.ToArray();
            floorMesh.uv = uvs.ToArray();
        }

        return floorMesh;
    }

    public static Mesh GenerateWallMesh(WallGrid wallGrid) {

        //Get how many Materials this grid has
        //int[] materialIDS = wallGrid.GetUsedMaterialsIds();
        Mesh wallMesh = new Mesh(); wallMesh.name = "wall_mesh";
        //wallMesh.subMeshCount = materialIDS.Length;

        //Initialize the Lists
        /*List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();*/

        //int lastVertex = -1;

        //Reset triangle List because it is another SubMesh
        //triangles = new List<int>();

        List<CombineInstance> meshesToCombine = new List<CombineInstance>();

        //Iterate throught the Grid
        for (int x = 0; x < wallGrid.GetLength(0); x++) {
            for (int z = 0; z < wallGrid.GetLength(1); z++) {

                //Obtain the WallNode of this cell
                WallNode wn = wallGrid[x, z];

                //IF connected to North
                if (wn.connectedNorth) {

                    //North Inner Wall
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = GetSidePlane(
                        new Vector3(x, 0f, z) + Vector3.right * wallThickness * 0.5f,
                        new Vector3(x, 0f, z) + Vector3.right * wallThickness * 0.5f + Vector3.forward * 1f + Vector3.up * wallHeight,
                        Vector3.right, 0
                        );
                    meshesToCombine.Add(ci);

                    //North Outer Wall
                    /*ci = new CombineInstance();
                    ci.mesh = GetSidePlane(
                        new Vector3(x, 0f, z) + -Vector3.right * wallThickness * 0.5f + Vector3.forward * 1f,
                        new Vector3(x, 0f, z) + -Vector3.right * wallThickness * 0.5f + Vector3.up * wallHeight,
                        -Vector3.right, 0
                        );
                    meshesToCombine.Add(ci);*/

                    //Add start border if needed
                    if(z == 0 || wallGrid[x, z - 1].connectedNorth == false) {
                        ci = new CombineInstance();
                        ci.mesh = GetSidePlane(
                            new Vector3(x, 0f, z) + -Vector3.right * wallThickness * 0.5f,
                            new Vector3(x, 0f, z) + Vector3.right * wallThickness * 0.5f + Vector3.up * wallHeight,
                            -Vector3.forward, 0
                            );
                        meshesToCombine.Add(ci);
                    }

                }

                //IF connected to East
                if (wn.connectedEast) {

                    //East Outer Wall
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = GetSidePlane(
                        new Vector3(x, 0f, z) + -Vector3.forward * wallThickness * 0.5f,
                        new Vector3(x, 0f, z) + -Vector3.forward * wallThickness * 0.5f + Vector3.right * 1f + Vector3.up * wallHeight,
                        -Vector3.forward, 0
                        );
                    meshesToCombine.Add(ci);

                    //East Inner Wall
                    /*ci = new CombineInstance();
                    ci.mesh = GetSidePlane(
                        new Vector3(x, 0f, z) + -Vector3.right * wallThickness * 0.5f + Vector3.forward * 1f,
                        new Vector3(x, 0f, z) + -Vector3.right * wallThickness * 0.5f + Vector3.up * wallHeight,
                        -Vector3.right, 0
                        );
                    meshesToCombine.Add(ci);*/

                    //Add end border if needed
                    if (x == wallGrid.GetLength(0) - 1 || wallGrid[x + 1, z].connectedEast == false) {
                        ci = new CombineInstance();
                        ci.mesh = GetSidePlane(
                            new Vector3(x, 0f, z) + -Vector3.forward * wallThickness * 0.5f + Vector3.right * 1f,
                            new Vector3(x, 0f, z) + -Vector3.forward * wallThickness * 0.5f + Vector3.right * 1f + Vector3.up * wallHeight + Vector3.forward * wallThickness,
                            Vector3.right, 0
                            );
                        meshesToCombine.Add(ci);
                    }
                }
            }
        }

        //Debug.Log("V:" + vertex.Count + ", T:" + triangles.Count + ", N:" + normals.Count + ", UV:" + uvs.Count);

        /*wallMesh.vertices = vertices.ToArray();
        wallMesh.SetTriangles(triangles.ToArray(), 0);
        wallMesh.normals = normals.ToArray();
        wallMesh.uv = uvs.ToArray();*/

        //Debug.Log(meshesToCombine.Count);
        wallMesh.CombineMeshes(meshesToCombine.ToArray(), true, false, false);

        return wallMesh;
    }

    static Mesh GetPart(Part partType, Vector3 start, int subMeshIndex) {

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        int lastVertex = -1;

        Mesh part = new Mesh();

        switch (partType) {
            case Part.WallNorth:

                //Armar el primer cuadro
                vertices.Add(start);
                vertices.Add(start + -Vector3.right * wallThickness * 0.5f); //Bottom Left
                vertices.Add(vertices[vertices.Count - 1] + Vector3.up * wallHeight); //Top Left
                vertices.Add(vertices[vertices.Count - 1] + Vector3.right * wallThickness); //Top Right
                vertices.Add(vertices[vertices.Count - 1] + Vector3.down * wallHeight); //Bottom Left
                break;
        }

        return part;

    }

    static Mesh GetSidePlane(Vector3 bottomLeftPos, Vector3 topRightPos, Vector3 normal, int subMesh) {
        Mesh planeMesh = new Mesh();

        //Initialize the Lists
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        int lastVertex = -1;

        //Armar el primer cuadro
        vertices.Add(bottomLeftPos); //Bottom Left
        vertices.Add(bottomLeftPos + Vector3.up * (topRightPos - bottomLeftPos).y); //Top Left
        vertices.Add(topRightPos); //Top Right
        vertices.Add(topRightPos + Vector3.down * (topRightPos - bottomLeftPos).y); //Bottom Right

        //Calcular los indices de los vertices para construir los tris
        int bottomLeft = lastVertex + 1;
        int topLeft = lastVertex + 2;
        int topRight = lastVertex + 3;
        int bottomRight = lastVertex + 4;

        //Construir los dos triangulos usando los vertices en sentido de las agujas del reloj
        triangles.Add(bottomLeft); triangles.Add(topLeft); triangles.Add(bottomRight);
        triangles.Add(bottomRight); triangles.Add(topLeft); triangles.Add(topRight);
        lastVertex += 4;

        //Añadir las normales
        normals.Add(normal); normals.Add(normal);
        normals.Add(normal); normals.Add(normal);

        //Añadir los mapas UV para las texturas
        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(1, 0));

        planeMesh.vertices = vertices.ToArray();
        planeMesh.SetTriangles(triangles.ToArray(), 0);
        planeMesh.normals = normals.ToArray();
        planeMesh.uv = uvs.ToArray();

        return planeMesh;
    }

    public enum Part { FloorTop, FloorTicknes, WallNorth, WallEast, WallIntersection}
}