using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class ExpensiveMeshFinder : EditorWindow 
{
    string [] staticOptions = new string[] { "Static and Non-Static", "Static Only", "Non-Static Only" };
    string[] sortOptions = new string[] { "Vertex Count", "Vertex Attributes", "Lightmap Size" };
    int staticIndex, sortIndex;

    bool includeShadowCasters = true, includeShadowReceivers = true, includeMultipleMats = true, includeSingleMats = true;
    private static MeshFilter[] sceneMeshes;
    private static List<float> lightMapSortedValues = new List<float>();
    Vector2 scrollPosResults, scrollPosSettings;
    [MenuItem("Window/Groove Jones/Find Expensive Meshes")] 
    public static void ShowWindow()
    {
        GetWindow<ExpensiveMeshFinder>();
    }


    void OnGUI()
    {
            GUILayout.Label("Mesh Filters in the scene", EditorStyles.boldLabel);
            scrollPosResults = EditorGUILayout.BeginScrollView(scrollPosResults, GUILayout.Width(position.width - 10), GUILayout.Height(position.height - 200));
            if(sceneMeshes != null && sceneMeshes.Length > 0)
            {
                for (int i = 0; i < sceneMeshes.Length; i++)
                {
                    sceneMeshes[i] = (MeshFilter)EditorGUILayout.ObjectField(sceneMeshes[i], typeof(MeshFilter), true);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    switch (sortIndex)
                    {
                        case 0:
                            GUILayout.Label("Vertex count: " + sceneMeshes[i].sharedMesh.vertexCount);
                            break;
                        case 1:
                            GUILayout.Label("Attribute count: " + sceneMeshes[i].sharedMesh.vertexBufferCount);
                            break;
                        case 2:
                            if(lightMapSortedValues.Count > 0)
                                GUILayout.Label("Rough lightmap size: " + lightMapSortedValues[i]);
                            break;
                    }

                        EditorGUILayout.EndHorizontal();
                }
            }
        else
        {
            GUILayout.Space(15);
            GUILayout.Label("(Select the button below to refresh the list)", EditorStyles.wordWrappedLabel);
        }
                
            GUILayout.EndScrollView();

        if (GUILayout.Button("Sort meshes in scene"))
        {
            SortSceneMeshFilters();
        }

            GUILayout.Label("Sort Results by:", EditorStyles.boldLabel);
            sortIndex = EditorGUILayout.Popup(sortIndex, sortOptions);
        
            GUILayout.Label("Filter Results:", EditorStyles.boldLabel);
            staticIndex = EditorGUILayout.Popup(staticIndex, staticOptions);            
                includeShadowCasters = EditorGUILayout.Toggle("Include shadow casters", includeShadowCasters);
                includeShadowReceivers = EditorGUILayout.Toggle("Include shadow receivers", includeShadowReceivers);
                //EditorGUILayout.BeginHorizontal();
                    includeMultipleMats = EditorGUILayout.Toggle("Multiple material IDs", includeMultipleMats);
                    includeSingleMats = EditorGUILayout.Toggle("Single material ID", includeSingleMats);
                //EditorGUILayout.EndHorizontal();

    }

    void RefreshList()
    {
        sceneMeshes = FindObjectsOfType<MeshFilter>();

        List<MeshFilter> filteredMeshes = new List<MeshFilter>(sceneMeshes);

        for (int i = 0; i < filteredMeshes.Count; i++)
        {
            var rend = filteredMeshes[i].GetComponent<Renderer>();
            if(rend.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On && !includeShadowCasters)
            {
                filteredMeshes.RemoveAt(i);
                i--;
                continue;
            }
            if(rend.receiveShadows && !includeShadowReceivers)
            {
                filteredMeshes.RemoveAt(i);
                i--;
                continue;
            }
            if (rend.sharedMaterials.Length > 1 && !includeMultipleMats)
            {
                filteredMeshes.RemoveAt(i);
                i--;
                continue;
            }
            if(rend.sharedMaterials.Length < 2 && !includeSingleMats)
            {
                filteredMeshes.RemoveAt(i);
                i--;
                continue;
            }

            switch (staticIndex)
            {
                case 1:
                    if (!filteredMeshes[i].gameObject.isStatic)
                    {
                        filteredMeshes.RemoveAt(i);
                        i--;
                        continue;
                    }
                    break;
                case 2:
                    if (filteredMeshes[i].gameObject.isStatic)
                    {
                        filteredMeshes.RemoveAt(i);
                        i--;
                        continue;
                    }
                    break;
            }
        }

        sceneMeshes = filteredMeshes.ToArray();
    }

    void SortSceneMeshFilters()
    {
        RefreshList();

        switch (sortIndex)
        {
            case 0:
                Array.Sort(sceneMeshes,
            delegate (MeshFilter x, MeshFilter y) { return y.sharedMesh.vertexCount.CompareTo(x.sharedMesh.vertexCount); });
                break;
            case 1:
                Array.Sort(sceneMeshes,
            delegate (MeshFilter x, MeshFilter y) { return y.sharedMesh.vertexBufferCount.CompareTo(x.sharedMesh.vertexBufferCount); });
                break;
            case 2:
                SortLightMapBounds();
                break;
        }
        
    }

    public void SortLightMapBounds()
    {
        Dictionary<MeshFilter, float> _dic = new Dictionary<MeshFilter, float>();
        for (int i = 0; i < sceneMeshes.Length; i++)
        {
            Renderer rend = sceneMeshes[i].GetComponent<Renderer>();
            float lightmapBounds = rend.bounds.extents.magnitude * rend.lightmapScaleOffset.x * rend.lightmapScaleOffset.y;
            _dic.Add(sceneMeshes[i], lightmapBounds);
        }

        _dic = _dic.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        lightMapSortedValues.Clear();
        foreach (KeyValuePair<MeshFilter, float> _d in _dic)
        {
            lightMapSortedValues.Add(_d.Value);
        }

        List<MeshFilter> keys = new List<MeshFilter>();
        keys.AddRange(_dic.Keys);
        sceneMeshes = keys.ToArray();
    }
}