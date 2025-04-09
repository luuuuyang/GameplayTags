using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameplayTags
{
    public static class TagReferenceChecker
    {
        public static bool IsTagReferencedInAsset(Object asset, string tag)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            SerializedProperty property = serializedObject.GetIterator();

            return CheckPropertyForTag(property, tag);
        }

        private static bool CheckPropertyForTag(SerializedProperty property, string tag)
        {
            while (property.NextVisible(true))
            {
                // 检查当前属性
                if (property.propertyType == SerializedPropertyType.Generic && property.isArray == false)
                {
                    if (property.type == nameof(GameplayTag))
                    {
                        if (property.boxedValue is GameplayTag gameplayTag && gameplayTag.TagName == tag)
                        {
                            return true;
                        }
                    }
                    else if (property.type == nameof(GameplayTagContainer))
                    {
                        if (property.boxedValue is GameplayTagContainer container && container.GameplayTags.Contains(new GameplayTag(tag)))
                        {
                            return true;
                        }
                    }
                }

                // // 如果是嵌套结构，递归检查
                // if (property.isArray)
                // {
                //     for (int i = 0; i < property.arraySize; i++)
                //     {
                //         var element = property.GetArrayElementAtIndex(i);
                //         if (CheckPropertyForTag(element, tag))
                //         {
                //             return true;
                //         }
                //     }
                // }
            }

            return false;
        }
        public static bool IsTagReferencedInProject(string tag)
        {
            // 查找所有预制件
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var prefabGuid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (IsTagReferencedInAsset(prefab, tag))
                {
                    return true;
                }
            }

            // 查找所有 ScriptableObject
            var scriptableObjects = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var scriptableObjectGuid in scriptableObjects)
            {
                var scriptableObjectPath = AssetDatabase.GUIDToAssetPath(scriptableObjectGuid);
                var scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(scriptableObjectPath);
                if (IsTagReferencedInAsset(scriptableObject, tag))
                {
                    return true;
                }
            }

            if (IsTagReferencedInAllScenes(tag))
            {
                return true;
            }

            return false;
        }

        public static bool IsTagReferencedInCurrentScene(string tag)
        {
            // 获取当前场景中的所有 GameObject
            var gameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var gameObject in gameObjects)
            {
                if (IsTagReferencedInAsset(gameObject, tag))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTagReferencedInAllScenes(string tag)
        {
            // 查找所有场景文件
            var sceneGuids = AssetDatabase.FindAssets("t:Scene a:assets");
            foreach (var sceneGuid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

                // 加载场景
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    // 检查当前场景
                    if (IsTagReferencedInCurrentScene(tag))
                    {
                        return true;
                    }
                }
                finally
                {
                    // 关闭场景
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            return false;
        }
    }
}
