using Moq;

namespace UnitTests;

[TestClass]
public class GronsfeldCipherTests
{
    
    [TestMethod]
    public void TestEncryptDecrypt()
    {
        GronsfeldCipher gronsfeld = new GronsfeldCipher();

        string originalText = "HelloWorld";
        string key = "123";

        string encryptedText = gronsfeld.Encrypt(originalText, key);
        string decryptedText = gronsfeld.Decrypt(encryptedText, key);

        Assert.AreEqual(originalText, decryptedText);
    }

    [TestMethod]
    public void TestEncrypt()
    {
        GronsfeldCipher gronsfeld = new GronsfeldCipher();

        string originalText = "HelloWorld";
        string key = "123";

        string encryptedText = gronsfeld.Encrypt(originalText, key);

        Assert.AreNotEqual(originalText, encryptedText);
    }

    [TestMethod]
    public void TestEncryptDifferentKeys()
    {
        GronsfeldCipher gronsfeld = new GronsfeldCipher();

        string text = "SensitiveData";
        string key1 = "123";
        string key2 = "321";

        string encryptedText1 = gronsfeld.Encrypt(text, key1);
        string encryptedText2 = gronsfeld.Encrypt(text, key2);

        Assert.AreNotEqual(encryptedText1, encryptedText2);
    }

    [TestMethod]
    public void TestRegistrEncryptDecrypt()
    {
        GronsfeldCipher gronsfeld = new GronsfeldCipher();

        string text = "HelloWORLD";
        string key = "987";

        string encryptedText = gronsfeld.Encrypt(text, key);
        string decryptedText = gronsfeld.Decrypt(encryptedText, key);

        Assert.AreEqual(text, decryptedText);
    }

}

[TestClass]
public class TextManagerTests
{
    string _username = "TestUser";

    [TestMethod]
    public void TestAddText()
    {
        TextManager tm = new TextManager();
        tm.AddText(_username, "First Text");

        var texts = tm.ViewAllTexts(_username);
        Assert.AreEqual(1, texts.Count);
        Assert.AreEqual("First Text", texts[0]);
    }
        
    [TestMethod]
    public void TestUpdateText()
    {
        TextManager tm = new TextManager();
        tm.AddText(_username, "Old Text");

        bool result = tm.UpdateText(_username, 1, "Updated Text");

        Assert.IsTrue(result);
        Assert.AreEqual("Updated Text", tm.ViewText(_username, 1));
    }

    [TestMethod]
    public void TestDeleteText()
    {
        TextManager tm = new TextManager();
        tm.AddText(_username, "Text to be deleted");

        bool result = tm.DeleteText(_username, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, tm.ViewAllTexts(_username).Count);
    }

    [TestMethod]
    public void TestViewText()
    {
        TextManager tm = new TextManager();
        tm.AddText(_username, "Sample Text");

        string? text = tm.ViewText(_username, 1);

        Assert.IsNotNull(text);
        Assert.AreEqual("Sample Text", text);
    }


    [TestMethod]
    public void TestViewAllTexts()
    {
        TextManager tm = new TextManager();
        tm.AddText(_username, "Text 1");
        tm.AddText(_username, "Text 2");

        var texts = tm.ViewAllTexts(_username);

        Assert.AreEqual(2, texts.Count);
        Assert.AreEqual("Text 1", texts[0]);
        Assert.AreEqual("Text 2", texts[1]);
    }



}