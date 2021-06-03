using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SplitTerrain
{
    //输入
    public Terrain baseTerrain;
    public SplitSize splitSize = SplitSize._2x2;
    public int resolutionPerPatch = 8;
	public bool copyAllDetails = true;
	public bool copyAllTrees = true;
	
    //数据
	public TerrainData baseData;
    public string baseFileName;
    public int baseSize;
    public int splitTerrainsX;
	public int splitTerrainsZ;
    public float oldSizeX;
	public float oldSizeY;
	public float oldSizeZ;
    public float oldPosX;
    public float oldPosY;
    public float oldPosZ;
    public Material oldMaterial;
    public float grassStrength;
	public float grassAmount;
	public float grassSpeed;
	public Color grassTint;
    public float treeDistance;
    public float treeBillboardDistance;
	public float treeCrossFadeLength;
	public int treeMaximumFullLODCount;
	public float detailObjectDistance;
	public float detailObjectDensity;
	public float heightmapPixelError;
	public int heightmapMaximumLOD;
	public float basemapDistance;
	public int lightmapIndex;
	public ShadowCastingMode castShadows;
	public Material materialTemplate;
	public TerrainRenderFlags editorRenderFlags;

    public float newWidth;
	public float newLength;
    public int newHeightMapResolution;
	public int newEvenHeightMapResolution;
	public int newDetailResolution;
	public int newAlphaMapResolution;
	public int newBaseMapResolution;

    public GameObject[] terrainGameObjects;
    public Terrain[] terrains;
    public TerrainData[] terrain_datas;
    public TerrainLayer[] splatProtos;
	public DetailPrototype[] detailProtos;
	public TreePrototype[] treeProtos;
	public TreeInstance[] treeInsts;
    public int[] layers;

    private static SplitTerrain instance = new SplitTerrain();
    public static SplitTerrain getInstance()
    {
        return instance;
    }

    public void CopyTerrainData(int zIndex,int xIndex,int arrayPos,string terrainSavePath)
    {
		TerrainData td = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainSavePath + "/" + baseTerrain.name + "_Data_" + (zIndex + 1) + "_" + (xIndex + 1) + ".asset");
		terrainGameObjects[arrayPos] = Terrain.CreateTerrainGameObject(td);
		terrainGameObjects[arrayPos].name = baseTerrain.name + "_Split_" + (zIndex + 1) + "_" + (xIndex + 1);
		terrains[arrayPos] = terrainGameObjects[arrayPos].GetComponent<Terrain>();
        terrains[arrayPos].materialTemplate = oldMaterial;
        //lightmap
        terrains[arrayPos].lightmapScaleOffset = new Vector4(1f/ splitTerrainsZ, 1f/ splitTerrainsX, 1f / splitTerrainsX * (xIndex), 1f / splitTerrainsZ * (zIndex));

        terrain_datas[arrayPos] = td;
		terrain_datas[arrayPos].heightmapResolution = newEvenHeightMapResolution;
		terrain_datas[arrayPos].alphamapResolution = newAlphaMapResolution;
		terrain_datas[arrayPos].baseMapResolution = newBaseMapResolution;
		terrain_datas[arrayPos].SetDetailResolution(newDetailResolution, resolutionPerPatch);
		terrain_datas[arrayPos].size =new Vector3(newWidth, oldSizeY, newLength);

        //Splat prototypes
        terrain_datas[arrayPos].terrainLayers = baseData.terrainLayers;

        layers = baseData.GetSupportedLayers(xIndex * terrain_datas[arrayPos].detailWidth - 1, zIndex * terrain_datas[arrayPos].detailHeight - 1, terrain_datas[arrayPos].detailWidth, terrain_datas[arrayPos].detailHeight);
		int layerLength = layers.Length;

		if (copyAllDetails)
			terrain_datas[arrayPos].detailPrototypes = detailProtos;
		else
		{
			DetailPrototype[] tempDetailProtos = new DetailPrototype[layerLength];
			for (int i = 0; i < layerLength; i++)
				tempDetailProtos[i] = detailProtos[layers[i]];
			terrain_datas[arrayPos].detailPrototypes = tempDetailProtos;
		}

		for (int i = 0; i < layerLength; i++)
			terrain_datas[arrayPos].SetDetailLayer(0, 0, i, baseData.GetDetailLayer(xIndex * terrain_datas[arrayPos].detailWidth, zIndex * terrain_datas[arrayPos].detailHeight, terrain_datas[arrayPos].detailWidth, terrain_datas[arrayPos].detailHeight, layers[i]));

		System.Array.Clear(layers, 0, layers.Length);

		//if copy all trees is checked, we can just set each terrains tree prototypes to the base terrain. We'll skip this step if it's unchecked, and execute
		//a more complicated algorithm below instead.
		if (copyAllTrees)
			terrain_datas[arrayPos].treePrototypes = treeProtos;

		terrain_datas[arrayPos].wavingGrassStrength = grassStrength;
		terrain_datas[arrayPos].wavingGrassAmount = grassAmount;
		terrain_datas[arrayPos].wavingGrassSpeed = grassSpeed;
		terrain_datas[arrayPos].wavingGrassTint = grassTint;

		terrain_datas[arrayPos].SetHeights(0, 0, baseData.GetHeights(xIndex * (terrain_datas[arrayPos].heightmapResolution - 1), zIndex * (terrain_datas[arrayPos].heightmapResolution - 1), terrain_datas[arrayPos].heightmapResolution, terrain_datas[arrayPos].heightmapResolution));

        float[,,] map = new float[newAlphaMapResolution, newAlphaMapResolution, splatProtos.Length];
        map = baseData.GetAlphamaps(xIndex * terrain_datas[arrayPos].alphamapWidth, zIndex * terrain_datas[arrayPos].alphamapHeight, terrain_datas[arrayPos].alphamapWidth, terrain_datas[arrayPos].alphamapHeight);
        terrain_datas[arrayPos].SetAlphamaps(0, 0, map);

        terrainGameObjects[arrayPos].GetComponent<TerrainCollider>().terrainData = terrain_datas[arrayPos];

		terrainGameObjects[arrayPos].transform.position = new Vector3(xIndex * newWidth + oldPosX, oldPosY, zIndex * newLength + oldPosZ);

    }

    public void DealwithCopyTrees()
    {
        for (int y = 0; y < terrains.Length; y++)
        {
            terrains[y].treeDistance = treeDistance;
            terrains[y].treeBillboardDistance = treeBillboardDistance;
            terrains[y].treeCrossFadeLength = treeCrossFadeLength;
            terrains[y].treeMaximumFullLODCount = treeMaximumFullLODCount;
            terrains[y].detailObjectDistance = detailObjectDistance;
            terrains[y].detailObjectDensity = detailObjectDensity;
            terrains[y].heightmapPixelError = heightmapPixelError;
            terrains[y].heightmapMaximumLOD = heightmapMaximumLOD;
            terrains[y].basemapDistance = basemapDistance;
            terrains[y].lightmapIndex = lightmapIndex;
            terrains[y].shadowCastingMode = castShadows;
            terrains[y].editorRenderFlags = editorRenderFlags;
        }
        //Only execute these lines of code if copyAllTrees is false
        int[,] treeTypes = new int[splitTerrainsX * splitTerrainsZ, treeProtos.Length];
        if (!copyAllTrees)
        {
            //Loop through every single tree
            for (int i = 0; i < treeInsts.Length; i++)
            {
                Vector3 origPos2 = Vector3.Scale(new Vector3(oldSizeX, 1, oldSizeZ), new Vector3(treeInsts[i].position.x, treeInsts[i].position.y, treeInsts[i].position.z));

                int column2 = Mathf.FloorToInt(origPos2.x / newWidth);
                int row2 = Mathf.FloorToInt(origPos2.z / newLength);

                treeTypes[(row2 * splitTerrainsX) + column2, treeInsts[i].prototypeIndex] = 1;
            }

            for (int i = 0; i < splitTerrainsX * splitTerrainsZ; i++)
            {
                int numOfPrototypes = 0;
                for (int y = 0; y < treeProtos.Length; y++)
                    if (treeTypes[i, y] == 1)
                        numOfPrototypes++;
                //else --not necessary I think
                //treeTypes[i,y] = treeProtos.Length; //replace the 0 at this spot with the length of the treeProtos array. Later, if we find this spot has this value,
                //we'll know that this prototype is not found on this terrain. We will need to know this.
                TreePrototype[] tempPrototypes = new TreePrototype[numOfPrototypes];
                int tempIndex = 0;
                for (int y = 0; y < treeProtos.Length; y++)
                    if (treeTypes[i, y] == 1)
                    {
                        tempPrototypes[tempIndex] = treeProtos[y];
                        //In addition, replace the value at tempTypes[i,y] with the index of where that prototype is stored for that terrain, like this
                        treeTypes[i, y] = tempIndex;
                        tempIndex++;
                    }

                terrain_datas[i].treePrototypes = tempPrototypes;
            }
        }

        for (int i = 0; i < treeInsts.Length; i++)
        {
            Vector3 origPos = Vector3.Scale(new Vector3(oldSizeX, 1, oldSizeZ), new Vector3(treeInsts[i].position.x, treeInsts[i].position.y, treeInsts[i].position.z));
            int column = Mathf.FloorToInt(origPos.x / newWidth);
            int row = Mathf.FloorToInt(origPos.z / newLength);

            Vector3 tempVect = new Vector3((origPos.x - (newWidth * column)) / newWidth, origPos.y, (origPos.z - (newLength * row)) / newLength);
            TreeInstance tempTree = new TreeInstance();

            tempTree.position = tempVect;
            tempTree.widthScale = treeInsts[i].widthScale;
            tempTree.heightScale = treeInsts[i].heightScale;
            tempTree.color = treeInsts[i].color;
            tempTree.lightmapColor = treeInsts[i].lightmapColor;

            if (copyAllTrees)
                tempTree.prototypeIndex = treeInsts[i].prototypeIndex;
            else
                tempTree.prototypeIndex = treeTypes[(row * splitTerrainsX) + column, treeInsts[i].prototypeIndex];

            terrains[(row * splitTerrainsX) + column].AddTreeInstance(tempTree);

        }
        //refresh prototypes
        for (int i = 0; i < splitTerrainsX * splitTerrainsZ; i++)
        {
            terrain_datas[i].RefreshPrototypes();
        }
    }
}

public enum SplitSize
{
    _2x2 = 2,
    _4x4 = 4,
    _8x8 = 8,
}