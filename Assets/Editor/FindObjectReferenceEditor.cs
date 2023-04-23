using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.UI;
//스크립트 정보 볼때 쓰임.
using System.Reflection;
using TMPro;
using UnityEditor.SceneManagement;

public enum eAssetType
{
    MATERIAL,
    TEXTURE2D,
    ANIMATOR_CONTROLLER,
    RENDER_TEXTURE,
    PREFAB,
    AUDIO_CLIP,
    SPRITE,
    MONO_BEHAVIOUR,
    TMP_FONT
}

/// <summary>
/// 프로젝트 내에 Target이 참조 되어 있는 모든 오브젝트를 찾아줍니다.
/// </summary>
public class FindObjectReferenceEditor : EditorWindow
{
    private static bool init = false;
    private UnityEngine.Object target;
    private UnityEngine.Object PreTarget;
    private eAssetType assetType;
    private Dictionary<string, List<Transform>> referencedList = new Dictionary<string, List<Transform>>();
    //리스트 항목 접힘 상태 저장
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    private Vector2 scrollPosition;
    private string scriptName;

    [MenuItem("Tools/Find Object Referenced By Prefab")]
    public static void ShowMyEditor()
    {
        EditorWindow wnd = GetWindow<FindObjectReferenceEditor>();
        wnd.titleContent = new GUIContent("Find_Object_Referenced");
        wnd.minSize = new Vector2(300, 500);
        wnd.maxSize = new Vector2(800, 720);
        init = false;
    }

