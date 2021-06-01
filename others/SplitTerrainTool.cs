using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

using System.IO;

public class TerrainSlicing : Editor

{

    //分割后存储的位置

    public static string TerrainSavePath = "Assets/MyTestTerrain/New2/";
    static Terrain parentTerrain = null;

    //分割份数（必须是2整数幂）

    public static int SLICING_NUM = 4;

    //检查是否有地形信息

    private static TerrainData Check()
    {

        Debug.Log(Selection.activeObject.GetType());

        TerrainData mainData = null;

        if (Selection.activeObject != null)
        {

            if (Selection.activeObject.GetType() == typeof(GameObject))
            {

                var obj = (GameObject)Selection.activeObject;
                parentTerrain = obj.GetComponent<Terrain>();
                mainData = parentTerrain.terrainData;
            }

            if (mainData == null)
            {

                Debug.Log("没有地形信息");

            }

        }
        else
        {

            Debug.Log("没有地形信息");

        }

        return mainData;

    }

    [MenuItem("Assets/TerrainAlignment(地形贴图校准防止有明显切线)")]

    private static void Alignment()
    {

        TerrainData mainData = Check();

        if (mainData == null)

            return;

        var alphamapX = mainData.alphamapWidth / SLICING_NUM;

        var alphamapY = mainData.alphamapHeight / SLICING_NUM;

        float[,,] alphamaps = mainData.GetAlphamaps(0, 0, mainData.alphamapWidth, mainData.alphamapHeight);

        for (int x = 1; x < SLICING_NUM; x++)
        {

            for (int y = 0; y < mainData.alphamapHeight; y++)
            {

                for (int i = 0; i < mainData.splatPrototypes.Length; i++)
                {

                    alphamaps[x * alphamapX, y, i] = alphamaps[x * alphamapX - 1, y, i];

                }

            }

        }

        for (int y = 1; y < SLICING_NUM; y++)
        {

            for (int x = 0; x < mainData.alphamapWidth; x++)
            {

                for (int i = 0; i < mainData.splatPrototypes.Length; i++)
                {

                    alphamaps[x, y * alphamapY, i] = alphamaps[x, y * alphamapY - 1, i];

                }

            }

        }

        mainData.SetAlphamaps(0, 0, alphamaps);

    }

    [MenuItem("Assets/TerrainSlicing(分割地形)")]

    private static void Slicing()

    {

        TerrainData mainData = Check();

        if (mainData != null)
        {

            if (Directory.Exists(TerrainSavePath))

                Directory.Delete(TerrainSavePath, true);

            Directory.CreateDirectory(TerrainSavePath);

            Directory.CreateDirectory(TerrainSavePath + "/TerrainData");

            Directory.CreateDirectory(TerrainSavePath + "/Prefab");

            //得到新地图 高度和贴图透明度纹理 范围

            var alphamapX = mainData.alphamapWidth / SLICING_NUM;

            var alphamapY = mainData.alphamapHeight / SLICING_NUM;

            var heightX = (mainData.heightmapWidth - 1) / SLICING_NUM;

            var heightY = (mainData.heightmapHeight - 1) / SLICING_NUM;

            for (int x = 0; x < SLICING_NUM; x++)
            {

                for (int y = 0; y < SLICING_NUM; y++)
                {

                    EditorUtility.DisplayProgressBar("正在分割地形", mainData.name + "     " + (x * SLICING_NUM + y) + "/" + (SLICING_NUM * SLICING_NUM), (float)(x * SLICING_NUM + y) / (float)(SLICING_NUM * SLICING_NUM));

                    var newData = new TerrainData();

                    //必须先创建（否则 贴图透明度纹[不知道叫啥] 理无法储存）

                    AssetDatabase.CreateAsset(newData, string.Format("{0}/TerrainData/{1}_{2}.asset", TerrainSavePath, x, y));

                    //赋值一些基本属性

                    newData.heightmapResolution = (mainData.heightmapResolution - 1) / SLICING_NUM;

                    newData.alphamapResolution = mainData.alphamapResolution / SLICING_NUM;

                    newData.baseMapResolution = mainData.baseMapResolution / SLICING_NUM;

                    newData.size = new Vector3(mainData.size.x / SLICING_NUM, mainData.size.y, mainData.size.z / SLICING_NUM);

                    //设置地形原型（原始贴图）

                    var splatProtos = mainData.splatPrototypes;

                    SplatPrototype[] newSplats = new SplatPrototype[splatProtos.Length];

                    for (int i = 0; i < splatProtos.Length; ++i)
                    {

                        newSplats[i] = new SplatPrototype();

                        newSplats[i].texture = splatProtos[i].texture;

                        newSplats[i].tileSize = splatProtos[i].tileSize;

                        newSplats[i].normalMap = splatProtos[i].normalMap;

                        //计算贴图偏移

                        float offsetX = (newData.size.x * x) % splatProtos[i].tileSize.x + splatProtos[i].tileOffset.x;

                        float offsetY = (newData.size.z * y) % splatProtos[i].tileSize.y + splatProtos[i].tileOffset.y;

                        newSplats[i].tileOffset = new Vector2(offsetX, offsetY);

                    }

                    newData.splatPrototypes = newSplats;

                    //赋值 高度 和 贴图透明度纹理 信息

                    float[,,] alphamaps = mainData.GetAlphamaps(x * alphamapX, y * alphamapY, alphamapX, alphamapY);

                    float[,] heights = mainData.GetHeights(x * heightX, y * heightY, heightX + 1, heightY + 1);//+1 防止边界高度为默认值0

                    newData.SetAlphamaps(0, 0, alphamaps);

                    newData.SetHeights(0, 0, heights);

                    //预览

                    GameObject parent = GameObject.Find("BigMap");

                    if (parent == null)
                    {

                        parent = new GameObject("BigMap");

                    }

                    var name = string.Format("{0}_{1}", x, y);

                    GameObject obj = new GameObject(name);

                    obj.transform.SetParent(parent.transform);

                    obj.transform.localPosition = new Vector3(x * newData.size.x, 0, y * newData.size.z);

                    Terrain terrain = obj.AddComponent<Terrain>();
                    terrain.materialTemplate = parentTerrain.materialTemplate;
                    terrain.terrainData = newData;

                    TerrainCollider terrainCollider = obj.AddComponent<TerrainCollider>();

                    terrainCollider.terrainData = newData;

                    //创建预制体

                    Object prefab = PrefabUtility.CreateEmptyPrefab(string.Format("{0}/Prefab/{1}_{2}.prefab", TerrainSavePath, x, y));

                    PrefabUtility.ReplacePrefab(obj, prefab);

                }

            }

            EditorUtility.ClearProgressBar();

        }

    }

}
