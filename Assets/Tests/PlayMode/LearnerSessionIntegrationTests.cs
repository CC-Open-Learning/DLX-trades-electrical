using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using VARLab.SCORM;
using VARLab.TradesElectrical;

/// <summary>
///     Tests for integrating CloudSave and SCORM services through the 
///     boilerplate code provided in the DLX template
/// </summary>

public class LearnerSessionIntegrationTests
{
    private const string TestUsername = "TestUsername";

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        // Since the LearnerSessionHandler will be instantiated on session startup, it can be destroyed 
        // so that we can control and monitor its instantiate process
        if (LearnerSessionHandler.Instance)
        {
            Object.Destroy(LearnerSessionHandler.Instance.gameObject);
        }

        yield return null;

        LearnerSessionHandler.Analytics = new MockAnalyticsWrapper();
        LearnerSessionHandler.LoadLoginHandler();

        yield return null;  // Wait additional frame so that Start() can be invoked
    }

    /// <summary>
    ///     SessionHandler Instance should be non-null at any point during runtime
    /// </summary>
    /// <remarks>
    ///     Since there is a dependency on the Analytics system, the LearnerSessionHandler
    ///     itself may throw an exception during startup, but the deployment steps prior to
    ///     Analytics should still run
    /// </remarks>
    [Test]
    [Category("BuildServer")]
    public void LearnerSession_Startup_ShouldInitialize()
    {
        // Assert
        Assert.IsNotNull(LearnerSessionHandler.Instance);
        Assert.IsTrue(LearnerSessionHandler.Instance.Initialized);
    }

    /// <summary>
    ///     Expects that when the LoginHandler receives an "Initialized" event from SCORM, 
    ///     the LoginCompleted event is then invoked for other objects to listen for
    /// </summary>
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator LearnerSession_Login_ShouldExecuteCallback()
    {
        // Arrange
        bool usernameMatch = false;
        string nameExpected = TestUsername;
        string nameActual = string.Empty;

        LearnerSessionHandler.Instance.SessionStarted.AddListener(call: (username) => { nameActual = username; });
        LearnerSessionHandler.Instance.LearnerId = nameExpected;

        // Act
        yield return null;
        LearnerSessionHandler.Instance.HandleScormMessage(ScormManager.Event.Initialized);

        // Wait for at most 60 frames to ensure that coroutines are able to run
        int frame = 0;
        while (frame < 60 && !usernameMatch)
        {
            frame++;
            usernameMatch = string.Equals(nameExpected, nameActual);
            yield return null;
        }

        Debug.Log($"Waited {frame} frames before completing");

        // Assert
        Assert.IsTrue(LearnerSessionHandler.Instance.ScormLoginReceived);
        Assert.IsTrue(usernameMatch);
    }


    /// <summary>
    ///     Expects that when the LoginHandler receives an "Initialized" event from SCORM, 
    ///     the LoginCompleted event is then invoked for other objects to listen for
    /// </summary>
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator LearnerSession_AnalyticsCallback()
    {
        // Arrange
        bool analyicsCallbackInvoked = false;
        string expectedSessionId = TestUsername.GetHashCode().ToString();
        string actualSessionId = string.Empty;

        // The MockAnalyticsWrapper attempts to hash the learner ID and provide it as the session ID
        LearnerSessionHandler.Instance.LearnerId = TestUsername;
        LearnerSessionHandler.Instance.AnalyticsConnected.AddListener(call: (id) =>
        {
            actualSessionId = id;
            analyicsCallbackInvoked = true;
        });

        // Act
        yield return LearnerSessionHandler.Instance.AnalyticsLoginCoroutine();

        // Assert
        Assert.That(analyicsCallbackInvoked);
        Assert.AreEqual(expectedSessionId, actualSessionId);
    }



    /// <summary>
    ///     Expects that when the LoginHandler receives an "Initialized" event from SCORM, 
    ///     the LoginCompleted event is then invoked for other objects to listen for
    /// </summary>
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator LearnerSession_AnalyticsError()
    {
        // Arrange
        bool analyicsCallbackInvoked = false;
        string sessionId = string.Empty;

        // The MockAnalyticsWrapper will throw an error if an empty string is passed as the username
        LearnerSessionHandler.Instance.LearnerId = string.Empty;
        LearnerSessionHandler.Instance.AnalyticsConnected.AddListener(call: (id) =>
        {
            sessionId = id;
            analyicsCallbackInvoked = true;
        });

        // Act
        yield return LearnerSessionHandler.Instance.AnalyticsLoginCoroutine();

        // Assert
        Assert.IsFalse(analyicsCallbackInvoked);
#if UNITY_EDITOR
        // Only in UnityEditor, in case these tests are run on a platform
        LogAssert.Expect(LogType.Warning, MockAnalyticsWrapper.GenericErrorMessage);
#endif
    }


    /// <summary>
    ///     Expects that when the LoginHandler receives an "Initialized" event from SCORM, 
    ///     the LoginCompleted event is then invoked for other objects to listen for
    /// </summary>
    [Test]
    [Category("BuildServer")]
    public void LearnerSession_DevCommand_GetInfo()
    {
        string result = LearnerSessionHandler.GetInfo();

        Assert.That(result.Contains("SCORM Info"));
    }

    /// <summary>
    ///     A mocked analytics library used for testing
    /// </summary>
    internal class MockAnalyticsWrapper : IAnalyticsWrapper
    {
        public const string GenericErrorMessage = "Generic error message";

        private string id;

        public void Initialize()
        {
            Debug.Log("Mock initialize analytics system");
        }

        public void Login(string username, System.Action<string> successCallback, System.Action<string> errorCallback)
        {
            id = username.GetHashCode().ToString();

            if (!string.IsNullOrWhiteSpace(username))
            {
                successCallback?.Invoke(id);
            }
            else
            {
                errorCallback?.Invoke(GenericErrorMessage);
            }
        }
    }
}
