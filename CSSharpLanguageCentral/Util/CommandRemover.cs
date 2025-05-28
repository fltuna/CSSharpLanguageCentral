using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using Microsoft.Extensions.Logging;

namespace CSSharpLanguageCentral.Util;


public static class CommandRemover
{
    public static bool RemoveCommandByDefinition(string commandName)
    {
        try
        {
            var app = Application.Instance;

            var appType = typeof(Application);
            var commandManagerField = appType.GetField("_commandManager", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (commandManagerField == null)
            {
                Console.WriteLine("_commandManager field not found");
                return false;
            }

            var commandManager = commandManagerField.GetValue(app);
            if (commandManager == null)
            {
                Console.WriteLine("CommandManager is null");
                return false;
            }

            var commandManagerType = commandManager.GetType();
            var commandDefinitionsField = commandManagerType.GetField("_commandDefinitions",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (commandDefinitionsField == null)
            {
                Console.WriteLine("_commandDefinitions field not found");
                return false;
            }

            var commandDefinitions = commandDefinitionsField.GetValue(commandManager) as 
                Dictionary<string, IList<CommandDefinition>>;

            if (commandDefinitions == null)
            {
                Console.WriteLine("commandDefinitions is null");
                return false;
            }

            Console.WriteLine($"Total registered commands: {commandDefinitions.Count}");
            Console.WriteLine($"Command keys: {string.Join(", ", commandDefinitions.Keys)}");

            if (!commandDefinitions.TryGetValue(commandName, out var commandDefList))
            {
                Console.WriteLine($"Command '{commandName}' not found in dictionary");
                return false;
            }

            if (commandDefList.Count == 0)
            {
                Console.WriteLine($"Command '{commandName}' has no definitions");
                return false;
            }

            Console.WriteLine($"Found command '{commandName}' with {commandDefList.Count} definition(s)");

            var removeCommandMethod = commandManagerType.GetMethod("RemoveCommand", 
                BindingFlags.Public | BindingFlags.Instance);

            if (removeCommandMethod == null)
            {
                Console.WriteLine("RemoveCommand method not found");
                return false;
            }

            Console.WriteLine("RemoveCommand method found");

            // Remove all CommandDefinition
            // ToList() is used to avoid modifying the collection while iterating
            var definitionsToRemove = commandDefList.ToList();
            var removedCount = 0;

            foreach (var definition in definitionsToRemove)
            {
                try
                {
                    Console.WriteLine($"Removing CommandDefinition: Name='{definition.Name}', Description='{definition.Description}'");
                    removeCommandMethod.Invoke(commandManager, [definition]);
                    removedCount++;
                    Console.WriteLine($"Successfully removed CommandDefinition #{removedCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to remove CommandDefinition: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                    }
                    return false;
                }
            }

            Console.WriteLine($"Successfully removed {removedCount} CommandDefinition(s) for '{commandName}'");

            var stillExists = commandDefinitions.ContainsKey(commandName);
            Console.WriteLine($"Command '{commandName}' still exists in dictionary: {stillExists}");

            return removedCount > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RemoveCommandByDefinition: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public static void InspectCommand(string commandName)
    {
        try
        {
            var app = Application.Instance;
            var commandManagerField = typeof(Application).GetField("_commandManager", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var commandManager = commandManagerField?.GetValue(app);
            
            if (commandManager == null) return;

            var commandManagerType = commandManager.GetType();
            var commandDefinitionsField = commandManagerType.GetField("_commandDefinitions",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var commandDefinitions = commandDefinitionsField?.GetValue(commandManager) as 
                Dictionary<string, IList<CommandDefinition>>;

            if (commandDefinitions == null) return;

            Console.WriteLine($"=== Inspecting command: {commandName} ===");

            if (commandDefinitions.TryGetValue(commandName, out var commandDefList))
            {
                Console.WriteLine($"Command found with {commandDefList.Count} definition(s):");
                
                for (int i = 0; i < commandDefList.Count; i++)
                {
                    var def = commandDefList[i];
                    Console.WriteLine($"  Definition #{i + 1}:");
                    Console.WriteLine($"    Name: {def.Name}");
                    Console.WriteLine($"    Description: {def.Description}");
                    Console.WriteLine($"    Callback: {(def.Callback?.Method?.Name ?? "null")}");
                    Console.WriteLine($"    ExecutableBy: {def.ExecutableBy}");
                    Console.WriteLine($"    MinArgs: {def.MinArgs}");
                    Console.WriteLine($"    UsageHint: {def.UsageHint ?? "null"}");
                }
            }
            else
            {
                Console.WriteLine("Command not found");
            }

            Console.WriteLine($"All registered commands: {string.Join(", ", commandDefinitions.Keys)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inspecting command: {ex.Message}");
        }
    }
}