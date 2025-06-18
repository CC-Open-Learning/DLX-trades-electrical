using System;
using VARLab.Analytics;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     An interface which abstracts calls to the CORE Analytics system so that 
    ///     they can be mocked for testing.
    /// </summary>
    public interface IAnalyticsWrapper
    {
        public void Initialize();
        public void Login(string username, Action<string> successCallback, Action<string> errorCallback);
    }

    /// <summary>
    ///     The concrete implementation of CORE Analytics through the IAnalyticsWrapper interface
    /// </summary>
    public class CoreAnalyticsWrapper : IAnalyticsWrapper
    {
        public void Initialize() => CoreAnalytics.Initialize();

        /// <summary>
        ///     Wrapped both callbacks in lambda expressions to avoid using the PlayFab libraries
        ///     directly here. Otherwise the callback methods would need to have access to
        ///     PlayFab-specific classes
        /// </summary>
        /// <param name="username"></param>
        /// <param name="successCallback"></param>
        /// <param name="errorCallback"></param>
        public void Login(string username, Action<string> successCallback, Action<string> errorCallback)
        {
            CoreAnalytics.LoginUser(username,
                (response) => successCallback(response.PlayFabId),
                (response) => errorCallback(response.ErrorMessage));
        }
    }
}
