using System;
using System.Security.Cryptography;
using System.Text;
using ZkData;

namespace PlasmaShared
{
   public class RsaSignatures
    {
        public class KeyPair
        {
            public string PrivKey;
            public string PubKey;
        }
        
        private RSA rsa;

        public RsaSignatures(string key)
        {
            rsa = RSA.Create();
            rsa.FromXmlString(key.Base64Decode());
        }
        
        public static KeyPair GenerateKeys()
        {
            var rsa = RSA.Create();
            var privKey = rsa.ToXmlString(true).Base64Encode();
            var pubKey = rsa.ToXmlString(false).Base64Encode();
            return new KeyPair() {PrivKey = privKey, PubKey = pubKey};
        }


        /// <summary>
        /// requires rsa with priv key
        /// </summary>
        public string Sign(string data)
        {
            var signature = rsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }


        public static string Sign(string data, string privKey)
        {
            return new RsaSignatures(privKey).Sign(data);
        }
        
        
        /// <summary>
        /// requires rsa with pub key
        /// </summary>
        public bool VerifySignature(string data, string signature)
        {
            return rsa.VerifyData(Encoding.UTF8.GetBytes(data), Convert.FromBase64String(signature), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        }

        public static bool VerifySignature(string data, string signature, string pubKey)
        {
            return new RsaSignatures(pubKey).VerifySignature(data, signature);
        }
        
        
       
        /// <summary>
        /// requires rsa with pub key
        /// </summary>
        public string Encrypt(string data)
        {
            var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encrypted);
        }

        public static string Encrypt(string data, string pubKey)
        {
            return new RsaSignatures(pubKey).Encrypt(data);
        }
        
        /// <summary>
        /// requires rsa with priv key
        /// </summary>
        public string Decrypt(string data)
        {
            var decrypted = rsa.Decrypt(Convert.FromBase64String(data), RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(decrypted);
        }
        
        
        public static string Decrypt(string data, string privKey)
        {
            return new RsaSignatures(privKey).Decrypt(data);
        }
    }
}