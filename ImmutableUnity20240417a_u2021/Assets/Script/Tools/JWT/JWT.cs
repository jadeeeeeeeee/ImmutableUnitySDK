﻿
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace UnityEngine.JWT
{
    public enum JwtHashAlgorithm
    {
        HS256,
        HS384,
        HS512,
        RS256
    }

    /// <summary>
    /// Provides methods for encoding and decoding JSON Web Tokens.
    /// </summary>
    public static class JsonWebToken
    {
        private static readonly IDictionary<JwtHashAlgorithm, Func<byte[], byte[], byte[]>> HashAlgorithms;

        /// <summary>
        /// Pluggable JSON Serializer
        /// </summary>
        //public static IJsonSerializer JsonSerializer = new DefaultJsonSerializer();
        public static IJsonSerializer JsonSerializer = new DefaultJsonSerializer();

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        static JsonWebToken()
        {
            HashAlgorithms = new Dictionary<JwtHashAlgorithm, Func<byte[], byte[], byte[]>>
            {
                { JwtHashAlgorithm.HS256, (key, value) => { using (var sha = new HMACSHA256(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.HS384, (key, value) => { using (var sha = new HMACSHA384(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.HS512, (key, value) => { using (var sha = new HMACSHA512(key)) { return sha.ComputeHash(value); } } },
                //{ JwtHashAlgorithm.RS256, (key, value) => { using (var sha = new rs(key)) { return sha.ComputeHash(value); } } },
            };
        }

        /// <summary>
        /// Creates a JWT given a header, a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="extraHeaders">An arbitrary set of extra headers. Will be augmented with the standard "typ" and "alg" headers.</param>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key bytes used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(IDictionary<string, object> extraHeaders, object payload, byte[] key, JwtHashAlgorithm algorithm)
        {
            var segments = new List<string>();
            var header = new Dictionary<string, object>(extraHeaders)
            {
                { "typ", "JWT" },
                { "alg", algorithm.ToString() }
            };

            byte[] headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header));
            byte[] payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            segments.Add(Base64UrlEncode(headerBytes));
            segments.Add(Base64UrlEncode(payloadBytes));

            var stringToSign = string.Join(".", segments.ToArray());

            var bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

            byte[] signature = HashAlgorithms[algorithm](key, bytesToSign);
            segments.Add(Base64UrlEncode(signature));

            return string.Join(".", segments.ToArray());
        }

        public static string Encode(string payload, string key, JwtHashAlgorithm algorithm)
        {
            var segments = new List<string>();
            //var header = new JsonData();
            //header["typ"] = "JWT";
            //header["alg"] = algorithm.ToString();

            //byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
            var header = new Dictionary<string, object>()
            {
                { "typ", "JWT" },
                { "alg", algorithm.ToString() }
            };

            byte[] headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header));
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            segments.Add(Base64UrlEncode(headerBytes));
            segments.Add(Base64UrlEncode(payloadBytes));

            var stringToSign = string.Join(".", segments.ToArray());

            var bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

            byte[] signature = HashAlgorithms[algorithm](Encoding.UTF8.GetBytes(key), bytesToSign);
            segments.Add(Base64UrlEncode(signature));

            return string.Join(".", segments.ToArray());
        }

        /// <summary>
        /// Creates a JWT given a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(object payload, byte[] key, JwtHashAlgorithm algorithm)
        {
            return Encode(new Dictionary<string, object>(), payload, key, algorithm);
        }

        /// <summary>
        /// Creates a JWT given a set of arbitrary extra headers, a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="extraHeaders">An arbitrary set of extra headers. Will be augmented with the standard "typ" and "alg" headers.</param>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key bytes used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(IDictionary<string, object> extraHeaders, object payload, string key, JwtHashAlgorithm algorithm)
        {
            return Encode(extraHeaders, payload, Encoding.UTF8.GetBytes(key), algorithm);
        }

        /// <summary>
        /// Creates a JWT given a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(object payload, string key, JwtHashAlgorithm algorithm)
        {
            return Encode(new Dictionary<string, object>(), payload, Encoding.UTF8.GetBytes(key), algorithm);
        }

        /// <summary>
        /// Given a JWT, decode it and return the JSON payload.
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key bytes that were used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>A string containing the JSON payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        public static string Decode(string token, byte[] key, bool verify = true)
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Token must consist from 3 delimited by dot parts");
            }
            var header = parts[0];
            var payload = parts[1];
            var crypto = Base64UrlDecode(parts[2]);

            var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));

            var headerData = JsonSerializer.Deserialize<Dictionary<string, object>>(headerJson);
            //var payloadData = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
            Debug.LogError("解析 但是未校验单的数据:" + payloadJson);

            if (verify)
            {
                var bytesToSign = Encoding.UTF8.GetBytes(string.Concat(header, ".", payload));
                var algorithm = (string)headerData["alg"];

                var signature = HashAlgorithms[GetHashAlgorithm(algorithm)](key, bytesToSign);
                var decodedCrypto = Convert.ToBase64String(crypto);
                var decodedSignature = Convert.ToBase64String(signature);
                Debug.LogError("decodedCrypto:" + decodedCrypto + ", decodedSignature:"+ decodedSignature);
                Verify(decodedCrypto, decodedSignature, payloadJson);
            }

            return payloadJson;
        }

		public static bool Verify(string token, string key)
		{
			var parts = token.Split('.');
			if (parts.Length != 3)
			{
				throw new ArgumentException("Token must consist from 3 delimited by dot parts");
			}
			var header = parts[0];
			var payload = parts[1];
			var crypto = Base64UrlDecode(parts[2]);

			var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
			var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));

			var headerData = JsonSerializer.Deserialize<Dictionary<string, object>>(headerJson);

			var bytesToSign = Encoding.UTF8.GetBytes(string.Concat(header, ".", payload));
			var algorithm = (string)headerData["alg"];

			var signature = HashAlgorithms[GetHashAlgorithm(algorithm)](Encoding.UTF8.GetBytes(key), bytesToSign);
			var decodedCrypto = Convert.ToBase64String(crypto);
			var decodedSignature = Convert.ToBase64String(signature);

			return Verify(decodedCrypto, decodedSignature, payloadJson);
		}

		private static bool Verify(string decodedCrypto, string decodedSignature, string payloadJson)
        {
			bool match = false;
			bool valid = false;
			if (decodedCrypto != decodedSignature) {
				match = false;
			} else {
				match = true;
			}

            // verify exp claim https://tools.ietf.org/html/draft-ietf-oauth-json-web-token-32#section-4.1.4
            var payloadData = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
			if (payloadData.ContainsKey ("exp") && payloadData ["exp"] != null) {
				// safely unpack a boxed int 
				int exp = Convert.ToInt32 (payloadData ["exp"]);

				var secondsSinceEpoch = Math.Round ((DateTime.UtcNow - UnixEpoch).TotalSeconds);
				if (secondsSinceEpoch >= exp) {
					valid = false;
				} else {
					valid = true;
				}
			} else {
				valid = true;
			}

			if (match && valid)
				return true;
			else
				return false;
        }

        /// <summary>
        /// Given a JWT, decode it and return the JSON payload.
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>A string containing the JSON payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        public static string Decode(string token, string key, bool verify = true)
        {
            return Decode(token, Encoding.UTF8.GetBytes(key), verify);
        }

        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        public static object DecodeToObject(string token, byte[] key, bool verify = true)
        {
            var payloadJson = Decode(token, key, verify);
            var payloadData = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
            return payloadData;
        }

        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        public static object DecodeToObject(string token, string key, bool verify = true)
        {
            return DecodeToObject(token, Encoding.UTF8.GetBytes(key), verify);
        }

        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to return</typeparam>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        public static T DecodeToObject<T>(string token, byte[] key, bool verify = true)
        {
            var payloadJson = Decode(token, key, verify);
            var payloadData = JsonSerializer.Deserialize<T>(payloadJson);
            return payloadData;
        }

        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to return</typeparam>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        public static T DecodeToObject<T>(string token, string key, bool verify = true)
        {
            return DecodeToObject<T>(token, Encoding.UTF8.GetBytes(key), verify);
        }

        private static JwtHashAlgorithm GetHashAlgorithm(string algorithm)
        {
            Debug.LogError("GetHashAlgorithm:" + algorithm);
            switch (algorithm)
            {
                case "HS256": return JwtHashAlgorithm.HS256;
                case "HS384": return JwtHashAlgorithm.HS384;
                case "HS512": return JwtHashAlgorithm.HS512;
                case "RS256": return JwtHashAlgorithm.RS256;
                default: throw new SignatureVerificationException("Algorithm not supported.");
            }
        }

        // from JWT spec
        public static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }

        // from JWT spec
        public static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break;  // One pad char
                default: throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }

	public class JWT {
		public static void ENCODE(Dictionary<string, object> payload, string secretkey, Action<string> callback = null)
		{
			string token = JsonWebToken.Encode (payload, secretkey, JwtHashAlgorithm.HS256);
			if (callback != null)
				callback (token);
		}

        public static string ENCODE(string payload, string secretkey)
        {
            return JsonWebToken.Encode(payload, secretkey, JwtHashAlgorithm.HS256);
        }

        public static void DECODE (string token, string secretkey, Action<string> callback = null, bool verify = true)
		{
			try {
				string results = JsonWebToken.Decode(token, secretkey, verify);
				if(callback != null)
					callback(results);
			} catch(UnityException e) {
				Debug.Log (e);
			}
		}  
        
        public static void DECODEOBJ (string token, string secretkey, Action<object> callback = null, bool verify = true)
		{
			try {
				object results = JsonWebToken.DecodeToObject(token, secretkey, verify);
				if(callback != null)
					callback(results);
			} catch(UnityException e) {
				Debug.Log (e);
			}
		}

		public static bool VERIFY(string token, string secretkey)
		{
			bool v = false;
			try {
				v = JsonWebToken.Verify(token, secretkey);
			} catch(UnityException e) {
				Debug.Log (e);
				v = false;
			}
			return v;
		}
	}
}
