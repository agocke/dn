// See https://aka.ms/new-console-template for more information

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

// Expect the path to Microsoft.Build.Tasks.CodeAnalysis.dll as the first argument
var buildTaskPath = args[0];

var buildTask = AssemblyDefinition.ReadAssembly(args[0]);

var managedToolTask = buildTask.MainModule.Types.Single(t => t.Name == "ManagedToolTask");

var pathToManagedTool = managedToolTask.Properties.Single(p => p.Name == "PathToManagedTool");

//Console.WriteLine("IsVirtual: " + pathToManagedTool.GetMethod.IsVirtual);
//Console.WriteLine("Internal: " + pathToManagedTool.GetMethod.IsAssembly);
//Console.WriteLine("Internal: " + pathToManagedTool.GetMethod.IsFamily);

pathToManagedTool.GetMethod.IsVirtual = true;
pathToManagedTool.GetMethod.IsFamily = true;

// Console.WriteLine();
//
// Console.WriteLine("IsVirtual: " + pathToManagedTool.GetMethod.IsVirtual);
// Console.WriteLine("Internal: " + pathToManagedTool.GetMethod.IsAssembly);
// Console.WriteLine("Internal: " + pathToManagedTool.GetMethod.IsFamily);

var managedCompiler = buildTask.MainModule.Types.Single(t => t.Name == "ManagedCompiler");

var executeTool = managedCompiler.Methods.Single(m => m.Name == "ExecuteTool" && m.Parameters.Count == 4);
RewriteInstrsCallToCallvirt(executeTool.Body.Instructions);

var pathToManagedToolWithoutExtension = managedToolTask.Methods.Single(m => m.Name == "get_PathToManagedToolWithoutExtension");
RewriteInstrsCallToCallvirt(pathToManagedToolWithoutExtension.Body.Instructions);

var dirName = Directory.GetParent(args[0])!.Parent!.FullName;
var fileName = "Microsoft.Build.Tasks.CodeAnalysis.dll";
buildTask.Write(Path.Combine(dirName, fileName));

static void RewriteInstrsCallToCallvirt(Collection<Instruction> instrs)
{
    var newInstrs = instrs.Select(instr =>
    {
        if (instr.OpCode == OpCodes.Call && instr.Operand is MethodReference { Name: "get_PathToManagedTool" })
        {
            instr.OpCode = OpCodes.Callvirt;
        }
        return instr;
    }).ToArray();
    instrs.Clear();
    foreach (var instr in newInstrs)
    {
        instrs.Add(instr);
    }
}