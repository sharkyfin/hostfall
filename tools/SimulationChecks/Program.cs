using System;
using System.Reflection;
using Hostfall.Tests;
using NUnit.Framework;

// Execute the same pure EditMode cases without starting the licensed Unity Editor.
// This does not replace PlayMode tests or Unity package/API validation.
int passed = 0, failed = 0;
foreach (var type in new[] { typeof(SimulationTests),typeof(ActionRouteTests) })
foreach (var method in type.GetMethods())
{
    var cases = method.GetCustomAttributes<TestCaseAttribute>();
    foreach (var item in cases) Run(method, item.Arguments);
    if (method.GetCustomAttribute<TestAttribute>() != null) Run(method, Array.Empty<object>());
}
Console.WriteLine($"Simulation behavioral checks: {passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;

void Run(MethodInfo method, object[] arguments)
{
    try { method.Invoke(Activator.CreateInstance(method.DeclaringType), arguments); passed++; Console.WriteLine("PASS " + method.Name); }
    catch (Exception error)
    {
        failed++;
        Console.Error.WriteLine("FAIL " + method.Name + ": " + (error is TargetInvocationException ? error.InnerException : error));
    }
}
