namespace SourceGeneration.ActionDispatcher.SourceGenerator.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string source = @"

using System;
using SourceGeneration.ActionDispatcher;

internal interface IHandler
{
    [ActionHandler] void Handle(Action1 action);
}
";
            var result = CSharpTestGenerator.Generate<ActionRoutesSourceGenerator>(source, typeof(IActionDispatcher).Assembly);
            var script = result.RunResult.GeneratedTrees.FirstOrDefault()?.GetText();

        }
    }
}