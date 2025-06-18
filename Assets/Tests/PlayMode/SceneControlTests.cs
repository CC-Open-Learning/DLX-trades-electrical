using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using VARLab.TradesElectrical;

public class SceneControlTests
{


    /// <summary>
    ///     Tests the scene controller to ensure that the methods provided 
    ///     enable and disable the proper GameObject references
    /// </summary>
    [UnityTest]
    public IEnumerator SceneControlTestsWithEnumeratorPasses()
    {
        GameObject fakeObjectRoughInHouse = new("Rough-In House");
        GameObject fakeObjectFinalHouse = new("Final House");

        SceneController sceneController = new GameObject("Scene Controller").AddComponent<SceneController>();
        sceneController.HouseRoughIn = fakeObjectRoughInHouse;
        sceneController.HouseFinal = fakeObjectFinalHouse;
        
        // Ensure both "fakes" are active after their creation
        yield return null;
        Assert.IsTrue(fakeObjectRoughInHouse.activeSelf);
        Assert.IsTrue(fakeObjectFinalHouse.activeSelf);

        // 
        fakeObjectFinalHouse.SetActive(false); // Set false as would be in scene
        sceneController.SetHouseRoughInActive();
        yield return null;

        //
        Assert.IsTrue(fakeObjectRoughInHouse.activeSelf);
        Assert.IsFalse(fakeObjectFinalHouse.activeSelf);

        yield return null;
        sceneController.SetHouseFinalActive();
        yield return null;

        Assert.IsFalse(fakeObjectRoughInHouse.activeSelf);
        Assert.IsTrue(fakeObjectFinalHouse.activeSelf);
    }
}
