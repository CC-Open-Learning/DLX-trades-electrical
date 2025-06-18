using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class TestUtils
{
    /// <summary>
    ///     Returns a list of all GameObjects.
    /// </summary>
    public static GameObject[] GetAllObjects()
    {
        return Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
    }

    /// <summary>
    ///     Find a Component of a GameObject using the GameObject's name and the Component
    ///     type in a given GameObject list.
    /// </summary>
    /// <typeparam name="T">Desired Component type.</typeparam>
    /// <param name="allObjects">GameObject list that used to traverse.</param>
    /// <param name="name">Name of the desired GameObject.</param>
    /// <returns>A tuple cosists of resulting GameObject and Component.</returns>
    /// <exception cref="Exception">
    ///     Throws when the desired GameObject or Component not found.
    /// </exception>
    public static (GameObject, T) FindGameObject<T>(GameObject[] allObjects, string name)
    {
        foreach (GameObject gameObject in allObjects)
        {
            T component = gameObject.GetComponent<T>();
            if (component != null && gameObject.name == name)
            {
                return (gameObject, component);
            }
        }

        throw new Exception($"Cannot find {typeof(T)} \"{name}\"");
    }

    /// <summary>
    ///     This method unloads the previous test scene async and creates a new one for the next test
    /// </summary>
    /// <param name="sceneCounter">int to append on the end of the scene name to keep names unique</param>
    /// <param name="scenename">
    ///     Name of the scene. This had to be added in order to keep all tests passing when ran together as
    ///     scene unload is async
    /// </param>
    /// <returns></returns>
    public static int ClearScene(int sceneCounter, string scenename)
    {
        SceneManager.SetActiveScene(SceneManager.CreateScene(scenename + sceneCounter));

        if (sceneCounter > 0)
        {
            SceneManager.UnloadSceneAsync(scenename + (sceneCounter - 1));
        }

        return ++sceneCounter;
    }

    /// <summary>
    ///     This coroutine is used to simulate clicking on a button on a test case
    /// </summary>
    /// <param name="button">The button to click</param>
    public static IEnumerator ClickOnButton(Button button)
    {
        // Ensure that the button is enabled in the hierarchy
        button.SetEnabled(true);

        var navigationEvent = new NavigationSubmitEvent() { target = button };
        button.SendEvent(navigationEvent);

        yield return null;
    }
}