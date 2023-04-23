using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.UI;
//��ũ��Ʈ ���� ���� ����.
using System.Reflection;
using TMPro;
using UnityEditor.SceneManagement;
using FindObjectReferenced;

public enum eAssetType
{
    MATERIAL,
    TEXTURE2D,
    ANIMATOR_CONTROLLER,
    RENDER_TEXTURE,
    AUDIO_CLIP,
    SPRITE,
    MONO_BEHAVIOUR,
    TMP_FONT,
    FONT,
    /// <summary>
    /// �̱���
    /// </summary>
    PREFAB,  
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
        FindObjectReferenceEditor window = GetWindow<FindObjectReferenceEditor>();
        window.titleContent = new GUIContent("Find Object Referenced By Prefab", "������Ʈ ���� Ÿ�� ������Ʈ�� ���� �ϰ� �ִ� ��� �������� Ȯ���մϴ�");
        window.minSize = new Vector2(400, 400);
        window.maxSize = new Vector2(600, 500);
        window.Initialize();
    }

    /// <summary>
    /// �ʱ�ȭ.
    /// </summary>
    public void Initialize()
    {
        if (init == false)
        {
            scriptName = null;
            referencedList = new Dictionary<string, List<Transform>>();
            foldoutStates = new Dictionary<string, bool>();
            init = true;
        }
    }

    public void OnGUI()
    {
        DrawLine(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("��� ���θ� üũ�� ���ҽ��� ��� �� �ּ���.");
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
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
        else if (PreTarget == null)
        {
            PreTarget = target;
        }
        EditorGUILayout.EndHorizontal();

        if (target != null)
        {
            GUILayout.Space(5);
            //����Ÿ���� �����Ѵ�.
            SettingAssetTypeByTarget();
        }
        else
        {
            referencedList.Clear();
            foldoutStates.Clear();
            return;
        }
        DrawLine(10);
        //Target�� ������ ��� ������ ã�� ����.
        FindReferencedObject();

        if (referencedList.Count == 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.red;
            GUILayout.Label("[" + target.GetType().Name + "] ���� ������ �������� �����ϴ�.", style);
            return;
        }
        //��� ȭ�� �����ֱ�
        DrawResultScrollView();
    }

    /// <summary>
    /// Ÿ���� �����Ǹ� ����Ÿ������ �����մϴ�.
    /// </summary>
    private void SettingAssetTypeByTarget()
    {
        scriptName = null;
        Type curType = target.GetType();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        GUILayout.Label("[" + curType.Name + "] ���� ������ �������� �˻� �մϴ�.", style);
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
            case "Font": assetType = eAssetType.FONT; break;
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
            //�ش� Transform�� ��� �ڽ� ����
            List<Transform> allChild = parent.GetAllChildren();
            for (int j = 0; j < allChild.Count; j++)
            {
                isReferenced = CheckReferencedByTarget(allChild[j]);
                if (isReferenced)
                {
                    childList.Add(allChild[j]);
                }
            }
            //Root ������ Path�� Key�� �����ϰ� �������� ������Ʈ ����Ʈ ����
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
                    //�ִϸ����� üũ
                    isReferenced = CheckAnimatorByAnimatorController(tr);
                    if (isReferenced) return true;
                }break;

            //���͸����� ���� �� Component ��� üũ
            case eAssetType.MATERIAL:
                {
                    //�׷��� üũ
                    isReferenced = CheckGraphicByMaterial(tr);
                    if (isReferenced) return true;
                    //������ üũ
                    isReferenced = CheckRendererByMaterial(tr);
                    if (isReferenced) return true;
                }break;

            case eAssetType.AUDIO_CLIP:
                {
                    isReferenced = CheckAudioSourceByAudioClip(tr);
                    if (isReferenced) return true;
                }break;

            case eAssetType.RENDER_TEXTURE:
            case eAssetType.TEXTURE2D:
            case eAssetType.SPRITE:
                {
                    //�׷��� üũ
                    isReferenced = CheckGraphicByTexture(tr);
                    if (isReferenced) return true;
                    //��������Ʈ ������
                    isReferenced = CheckSpriteRendererByTexture(tr);
                    if (isReferenced) return true;
                }
                break;
            case eAssetType.MONO_BEHAVIOUR:
                {
                    //MonoScript üũ
                    isReferenced = CheckMonoBehaviorByMonoScript(tr);
                    if (isReferenced) return true;
                }break;
            case eAssetType.TMP_FONT:
                {
                    isReferenced = CheckTmpTextByTmpFontAsset(tr);
                    if (isReferenced) return true;
                }break;

            case eAssetType.FONT:
                {
                    isReferenced = CheckTextByFont(tr);
                    if (isReferenced) return true;
                }
                break;

                #region [������ ���� - ��� �̱���]
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
                #endregion

        }
        return isReferenced;
    }

    /// <summary>
    /// ������ ������Ʈ ����Ʈ��� �����ֱ�
    /// </summary>
    private void DrawResultScrollView()
    {
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
            foldoutStates[obj.Key] = GUILayout.Toggle(foldoutStates[obj.Key], "( " + obj.Value.Count + " ) " + obj.Key, GUI.skin.button, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Go To", GUILayout.Width(100)))
            {
                PrefabStageUtility.OpenPrefab(obj.Key);
                //Selection.activeObject = tr.gameObject;
            }
            GUILayout.EndHorizontal();

            if (foldoutStates[obj.Key])
            {
                foreach (Transform child in obj.Value)
                {
                    if (GUILayout.Button(child.name))
                    {
                        Selection.activeGameObject = child.gameObject;
                    }
                }
            }
        }
        // ��ũ�Ѻ� ����
        GUILayout.EndScrollView();
    }

    /// <summary>���� �׸���</summary>
    private void DrawLine(int aSpace)
    {
        GUILayout.Space(aSpace);
        var rect = EditorGUILayout.BeginHorizontal();
        Handles.color = Color.gray;
        Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(aSpace);
    }

    /// <summary>
    /// �׷��ȿ��� ���͸��� ���� üũ
    /// </summary>
    private bool CheckGraphicByMaterial(Transform tr)
    {
        bool isReferenced = false;
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
        return isReferenced;
    }

    /// <summary>
    /// �׷��ȿ��� �ؽ��� ���� üũ
    /// </summary>
    private bool CheckGraphicByTexture(Transform tr)
    {
        bool isReferenced = false;
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
        return isReferenced;
    }

    /// <summary>
    /// ���������� ���͸��� ���� üũ
    /// </summary>    
    private bool CheckRendererByMaterial(Transform tr)
    {
        bool isReferenced = false;
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

    /// <summary>
    /// ��������Ʈ ���������� Texture ���� üũ
    /// </summary>
    private bool CheckSpriteRendererByTexture(Transform tr)
    {
        bool isReferenced = false;
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

    /// <summary>
    /// �ִϸ����� ��Ʈ�ѷ� üũ
    /// </summary>
    private bool CheckAnimatorByAnimatorController(Transform tr)
    {
        bool isReferenced = false;
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

    /// <summary>
    /// ����� Ŭ�� ���� üũ
    /// </summary>
    private bool CheckAudioSourceByAudioClip(Transform tr)
    {
        bool isReferenced = false;
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

    /// <summary>
    /// Mono Behavior �� ���� üũ
    /// </summary>
    private bool CheckMonoBehaviorByMonoScript(Transform tr)
    {
        bool isReferenced = false;
        MonoBehaviour s = tr.GetComponent<MonoBehaviour>();
        if (s != null)
        {
            Type type = s.GetType();
            isReferenced = scriptName == type.Name;
            if (isReferenced) return true;
        }
        return isReferenced;
    }

    /// <summary>
    /// TMP Text�� ����ϴ� ������Ʈ�� TMP Font ���� üũ
    /// </summary>
    private bool CheckTmpTextByTmpFontAsset(Transform tr)
    {
        bool isReferenced = false;
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

    /// <summary>
    /// Text�� ����ϴ� ������Ʈ�� Font ���� üũ
    /// </summary>
    private bool CheckTextByFont(Transform tr)
    {
        bool isReferenced = false;
        Text text = tr.GetComponent<Text>();
        if (text != null)
        {
            if (text.font != null)
            {
                isReferenced = target.GetInstanceID() == text.font.GetInstanceID();
                if (isReferenced) return true;
            }
        }
        return isReferenced;
    }
}

namespace FindObjectReferenced
{
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
}