using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class MobileValidationRunner
{
    private static readonly TestRunnerApi Api;
    static MobileValidationRunner()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new Results());
    }

    [MenuItem("Tools/Mobile/Run input tests")]
    public static void Run()
    {
        SessionState.SetBool("MobileValidationRunning", true);
        Api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode,
            testNames = new[]
            {
                "MobileControlsTests.TouchControlsDriveGameplayAndSurviveRestart",
                "MobileControlsTests.DesktopInputRemainsAvailable",
                "PerkMachineTests.MachinesShowPromptsAndSellPerksAfterRestart"
            }
        }));
    }

    private class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool("MobileValidationRunning", false)) return;
            SessionState.SetBool("MobileValidationRunning", false);
            Directory.CreateDirectory("Logs");
            TestRunnerApi.SaveResultToFile(result, Path.GetFullPath("Logs/mobile-tests.xml"));
            Debug.Log("Mobile input tests: " + result.ResultState + "; passed=" + result.PassCount + "; failed=" + result.FailCount);
        }
    }
}
