using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TerrainSplitTool : EditorWindow
{
    [MenuItem("Tools/场景/地形切割工具")]
    public static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(TerrainSplitTool), false, "TerrainSplitTool");
        window.position = new Rect(Screen.width / 2 + 300, 400, 600, 300);
    }

	private GUIContent label1;
	private GUIContent label2;
	private GUIContent label3;
	private GUIContent label4;
	private GUIContent label5;
	private GUIContent label6;
	private GUIContent label7;
	private GUIContent label8;
	private GUIContent label9;
	private GUIContent label10;
	private bool isPlaying;
	private string terrainSavePath;
	private GameObject[] selection;
	private SplitTerrain splitScript;

	private bool overwrite = true;
	private bool blend = true;
	private bool copyAllTrees = true;
	private bool copyAllDetails = true;
	private string savePathKey = "Select Terrain Path";
	private float progress;
	private float progressScale;
	public void OnEnable()
	{

		minSize = new Vector2(660, 470);
		if (Application.isPlaying)
			isPlaying = true;
		else
			isPlaying = false;

		if (!isPlaying)
		{
			splitScript = SplitTerrain.getInstance();
			if (!PlayerPrefs.HasKey(savePathKey))
			{
				PlayerPrefs.SetString(savePathKey, "Assets/TerrainSplit/TerrainData");
				terrainSavePath = "Assets/TerrainSplit/TerrainData";
			}
			else
				terrainSavePath = PlayerPrefs.GetString(savePathKey);

			selection = Selection.gameObjects;

			if (selection.Length == 1)
				if (selection[0].GetComponent<Terrain>() != null)
					splitScript.baseTerrain = selection[0].GetComponent<Terrain>();
				else
					Debug.Log("Selection Error - Could not get selection : Selection is not a terrain!");
			else if (selection.Length > 1)
				Debug.Log("Selection Error - Could not get selection : Too many objects selected!");

			//Create the tooltips
			label1 = new GUIContent("Base Terrain to Slice", "This terrain's resolution data must be large enough so that when sliced," +
			"the resulting terrain pieces resolutions are greater than their minimum allowable values. Minimum values are:\n" +
			"Heightmap - 33\nBaseMap - 16\nControl Texture - 16\nDetail - Cannot be 0");

			label2 = new GUIContent("Resolution Per Patch", "Ideally, this should be the same as your base terrain's detail resolution per patch.");

			label3 = new GUIContent("Slicing Dimensions", "ex: 2 x 2 will divide terrain by 2 along x axis and 2 along z axis, producing 4 terrain slices.\n" +
			"4 x 4 will divide by 4, producing 16 terrain slices, and so on . . .");

			label4 = new GUIContent("File Path to Store Data", "This is the file path where the created terrain data will be stored.");

			label5 = new GUIContent("Reset File Path to Default: " + PlayerPrefs.GetString("File Path"), "This button simply resets the field above with the " +
			"default file path stored in player prefs (which you can change at any time by entering a new file path above and clicking the button below this one)." +
			"Use this if you make a mistake or need to reset the file path to the default for any reason.");

			label6 = new GUIContent("Save Current File Path as Default File Path", "Click this if you want the file path entered above to be saved as the default file path." +
			"This will make this file path the default file path shown in the above field when you open the Terrain Split Tool.");

			label7 = new GUIContent("Overwrite Terrain Data", "The terrain data names are derived from the base terrain's name, so if you try to slice a terrain with the same name as a terrain that you've " +
			"sliced in the past, you risk overwriting the existing terrain data.\n\nYou may wish for this data to be overwritten, but to keep you from overwriting data on accident " +
			"I've included this checkbox field. By default it is unchecked, and the program will not let you overwrite data while it is left unchecked. So if you intentionally want " +
			"to overwrite data, check this box.");

			label8 = new GUIContent("Blend Alpamap Edges", "This option will set the alphamap edges of neighboring terrains to the same value, which blends the edges " +
			"of the neighboring terrain's alphamaps so that there is no visible seem between the two (also blends corner between 4 terrains).\n\n" +
			"This blending will very slightly alter the alphamaps of the terrains, which you will notice in some instances, but these changes should not present much of a problem.\n\n" +
			"You can also try slicing with this option unchecked, but you will need to manually check the seems between terrains to ensure none are visible. If they are, you will have to re-slice with the blending option checked.");

			label9 = new GUIContent("Copy All Trees", "The base terrain contains a list of trees which you can paint on it. By default the program will copy every " +
			"tree from this list to every terrain slice created during the slicing process, regardless of whether that terrain slice currently has that tree painted on it.\n\n" +
			"If you want each newly created terrain slice to have the full list of trees from the base terrain, leave this box checked.\n\n" +
			"However, if you would rather copy only those trees which the terrain slice has painted on it, uncheck this box.\n\nRegardless of the option you choose, all visible trees on your terrain will be copied to the new terrains accurately.");

			label10 = new GUIContent("Copy All Detail Meshes", "The base terrain contains a list of detail meshes (plants and grasses which you can paint on it. By default the program will copy every " +
			"detail mesh from this list to every terrain slice created during the slicing process, regardless of whether that terrain slice currently has the detail mesh painted on it.\n\n" +
			"If you want each newly created terrain slice to have the full list of detail meshes from the base terrain, leave this box checked.\n\n" +
			"However, if you would rather copy only those detail meshes which the terrain slice has painted on it, uncheck this box.\n\nRegardless of the option you choose, all visible detail meshes on your terrain will be copied to the new terrains with high accuracy.");
		}
	}

	public void OnGUI()
	{
		if (Application.isPlaying)
			isPlaying = true;

		if (!isPlaying)
		{
			splitScript = SplitTerrain.getInstance();
			GUILayout.Label("", EditorStyles.boldLabel);

			EditorGUILayout.LabelField("Hover over the field labels (left of each field) or buttons to view more detailed information about each field.");
			EditorGUILayout.LabelField("");
			splitScript.baseTerrain = EditorGUILayout.ObjectField(label1, splitScript.baseTerrain, typeof(Terrain), true) as Terrain;

			EditorGUILayout.LabelField("");// Used for Spacing only// Used for Spacing only

			splitScript.resolutionPerPatch = EditorGUILayout.IntField(label2, splitScript.resolutionPerPatch);
			EditorGUILayout.LabelField(""); // Used for Spacing only

			splitScript.baseSize = System.Convert.ToInt32(EditorGUILayout.EnumPopup(label3, splitScript.splitSize));
			splitScript.splitSize = (SplitSize)splitScript.baseSize;
			EditorGUILayout.LabelField("");// Used for Spacing only

			terrainSavePath = EditorGUILayout.TextField(label4, terrainSavePath);
			if (GUILayout.Button(label5))
			{
				GUIUtility.keyboardControl = 0;
				terrainSavePath = PlayerPrefs.GetString(savePathKey);
			}

			if (GUILayout.Button(label6))
				SaveFilePath();
			EditorGUILayout.LabelField(""); // Used for Spacing only

			overwrite = EditorGUILayout.Toggle(label7, overwrite);
			blend = EditorGUILayout.Toggle(label8, blend);

			copyAllTrees = EditorGUILayout.Toggle(label9, copyAllTrees);
			splitScript.copyAllTrees = copyAllTrees;
			copyAllDetails = EditorGUILayout.Toggle(label10, copyAllDetails);
			splitScript.copyAllDetails = copyAllDetails;

			EditorGUILayout.LabelField(""); // Used for Spacing only

			if (GUILayout.Button("Create Terrain"))
			{
				if (splitScript.baseTerrain != null)
				{
					StoreData();
					if (CheckForUserInput())
					{
						CreateTerrainData();
						CopyTerrainData();
                        ////Optional step
                        //if (blend)
                        //	BlendEdges();
                        //SetNeighbors();
                        EditorUtility.ClearProgressBar();
                        this.Close();
					}
				}
				else
				{
					this.ShowNotification(new GUIContent("Base Terrain must be selected."));
					GUIUtility.keyboardControl = 0; // Added to shift focus to original window rather than the notification
				}
			}
		}
		else
			EditorGUILayout.LabelField("The Terrain Slicing Tool cannot operate in play mode. Exit play mode and reselect Slicing Option.");


	}//End the OnGUI function

	public void SaveFilePath()
	{
		PlayerPrefs.SetString(savePathKey, terrainSavePath);
		label5 = new GUIContent("Reset File Path to Default: " + PlayerPrefs.GetString("File Path"), "This button simply resets the field above with the " +
		"default file path stored in player prefs (which you can change at any time by entering a new file path above and clicking the button below this one)." +
		"Use this if you make a mistake or need to reset the file path to the default for any reason.");
	}

	public void StoreData()
	{
		int splitSize = splitScript.baseSize;
		splitScript.splitTerrainsX = splitSize;
		splitScript.splitTerrainsZ = splitSize;
		Terrain baseTerrain = splitScript.baseTerrain;
		TerrainData baseData = baseTerrain.terrainData;
		splitScript.baseData = baseData;

		splitScript.oldSizeX = baseData.size.x;
		splitScript.oldSizeY = baseData.size.y;
		splitScript.oldSizeZ = baseData.size.z;

		splitScript.newWidth = splitScript.oldSizeX / splitScript.splitTerrainsX;
		splitScript.newLength = splitScript.oldSizeZ / splitScript.splitTerrainsZ;

		splitScript.oldPosX = baseTerrain.GetPosition().x;
		splitScript.oldPosX = baseTerrain.GetPosition().y;
		splitScript.oldPosX = baseTerrain.GetPosition().z;

        splitScript.oldMaterial = baseTerrain.materialTemplate;

		splitScript.newHeightMapResolution = ((baseData.heightmapResolution - 1) / splitSize) + 1;
		splitScript.newEvenHeightMapResolution = splitScript.newHeightMapResolution - 1;

		splitScript.newDetailResolution = baseData.detailResolution / splitSize;
		splitScript.newAlphaMapResolution = baseData.alphamapResolution / splitSize;
		splitScript.newBaseMapResolution = baseData.baseMapResolution / splitSize;

		splitScript.treeDistance = baseTerrain.treeDistance;
		splitScript.treeBillboardDistance = baseTerrain.treeBillboardDistance;
		splitScript.treeCrossFadeLength = baseTerrain.treeCrossFadeLength;
		splitScript.treeMaximumFullLODCount = baseTerrain.treeMaximumFullLODCount;
		splitScript.detailObjectDistance = baseTerrain.detailObjectDistance;
		splitScript.detailObjectDensity = baseTerrain.detailObjectDensity;
		splitScript.heightmapPixelError = baseTerrain.heightmapPixelError;
		splitScript.heightmapMaximumLOD = baseTerrain.heightmapMaximumLOD;
		splitScript.basemapDistance = baseTerrain.basemapDistance;
		splitScript.lightmapIndex = baseTerrain.lightmapIndex;
		splitScript.castShadows = baseTerrain.shadowCastingMode;
		splitScript.editorRenderFlags = baseTerrain.editorRenderFlags;

		splitScript.splatProtos = baseData.terrainLayers;
		splitScript.detailProtos = baseData.detailPrototypes;
		splitScript.treeProtos = baseData.treePrototypes;
		splitScript.treeInsts = baseData.treeInstances;

		splitScript.grassStrength = baseData.wavingGrassStrength;
		splitScript.grassAmount = baseData.wavingGrassAmount;
		splitScript.grassSpeed = baseData.wavingGrassSpeed;
		splitScript.grassTint = baseData.wavingGrassTint;
	}

	//Check for any errors with User Input
	public bool CheckForUserInput()
	{
		if(splitScript.resolutionPerPatch< 8)
		{
			this.ShowNotification(new GUIContent("Resolution Per Patch must be 8 or greater"));
			GUIUtility.keyboardControl = 0; // Added to shift focus to original window rather than the notification
			return false;
		}
		else if(!Mathf.IsPowerOfTwo(splitScript.resolutionPerPatch))
		{
			this.ShowNotification(new GUIContent("Resolution Per Patch must be a power of 2"));
			GUIUtility.keyboardControl = 0;
			return false;
}
		else if(splitScript.newHeightMapResolution < 33)
		{
			this.ShowNotification(new GUIContent("Error with Heightmap Resolution - See Console for More Information"));
			GUIUtility.keyboardControl = 0;
			Debug.Log("The Heightmap Resolution for the new terrains must be 33 or larger. Currently it is " + splitScript.newHeightMapResolution.ToString() + ".\nThe new Heightmap Resolution is calculated as"
			+ "follows: New Resolution = ((Old Resolution - 1) / New Dimension Width) + 1 -- For example, a 4x4 grid has a New Dimension Width of 4.\n You can rectify this problem by"
			+ "either increasing the heightmap resolution of the base terrain, or reducing the number of new terrains to be created.");
			return false;
}
		else if(splitScript.newAlphaMapResolution < 16)
		{
			this.ShowNotification(new GUIContent("Error with AlphaMap Resolution - See Console for More Information"));
			GUIUtility.keyboardControl = 0;
			Debug.Log("The Alpha Map Resolution of the new terrains is too small. Value must be 16 or greater. Current value is " + splitScript.newAlphaMapResolution.ToString()
			+ ".\nPlease increase the Base Terrains alpha map resolution or reduce the number of new terrains to be created.");
			return false;
}
		else if(splitScript.newBaseMapResolution < 16)
		{
			this.ShowNotification(new GUIContent("Error with BaseMap Resolution - See Console for More Information"));
			GUIUtility.keyboardControl = 0;
			Debug.Log("The Base Map Resolution of the new terrains is too small. Value must be 16 or greater. Current value is " + splitScript.newBaseMapResolution.ToString()
			+ ".\nPlease increase the Base Terrains base map resolution or reduce the number of new terrains to be created.");
			return false;
}
		else if(splitScript.baseData.detailResolution % splitScript.baseSize != 0)
		{
			this.ShowNotification(new GUIContent("Error with Detail Resolution - See Console for More Information"));
			GUIUtility.keyboardControl = 0;
			Debug.Log("The Base Terrains detail resolution does not divide perfectly. Please change the detail resolution or number of terrains to be created to rectify this issue.");
			return false;
}
		else if(!overwrite && AssetDatabase.LoadAssetAtPath<TerrainData>(terrainSavePath + "/" + splitScript.baseTerrain.name + "_Data_" + 1 + "_" + 1 + ".asset") != null)
		{
			this.ShowNotification(new GUIContent("Terrain Data with this name already exist. Please check 'Overwrite' if you wish to overwrite the existing Data"));
			GUIUtility.keyboardControl = 0;
			return false;
		}
		return true;
	}

	//Create the terrain data (including
	public void CreateTerrainData()
	{
		progress = 0.0f;
		EditorUtility.DisplayProgressBar("Progress", "Generating Terrains", progress);

		if (!Mathf.IsPowerOfTwo(splitScript.newDetailResolution))
			Debug.Log("Detail Resolution of new terrains is not a power of 2. Accurate results are not guaranteed.");

		if (splitScript.newDetailResolution % splitScript.resolutionPerPatch != 0)
			Debug.Log("Detail Resolution of new terrains does not divide resolution per patch value evenly. Unity will\n" +
			" automatically downgrade resolution to a value that does divide evenly, however, accurate results are not guaranteed.");

		int terrainX = splitScript.splitTerrainsX;
		int terrainZ = splitScript.splitTerrainsZ;
		splitScript.terrainGameObjects = new GameObject[terrainX * terrainZ];
		splitScript.terrains = new Terrain[terrainX * terrainZ];
		splitScript.terrain_datas = new TerrainData[terrainX * terrainZ];

		progressScale = .9f / (terrainX * terrainZ);

		for (int y = 0; y < terrainZ; y++)
		{
			for (int x = 0; x < terrainX; x++)
			{
				AssetDatabase.CreateAsset(new TerrainData(), terrainSavePath + "/" + splitScript.baseTerrain.name + "_Data_" + (y + 1) + "_" + (x + 1) + ".asset");
				progress += progressScale;
				EditorUtility.DisplayProgressBar("Progress", "Generating Terrains", progress);
			}
		}
	}

	public void CopyTerrainData()
	{
		int terrainZ = splitScript.splitTerrainsZ;
		int terrainX = splitScript.splitTerrainsX;
		progressScale = .2f / (terrainZ * terrainX);
		int arrayPos = 0;
		for (int y = 0; y < terrainZ; y++)
		{
			for (int x = 0; x < terrainX; x++)
			{
				splitScript.CopyTerrainData(y, x,arrayPos, terrainSavePath);
				arrayPos++;
				progress += progressScale;
				EditorUtility.DisplayProgressBar("Progress", "Generating Terrains", progress);
			}
		}

        splitScript.DealwithCopyTrees();
	}
}
