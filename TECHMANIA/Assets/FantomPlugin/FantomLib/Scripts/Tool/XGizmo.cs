using System;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Gizmo shpere, cube, line, mesh shape
    /// http://fantom1x.blog130.fc2.com/blog-entry-228.html
    /// 
    /// 球・箱・線・メッシュ状の Gizmo
    /// http://fantom1x.blog130.fc2.com/blog-entry-228.html
    /// </summary>
    public class XGizmo : MonoBehaviour
    {
        //Gizmo's shape  //Gizmo の形状
        [Serializable]
        public enum Shape
        {
            Sphere = 0,
            Cube,
            Line,
            WireSphere,
            WireCube,
            Mesh,
            WireMesh,
        }

        public bool visible = true;         //Visible state (because it is displayed on the editor even if you do not play)     //可視状態（プレイしなくてもエディタ上では表示されるため）
        public Color color = Color.yellow;  //Gizmo's color
        public Space space = Space.Self;    //World or Local values

        public bool colliderSync = false;   //Synchronize to the size of the attached collider.     //アタッチされてるコライダのサイズに同期する
        public int colliderIndex = 0;       //Serial number of the attached corridor (0 to n-1).    //アタッチされてるコライダの連番（0～n-1）

        public bool scaleSync = true;       //Synchronize to scale                                  //スケールに同期する

        //Use your own icon (linked to the editor 's Gizmo size setting (slider))
        //(*) The icon image to use must create a folder named Gizmos and put it in there.
        //(*) For the scale of the icon, the slider of 'Gizmos > 3D Icons' in the scene view is applied.
        //
        //独自のアイコンを使用する（エディタの Gizmo のサイズ設定(スライダ)に連動する）
        //※使用するアイコン画像は Gizmos という名前のフォルダを作成し、そこに格納しておく必要がある。
        //※アイコンのスケールはシーンビューの「Gizmos＞3D Icons」のスライダーが適用される。
        public string iconImage = "";       //Not need the extension    //拡張子はいらない

        public Shape shape = Shape.Sphere;

        public Sphere sphereParam = new Sphere(Vector3.zero, 0.1f);
        public Cube cubeParam = new Cube(Vector3.zero, Vector3.one);
        public Line lineParam = new Line(Vector3.zero, Vector3.one);

        public Mesh mesh;


        // Use this for initialization
        void Start()
        {
            
        }

        //Update is called once per frame
        //void Update () {
        //
        //}

        void OnDrawGizmos()
        {
            if (!visible)
                return;

            Gizmos.color = color;

            //Gizmo's icon (It needs to be in the 'Gizmos' folder)
            if (!string.IsNullOrEmpty(iconImage))
            {
                Gizmos.DrawIcon(transform.position, iconImage, true);
            }


            Vector3 scale = scaleSync ? transform.localScale : Vector3.one;

            switch (shape)
            {
                case Shape.Sphere:
                    if (colliderSync)
                    {
                        SphereCollider[] colliders = GetComponents<SphereCollider>();
                        if (colliders.Length > 0 && colliderIndex < colliders.Length)
                        {
                            sphereParam.center = colliders[colliderIndex].center;
                            sphereParam.radius = colliders[colliderIndex].radius;
                        }
                    }

                    if (space == Space.Self)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                        Gizmos.DrawSphere(sphereParam.center, sphereParam.radius);
                    }
                    else
                    {
                        Gizmos.matrix = Matrix4x4.TRS(sphereParam.center, transform.rotation, scale);
                        Gizmos.DrawSphere(Vector3.zero, sphereParam.radius);
                    }
                    break;

                case Shape.WireSphere:
                    if (colliderSync)
                    {
                        SphereCollider[] colliders = GetComponents<SphereCollider>();
                        if (colliders.Length > 0 && colliderIndex < colliders.Length)
                        {
                            sphereParam.center = colliders[colliderIndex].center;
                            sphereParam.radius = colliders[colliderIndex].radius;
                        }
                    }

                    if (space == Space.Self)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                        Gizmos.DrawWireSphere(sphereParam.center, sphereParam.radius);
                    }
                    else
                    {
                        Gizmos.matrix = Matrix4x4.TRS(sphereParam.center, transform.rotation, scale);
                        Gizmos.DrawWireSphere(Vector3.zero, sphereParam.radius);
                    }
                    break;

                case Shape.Cube:
                    if (colliderSync)
                    {
                        BoxCollider[] colliders = GetComponents<BoxCollider>();
                        if (colliders.Length > 0 && colliderIndex < colliders.Length)
                        {
                            cubeParam.center = colliders[colliderIndex].center;
                            cubeParam.size = colliders[colliderIndex].size;
                        }
                    }

                    if (space == Space.Self)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                        Gizmos.DrawCube(cubeParam.center, cubeParam.size);
                    }
                    else
                    {
                        Gizmos.matrix = Matrix4x4.TRS(cubeParam.center, transform.rotation, scale);
                        Gizmos.DrawCube(Vector3.zero, cubeParam.size);
                    }
                    break;

                case Shape.WireCube:
                    if (colliderSync)
                    {
                        BoxCollider[] colliders = GetComponents<BoxCollider>();
                        if (colliders.Length > 0 && colliderIndex < colliders.Length)
                        {
                            cubeParam.center = colliders[colliderIndex].center;
                            cubeParam.size = colliders[colliderIndex].size;
                        }
                    }

                    if (space == Space.Self)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                        Gizmos.DrawWireCube(cubeParam.center, cubeParam.size);
                    }
                    else
                    {
                        Gizmos.matrix = Matrix4x4.TRS(cubeParam.center, transform.rotation, scale);
                        Gizmos.DrawWireCube(Vector3.zero, cubeParam.size);
                    }
                    break;

                case Shape.Line:
                    if (space == Space.Self)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                        Gizmos.DrawLine(lineParam.from, lineParam.to);
                    }
                    else
                    {
                        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, scale);
                        Gizmos.DrawLine(lineParam.from, lineParam.to);
                    }
                    break;

                case Shape.Mesh:
                    if (space == Space.Self)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                        Gizmos.DrawMesh(mesh);
                    }
                    else
                    {
                        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, scale);
                        Gizmos.DrawMesh(mesh);
                    }
                    break;

                case Shape.WireMesh:
                    if (space == Space.Self)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                        Gizmos.DrawWireMesh(mesh);
                    }
                    else
                    {
                        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, scale);
                        Gizmos.DrawWireMesh(mesh);
                    }
                    break;

                default:
                    break;
            }
        }

