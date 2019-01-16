using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class containing extension methods
/// </summary>
public static class ExtensionMethods
{

    /// <summary>
    /// Returns component-wise absolute value.
    /// </summary>
    /// <returns>Vector3 with absolute components.</returns>
    /// <param name="vector3">Vector3.</param>
    public static Vector3 ComponentAbs(this Vector3 vector3)
    {
        vector3.x = Mathf.Abs(vector3.x);
        vector3.y = Mathf.Abs(vector3.y);
        vector3.z = Mathf.Abs(vector3.z);
        return vector3;
    }

    /// <summary>
    /// Returns component-wise sum of Vector3
    /// </summary>
    /// <returns>Component-wise sum of Vector3.</returns>
    /// <param name="vector3">Vector3.</param>
    public static float ComponentSum(this Vector3 vector3)
    {
        return vector3.x + vector3.y + vector3.z;
    }

    /// <summary>
    /// Returns component-wise sum of Vector3 and <paramref name="num"/>
    /// </summary>
    /// <returns>Component-wise sum of Vector3.</returns>
    /// <param name="vector3">Vector3.</param>
    public static Vector3 ComponentPlus(this Vector3 vector3, float num)
    {
        vector3.x = vector3.x + num;
        vector3.y = vector3.y + num;
        vector3.z = vector3.z + num;
        return vector3;
    }

    /// <summary>
    /// Returns Vector3 with it's components inverted.
    /// </summary>
    /// <returns>Vector3 with inverted components.</returns>
    /// <param name="vector3">Vector3.</param>
    public static Vector3 ComponentInverse(this Vector3 vector3)
    {
        if (!Mathf.Approximately(vector3.x, 0))
        { vector3.x = 1f / vector3.x; }
        if (!Mathf.Approximately(vector3.y, 0))
        { vector3.y = 1f / vector3.y; }
        if (!Mathf.Approximately(vector3.z, 0))
        { vector3.z = 1f / vector3.z; }
        return vector3;
    }

    /// <summary>
    /// Returns boolean reflecting whether two Vector3's were component-wise identical.
    /// </summary>
    /// <returns><c>true</c>, if are components are equal, <c>false</c> otherwise.</returns>
    /// <param name="main">Vector3.</param>
    /// <param name="other">Vector3.</param>
    public static bool ComponentsAreApproxEqualTo(this Vector3 main, Vector3 other)
    {
        if (Mathf.Approximately(main.x, other.x) && Mathf.Approximately(main.y, other.y) && Mathf.Approximately(main.z, other.z))
        { return true; }
        else
        { return false; }
    }

    /// <summary>
    /// Returns boolean reflecting whether Vector3 is component-wise infinite.
    /// </summary>
    /// <returns><c>true</c>, if are components are infinite, <c>false</c> otherwise.</returns>
    /// <param name="main">Vector3.</param>
    public static bool IsInfinite(this Vector3 main)
    {
        if (float.IsInfinity(main.x) && float.IsInfinity(main.y) && float.IsInfinity(main.z))
        { return true; }
        else
        { return false; }
    }

    /// <summary>
    /// Returns boolean reflecting whether Vector3 is component-wise NaN.
    /// </summary>
    /// <returns><c>true</c>, if are components are NaN, <c>false</c> otherwise.</returns>
    /// <param name="main">Vector3.</param>
    public static bool IsNaN(this Vector3 main)
    {
        if (float.IsNaN(main.x) && float.IsNaN(main.y) && float.IsNaN(main.z))
        { return true; }
        else
        { return false; }
    }

    /// <summary>
    /// Returns Vector3 with all components NaN'ed.
    /// </summary>
    /// <returns><c>true</c>, if are components are NaN, <c>false</c> otherwise.</returns>
    /// <param name="main">Vector3.</param>
    public static Vector3 NaN(this Vector3 main)
    {
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }


