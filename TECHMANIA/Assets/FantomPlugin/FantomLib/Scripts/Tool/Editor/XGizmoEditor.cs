using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(XGizmo))]
    [CanEditMultipleObjects]
    public class XGizmoEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            XGizmo obj = target as XGizmo;

            EditorGUI.BeginChangeCheck();
            obj.visible = EditorGUILayout.Toggle("Visible", obj.visible);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.visible = obj.visible;
            }

            EditorGUI.BeginChangeCheck();
            obj.color = EditorGUILayout.ColorField("Color", obj.color);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.color = obj.color;
            }

            EditorGUI.BeginChangeCheck();
            obj.space = (Space)EditorGUILayout.EnumPopup("Space", obj.space);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.space = obj.space;
            }

            EditorGUI.BeginChangeCheck();
            obj.colliderSync = EditorGUILayout.Toggle("Collider Sync", obj.colliderSync);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.colliderSync = obj.colliderSync;
            }

            EditorGUI.BeginChangeCheck();
            obj.colliderIndex = EditorGUILayout.IntField("Collider Index", obj.colliderIndex);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.colliderIndex = obj.colliderIndex;
            }

            EditorGUI.BeginChangeCheck();
            obj.scaleSync = EditorGUILayout.Toggle("Scale Sync", obj.scaleSync);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.scaleSync = obj.scaleSync;
            }

            EditorGUI.BeginChangeCheck();
            obj.shape = (XGizmo.Shape)EditorGUILayout.EnumPopup("Shape", obj.shape);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.shape = obj.shape;
            }


            EditorGUI.indentLevel++;

            switch (obj.shape)
            {
                case XGizmo.Shape.Sphere:
                case XGizmo.Shape.WireSphere:
                    EditorGUI.BeginChangeCheck();
                    obj.sphereParam.center = EditorGUILayout.Vector3Field("Center", obj.sphereParam.center);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (XGizmo o in targets) o.sphereParam.center = obj.sphereParam.center;
                    }

                    EditorGUI.BeginChangeCheck();
                    obj.sphereParam.radius = EditorGUILayout.FloatField("Radius", obj.sphereParam.radius);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (XGizmo o in targets) o.sphereParam.radius = obj.sphereParam.radius;
                    }
                    break;

                case XGizmo.Shape.Cube:
                case XGizmo.Shape.WireCube:
                    EditorGUI.BeginChangeCheck();
                    obj.cubeParam.center = EditorGUILayout.Vector3Field("Center", obj.cubeParam.center);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (XGizmo o in targets) o.cubeParam.center = obj.cubeParam.center;
                    }

                    EditorGUI.BeginChangeCheck();
                    obj.cubeParam.size = EditorGUILayout.Vector3Field("Size", obj.cubeParam.size);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (XGizmo o in targets) o.cubeParam.size = obj.cubeParam.size;
                    }
                    break;

                case XGizmo.Shape.Line:
                    EditorGUI.BeginChangeCheck();
                    obj.lineParam.from = EditorGUILayout.Vector3Field("From", obj.lineParam.from);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (XGizmo o in targets) o.lineParam.from = obj.lineParam.from;
                    }

                    EditorGUI.BeginChangeCheck();
                    obj.lineParam.to = EditorGUILayout.Vector3Field("To", obj.lineParam.to);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (XGizmo o in targets) o.lineParam.to = obj.lineParam.to;
                    }
                    break;

#pragma warning disable 0618
                case XGizmo.Shape.Mesh:
                case XGizmo.Shape.WireMesh:
                    EditorGUI.BeginChangeCheck();
                    obj.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", obj.mesh, typeof(Mesh));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (XGizmo o in targets) o.mesh = obj.mesh;
                    }
                    break;
#pragma warning restore 0618

                default:
                    break;
            }

            EditorGUI.indentLevel--;


            EditorGUI.BeginChangeCheck();
            obj.iconImage = EditorGUILayout.TextField("Icon Image", obj.iconImage);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (XGizmo o in targets) o.iconImage = obj.iconImage;
            }


            EditorUtility.SetDirty(target);
        }

    }
}