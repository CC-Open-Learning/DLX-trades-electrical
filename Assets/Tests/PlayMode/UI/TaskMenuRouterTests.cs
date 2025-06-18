using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class TaskMenuRouterTests
{

    // Setup objects
    private GameObject menuHolder;
    private TaskMenuRouter router;


    [Category("BuildServer")]
    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        PanelSettings panelSettings =
            AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/Panel Settings.asset");

        if (panelSettings == null)
        {
            throw new ArgumentNullException(nameof(panelSettings));
        }

        // Configure UI Document with blank content (content will not be relevant to testing)
        menuHolder = new GameObject("UI");

        // Initializing the MenuControllers first ensures a race condition with the Closed() event is not introduced
        List<MenuController> menus = new();
        for (int i = 0; i < 12; i++)
        {
            menus.Add(GenerateMockController(panelSettings));
        }

        yield return null;
        yield return null;

        router = menuHolder.AddComponent<TaskMenuRouter>();

        // Set all internal menu controllers to the mock object
        router.TaskSelectionMenu = menus[0];
        router.BoxMountingSelectionMenu = menus[1];
        router.RunSupplyCableMenu = menus[2];
        router.SupplyCableSelectionMenu = menus[3];
        router.TerminateCablesMenu = menus[4];
        router.TerminateGangBoxSubMenu = menus[5];
        router.DeviceSelectionMenu = menus[6];
        router.RunCablesQuizMenu = menus[7];
        router.TerminateCablesQuizMenu = menus[8];
        router.CircuitTestingQuizMenu = menus[9];
        router.SceneTransitionDialog = menus[10];
        router.FinalSceneTransitionDialog = menus[11];

        router.TaskHandler = menuHolder.AddComponent<TaskHandler>();

        yield return null;

        Debug.Log($"Starting context is {router.MenuContext}");

        yield return null;

    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(menuHolder);
        MockMenuController.ControllerCount = 0;
    }


    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator Initialization_CorrectContext()
    {
        Assert.IsNotNull(router);

        Debug.Log($"Menu Context is controller {(router.MenuContext as MockMenuController).ControllerId}");
        Debug.Log($"BoxMounting is controller {(router.BoxMountingSelectionMenu as MockMenuController).ControllerId}");

        Assert.AreEqual(router.MenuContext, router.TaskSelectionMenu);

        if (!router.IsTaskMenuDisplayed)
        {
            router.ToggleRelevantTaskMenu();
        }

        yield return null;

        Assert.That(router.IsTaskMenuDisplayed);

        router.ToggleRelevantTaskMenu();

        yield return null;

        Assert.That(!router.IsTaskMenuDisplayed);

        Assert.IsFalse(router.TaskSelectionMenu.IsOpen);
        Assert.IsFalse(router.BoxMountingSelectionMenu.IsOpen);
        Assert.IsFalse(router.RunSupplyCableMenu.IsOpen);
        Assert.IsFalse(router.SupplyCableSelectionMenu.IsOpen);
        Assert.IsFalse(router.TerminateCablesMenu.IsOpen);
        Assert.IsFalse(router.TerminateGangBoxSubMenu.IsOpen);
        Assert.IsFalse(router.DeviceSelectionMenu.IsOpen);
        Assert.IsFalse(router.RunCablesQuizMenu.IsOpen);
        Assert.IsFalse(router.TerminateCablesQuizMenu.IsOpen);
        Assert.IsFalse(router.SceneTransitionDialog.IsOpen);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator BoxMounting_UpdateContext()
    {
        if (!router.IsTaskMenuDisplayed)
        {
            router.ToggleRelevantTaskMenu();
        }

        yield return null;

        // Now step into BoxMountingMenu
        router.BoxMountingSelectionMenu.Open();

        yield return null;

        // Turns all menus off
        router.ToggleRelevantTaskMenu();

        yield return null;

        Assert.That(!router.IsTaskMenuDisplayed);

        // Turns menus back on, should open to BoxMountingSelection
        router.ToggleRelevantTaskMenu();

        yield return null;

        Assert.IsTrue(router.BoxMountingSelectionMenu.IsOpen);
        Assert.IsFalse(router.TaskSelectionMenu.IsOpen);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator BoxMountingTasksCorrect_ChangeContext()
    {
        router.CloseAllTaskMenus();

        yield return null;

        router.TaskHandler.BoxMountingTasksCorrect?.Invoke();

        yield return null;

        router.OpenRelevantTaskMenu();

        yield return null;

        Assert.That(router.IsTaskMenuDisplayed);

        // 'Override Menu Context' should be true, which means opening a menu should open the top-level menu
        Assert.IsTrue(router.TaskSelectionMenu.IsOpen);
        //Assert.AreEqual(router.RunSupplyCableMenu.IsOpen, false); //??????????

        Debug.Log($"Menu Context is controller {(router.MenuContext as MockMenuController).ControllerId}");
        Debug.Log($"RunSupplyCable is controller {(router.RunSupplyCableMenu as MockMenuController).ControllerId}");

        Assert.AreEqual(router.MenuContext, router.TaskSelectionMenu);

    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator SupplyCableTasksCorrect_ChangeContext()
    {
        router.TaskHandler.SupplyCablesTaskCorrect?.Invoke();

        yield return null;


        // Turn menu on, should show SceneTransitionDialog immediiately
        router.ToggleRelevantTaskMenu();

        // 'Override Menu Context' should be false when the SceneTransitionDialog is the current context
        Assert.IsFalse(router.TaskSelectionMenu.IsOpen);
        Assert.IsTrue(router.SceneTransitionDialog.IsOpen);

        Debug.Log($"Menu Context is controller {(router.MenuContext as MockMenuController).ControllerId}");
        Debug.Log($"RunSupplyCable is controller {(router.RunSupplyCableMenu as MockMenuController).ControllerId}");

        Assert.AreEqual(router.MenuContext, router.SceneTransitionDialog);

        // Need to intentionally close SceneTransitionDialog as would be the case in the scene,
        // since the Toolbar would not be available
        router.SceneTransitionDialog.Close();

        Assert.That(!router.SceneTransitionDialog.IsOpen);

        // This should now swap context to DeviceSelectionMenu, with context overridden
        router.ToggleRelevantTaskMenu();

        Assert.That(router.TaskSelectionMenu.IsOpen);
        router.DeviceSelectionMenu.Open();  // Click to navigate in
        router.ToggleRelevantTaskMenu();    // Turn menus off

        Assert.That(!router.IsTaskMenuDisplayed);

        router.ToggleRelevantTaskMenu();    // Turn menus back on

        Assert.That(router.DeviceSelectionMenu.IsOpen);
    }

    private static MenuController GenerateMockController(PanelSettings settings)
    {
        GameObject container = new();

        // Configure UI Document with blank content (content will not be relevant to testing)
        SerializedObject serializedDocument = new(container.AddComponent<UIDocument>());
        serializedDocument.FindProperty("m_PanelSettings").objectReferenceValue = settings;
        serializedDocument.FindProperty("sourceAsset").objectReferenceValue = ScriptableObject.CreateInstance<VisualTreeAsset>(); ;
        serializedDocument.ApplyModifiedProperties();

        return container.AddComponent<MockMenuController>();
    }
}

/// <summary>
///     A specialization of <see cref="MenuController"/> specifically for test mocking, since
///     MenuController is an abstract MonoBehaviour class
/// </summary>
public class MockMenuController : MenuController
{
    public static int ControllerCount = 0;

    public int ControllerId;

    public override void Initialize()
    {
        ControllerId = ControllerCount++;
        Debug.Log($"Initializing mock MenuController {ControllerId}");
    }
}