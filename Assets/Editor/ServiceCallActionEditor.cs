#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Linq;

[CustomEditor(typeof(ServiceCallAction))]
public class ServiceCallActionEditor : Editor
{
    SerializedProperty payloadProp;

    Type[] payloadTypes;
    string[] payloadTypeNames;

    void OnEnable()
    {
        payloadProp = serializedObject.FindProperty("payload");

        // 파생 타입들 수집(에디터에서만 제공되는 TypeCache 사용이 가장 빠름)
        payloadTypes = TypeCache.GetTypesDerivedFrom<CallPayload>()
                                .Where(t => !t.IsAbstract && t.IsClass && t.IsSerializable)
                                .OrderBy(t => t.FullName)
                                .ToArray();

        payloadTypeNames = payloadTypes.Select(Nicify).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Payload", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            string full = payloadProp.managedReferenceFullTypename;
            EditorGUILayout.LabelField("Type", string.IsNullOrEmpty(full) ? "None" : TypeNameFromManagedRef(full));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Payload Type...", GUILayout.Height(22)))
                    ShowTypeMenu();

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(full)))
                {
                    if (GUILayout.Button("Clear", GUILayout.Width(70)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (target == null) return;
                            var so = new SerializedObject(target);
                            var prop = so.FindProperty("payload");
                            prop.managedReferenceValue = null;
                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(target);
                        };
                        // 여기서도 ExitGUI 호출하지 않음
                    }

                }
            }

            // 타입이 설정된 경우에만 그리기
            if (!string.IsNullOrEmpty(payloadProp.managedReferenceFullTypename))
            {
                EditorGUI.indentLevel++;
                // includeChildren=true로 내부 직렬 필드 표시
                EditorGUILayout.PropertyField(payloadProp, includeChildren: true);
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }


    void ShowTypeMenu()
    {
        var menu = new GenericMenu();
        for (int i = 0; i < payloadTypes.Length; i++)
        {
            var selectedType = payloadTypes[i];
            menu.AddItem(new GUIContent(payloadTypeNames[i]), false, () =>
            {
                // 다음 에디터 루프에서 안전하게 적용
                EditorApplication.delayCall += () =>
                {
                    if (target == null) return; // 인스펙터가 닫혔을 수 있음
                    var so = new SerializedObject(target);
                    var prop = so.FindProperty("payload");
                    prop.managedReferenceValue = Activator.CreateInstance(selectedType);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    // GUIUtility.ExitGUI() 필요 없음
                };
            });
        }
        // menu.DropDown(GUILayoutUtility.GetLastRect()); // 컨텍스트로 띄우고 싶다면 ↓
        menu.ShowAsContext();
    }


    static string TypeNameFromManagedRef(string full)
    {
        // e.g. "AssemblyName TypeFullName"
        int space = full.IndexOf(' ');
        return space >= 0 ? full.Substring(space + 1) : full;
    }

    static string Nicify(Type t)
    {
        // 메뉴 표시에 보기 좋게 변환
        return t.FullName.Replace('+', '.'); // nested 타입 보호
    }
}
#endif
