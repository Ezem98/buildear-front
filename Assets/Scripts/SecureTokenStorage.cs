using System;
using System.Text;
using UnityEngine;

public static class SecureTokenStorage
{
    private const string Alias = "com.buildear.refresh-token";
    private const string EncryptedTokenKey = "refreshTokenEncrypted";
    private const string DevelopmentTokenKey = "refreshTokenDevelopment";

    public static bool Save(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Delete();
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaObject secretKey = GetOrCreateKey();
            using AndroidJavaClass cipherClass = new("javax.crypto.Cipher");
            using AndroidJavaObject cipher = cipherClass.CallStatic<AndroidJavaObject>(
                "getInstance",
                "AES/GCM/NoPadding"
            );
            cipher.Call("init", 1, secretKey);
            byte[] iv = cipher.Call<byte[]>("getIV");
            byte[] encrypted = cipher.Call<byte[]>(
                "doFinal",
                Encoding.UTF8.GetBytes(token)
            );
            PlayerPrefs.SetString(
                EncryptedTokenKey,
                Convert.ToBase64String(iv) + "." + Convert.ToBase64String(encrypted)
            );
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("No se pudo guardar el refresh token en Android Keystore: " + exception.Message);
            Delete();
            return false;
        }
#else
        PlayerPrefs.SetString(DevelopmentTokenKey, token);
        PlayerPrefs.Save();
        return true;
#endif
    }

    public static string Load()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string payload = PlayerPrefs.GetString(EncryptedTokenKey, "");
        if (string.IsNullOrWhiteSpace(payload)) return null;

        try
        {
            string[] parts = payload.Split('.');
            if (parts.Length != 2) return null;

            using AndroidJavaObject secretKey = GetOrCreateKey();
            using AndroidJavaClass cipherClass = new("javax.crypto.Cipher");
            using AndroidJavaObject cipher = cipherClass.CallStatic<AndroidJavaObject>(
                "getInstance",
                "AES/GCM/NoPadding"
            );
            using AndroidJavaObject spec = new(
                "javax.crypto.spec.GCMParameterSpec",
                128,
                Convert.FromBase64String(parts[0])
            );
            cipher.Call("init", 2, secretKey, spec);
            byte[] decrypted = cipher.Call<byte[]>(
                "doFinal",
                Convert.FromBase64String(parts[1])
            );
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception exception)
        {
            Debug.LogError("No se pudo leer el refresh token desde Android Keystore: " + exception.Message);
            Delete();
            return null;
        }
#else
        return PlayerPrefs.GetString(DevelopmentTokenKey, null);
#endif
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(EncryptedTokenKey);
        PlayerPrefs.DeleteKey(DevelopmentTokenKey);
        PlayerPrefs.Save();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject GetOrCreateKey()
    {
        using AndroidJavaClass keyStoreClass = new("java.security.KeyStore");
        AndroidJavaObject keyStore = keyStoreClass.CallStatic<AndroidJavaObject>(
            "getInstance",
            "AndroidKeyStore"
        );
        keyStore.Call("load", new object[] { null });

        if (!keyStore.Call<bool>("containsAlias", Alias))
        {
            using AndroidJavaClass properties = new(
                "android.security.keystore.KeyProperties"
            );
            int encrypt = properties.GetStatic<int>("PURPOSE_ENCRYPT");
            int decrypt = properties.GetStatic<int>("PURPOSE_DECRYPT");
            using AndroidJavaObject builder = new(
                "android.security.keystore.KeyGenParameterSpec$Builder",
                Alias,
                encrypt | decrypt
            );
            builder.Call<AndroidJavaObject>(
                "setBlockModes",
                new string[] { "GCM" }
            );
            builder.Call<AndroidJavaObject>(
                "setEncryptionPaddings",
                new string[] { "NoPadding" }
            );
            using AndroidJavaObject specification = builder.Call<AndroidJavaObject>(
                "build"
            );
            using AndroidJavaClass generatorClass = new("javax.crypto.KeyGenerator");
            using AndroidJavaObject generator = generatorClass.CallStatic<AndroidJavaObject>(
                "getInstance",
                "AES",
                "AndroidKeyStore"
            );
            generator.Call("init", specification);
            using AndroidJavaObject generatedKey = generator.Call<AndroidJavaObject>(
                "generateKey"
            );
        }

        AndroidJavaObject key = keyStore.Call<AndroidJavaObject>(
            "getKey",
            Alias,
            null
        );
        keyStore.Dispose();
        return key;
    }
#endif
}
