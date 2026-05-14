using System.Runtime.CompilerServices;
using UnityEngine;

public interface ICommand
{
    string CommandName { get; }
    string Description { get; }
    bool ExecuteCommand(string[] args);
}