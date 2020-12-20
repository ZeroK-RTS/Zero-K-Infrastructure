using NUnit.Framework;
using PlasmaShared;

namespace Tests
{
    [TestFixture]
    public class TestRsa
    {
        [Test]
        public void TestRsaSignatures() {
            var keys = RsaSignatures.GenerateKeys();
            
            Assert.AreNotEqual(keys.PrivKey, keys.PubKey);
            
            Assert.IsTrue(keys.PubKey.Length < keys.PrivKey.Length);
            
            var dataToSign = "some super secret data we need to sign";

            var signature = RsaSignatures.Sign(dataToSign, keys.PrivKey);


            var resultOk = RsaSignatures.VerifySignature(dataToSign, signature, keys.PubKey);
            Assert.AreEqual(true, resultOk);
            
            
            var resultFail = RsaSignatures.VerifySignature(dataToSign + "modify", signature, keys.PubKey);
            Assert.AreEqual(false, resultFail);
            
            
            var dataToEncrypt = "some super secret data we need to encrypt";

            var encrypted = RsaSignatures.Encrypt(dataToEncrypt, keys.PubKey);
            
            Assert.AreNotEqual(encrypted, dataToEncrypt);

            var decrypted = RsaSignatures.Decrypt(encrypted, keys.PrivKey);
            
            Assert.AreEqual(decrypted, dataToEncrypt);        }

    }
}