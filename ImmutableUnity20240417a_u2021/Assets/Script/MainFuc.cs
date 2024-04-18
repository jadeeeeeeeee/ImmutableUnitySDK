using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Immutable.Passport;
using Immutable.Passport.Model;
using System;
using UnityEngine.JWT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



public class MainFuc : MonoBehaviour
{

    class IMXUser 
    {
        //public passportData passport;
        public string nickname;
        public string name;
        //public string picture;
        public string updated_at;
        //public string email;
        public string email_verified;
        //public string iss;
        public string aud;
        //public int iat;
        //public int exp;
        public string sub;
        public string sid;
    }
    
    class passportData
    {
        public string ether_key;
        public string imx_eth_address;
        public string imx_stark_address;
        public string imx_user_admin_address;
        public string stark_key;
        public string user_admin_key;
    }


    public static string CLIENTID  = "gOl4fu7oBM6q9pzSeOcXrUWaX8jhrBOL"; //测试key
    void JWTDecode(string token)
    {
        if (string.IsNullOrEmpty(token)) 
        {
            Debug.LogError("token is NullOrEmpty");
            return;
        }
        JWT.DECODEOBJ(token, "", (results) =>
        {
            if (results == null)
            {
                Debug.LogError("==JWT2 results == null:");
            }
            else 
            {
                //IMXUser verifyResponse = JObject.Parse(results).ToObject<IMXUser>();
                //IMXUser verifyResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<IMXUser>(results);
                Debug.LogError("==imx sub:" + (string)((Dictionary<string, object>)results)["sub"]);
            }
           


           
        },false);
    }


    private Passport passport;

    // Start is called before the first frame update
    async void Start()
    {
        Debug.LogError("Start1 Passport.Init ， passport == null:" + (passport == null));
        if (passport != null) 
        {

            return;
        }
        try
        {


           
            string environment = Immutable.Passport.Model.Environment.SANDBOX;
            string redirectUri = null;
            string logoutRedirectUri = null;

            // macOS editor (play scene) does not support deeplinking
#if UNITY_ANDROID || UNITY_IPHONE || (UNITY_STANDALONE_OSX && !UNITY_EDITOR_OSX)
            redirectUri = "imxbogx://callback";
            logoutRedirectUri = "imxbogx://logout";
#endif

            passport = await Passport.Init(CLIENTID, environment, redirectUri, logoutRedirectUri);


            //// Check if user's logged in before
            bool hasCredsSaved = await passport.HasCredentialsSaved();
            Debug.LogError(hasCredsSaved ? "Has credentials saved" : "Does not have credentials saved");


            Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError("Start() error:"+ ex.Message);
        }

    }


//    public async void Login()
//    {
//        Debug.Log("start Login");
//        try
//        {
          

//            // macOS editor (play scene) does not support deeplinking
//#if UNITY_ANDROID || UNITY_IPHONE || (UNITY_STANDALONE_OSX && !UNITY_EDITOR_OSX)
//            await passport.LoginPKCE();
//#else
//            await passport.Login();
//#endif

//            Debug.LogError("LoginPKCE END");
//            GetAccessToken();
//            GetIdToken();
//            GetEmail();
//        }
//        catch (Exception ex)
//        {
//            string error;
//            if (ex is PassportException passportException && passportException.IsNetworkError())
//            {
//                error = $"Login() error: Check your internet connection and try again";
//            }
//            else if (ex is OperationCanceledException)
//            {
//                error = "Login() cancelled";
//            }
//            else
//            {
//                error = $"Login() error: {ex.Message}";
//                // Restart everything
//                await passport.Logout();
//            }

//            Debug.LogError(error);
            

//        }


//    }



    public async void GetAccessToken()
    {
        string accessToken = await passport.GetAccessToken();
        Debug.LogError("accessToken:" + accessToken ?? "No access token");
        //JWTDECODE(accessToken);
    }

    public async void GetIdToken()
    {
        string idToken = await passport.GetIdToken();
        Debug.LogError("idToken:" + idToken ?? "No ID token");
        JWTDecode(idToken);
    }

    public async void GetEmail()
    {
        string email = await passport.GetEmail();
        Debug.LogError("email:" + email ?? "No email");
    }


    public async void Connect()
    {
        try
        {
   

            // macOS editor (play scene) does not support deeplinking
#if UNITY_ANDROID || UNITY_IPHONE || (UNITY_STANDALONE_OSX && !UNITY_EDITOR_OSX)
            await passport.ConnectImxPKCE();
#else
            await passport.ConnectImx();
#endif

            GetIdToken();
        }
        catch (Exception ex)
        {
            string error;
            if (ex is PassportException passportException && passportException.IsNetworkError())
            {
                error = $"Connect() error: Check your internet connection and try again";
            }
            else if (ex is OperationCanceledException)
            {
                error = "Connect() cancelled";
            }
            else
            {
                error = $"Connect() error: {ex.Message}";
                // Restart everything
                await passport.Logout();
            }

            Debug.LogError(error);

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX
          
#endif
        }
    }

    public async void Reconnect()
    {
        try
        {
            // Use existing credentials to connect to Passport
            Debug.LogError("Reconnecting into Passport using saved credentials...");
           
            bool connected = await passport.ConnectImx(useCachedSession: true);
            if (connected)
            {
               
            }
            else
            {
                Debug.LogError($"Could not connect using saved credentials");
                ClearStorageAndCache();

            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Reconnect() error: {ex.Message}");
            ClearStorageAndCache();
        }
    }

    public void ClearStorageAndCache()
    {
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        passport.ClearStorage();
        passport.ClearCache(true);
         Debug.LogError("Cleared storage and cache");
#else
        Debug.LogError("Support on Android and iOS devices only");
#endif
    }












    // Update is called once per frame
    void Update()
    {
        
    }
}
