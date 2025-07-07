//using System.Collections;
//using System.Collections.Generic;
//using NUnit.Framework;
//using UnityEngine;
//using UnityEngine.TestTools;
//using VARLab.CloudSave;
//using VARLab.TradesElectrical;


///// <summary>
/////     These tests will verify the methods in <see cref="CloudSaveAdapter"/> and 
/////     the custom functionality therein.
/////     <para />
/////     
/////     They will use mocked and injected versions of <see cref="ICloudSerializer"/>
/////     and <see cref="ICloudSaveSystem"/> to avoid having to make network calls
/////     during testing.
/////     <para />
/////     
/////     Additional tests may be created that DO use network calls
///// </summary>
//public class CloudSaveTests
//{
//    /// <summary>
//    ///     Attempts to replicate the blob list format returned from the
//    ///    'real' CloudSaveSystem class
//    /// </summary>
//    const string CustomListData = "{\"File1\", \"File2\", \"File3\"}";

//    const string CustomSaveData = "";

//    const string CustomFileName = "File1";

//    /// <summary> This file name is not present in CustomListData </summary>
//    const string MissingFileName = "File4";

//    /// <summary> Just wait the approx length of one frame </summary>
//    const float CustomTimeDelay = 0.16f;

//    /// <summary> 
//    ///     The maximum amount of time in seconds that a test should be 
//    ///     allowed to run. If the test exceeds this time limit, it will fail
//    /// </summary>
//    const float MaxExecuteTime = 1f;

//    GameObject cloudSaveObject;
//    CloudSaveAdapter adapter;
//    MockSaveSystem saveSystem;

//    [UnitySetUp] 
//    public IEnumerator UnitySetUp()
//    {
//        cloudSaveObject = new();
//        cloudSaveObject.SetActive(false);

//        adapter = cloudSaveObject.AddComponent<CloudSaveAdapter>();
//        saveSystem = cloudSaveObject.AddComponent<MockSaveSystem>();

//        // Insert test data strings into SaveSystem
//        saveSystem.DelayTimeSeconds = CustomTimeDelay;
//        saveSystem.SaveData = CustomSaveData;
//        saveSystem.ListData = CustomListData;
//        adapter.Blob = CustomFileName;
//        adapter.FailedAttemptDelaySeconds = CustomTimeDelay;

//        yield return null;

//        // Before Awake() inject ICloudSaveSystem
//        adapter.SaveSystem = saveSystem;

//        cloudSaveObject.SetActive(true);

//        yield return null;
        
//        // After Awake() inject ICloudSerializer
//        adapter.CloudSerializer = new MockSerializer();
//    }

//    [UnityTearDown]
//    public void UnityTearDown()
//    {
//        Object.Destroy(cloudSaveObject);
//    }

//    /// <summary>
//    ///     This ensures that the mocked classes have been injected appropriately
//    /// </summary>
//    [Test]
//    public void A_RunFirst_MetaTest_AssertMockedClassesInjected()
//    {
//        Assert.That(adapter.SaveSystem is MockSaveSystem);
//        Assert.That(adapter.CloudSerializer is MockSerializer);
//    }


//    [UnityTest]
//    public IEnumerator HandleCustomListRequest_FileFound()
//    {
//        // Arrange 
//        bool callbackExecuted = false;
//        bool? fileFound = null;

//        adapter.SaveFileFound?.AddListener(() => { fileFound = true; callbackExecuted = true; });
//        adapter.SaveFileNotFound?.AddListener(() => { fileFound = false; callbackExecuted = true; });


//        // Act
//        // Calls the List() action which invokes the custom save file matching functionality
//        // defined in CloudSaveAdapter
//        adapter.List();

//        float elapsed = 0f;

//        while (elapsed < MaxExecuteTime && !callbackExecuted)
//        {
//            yield return null;
//            elapsed += Time.unscaledDeltaTime;
//        }


//        // Assert
//        Assert.That(callbackExecuted);
//        Assert.IsNotNull(fileFound);
//        Assert.IsTrue(fileFound);
//    }

//    [UnityTest]
//    public IEnumerator HandleCustomListRequest_FileNotFound()
//    {
//        // Arrange 
//        bool callbackExecuted = false;
//        bool? fileFound = null;

//        adapter.SaveFileFound?.AddListener(() => { fileFound = true; callbackExecuted = true; });
//        adapter.SaveFileNotFound?.AddListener(() => { fileFound = false; callbackExecuted = true; });

//        // Overwrite file name such that this file should NOT be found in the list of blobs
//        adapter.Blob = MissingFileName;


//        // Act
//        // Calls the List() action which invokes the custom save file matching functionality
//        // defined in CloudSaveAdapter
//        adapter.List();

//        float elapsed = 0f;

//        while (elapsed < MaxExecuteTime && !callbackExecuted)
//        {
//            yield return null;
//            elapsed += Time.unscaledDeltaTime;
//        }

//        // Assert
//        Assert.That(callbackExecuted);
//        Assert.IsNotNull(fileFound);
//        Assert.IsFalse(fileFound);
//    }


