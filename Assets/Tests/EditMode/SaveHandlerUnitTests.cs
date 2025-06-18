using NUnit.Framework;
using UnityEngine;
using VARLab.TradesElectrical;

/// <summary>
///     This class should test the <see cref="CustomSaveHandler"/>
/// </summary>
public class SaveHandlerUnitTests
{
    private const string TestUsername = "TestUsername";

    // 
    private CloudSaveAdapter saveHandler;

    /// <summary>
    ///     Validates that once the SaveHandler has received a username 
    ///     from a successful login, the 'Blob' (save file) name contains the username
    /// </summary>
    [Test]
    [Category("BuildServer")]
    public void SaveHandler_HandleLogin_ShouldUpdateBlobName()
    {
        saveHandler = new GameObject().AddComponent<CloudSaveAdapter>();

        // Arrange
        string nameExpected = TestUsername;

        // Act
        saveHandler.UpdateFileName(TestUsername);

        // Assert
        Assert.That(saveHandler.Blob.Contains(nameExpected));
    }
}