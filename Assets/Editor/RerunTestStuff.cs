using UnityEngine;
using SingularityGroup.HotReload;
using System.Reflection;
using System.Collections.Generic;

public static class RerunTestStuff
{
    // [InvokeOnHotReload]
    // private static void HandleMethodPatches()
    // {
    //     // Debug.Log("he");
    //     // foreach (var patch in patches)
    //     // {
    //     //     MethodBase method = patch.originalMethod;

    //     //     // Check if the method has NUnit's [Test] or Unity's [UnityTest] attribute
    //     //     if (method.IsDefined(typeof(NUnit.Framework.TestAttribute), inherit: true)
    //     //         || method.IsDefined(typeof(UnityEngine.TestTools.UnityTestAttribute), inherit: true))
    //     //     {
    //     //         EditorUtils.TestRunnerShortcuts.RunSelected();
    //     //     }
    //     // }
    // }
}
