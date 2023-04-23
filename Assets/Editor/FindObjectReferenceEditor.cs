using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.UI;
//스크립트 정보 볼때 쓰임.
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
    /// 미구현
    /// </summary>
    PREFAB,  
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
        FindObjectReferenceEditor window = GetWindow<FindObjectReferenceEditor>();
        window.titleContent = new GUIContent("Find Object Referenced By Prefab", "프로젝트 내에 타겟 오브젝트가 참조 하고 있는 모든 프리팹을 확인합니다");
        window.minSize = new Vector2(400, 400);
        window.maxSize = new Vector2(600, 500);
        window.Initialize();
    }

    /// <summary>
    /// 초기화.
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
        GUILayout.Label("사용 여부를 체크할 리소스를 등록 해 주세요.");
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
            //에셋타입을 지정한다.
            SettingAssetTypeByTarget();
        }
        else
        {
            referencedList.Clear();
            foldoutStates.Clear();
            return;
        }
        DrawLine(10);
        //Target이 참조된 모든 프리팹 찾기 시작.
        FindReferencedObject();

        if (referencedList.Count == 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.red;
            GUILayout.Label("[" + target.GetType().Name + "] 으로 참조된 프리팹이 없습니다.", style);
            return;
        }
        //결과 화면 보여주기
        DrawResultScrollView();
    }

    /// <summary>
    /// 타겟이 지정되면 에셋타입으로 변경합니다.
    /// </summary>
    private void SettingAssetTypeByTarget()
    {
        scriptName = null;
        Type curType = target.GetType();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        GUILayout.Label("[" + curType.Name + "] 으로 참조된 프리팹을 검색 합니다.", style);
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
            //해당 Transform의 모든 자식 리턴
            List<Transform> allChild = parent.GetAllChildren();
            for (int j = 0; j < allChild.Count; j++)
            {
                isReferenced = CheckReferencedByTarget(allChild[j]);
                if (isReferenced)
                {
                    childList.Add(allChild[j]);
                }
            }
            //Root 프리팹 Path를 Key로 저장하고 참조중인 오브젝트 리스트 저장
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
                    //애니메이터 체크
                    isReferenced = CheckAnimatorByAnimatorController(tr);
                    if (isReferenced) return true;
                }break;

            //매터리얼이 포함 된 Component 모두 체크
            case eAssetType.MATERIAL:
                {
                    //그래픽 체크
                    isReferenced = CheckGraphicByMaterial(tr);
                    if (isReferenced) return true;
                    //렌더러 체크
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
                    //그래픽 체크
                    isReferenced = CheckGraphicByTexture(tr);
                    if (isReferenced) return true;
                    //스프라이트 렌더러
                    isReferenced = CheckSpriteRendererByTexture(tr);
                    if (isReferenced) return true;
                }
                break;
            case eAssetType.MONO_BEHAVIOUR:
                {
                    //MonoScript 체크
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

                #region [프리팹 참조 - 기능 미구현]
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
    /// 참조된 오브젝트 리스트뷰로 보여주기
    /// </summary>
    private void DrawResultScrollView()
    {
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
        // 스크롤뷰 종료
        GUILayout.EndScrollView();
    }

    /// <summary>라인 그리기</summary>
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
    /// 그래픽에서 매터리얼 참조 체크
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
    /// 그래픽에서 텍스쳐 참조 체크
    /// </summary>
    private bool CheckGraphicByTexture(Transform tr)
    {
        bool isReferenced = false;
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
        return isReferenced;
    }

    /// <summary>
    /// 렌더러에서 매터리얼 참조 체크
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
    /// 스프라이트 렌더러에서 Texture 참조 체크
    /// </summary>
    private bool CheckSpriteRendererByTexture(Transform tr)
    {
        bool isReferenced = false;
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

    /// <summary>
    /// 애니메이터 컨트롤러 체크
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
    /// 오디오 클립 참조 체크
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
    /// Mono Behavior 를 참조 체크
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
    /// TMP Text를 사용하는 컴포넌트의 TMP Font 참조 체크
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
    /// Text를 사용하는 컴포넌트의 Font 참조 체크
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
}