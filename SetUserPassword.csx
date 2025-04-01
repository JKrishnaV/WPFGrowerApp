#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.SqlClient, 5.1.5" // Or the version used in your project
#r "nuget: Dapper, 2.1.35" // Or the version used in your project
#r "nuget: System.Configuration.ConfigurationManager, 8.0.0" // Or the version used in your project
#load "Infrastructure/Logging/Logger.cs" // Load logger dependency
#load "Infrastructure/Security/PasswordHasher.cs" // Load hasher dependency
#load "DataAccess/Models/User.cs" // Load model dependency
#load "DataAccess/Interfaces/IUserService.cs" // Load interface dependency
#load "DataAccess/BaseDatabaseService.cs" // Load base service dependency
#load "DataAccess/Services/UserService.cs" // Load the service implementation

using System;
using System.Configuration;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Services; // Namespace for UserService

// --- Configuration ---
// Get username and password from command line arguments
if (Args.Count < 2)
{
    Console.WriteLine("Usage: dotnet script SetUserPassword.csx <username> <new_password>");
    return;
}
string username = Args[0];
string newPassword = Args[1];
// ---------------------


// --- Execution ---
Console.WriteLine($"Attempting to set password for user: {username}");

try
{
    // Manually create an instance of UserService (DI container not available here)
    // It will read the connection string from App.config via BaseDatabaseService constructor
    var userService = new UserService(); 

    bool success = await userService.SetPasswordAsync(username, newPassword);

    if (success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Password for user '{username}' set successfully.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Failed to set password for user '{username}'. User might not exist.");
        Console.ResetColor();
    }
}
catch (ConfigurationErrorsException confEx)
{
     Console.ForegroundColor = ConsoleColor.Red;
     Console.WriteLine($"Configuration Error: {confEx.Message}");
     Console.WriteLine("Ensure App.config exists in the execution directory or connection string is valid.");
     Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An error occurred: {ex.Message}");
    Console.ResetColor();
    // Consider logging the full exception details if needed
}