#region Shape parameters

        [Serializable]
        public class Sphere
        {
            public Vector3 center = Vector3.zero;
            public float radius = 1f;

            public Sphere() { }

            public Sphere(Vector3 center, float radius)
            {
                this.center = center;
                this.radius = radius;
            }

            public Sphere(float x, float y, float z, float radius)
            {
                this.center = new Vector3(x, y, z);
                this.radius = radius;
            }
        }

        [Serializable]
        public class Cube
        {
            public Vector3 center = Vector3.zero;
            public Vector3 size = Vector3.one;

            public Cube() { }

            public Cube(Vector3 center, Vector3 size)
            {
                this.center = center;
                this.size = size;
            }

            public Cube(float x, float y, float z, float scaleX, float scaleY, float scaleZ)
            {
                center = new Vector3(x, y, z);
                size = new Vector3(scaleX, scaleY, scaleZ);
            }
        }

        [Serializable]
        public class Line
        {
            public Vector3 from = Vector3.zero;
            public Vector3 to = Vector3.zero;

            public Line() { }

            public Line(Vector3 from, Vector3 to)
            {
                this.from = from;
                this.to = to;
            }

            public Line(float x1, float y1, float z1, float x2, float y2, float z2)
            {
                from = new Vector3(x1, y1, z1);
                to = new Vector3(x2, y2, z2);
            }
        }
#endregion
    }
}