//    /// <summary>
//    ///     If the List() action fails, it will re-attempt the action 4 more times,
//    ///     with a short delay between each attempt, before invoking 
//    ///     the <see cref="CloudSaveAdapter.CloudSaveNetworkError"/> event
//    /// </summary>
//    [UnityTest]
//    public IEnumerator HandleCustomListRequest_ListActionFailed()
//    {
//        // Arrange 
//        float reducedDelayTime = 0.01f;
//        float increasedMaxExecuteTime = MaxExecuteTime * 2; // Give a bit more time for this test, even with the reduced delay time
//        string errorStringExpected = "Unable to reach Cloud Save service after 5 attempts";
//        bool callbackExecuted = false;
//        bool? fileFound = null;
//        int totalRequestsExpected = 5;
//        int totalRequestsActual = 0;

//        // Increments counter each time the List() action is received
//        saveSystem.RequestCompleted += (sender, args) => totalRequestsActual++;

//        // Looking for error event callback to be invoked 
//        adapter.CloudSaveNetworkError?.AddListener(() => callbackExecuted = true);

//        // Neither of these file callbacks should be executed,
//        // keeping bool? from previous tests to confirm
//        adapter.SaveFileFound?.AddListener(() => fileFound = true);
//        adapter.SaveFileNotFound?.AddListener(() => fileFound = false);

//        // Tell the mocked Save System that the action should fail
//        saveSystem.IsActionSuccessful = false;

//        // Significantly reduce delay between failed attempts.
//        // Realtime delay is 1 second to account for slow connections / provide
//        // time for the system to come back online after a failed attempt, but
//        // since the back-end here is mocked I simply want to test that CloudSaveAdapter
//        // invokes the appropriate event after the specified number of failed attempts
//        adapter.FailedAttemptDelaySeconds = reducedDelayTime;

//        // Act
//        // Calls the List() action which invokes the custom save file matching functionality
//        // defined in CloudSaveAdapter
//        adapter.List();

//        float elapsed = 0f;

//        while (elapsed < increasedMaxExecuteTime && !callbackExecuted)
//        {
//            yield return null;
//            elapsed += Time.unscaledDeltaTime;
//        }

//        // Assert
//        LogAssert.Expect(LogType.Error, errorStringExpected); // Since the error callback section logs an error, expect it here
//        Assert.That(callbackExecuted);
//        Assert.IsNull(fileFound); // Neither of the file callbacks should be executed
//        Assert.AreEqual(totalRequestsExpected, totalRequestsActual);
//    }


//    /// <summary>
//    ///     Copied from com.varlab.cloudsave.tests since I cannot seem to
//    ///     reference a test assembly from another assembly
//    /// </summary>
//    internal class MockSerializer : ICloudSerializer
//    {
//        public object MockDataObject { get; set; }

//        public void Deserialize(string serializedData)
//        {
//            MockDataObject = serializedData;
//        }

//        public string Serialize()
//        {
//            return MockDataObject?.ToString();
//        }
//    }


//    // Ignore warning for unused parameters, as these params are
//    // part of the public API
//#pragma warning disable IDE0060

//    /// <summary>
//    ///     Copied from com.varlab.cloudsave.tests since I cannot seem to
//    ///     reference a test assembly from another assembly
//    /// </summary>
//    public class MockSaveSystem : MonoBehaviour, ICloudSaveSystem
//    {
//        public string ListData;

//        public string SaveData;

//        public event RequestCompletedEventHandler RequestCompleted;

//        public float DelayTimeSeconds = 0;

//        public bool IsActionSuccessful = true;


//        public Coroutine Save(string path, string saveData)
//        {
//            SaveData = saveData;

//            return StartCoroutine(SaveRoutine(path, saveData));
//        }

//        public IEnumerator SaveRoutine(string path, string saveData)
//        {
//            yield return new WaitForSeconds(DelayTimeSeconds);

//            RequestCompletedEventArgs args = new(RequestAction.Save, IsActionSuccessful, saveData);
//            RequestCompleted?.Invoke(this, args);

//            yield return null;
//        }

//        public Coroutine Load(string path)
//        {
//            return StartCoroutine(LoadRoutine(path));
//        }

//        public IEnumerator LoadRoutine(string path)
//        {
//            yield return new WaitForSeconds(DelayTimeSeconds);

//            RequestCompletedEventArgs args = new(RequestAction.Load, IsActionSuccessful, GetMockData());
//            RequestCompleted?.Invoke(this, args);

//            yield return null;
//        }

//        public Coroutine List(string path)
//        {
//            return StartCoroutine(ListRoutine(path));
//        }

//        public IEnumerator ListRoutine(string path)
//        {
//            yield return new WaitForSeconds(DelayTimeSeconds);
//            RequestCompletedEventArgs args = new(RequestAction.List, IsActionSuccessful, ListData);
//            RequestCompleted?.Invoke(this, args);

//            yield return null;
//        }

//        public Coroutine Delete(string path)
//        {
//            SaveData = null;

//            RequestCompletedEventArgs args = new(RequestAction.Delete, IsActionSuccessful);
//            RequestCompleted?.Invoke(this, args);

//            return null;
//        }

//        private string GetMockData()
//        {
//            return SaveData;
//        }
//    }
//#pragma warning restore IDE0060 // Unused parameter

//}
