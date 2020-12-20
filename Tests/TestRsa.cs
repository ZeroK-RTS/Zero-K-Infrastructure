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
            
            Assert.AreNotEqual(keys.privKey, keys.pubKey);
            
            Assert.IsTrue(keys.pubKey.Length < keys.privKey.Length);
            
            var dataToSign = "some super secret data we need to sign";

            var signature = RsaSignatures.Sign(dataToSign, keys.privKey);


            var resultOk = RsaSignatures.VerifySignature(dataToSign, signature, keys.pubKey);
            Assert.AreEqual(true, resultOk);
            
            
            var resultFail = RsaSignatures.VerifySignature(dataToSign + "modify", signature, keys.pubKey);
            Assert.AreEqual(false, resultFail);
            
            
            var dataToEncrypt = "some super secret data we need to encrypt";

            var encrypted = RsaSignatures.Encrypt(dataToEncrypt, keys.pubKey);
            
            Assert.AreNotEqual(encrypted, dataToEncrypt);

            var decrypted = RsaSignatures.Decrypt(encrypted, keys.privKey);
            
            Assert.AreEqual(decrypted, dataToEncrypt);        }

    }
}