    public void OnGUI()
    {
        if (init == false)
        {
            scriptName = null;
            referencedList = new Dictionary<string, List<Transform>>();
            foldoutStates = new Dictionary<string, bool>();
            init = true;
        }
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("사용 여부를 체크할 리소스를 등록 해 주세요.");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        target = EditorGUILayout.ObjectField(target, typeof(UnityEngine.Object), false);
        if (PreTarget != null && target != null)
        {
            if (PreTarget.GetInstanceID() != target.GetInstanceID())
            {
                referencedList.Clear();
                foldoutStates.Clear();
                PreTarget = target;
            }
        }
        else if(PreTarget == null)
        {
            PreTarget = target;
        }
        EditorGUILayout.EndHorizontal();
        if (target != null)
        {
            scriptName = null;
            Type curType = target.GetType();
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.yellow;
            GUILayout.Label("["+ curType.Name+ "] 으로 참조된 프리팹을 검색 합니다." , style);
            switch (curType.Name)
            {
                case "Material": assetType = eAssetType.MATERIAL; break;
                case "Texture2D": assetType = eAssetType.TEXTURE2D; break;
                case "GameObject": assetType = eAssetType.PREFAB; break;
                case "AnimatorController": assetType = eAssetType.ANIMATOR_CONTROLLER; break;
                case "RenderTexture": assetType = eAssetType.RENDER_TEXTURE; break;
                case "AudioClip": assetType = eAssetType.AUDIO_CLIP; break;
                case "Sprite": assetType = eAssetType.SPRITE; break;
                case "TMP_FontAsset": assetType = eAssetType.TMP_FONT; break;
                case "MonoScript":
                    {
                        assetType = eAssetType.MONO_BEHAVIOUR;
                        string path = AssetDatabase.GetAssetPath(target.GetInstanceID());
                        MonoScript mono = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
                        if (mono != null) scriptName = mono.name;
                    }
                    break;
            }
        }
        if (target == null)
        {
            referencedList.Clear();
            foldoutStates.Clear();
            return;
        }
        EditorGUILayout.Space(10);
        //Target이 참조된 모든 프리팹 찾기 시작.
        FindReferencedObject();

        if (referencedList.Count == 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.red;
            GUILayout.Label("[" + target.GetType().Name + "] 으로 참조된 프리팹이 없습니다.", style);
            return;
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        // 게임 오브젝트 리스트를 순회하며 각 항목을 보여준다
        foreach (KeyValuePair<string, List<Transform>> obj in referencedList)
        {
            Transform tr = (Transform)AssetDatabase.LoadAssetAtPath(obj.Key, typeof(Transform));
            if (!foldoutStates.ContainsKey(obj.Key))
            {
                foldoutStates[obj.Key] = false;
            }

            GUILayout.BeginHorizontal();
            foldoutStates[obj.Key] = GUILayout.Toggle(foldoutStates[obj.Key], "( " + obj.Value.Count + " ) "+ obj.Key, GUI.skin.button);
            if (GUILayout.Button("Go To"))
            {
                PrefabStageUtility.OpenPrefab(obj.Key);
                //Selection.activeObject = tr.gameObject;
            }
            GUILayout.EndHorizontal();

            if (foldoutStates[obj.Key])
            {
                /*
                PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                GameObject root = stage.prefabContentsRoot;
                */

                foreach (Transform child in obj.Value)
                {
                    if (GUILayout.Button(child.name +"_"+ child.GetInstanceID()))
                    {
                        Selection.activeGameObject = child.gameObject;
                    }
                }
            }
        }
        GUILayout.EndScrollView(); // 스크롤뷰 종료
    }
        
    /// <summary>
    /// Target을 참조 하고 있는 모든 프리팹을 찾아서 리스트에 저장한다.
    /// </summary>
    private void FindReferencedObject()
    {
        //프리팹 타입 모두 리턴
        string[] GUIDArray = AssetDatabase.FindAssets("t:GameObject");
        for (int i = 0; i < GUIDArray.Length; i++)
        {
            //리스트 생성.
            List<Transform> childList = new List<Transform>();
            string path = AssetDatabase.GUIDToAssetPath(GUIDArray[i]);
            Transform parent = (Transform)AssetDatabase.LoadAssetAtPath(path, typeof(Transform));
            //부모 자체에게 Target을 참조 하는것이 있는가?
            bool isReferenced = CheckReferencedByTarget(parent);
            if (isReferenced)
            {
                //리스트에 추가
                childList.Add(parent);
            }
            List<Transform> allChild = parent.GetAllChildren();
            for (int j = 0; j < allChild.Count; j++)
            {
                isReferenced = CheckReferencedByTarget(allChild[j]);
                if (isReferenced)
                {
                    childList.Add(allChild[j]);
                }
            }
            if (referencedList.ContainsKey(path) == false && childList.Count > 0)
            {
                referencedList.Add(path, childList);
                foldoutStates.Add(path, false);
            }
        }
    }

    /// <summary>
    /// Transform의 Component를 리턴하여 Target과 비교한다
    /// </summary>
    private bool CheckReferencedByTarget(Transform tr)
    {
        bool isReferenced = false;
        switch (assetType)
        {
            case eAssetType.ANIMATOR_CONTROLLER:
                {
                    Animator ani = tr.GetComponent<Animator>();
                    if (ani != null)
                    {
                        if (ani.runtimeAnimatorController != null)
                        {
                            isReferenced = target.GetInstanceID() == ani.runtimeAnimatorController.GetInstanceID();
                            if (isReferenced) return true;
                        }
                    }
                    return isReferenced;
                }

            //매터리얼이 포함 된 Component 모두 체크
            case eAssetType.MATERIAL:
                {
                    //모든 그래픽
                    Graphic[] graphicArray = tr.GetComponents<Graphic>();
                    if (graphicArray.Length > 0)
                    {
                        for (int i = 0; i < graphicArray.Length; i++)
                        {
                            if (graphicArray[i].material == null) continue;
                            isReferenced = target.GetInstanceID() == graphicArray[i].material.GetInstanceID();
                            if (isReferenced) return true;
                        }
                    }

                    //모든 Renderer 체크
                    Renderer[] rendererArr = tr.GetComponents<Renderer>();
                    if (rendererArr.Length > 0)
                    {
                        for (int i = 0; i < rendererArr.Length; i++)
                        {
                            for (int j = 0; j < rendererArr[i].sharedMaterials.Length; j++)
                            {
                                if (rendererArr[i].sharedMaterials[j] != null)
                                {
                                    isReferenced = target.GetInstanceID() == rendererArr[i].sharedMaterials[j].GetInstanceID();
                                    if (isReferenced) return true;
                                }
                            }
                        }
                    }
                    return isReferenced;
                }

            case eAssetType.AUDIO_CLIP:
                {
                    AudioSource audioSource = tr.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        if (audioSource.clip != null)
                        {
                            isReferenced = target.GetInstanceID() == audioSource.clip.GetInstanceID();
                        }
                    }
                    return isReferenced;
                }
            case eAssetType.RENDER_TEXTURE:
            case eAssetType.TEXTURE2D:
            case eAssetType.SPRITE:
                {
                    //모든 그래픽
                    Graphic[] graphicArray = tr.GetComponents<Graphic>();
                    if (graphicArray.Length > 0)
                    {
                        for (int i = 0; i < graphicArray.Length; i++)
                        {
                            if (graphicArray[i].mainTexture == null) continue;
                            isReferenced = target.GetInstanceID() == graphicArray[i].mainTexture.GetInstanceID();
                            if (isReferenced) return true;
                        }
                    }

                    //스프라이트 렌더러 체크
                    SpriteRenderer[] rendererArr = tr.GetComponents<SpriteRenderer>();
                    if (rendererArr.Length > 0)
                    {
                        for (int i = 0; i < rendererArr.Length; i++)
                        {
                            if (rendererArr[i].sprite == null) continue;
                            isReferenced = target.GetInstanceID() == rendererArr[i].sprite.GetInstanceID();
                            if (isReferenced) return true;
                        }
                    }
                    return isReferenced;
                }
            case eAssetType.MONO_BEHAVIOUR:
                {
                    MonoBehaviour s = tr.GetComponent<MonoBehaviour>();
                    if (s != null)
                    {
                        Type type = s.GetType();
                        isReferenced = scriptName == type.Name;
                        if (isReferenced) return true;
                    }
                    return isReferenced;
                }  
                /*
            case eAssetType.PREFAB:
                {
                    MonoBehaviour s = tr.GetComponent<MonoBehaviour>();
                    if (s != null)
                    {
                        Type type = s.GetType();
                        FieldInfo[] fieldInfoArr = type.GetFields();
                        foreach (FieldInfo field in fieldInfoArr)
                        {
                            if (field.IsPublic)
                            {
                                if (field.FieldType == typeof(GameObject))
                                {
                                    isReferenced = field.Equals(target);
                                }
                            }
                        }
                        if (isReferenced) return true;
                    }
                    return isReferenced;
                }
                */
            case eAssetType.TMP_FONT:
                {
                    TMP_Text tmp = tr.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        if (tmp.font != null)
                        {
                            isReferenced = target.GetInstanceID() == tmp.font.GetInstanceID();
                            if (isReferenced) return true;
                        }
                    }
                    return isReferenced;
                }
        }
        return isReferenced;
    }
}

/// <summary>
/// 자신을 제외한 자식을 리스트로 리턴한다 
/// </summary>
public static class TransformGetAllChildren
{
    public static List<Transform> GetAllChildren(this Transform parent, List<Transform> transformList = null)
    {
        if (transformList == null) transformList = new List<Transform>();
        foreach (Transform child in parent)
        {
            transformList.Add(child);
            child.GetAllChildren(transformList);
        }
        return transformList;
    }
}
