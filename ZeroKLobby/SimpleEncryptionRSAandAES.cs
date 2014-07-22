using System;
using System.Security.Cryptography;
using System.IO;

namespace ZeroKLobby
{
    //Reference: http://msdn.microsoft.com/en-us/library/bb397867(v=vs.110).aspx
    //reference: http://msdn.microsoft.com/en-us/library/as0w18af(v=vs.110).aspx
    //Reference: http://stackoverflow.com/questions/165808/simple-two-way-encryption-for-c-sharp
    //NOTE: always use base64 when transporting encrypted text. Reference:  http://stackoverflow.com/questions/15747583/bad-data-exception-when-decrypting-using-rsa-with-correct-private-and-public-key
    //base64 preserve bit: http://en.wikipedia.org/wiki/Base64
    //PLEASE MAKE SURE TO CALL CLEAR() when finished. Its really important to avoid a mess of keys hidden in system. Reference: http://stackoverflow.com/questions/1307204/how-to-generate-unique-public-and-private-key-via-rsa?rq=1
    public class SimpleCryptographicProvider
    {
        private RSACryptoServiceProvider rsa;
        private RijndaelManaged rjndl;
        private System.Text.UTF8Encoding UTFEncoder;
        private byte[] aesKey;
        private byte[] aesIV;
        //private int[] typicalRSAExponent = new int[5] { 3, 17, 35, 257, 65537 };
        //typical RSA exponent: 
        //Reference1: https://engineering.purdue.edu/kak/compsec/NewLectures/Lecture12.pdf
        //Reference2: http://security.stackexchange.com/questions/2335/should-rsa-public-exponent-be-only-in-3-5-17-257-or-65537-due-to-security-c

        // Key container name for RSA
        // private/public key value pair. 
        const string keyName = "Key01";

        public SimpleCryptographicProvider()
        {
            UTFEncoder = new System.Text.UTF8Encoding();
        }
        //ASYMMETRIC ENCRYPTION:
        public void InitializeRSAPublicKey(string keytxt)
        {
            rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(keytxt);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly == true)
                System.Diagnostics.Trace.TraceInformation("RSA: - Public Only -" + rsa.KeySize + " bit");
            else
                System.Diagnostics.Trace.TraceInformation("RSA: - Full Key Pair -" + rsa.KeySize + " bit");
        }

