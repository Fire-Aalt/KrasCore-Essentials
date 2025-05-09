#if UNITY_EDITOR

using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KrasCore.Essentials
{
    /// <summary>
    /// EditorPrefs data is common across projects, that means settings from one project will reflect in other, This wrapper class fixes that issue
    /// by generating a unique key by project path.
    /// </summary>
    public static class LocalEditorPrefs
    {
        /// <summary>
        /// Return MD5 hash of the text
        /// </summary>
        /// <param name="input">text</param>
        /// <returns></returns>
        private static string GetMd5Hash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sb.Append(data[i].ToString("x2"));
            return sb.ToString();
        }
        /**Unique id by project path, This id will be pre-pended to each key*/
        private readonly static string mAppKey = null;
        /// <summary>
        /// Static constructor to set unique kay based on project path
        /// </summary>
        static LocalEditorPrefs()
        {
            mAppKey = GetMd5Hash(Application.dataPath) + "-";
        }
        /// <summary>
        /// Checks if the given key exists
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>true if exists, else false</returns>
        public static bool HasKey(string key)
        {
            return EditorPrefs.HasKey(mAppKey + key);
        }
        /// <summary>
        /// Returns the value corresponding to key
        /// </summary>
        /// <param name="key">key to use</param>
        /// <returns>value of the given key</returns>
        /// @see GetString(string,string)
        public static string GetString(string key)
        {
            return EditorPrefs.GetString(mAppKey + key);
        }
        /// <summary>
        /// Returns the value corresponding to key if it exists, else returns default value
        /// </summary>
        /// <param name="key">key to use</param>
        /// <returns>value of the given key</returns>
        /// @see GetString(string)
        public static string GetString(string key, string defaultValue)
        {
            return EditorPrefs.GetString(mAppKey + key, defaultValue);
        }
        /// <summary>
        /// Sets the value of the preference identified by key. Note that EditorPrefs does not support null strings and will store an empty string instead.
        /// <param name="key">key</param>
        /// <param name="value">value to store</param>
        public static void SetString(string key, string value)
        {
            EditorPrefs.SetString(mAppKey + key, value);
        }
        /// <summary>
        /// Returns the value corresponding to key
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>coresponding value</returns>
        /// @see GetBool(string,bool)
        public static bool GetBool(string key)
        {
            return EditorPrefs.GetBool(mAppKey + key);
        }
        /// <summary>
        /// Returns the value corresponding to key if it exists, else default value
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>coresponding value</returns>
        /// @see GetBool(string)
        public static bool GetBool(string key, bool defaultValue)
        {
            if (HasKey(key))
                return EditorPrefs.GetBool(mAppKey + key);
            return defaultValue;
        }
        /// <summary>
        /// Sets the value of the preference identified by key.
        /// </summary>
        /// <param name="key">key to use</param>
        /// <param name="value">value to save</param>
        public static void SetBool(string key, bool value)
        {
            EditorPrefs.SetBool(mAppKey + key, value);
        }
        /// <summary>
        /// Returns the value corresponding to key in the preference file if it exists.
        /// </summary>
        /// <param name="key">key to use</param>
        /// <returns>corresponding value</returns>
        /// @see GetInt(string, int)
        public static int GetInt(string key)
        {
            return EditorPrefs.GetInt(mAppKey + key);
        }
        /// <summary>
        /// Returns the value corresponding to key in the preference file if it exists else default value
        /// </summary>
        /// <param name="key">key to use</param>
        /// <returns>corresponding value</returns>
        /// @see GetInt(string)
        public static int GetInt(string key, int defaultValue)
        {
            if (HasKey(key))
                return EditorPrefs.GetInt(mAppKey + key);
            return defaultValue;
        }
        /// <summary>
        /// Sets the value of the preference identified by key as an integer.
        /// </summary>
        /// <param name="key">key to use</param>
        /// <param name="value">value to save</param>
        public static void SetInt(string key, int value)
        {
            EditorPrefs.SetInt(mAppKey + key, value);
        }
        /// <summary>
        /// Returns the value corresponding to key in the preference file if it exists.
        /// </summary>
        /// <param name="key">key to use</param>
        /// <returns>corresponding value</returns>
        /// @see GetFloat(string)
        public static float GetFloat(string key)
        {
            return EditorPrefs.GetFloat(mAppKey + key);
        }
        /// <summary>
        /// Returns the value corresponding to key in the preference file if it exists else default value
        /// </summary>
        /// <param name="key">key to use</param>
        /// <returns>corresponding value</returns>
        /// @see GetFloat(string,float)
        public static float GetFloat(string key, float defaultValue)
        {
            if (HasKey(key))
                return EditorPrefs.GetFloat(mAppKey + key);
            return defaultValue;
        }
        /// <summary>
        /// Sets the value of the preference identified by key as a float.
        /// </summary>
        /// <param name="key">key to use</param>
        /// <param name="value">value to save</param>
        public static void SetFloat(string key, float value)
        {
            EditorPrefs.SetFloat(mAppKey + key, value);
        }
        /// <summary>
        /// Delete a key
        /// </summary>
        /// <param name="key"></param>
        public static void DeleteKey(string key)
        {
            EditorPrefs.DeleteKey(mAppKey + key);
        }
    }
}
#endif