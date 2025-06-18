using System;
using VARLab.DeveloperTools;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     A console command which prints information provided by 
    ///     the <see cref="LearnerSessionHandler"/> controller.
    /// </summary>
    public class ScormCommand : ICommand
    {
        /// <summary>
        ///     A function which returns a string containing the 
        ///     SCORM information to display
        /// </summary>
        public readonly Func<string> ContentFunction;

        public string Name => "scorm";

        public string Usage => Name;

        public string Description => "Prints information about the current SCORM context";


        /// <summary>
        ///     Constructor for the ScormInfoCommand
        /// </summary>
        /// <param name="function">
        ///     A function which returns a string containing the 
        ///     SCORM information to display
        /// </param>
        public ScormCommand(Func<string> function)
        {
            ContentFunction = function;
        }

        /// <summary>
        ///     When executed, the command provdes SCORM info as a
        ///     response in the CommandEventArgs provided.
        /// </summary>
        /// <param name="e">Event arguments provided by the command interpreter</param>
        /// <returns>
        ///     Always true, as the command does not care about additional arguments
        /// </returns>
        public bool Execute(CommandEventArgs e)
        {
            string output = ContentFunction?.Invoke();

            e.Response = output;
            return true;
        }
    }
}