        public void InitializeRSAKeyPair(int keySize = 1024)//, string modulus = null, string exponent = null)
        {
            //Check keysize: http://stackoverflow.com/questions/4852664/net-rsa-encryption-minimum-keysize
            //Note1: RSA keysize of 1024 only able to decrypt up to 117 bytes: http://stackoverflow.com/questions/2475861/rsa-encrypt-decrypt-problem-in-net
            //Note2: RSA keysize 2048 decrypt up to 245 bytes: https://community.oracle.com/thread/1751769?start=0&tstart=0
            //RSA parameters: http://msdn.microsoft.com/en-us/library/system.security.cryptography.rsaparameters(v=vs.110).aspx
            //Note3: RSA took too long to generate key in MONO. Reference: http://stackoverflow.com/questions/23653288/generating-a-key-pair-and-encypt-data-with-this-in-mono-c-sharp
            try
            {
                rsa = new RSACryptoServiceProvider(keySize); //NOTE: assigning 'CspKeyParameters' will make it not generate key in MONO
                rsa.ExportParameters(false); //MONO didn't initialize key yet until we export it.
                rsa.PersistKeyInCsp = true;
                if (rsa.PublicOnly == true)
                    System.Diagnostics.Trace.TraceInformation("RSA: - Public Only -" + rsa.KeySize + " bit");
                else
                    System.Diagnostics.Trace.TraceInformation("RSA: - Full Key Pair -" + rsa.KeySize + " bit");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message,e);
            }
        }

        public string GetRSAPublicKey()
        {
            if (rsa == null)
            {
                System.Diagnostics.Trace.TraceError("Key not set.");
                InitializeRSAKeyPair();
            }
            // Return the public key created by the RSA 
            return rsa.ToXmlString(false);
        }

        public string RSAEncryptTo64Base(string unencrypted, bool _64BaseSource)
        {
            if (rsa == null)
            {
                System.Diagnostics.Trace.TraceError("RSA not set.");
                InitializeRSAKeyPair();
            }
            byte[] unencryptedBytes;
            if (!_64BaseSource) unencryptedBytes = UTFEncoder.GetBytes(unencrypted);
            else unencryptedBytes = Convert.FromBase64String(unencrypted);
            //padding with OEAP tag is 41bytes: http://web.townsendsecurity.com/bid/29195/How-Much-Data-Can-You-Encrypt-with-RSA-Keys
            //padding without OEAP tag is 11bytes: https://github.com/digitalbazaar/forge/issues/108
            //list of options and padding size: http://msdn.microsoft.com/en-us/library/system.security.cryptography.rsacryptoserviceprovider.encrypt(v=vs.110).aspx
            int maxStringSize = (int)(rsa.KeySize / 8) -11; 
            string cryptedOutput = "";
            int index = 0;
            while (index < unencryptedBytes.Length)
            {
                int index2 = 0;
                int leftover = Math.Min(maxStringSize,unencryptedBytes.Length-index);
                byte[] temp = new byte[leftover];
                while (index2 < leftover)
                {
                    temp[index2] = unencryptedBytes[index];
                    index2 = index2 + 1;
                    index = index + 1;
                }
                cryptedOutput = cryptedOutput + (cryptedOutput != "" ?  "sprtor" : "") + Convert.ToBase64String(rsa.Encrypt(temp, true)); //split String by "sprtor" if its too big for RSA
            }
            return cryptedOutput;
        }

        public string RSADecryptFrom64Base(string encrypted,bool _64BaseOutput)
        {
            if (rsa == null)
            {
                System.Diagnostics.Trace.TraceError("RSA not set.");
                InitializeRSAKeyPair();
            }
            string[] splitString = encrypted.Split(new string[1] { "sprtor" }, System.StringSplitOptions.None);
            int index0 = 0;
            string uncryptedOutput = "";
            while (index0 < splitString.Length)
            {
                string uncryptedString;
                if (_64BaseOutput) uncryptedString = Convert.ToBase64String(rsa.Decrypt(Convert.FromBase64String(splitString[index0]), true));
                else uncryptedString = UTFEncoder.GetString(rsa.Decrypt(Convert.FromBase64String(splitString[index0]), true));

                uncryptedOutput = uncryptedOutput + uncryptedString;
                index0 = index0 + 1;
            }
            return uncryptedOutput;
        }

        //SYMMETRIC:
        public void InitializeAESWith64BaseKey(string key = null, string vector = null, int keySize = 128, int blocksize = 128,PaddingMode paddingMode = PaddingMode.ISO10126) //null or 128 if not specified
        {
            // Create instance of Rijndael for 
            // symetric encryption of the data.
            //Reference1: http://en.wikipedia.org/wiki/Key_size#Symmetric_algorithm_key_lengths
            rjndl = new RijndaelManaged();
            rjndl.KeySize = keySize;
            rjndl.BlockSize = blocksize;
            //NOTE: CipherMode.CBC only work for 1 whole string in NET but for multiple line in MONO (the later is silly to manage if you are 
            //deciphering message asynchronously for 2 or more people!). So we must use NET scheme (we reset ICryptoTransform every string). 
            //We actually implemented a simple IV-offset/cipher-mode to avoid same plaintext from repeating the same ciphertext. 
            //This is called "iv_offsetNumber" which is simply XOR-ing some number with the "Initializing Vector" (based on concept of CipherMode.CTR)
            rjndl.Mode = CipherMode.CBC;
            rjndl.Padding = paddingMode; //PaddingMode.ISO10126 is Random ending , Reference: http://www.di-mgt.com.au/cryptopad.html#exampleaes
            if (key != null && vector != null)
            {
                rjndl.Key = Convert.FromBase64String(key);
                rjndl.IV = Convert.FromBase64String(vector);
            }
            aesKey = rjndl.Key;
            aesIV = rjndl.IV;
            System.Diagnostics.Trace.TraceInformation("AES " + rjndl.KeySize + " bit");
        }

        public string GetAES64BaseKey()
        {
            return Convert.ToBase64String(rjndl.Key);
        }

        public string GetAES64BaseIV()
        {
            return Convert.ToBase64String(rjndl.IV);
        }

        public string AESEncryptTo64Base(string unencrypted, bool _64BaseSource, int offsetNumber=0)
        {
            if (rjndl == null)
            {
                System.Diagnostics.Trace.TraceError("AES not initialized.");
                InitializeAESWith64BaseKey();
            }
            string output = "";
            if (!_64BaseSource) output = Convert.ToBase64String(Transform(UTFEncoder.GetBytes(unencrypted),true,offsetNumber));
            else if (_64BaseSource) output = Convert.ToBase64String(Transform(Convert.FromBase64String(unencrypted), true, offsetNumber));
            return output;
        }

        public string AESDecryptFrom64Base(string encrypted, bool _64BaseOutput,int offsetNumber=0)
        {
            if (rjndl == null)
            {
                System.Diagnostics.Trace.TraceError("AES not initialized.");
                InitializeAESWith64BaseKey();
            }
            string output = "";
            if (!_64BaseOutput) output = UTFEncoder.GetString(Transform(Convert.FromBase64String(encrypted),false,offsetNumber));
            else if (_64BaseOutput) output = Convert.ToBase64String(Transform(Convert.FromBase64String(encrypted), false,offsetNumber));
            return output;
        }
        protected byte[] Transform(byte[] buffer, bool isEncrypt, int iv_offsetNumber)
        {   //NOTE: we recreate De/Encryptor for every string to fix IV do not reset in Mono: http://net-security.questionfor.info/q_dotnet-security_60895.html
            ICryptoTransform transform;
            if (isEncrypt) transform = rjndl.CreateEncryptor(aesKey, VectorXORnumber(aesIV, iv_offsetNumber));
            else transform = rjndl.CreateDecryptor(aesKey, VectorXORnumber(aesIV, iv_offsetNumber));

            byte[] output = AESTransform(buffer,transform);
            transform.Dispose();
            return output;
        }
        protected byte[] AESTransform(byte[] buffer, ICryptoTransform transform)
        {
            //Reference1: https://www.simple-talk.com/blogs/2012/02/28/oh-no-my-paddings-invalid/
            //Reference2:  http://stackoverflow.com/questions/4545387/using-aes-encryption-in-net-cryptographicexception-saying-the-padding-is-inva
            // encrypt the data using a CryptoStream
            byte[] output;
            using (MemoryStream encryptedStream = new MemoryStream())
            using (CryptoStream crypto = new CryptoStream(
                encryptedStream, transform, CryptoStreamMode.Write))
            {
                crypto.Write(buffer, 0, buffer.Length);

                // explicitly flush the final block of data
                crypto.FlushFinalBlock();

                output = encryptedStream.ToArray();
            }
            return output;
        }

        protected byte[] VectorXORnumber(byte[] originalIV, int number)
        {
            //CTR : http://en.wikipedia.org/wiki/Block_cipher_mode_of_operation
            //binary operation: http://www.codeproject.com/Articles/328740/Binary-operations-on-byte-arrays-with-parallelism
            //BitConverter: http://stackoverflow.com/questions/4176653/int-to-byte-array
            //NOTE: we using XOR to implement super simplistic form of CTR chipher mode concept.
            byte[] numberBytes = BitConverter.GetBytes(number);
            int shortlen = Math.Min(originalIV.Length, numberBytes.Length);
            byte[] newIV = new byte[originalIV.Length]; //newIV length must be exactly same as original
            
            for (int i = 0; i < shortlen; i++)
                newIV[i] = (byte)(originalIV[i] ^ numberBytes[i]); //XOR all intersecting bytes

            byte[] pl = (originalIV.Length > numberBytes.Length) ? originalIV : numberBytes;
            for (int i = shortlen; i < originalIV.Length; i++)
                newIV[i] = pl[i]; //copy remaining bytes

            return newIV;
        }

        public void Clear()
        {
            if (rjndl != null) rjndl.Clear(); //NOTE: MONO don't implement Dispose() for this
            if (rsa != null)
            {
                rsa.PersistKeyInCsp = false; //delete key
                rsa.Clear();
            }
            rsa = null;
            rjndl = null;
        }
    }
}
