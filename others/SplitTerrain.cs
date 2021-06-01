/// <summary>
/// Dvornik
/// </summary>
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

/// <summary>
/// Split terrain.
/// </summary>
public class SplitTerrain : EditorWindow
{

    List<TerrainData> terrainData = new List<TerrainData>();
    List<GameObject> terrainGo = new List<GameObject>();

    Terrain parentTerrain;

    const int terrainsCount = 16;
    const string TerrainSavePath = "Assets/MyTestTerrain/New1/";

    // Add submenu
    [MenuItem("Dvornik/Terrain/Split Terrain")]
    static void Init()
    {

        // Get existing open window or if none, make a new one:
        SplitTerrain window = (SplitTerrain)EditorWindow.GetWindow(typeof(SplitTerrain));

        window.minSize = new Vector2(100f, 100f);
        window.maxSize = new Vector2(200f, 200f);

        window.autoRepaintOnSceneChange = true;
        window.title = "Resize terrain";
        window.Show();


    }

    /// <summary>
    /// Determines whether this instance is power of two the specified x.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this instance is power of two the specified x; otherwise, <c>false</c>.
    /// </returns>
    /// <param name='x'>
    /// If set to <c>true</c> x.
    /// </param>
    bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0;
    }

    void SplitIt()
    {

        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No terrain was selected");
            return;
        }
        
        //if (Directory.Exists(TerrainSavePath)) Directory.Delete(TerrainSavePath, true);
        //Directory.CreateDirectory(TerrainSavePath);

        parentTerrain = Selection.activeGameObject.GetComponent(typeof(Terrain)) as Terrain;

        if (parentTerrain == null)
        {
            Debug.LogWarning("Current selection is not a terrain");
            return;
        }

        //parent height，splat，
        float[,] parentHeight = parentTerrain.terrainData.GetHeights(0, 0, parentTerrain.terrainData.heightmapResolution, parentTerrain.terrainData.heightmapResolution);
        float[,,] parentSplat = parentTerrain.terrainData.GetAlphamaps(0, 0, parentTerrain.terrainData.alphamapResolution, parentTerrain.terrainData.alphamapResolution);

        Vector3 parentPosition = parentTerrain.GetPosition();
        int terraPieces = (int)Mathf.Sqrt(terrainsCount);//几等分
        float spaceShiftX = parentTerrain.terrainData.size.z / terraPieces;//一份的宽
        float spaceShiftY = parentTerrain.terrainData.size.x / terraPieces;//一份的高                             
        int heightShift = parentTerrain.terrainData.heightmapResolution / terraPieces;//一份高度图的偏移
        int splatShift = parentTerrain.terrainData.alphamapResolution / terraPieces;//一份alphamap的偏移

        //Split terrain 
        for (int i = 0; i < terrainsCount; i++)
        {

            EditorUtility.DisplayProgressBar("Split terrain", "Process " + i, (float)i / terrainsCount);

            TerrainData td = new TerrainData();
            GameObject tgo = Terrain.CreateTerrainGameObject(td);

            tgo.name = parentTerrain.name + " " + i;

            terrainData.Add(td);
            terrainGo.Add(tgo);

            Terrain genTer = tgo.GetComponent(typeof(Terrain)) as Terrain;
            genTer.terrainData = td;

            

            AssetDatabase.CreateAsset(td, TerrainSavePath + genTer.name + ".asset");


            // Assign splatmaps
            genTer.terrainData.splatPrototypes = parentTerrain.terrainData.splatPrototypes;

            // Assign detail prototypes
            genTer.terrainData.detailPrototypes = parentTerrain.terrainData.detailPrototypes;

            // Assign tree information
            genTer.terrainData.treePrototypes = parentTerrain.terrainData.treePrototypes;


            // Copy parent terrain propeties
            #region parent properties
            genTer.basemapDistance = parentTerrain.basemapDistance;
            genTer.shadowCastingMode = parentTerrain.shadowCastingMode;
            genTer.detailObjectDensity = parentTerrain.detailObjectDensity;
            genTer.detailObjectDistance = parentTerrain.detailObjectDistance;
            genTer.heightmapMaximumLOD = parentTerrain.heightmapMaximumLOD;
            genTer.heightmapPixelError = parentTerrain.heightmapPixelError;
            genTer.treeBillboardDistance = parentTerrain.treeBillboardDistance;
            genTer.treeCrossFadeLength = parentTerrain.treeCrossFadeLength;
            genTer.treeDistance = parentTerrain.treeDistance;
            genTer.treeMaximumFullLODCount = parentTerrain.treeMaximumFullLODCount;

            #endregion

            //Start processing it			
            // Translate piece to position
            #region translate piece to right position 
            Debug.Log("Translate piece");

            //每份的原点偏移
            int Xshift = (i % terraPieces);
            int Yshift = (i / terraPieces);
            float xWShift = Xshift * spaceShiftX;
            float zWShift = Yshift * spaceShiftY;
            tgo.transform.position = new Vector3(tgo.transform.position.x + zWShift,
                                                  tgo.transform.position.y,
                                                  tgo.transform.position.z + xWShift);
            //每份的世界坐标
            tgo.transform.position = new Vector3(tgo.transform.position.x + parentPosition.x,
                                                  tgo.transform.position.y + parentPosition.y,
                                                  tgo.transform.position.z + parentPosition.z);
            #endregion

            // Split height
            #region split height
            Debug.Log("Split height");
            //Copy heightmap											
            td.heightmapResolution = parentTerrain.terrainData.heightmapResolution / terraPieces;
            //Keep y same
            td.size = new Vector3(parentTerrain.terrainData.size.x / terraPieces,
                                   parentTerrain.terrainData.size.y,
                                   parentTerrain.terrainData.size.z / terraPieces
                                  );

            int resolution;
            if (parentTerrain.terrainData.heightmapResolution % terraPieces == 0)
            {
                resolution = parentTerrain.terrainData.heightmapResolution / terraPieces;
            }
            else
            {
                resolution = parentTerrain.terrainData.heightmapResolution / terraPieces + 1;
            }
            float[,] pieceHeight = new float[resolution, resolution];

            int startX = 0;
            int startY = 0;
            int endX = 0;
            int endY = 0;

            startX = startY = 0;
            endX = endY = resolution;

            // iterate
            for (int x = startX; x < endX; x++)
            {
                EditorUtility.DisplayProgressBar("Split terrain", "Split height", (float)x / (endX - startX));
                for (int y = startY; y < endY; y++)
                {
                    int xShift = Xshift * heightShift;
                    int yShift = Yshift * heightShift;

                    float ph = parentHeight[x + xShift, y + yShift];
                    pieceHeight[x, y] = ph;
                }

            }

            EditorUtility.ClearProgressBar();

            // Set heightmap to child
            genTer.terrainData.SetHeights(0, 0, pieceHeight);
            #endregion

            // Split splat map
            #region split splat map	

            td.alphamapResolution = parentTerrain.terrainData.alphamapResolution / terraPieces;

            float[,,] pieceSplat = new float[parentTerrain.terrainData.alphamapResolution / terraPieces,
                parentTerrain.terrainData.alphamapResolution / terraPieces,
                parentTerrain.terrainData.alphamapLayers];

            startX = startY = 0;
            endX = endY = parentTerrain.terrainData.alphamapResolution / terraPieces;

            // iterate
            for (int s = 0; s < parentTerrain.terrainData.alphamapLayers; s++)
            {
                for (int x = startX; x < endX; x++)
                {
                    EditorUtility.DisplayProgressBar("Split terrain", "Split splat", (float)x / (endX - startX));
                    for (int y = startY; y < endY; y++)
                    {
                        int xShift = Xshift * splatShift;
                        int yShift = Yshift * splatShift;

                        if (x + xShift < parentSplat.GetLength(0) && y + yShift < parentSplat.GetLength(1))
                        {
                            float ph = parentSplat[x + xShift, y + yShift, s];
                            pieceSplat[x, y, s] = ph;
                        }
                    }

                }
            }

            EditorUtility.ClearProgressBar();

            // Set heightmap to child
            genTer.terrainData.SetAlphamaps(0, 0, pieceSplat);
            #endregion

            // Split detail map
            #region split detail map	

            td.SetDetailResolution(parentTerrain.terrainData.detailResolution / terraPieces, 8);

            for (int detLay = 0; detLay < parentTerrain.terrainData.detailPrototypes.Length; detLay++)
            {
                int[,] parentDetail = parentTerrain.terrainData.GetDetailLayer(0, 0, parentTerrain.terrainData.detailResolution, parentTerrain.terrainData.detailResolution, detLay);
                int[,] pieceDetail = new int[parentTerrain.terrainData.detailResolution / terraPieces,
                                              parentTerrain.terrainData.detailResolution / terraPieces];

                // Shift calc
                int detailShift = parentTerrain.terrainData.detailResolution / terraPieces;

                startX = startY = 0;
                endX = endY = parentTerrain.terrainData.detailResolution / terraPieces;

                // iterate				
                for (int x = startX; x < endX; x++)
                {

                    EditorUtility.DisplayProgressBar("Split terrain", "Split detail", (float)x / (endX - startX));

                    for (int y = startY; y < endY; y++)
                    {

                        int xShift = Xshift * detailShift;
                        int yShift = Yshift * detailShift;

                        if (x + xShift < parentDetail.GetLength(0) && y + yShift < parentDetail.GetLength(1))
                        {
                            int ph = parentDetail[x + xShift, y + yShift];
                            pieceDetail[x, y] = ph;
                        }
                    }

                }
                EditorUtility.ClearProgressBar();

                // Set heightmap to child
                genTer.terrainData.SetDetailLayer(0, 0, detLay, pieceDetail);

            }
            #endregion

            // Split tree data
            #region  split tree data

            for (int t = 0; t < parentTerrain.terrainData.treeInstances.Length; t++)
            {

                EditorUtility.DisplayProgressBar("Split terrain", "Split trees ", (float)t / parentTerrain.terrainData.treeInstances.Length);

                // Get tree instance					
                TreeInstance ti = parentTerrain.terrainData.treeInstances[t];

                float xLeft = (float)Xshift / terraPieces;
                float yLeft = (float)Yshift / terraPieces;

                if (ti.position.x >= yLeft && ti.position.x <= yLeft + (1f / terraPieces) &&
                     ti.position.z >= xLeft && ti.position.z <= xLeft + (1f / terraPieces))
                {
                    // Recalculate new tree position	
                    ti.position = new Vector3((ti.position.x - yLeft) * terraPieces, ti.position.y, (ti.position.z - xLeft) * terraPieces);

                    // Add tree instance						
                    genTer.AddTreeInstance(ti);
                }

            }
            #endregion

            AssetDatabase.SaveAssets();



        }

        EditorUtility.ClearProgressBar();



    }

    void OnGUI()
    {

        if (GUILayout.Button("Split terrain"))
        {

            SplitIt();
        }


    }



}
