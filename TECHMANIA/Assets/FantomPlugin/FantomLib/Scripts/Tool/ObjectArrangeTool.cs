using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FantomLib
{
    /// <summary>
    /// Arrange objects at equal intervals (mainly for UI)
    ///·Place it step by step based on objects[0].
    ///(*) Locking the inspector makes it easy to do multiple drops.
    /// 
    /// 
    /// オブジェクトを等間隔に並べる（主にUI用）
    ///・objects[0] を基準に step ごとに配置する。
    ///※インスペクタをロックすると、複数ドロップも簡単にできます。
    /// </summary>
    [ExecuteInEditMode]
    public class ObjectArrangeTool : MonoBehaviour
    {
#if UNITY_EDITOR

        //Inspector Settings
        [Serializable]
        public enum Axis {
            X, Y, Z
        }

        public Axis axis = Axis.Y;                  //Axis to be arranged       //並べる軸
        public float step = -100;                   //Alignment interval        //並べる間隔
        public Vector3 addPosition = Vector3.zero;  //Amount of translation     //平行移動の量
        public GameObject[] objects;                //Objects to be arranged    //並べるオブジェクト


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


#region Editor tool Section

        //Running flag      //実行中フラグ
        public bool executing {
            get; private set;
        }

        //Arrange objects at equal intervals based on objects[0].
        //objects[0] を基準にオブジェクトを等間隔に並べる
        public void Arrange()
        {
            if (objects == null || objects[0] == null || objects.Length < 2)
                return;

            executing = true;

            float start;
            if (axis == Axis.Y)
                start = objects[0].transform.localPosition.y;
            else if (axis == Axis.X)
                start = objects[0].transform.localPosition.x;
            else
                start = objects[0].transform.localPosition.z;

            for (int i = 1; i < objects.Length; i++)
            {
                if (objects[i] == null)
                    continue;

                Vector3 pos = objects[i].transform.localPosition;
                if (axis == Axis.Y)
                    pos.y = start + step * i;
                else if (axis == Axis.X)
                    pos.x = start + step * i;
                else
                    pos.z = start + step * i;

                objects[i].transform.localPosition = pos;
            }

            executing = false;
        }

        //Add it to the position of each object (move the whole in parallel).
        //各 object の位置に加算する（全体を平行移動する）。
        public void MoveObjects(Vector3 addPosition)
        {
            if (addPosition == Vector3.zero || objects == null || objects.Length == 0)
                return;

            executing = true;

            foreach (var item in objects)
            {
                if (item == null)
                    continue;

                Vector3 localPos = item.transform.localPosition;
                localPos += addPosition;
                item.transform.localPosition = localPos;
            }

            executing = false;
        }


        //Whether the index is within range.
        //インデクスが有効範囲内か？
        public bool IsValidIndex(int index)
        {
            return (objects != null && 0 <= index && index < objects.Length);
        }

        //Copy elements after 'from' index to 'to' index after.
        //'from'インデクス以降の要素を 'to'インデクス以降へコピーする
        public bool CopyElements(int from, int to)
        {
            if (from == to || !IsValidIndex(from) || !IsValidIndex(to))
                return false;

            executing = true;

            GameObject[] temp = (GameObject[])objects.Clone();
            for (int i = from, j = to; i < temp.Length && j < objects.Length; i++, j++)
                objects[j] = temp[i];

            executing = false;
            return true;
        }

        //Only when the object is unique, add it to the end of the array.
        //オブジェクトがユニークのときのみ、最後に追加する。
        public bool AddElement(GameObject go)
        {
            executing = true;

            bool added = false;

            if (objects == null)
            {
                objects = new GameObject[] { go };
                added = true;
            }
            else
            {
                if (ArrayUtility.IndexOf(objects, go) < 0)
                {
                    ArrayUtility.Add(ref objects, go);
                    added = true;
                }
            }

            executing = false;
            return added;
        }

        //Data at the specified index position is deleted.
        public bool RemoveElement(int index)
        {
            if (!IsValidIndex(index))
                return false;

            executing = true;
            ArrayUtility.RemoveAt(ref objects, index);
            executing = false;
            return true;
        }

        //Clear all elements
        public bool ClearElements()
        {
            if (objects == null || objects.Length == 0)
                return false;

            ArrayUtility.Clear(ref objects);
            return true;
        }

        //Change the length of the array. 
        //If it is shorter than the original length it is clipped and 
        //if it is longer than the original length an empty element is added.
        //配列の長さを変更する。元の長さより短い場合は切り取られ、元の長さより長い場合は空の要素が追加される。
        public bool ResizeLength(int length)
        {
            if (objects == null || objects.Length == 0 || objects.Length == length || length < 0)
                return false;

            executing = true;

            if (length < objects.Length)
                objects = objects.Where((e, i) => i < length).ToArray();
            else
                ArrayUtility.AddRange(ref objects, new GameObject[length - objects.Length]);

            executing = false;
            return true;
        }


        //Errors status
        public class ValidStatus
        {
            public List<int> emptyIndex = new List<int>();                      //Index of null element
            public HashSet<GameObject> duplicate = new HashSet<GameObject>();   //Duplicate objects
            public HashSet<GameObject> uniq = new HashSet<GameObject>();        //Unique objects

            public void ResetStatus()
            {
                emptyIndex.Clear();
                duplicate.Clear();
                uniq.Clear();
            }

            public string GetEmptyError()
            {
                return (emptyIndex.Count > 0) ? 
                    emptyIndex.Select(e => e.ToString()).Aggregate((s, e) => s + ", " + e) : "";
            }

            public string GetDuplicateError()
            {
                return (duplicate.Count > 0) ? 
                    duplicate.Select(e => e.name).Aggregate((s, e) => s + ", " + e) : "";
            }
        }

        //Check the validity of objects
        public void CheckValidity(ref ValidStatus validStatus)
        {
            validStatus.ResetStatus();

            if (objects == null || objects.Length == 0)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject go = objects[i];
                if (go != null)
                {
                    if (validStatus.uniq.Contains(go))
                        validStatus.duplicate.Add(go);
                    else
                        validStatus.uniq.Add(go);
                }
                else
                    validStatus.emptyIndex.Add(i);
            }
        }

#endregion

#endif
    }
}