    /// <summary>
    /// Returns boolean reflecting whether two Vector3Ints's were component-wise identical.
    /// </summary>
    /// <returns><c>true</c>, if are components are equal, <c>false</c> otherwise.</returns>
    /// <param name="main">Vector3Int.</param>
    /// <param name="other">Vector3Int.</param>
    public static bool ComponentsAreEqualTo(this Vector3Int main, Vector3Int other)
    {
        if (main.x == other.x && main.y == other.y && main.z == other.z)
        { return true; }
        else
        { return false; }
    }

    /// <summary>
    /// Returns vector3 containing the sign of each component.
    /// </summary>
    /// <returns>Vector3 of signs.</returns>
    /// <param name="vector3">Vector3.</param>
    public static Vector3 ComponentSign(this Vector3 vector3)
    {
        return new Vector3(Mathf.Sign(vector3.x), Mathf.Sign(vector3.y), Mathf.Sign(vector3.z));
    }

    /// <summary>
    /// Returns maximum of x/y/z components.
    /// </summary>
    /// <returns>Maximum of component values.</returns>
    /// <param name="vector3">Vector3.</param>
    public static float ComponentMax(this Vector3 vector3)
    {
        return Mathf.Max(vector3.x, vector3.y, vector3.z);
    }

    /// <summary>
    /// Returns minimum of x/y/z components.
    /// </summary>
    /// <returns>Minimum of component values.</returns>
    /// <param name="vector3">Vector3.</param>
    public static float ComponentMin(this Vector3 vector3)
    {
        return Mathf.Min(vector3.x, vector3.y, vector3.z);
    }

    /// <summary>
    /// Adds item to list if not present.
    /// </summary>
    /// <param name="list">List.</param>
    /// <param name="item">Item.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public static void AddIfNotPresent<T>(this List<T> list, T item)
    {
        if (!list.Contains(item)) { list.Add(item); }
    }

    /// <summary>
    /// Whether (unordered) contents are equal to contents of other list.
    /// </summary>
    /// <returns><c>true</c>, if contents are equal, <c>false</c> otherwise.</returns>
    /// <param name="list">List.</param>
    /// <param name="other">Other.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public static bool EqualContents<T>(this List<T> list, List<T> other)
    {
        if (list.Count != other.Count) { return false; }
        foreach (T item in other)
        {
            if (!list.Contains(item))
            { return false; }
        }
        return true;
    }

    /// <summary>
    /// Randomizes the order in list.
    /// </summary>
    /// <returns>The order.</returns>
    /// <param name="list">List.</param>
    /// <typeparam name="T">Type parameter.</typeparam>
    public static List<T> RandomizeOrder<T>(this List<T> list)
    {
        List<T> randOrderList = new List<T>(list.Count);
        int[] randIndices = Utilities.RandomIndices(list.Count);
        foreach (int ind in randIndices)
        { randOrderList.Add(list[ind]); }
        return randOrderList;
    }

    /// <summary>
    /// Contains(), but with list contents instead of list reference.
    /// </summary>
    /// <returns><c>true</c>, if contains list key with contents, <c>false</c> otherwise.</returns>
    /// <param name="dictionary">Dictionary.</param>
    /// <param name="listKey">List.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    /// <typeparam name="U">The 2nd type parameter.</typeparam>
    public static bool ContainsListKeyWithContents<T, U>(this Dictionary<List<T>, U> dictionary, List<T> listKey)
    {
        foreach (List<T> key in dictionary.Keys)
        { if (key.EqualContents(listKey)) { return true; } }
        return false;
    }

    /// <summary>
    /// Returns value belonging to key, where key is a list that needs to present
    /// content-wise, not by reference.
    /// </summary>
    /// <returns>The value from key with list key with equal contents.</returns>
    /// <param name="dictionary">Dictionary.</param>
    /// <param name="listKey">List key.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    /// <typeparam name="U">The 2nd type parameter.</typeparam>
    public static U ReturnValueFromListKeyWithEqualContents<T, U>(this Dictionary<List<T>, U> dictionary, List<T> listKey)
    {
        foreach (List<T> key in dictionary.Keys)
        {
            if (key.EqualContents(listKey)) { return dictionary[key]; }
        }
        throw new System.Exception("Key not present.");
    }
}
