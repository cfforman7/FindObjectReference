using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.UI;
//��ũ��Ʈ ���� ���� ����.
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
/// ������Ʈ ���� Target�� ���� �Ǿ� �ִ� ��� ������Ʈ�� ã���ݴϴ�.
/// </summary>
public class FindObjectReferenceEditor : EditorWindow
{
    private static bool init = false;
    private UnityEngine.Object target;
    private UnityEngine.Object PreTarget;
    private eAssetType assetType;
    private Dictionary<string, List<Transform>> referencedList = new Dictionary<string, List<Transform>>();
    //����Ʈ �׸� ���� ���� ����
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
        GUILayout.Label("��� ���θ� üũ�� ���ҽ��� ��� �� �ּ���.");
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
            GUILayout.Label("["+ curType.Name+ "] ���� ������ �������� �˻� �մϴ�." , style);
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
        //Target�� ������ ��� ������ ã�� ����.
        FindReferencedObject();

        if (referencedList.Count == 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.red;
            GUILayout.Label("[" + target.GetType().Name + "] ���� ������ �������� �����ϴ�.", style);
            return;
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        // ���� ������Ʈ ����Ʈ�� ��ȸ�ϸ� �� �׸��� �����ش�
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
        GUILayout.EndScrollView(); // ��ũ�Ѻ� ����
    }
        
    /// <summary>
    /// Target�� ���� �ϰ� �ִ� ��� �������� ã�Ƽ� ����Ʈ�� �����Ѵ�.
    /// </summary>
    private void FindReferencedObject()
    {
        //������ Ÿ�� ��� ����
        string[] GUIDArray = AssetDatabase.FindAssets("t:GameObject");
        for (int i = 0; i < GUIDArray.Length; i++)
        {
            //����Ʈ ����.
            List<Transform> childList = new List<Transform>();
            string path = AssetDatabase.GUIDToAssetPath(GUIDArray[i]);
            Transform parent = (Transform)AssetDatabase.LoadAssetAtPath(path, typeof(Transform));
            //�θ� ��ü���� Target�� ���� �ϴ°��� �ִ°�?
            bool isReferenced = CheckReferencedByTarget(parent);
            if (isReferenced)
            {
                //����Ʈ�� �߰�
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
    /// Transform�� Component�� �����Ͽ� Target�� ���Ѵ�
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

            //���͸����� ���� �� Component ��� üũ
            case eAssetType.MATERIAL:
                {
                    //��� �׷���
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

                    //��� Renderer üũ
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
                    //��� �׷���
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

                    //��������Ʈ ������ üũ
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
/// �ڽ��� ������ �ڽ��� ����Ʈ�� �����Ѵ� 
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
