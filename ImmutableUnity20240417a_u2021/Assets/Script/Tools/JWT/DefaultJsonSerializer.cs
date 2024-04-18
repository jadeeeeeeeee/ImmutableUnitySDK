﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.JWT
{
    /// <summary>
    /// JSON Serializer using JavaScriptSerializer
    /// </summary>
    public class DefaultJsonSerializer : IJsonSerializer
    {
        /// <summary>
        /// Serialize an object to JSON string
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>JSON string</returns>
        public string Serialize(object obj)
        {
            return JObject.FromObject(obj).ToString(Formatting.None);
        }

        /// <summary>
        /// Deserialize a JSON string to typed object.
        /// </summary>
        /// <typeparam name="T">type of object</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>typed object</returns>
        public T Deserialize<T>(string json)
        {
            return JObject.Parse(json).ToObject<T>();
        }
    }
}
