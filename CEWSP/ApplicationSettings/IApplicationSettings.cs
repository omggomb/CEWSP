/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 09/24/2013
 * Time: 19:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 * 
 * 
 * Note that prefixes are used loosely. This is done in order to convey the intended usage of the classes.
 */
using System;
using System.Collections.Generic;



namespace CEWSP.ApplicationSettings
{

	
	
	/// <summary>
	/// Holds all keys used inside the settings
	/// </summary>
	public class ESettingsStrings
	{

		public const string DESC_RootPath = "Path to the root of CE";
		public const string RootPath = "strRootPath";
		
		
		public const string DESC_GameFolderPath = "Name of the game folder to be used";		
		public const string GameFolderPath = "strGameFolderPath";
		
		/// <summary>
		/// Denotes that a certain value is not set or invalid
		/// </summary>
		public const string Invalid = "<invalid>";
		
		public const string DESC_TemplateFolderName = "Relative (to CEWSP.exe) path to the folder containing the startup files";
		public const string TemplateFolderName = "strFileTemplates";
		
		public const string DESC_SB64bitRelativePath = "Relative path to the 64bit Editor.exe, including the file";
		public const string SB64bitRelativePath = "strSB64bitRelativePath";
		
		public const string DESC_Game64bitRelativePath = "Relative path to the 64bit game executable,  including the file";
		public const string Game64bitRelativePath = "strGame64bitRelativePath";
		
		public const string DESC_SB32bitRelativePath = "Relative path to the 32bit Editor.exe,  including the file";
		public const string SB32bitRelativePath = "strSB32bitRelativePath";
		
		public const string DESC_Game32bitRelativePath = "Relative path to the 32bit game executable,  including the file";
		public const string Game32bitRelativePath = "strGame32bitRelativePath";
		
		public const string DESC_CodeSlnFileRelativePath = "Relative path to the code solution file, including the file";
		public const string CodeSlnFileRelativePath = "strCodeSlnFileRelativePath"; 
		
		public const string DESC_RCRelativePath = "Relative path to the resource compiler";
		public const string RCRelativePath = "strRCRelativePath";
		
		public const string DESC_ScriptStartupFileAbsolutePath =  "Absolute path to a startup file for editing scripts (an editor project file, an exe etc.)";
		public const string ScriptStartupFileAbsolutePath = "strScriptStartupFileAbsolutePath";
		
		public const string DESC_VerboseImportExport = "Notify if and why file has not been copied when importing or exporting tracked files";
		public const string VerboseImportExport = "bVerboseImportExport";
		
		public const string DESC_LastImportFiles = "Marks the last imported tracking file(s)";
		public const string LastImportFiles = "strLastImportFiles";
		
		public const string DESC_LastExportFiles = "Marks the last tracking files exported";
		public const string LastExportFiles = "strLastExportFiles";
		
		public const string DESC_ImportOnStartup = "Denotes whether CEWSP tries to import the last exported files on startup";
		public const string ImportOnStartup = "bImportOnStartup";
		
		public const string DESC_ExportOnExit = "Denotes whether CEWSP will export game and root files to the last export dir on closing";
		public const string ExportOnExit = "bExportOnExit";
		
		public const string DESC_AskImportOnStartup = "Ask if CEWSP should import last exported files";
		public const string AskImportOnStartup = "bAskImportOnStartup";
		
		public const string DESC_AskExportOnExit = "Ask if CEWSP should export source tracking file on application exit";
		public const string AskExportOnExit = "bAskExportOnExit";
		
		public const string DESC_SourceTrackerWatchDirs = "Denotes which directories the sourcetracker should watch (GAME, ROOT, BOTH, NONE)";
		public const string SourceTrackerWatchDirs = "strFileExplorerWatchDirs";
		
		public const string DESC_Game64bitArguments = "Commandline arguments for game 64bit quick acces buttons";
		public const string Game64bitArguments = "strGame64bitArguments";
		
		public const string DESC_Game32bitArguments = "Commandline arguments for game 32bit quick acces buttons";
		public const string Game32bitArguments = "strGame32bitArguments";
		
		public const string DESC_Editor64bitArguments = "Commandline arguments for Editor 64bit quick acces buttons";
		public const string Editor64bitArguments = "strEditor64bitArguments";
		
		public const string DESC_Editor32bitArguments = "Commandline arguments for Editor 32bit quick acces buttons";
		public const string Editor32bitArguments = "strEditor32bitArguments";
	}
	
	/// <summary>
	/// Description of IApplicationSettings.
	/// </summary>
	public interface IApplicationSettings
	{
		/// <summary>
		/// Initialises the application settings.
		/// Loads saved setting from file.
		/// </summary>
		/// <returns>True on succes.</returns>
		bool Init();
		
		/// <summary>
		/// Resets all values to default hardcoded ones.
		/// </summary>
		void Reset(bool bOnlyUserEditable);
		
		/// <summary>
		/// Save settings to file.
		/// </summary>
		void Shutdown();
		
		/// <summary>
		/// Checks whether the given root CE path is valid.
		/// Check whether it is CE conform, is not inside any user directory and actually contains rc.exe
		/// </summary>
		/// <param name="sRootPath">The root path to check</param> 
		/// <returns></returns>
		bool IsRootValid(string sRootPath);
		
		/// <summary>
		/// Checks for valid given root, on succes, changes the global CE root to the given one.
		/// </summary>
		/// <param name="sRootPath">The new desired root path.</param>
		/// <returns>True on succes.</returns>
		bool SetRootPath(string sRootPath);
		
		/// <summary>
		/// Checks whether a valid root path is set. If so does nothing. Else sets rootpath to '<invalid>'
		/// </summary>
		/// <returns>True if successfully invalidated root path.</returns>
		bool TryInvalidateRootPath();
		
		/// <summary>
		/// Checks whether the rootpath is set (not empty, '<invalid>')
		/// Possibly obsolete due to reset setting a value for it.
		/// </summary>
		/// <returns>True if any path is set (may be invalid though)</returns>
		bool IsRootPathSet();
		
		
		/// <summary>
		/// Retrieves a single value by its key
		/// </summary>
		/// <param name="sKey">The key of the value wanted</param>
		/// <returns>The value or null if there is no such key</returns>
		CSetting GetValue(string sKey);
		
		/// <summary>
		/// Set the specified entry to the given value. Creates a key if nonexistant.
		/// </summary>
		/// <param name="sKey">Key</param>
		/// <param name="val">Value</param>
		/// <returns>True if item already existed, else false.</returns>
		bool SetValue(CSetting setting);
		
		/// <summary>
		/// Retrieves the requested DCCProgram structure
		/// </summary>
		/// <param name="sName">Name of the requested DCCProgram</param>
		/// <returns>The structure or null if there is no such name</returns>
		CDCCDefinition GetDCCProgram(string sName);
		
		/// <summary>
		/// Returns a list of all available DCC programs
		/// </summary>
		/// <returns>A list object not equal to null</returns>
		List<string> GetAllDCCProgramNames();
		
		/// <summary>
		/// Adds a new dcc program to the list of available ones.
		/// It item already exists, overrides the exe path.
		/// </summary>
		/// <param name="program"></param>
		/// <returns>True if program was added, false if overriden.</returns>
		bool SetDCCProgram(CDCCDefinition program);
		
		/// <summary>
		/// Removes a DCC program from the list.
		/// </summary>
		/// <param name="sName">Name of the program to remove</param>
		/// <returns>True if found and removed, else false.</returns>
		bool RemoveDCCProgram(string sName);
	}
}
