﻿/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 26.09.2013
 * Time: 10:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.IO;

using CEWSP.Utils;
using CEWSP.Logging;
using CEWSP.Shortcuts;

using OmgUtils.UserInteraction;

namespace CEWSP.ApplicationSettings
{
	public enum ESettingsType
	{
		eST_String,
		eST_Boolean,
		eST_Integer,
		eST_Float,
		eST_Double
	}
	public class CSetting
	{
		public object Value;
		public string Key;
		public string Descripition;
		public bool UserEditable;
		
		public CSetting(string sKey, object val, string sDesc, bool bUserEditable = false)
		{
			Value = val.ToString();
			Key = sKey;
			Descripition = sDesc;
			UserEditable = bUserEditable;
		}
		
		public CSetting(string sKey, object val, bool bUserEditable = false) : this(sKey, val,
		                                                                            GetDescriptionSlow(sKey),
		                                                                            bUserEditable)
		{
		}
		
		public override string ToString()
		{
			return Key.ToString();
		}
		
		public bool GetValueBool()
		{
			return Boolean.Parse(Value as string);
		}
		
		public ESettingsType GetSettingType()
		{
			
			switch (Key[0])
			{
				case 'b':
					return ESettingsType.eST_Boolean;
				case 's':
					return ESettingsType.eST_String;
				case 'f':
					return ESettingsType.eST_Float;
				case 'd':
					return ESettingsType.eST_Double;
				default:
					return ESettingsType.eST_Integer;
			}
			
		}
		
		public string GetValueString()
		{
			return Value.ToString();
		}
		
		public static string GetDescriptionSlow(string sKey)
		{
			switch (sKey)
			{
				case ESettingsStrings.AskExportOnExit:
					return ESettingsStrings.DESC_AskExportOnExit;
					
				case ESettingsStrings.AskImportOnStartup:
					return ESettingsStrings.DESC_AskImportOnStartup;
					
				case ESettingsStrings.CheckIgnoredRegexSanityOnStartup:
					return ESettingsStrings.DESC_CheckIngoredRegexSanityOnStartup;
					
				case ESettingsStrings.CodeArguments:
					return ESettingsStrings.DESC_CodeArguments;
					
				case ESettingsStrings.CodeSlnFileRelativePath:
					return ESettingsStrings.DESC_CodeSlnFileRelativePath;
					
				case ESettingsStrings.Editor32bitArguments:
					return ESettingsStrings.DESC_Editor32bitArguments;
					
				case ESettingsStrings.Editor64bitArguments:
					return ESettingsStrings.DESC_Editor64bitArguments;
					
				case ESettingsStrings.ExportOnExit:
					return ESettingsStrings.DESC_ExportOnExit;
					
				case ESettingsStrings.Game32bitArguments:
					return ESettingsStrings.DESC_Game32bitArguments;
					
				case ESettingsStrings.Game32bitRelativePath:
					return ESettingsStrings.DESC_Game32bitRelativePath;
					
				case ESettingsStrings.Game64bitArguments:
					return ESettingsStrings.DESC_Game64bitArguments;
					
				case ESettingsStrings.Game64bitRelativePath:
					return ESettingsStrings.DESC_Game64bitRelativePath;
					
				case ESettingsStrings.GameFolderPath:
					return ESettingsStrings.DESC_GameFolderPath;
					
				case ESettingsStrings.GFXRelativePath:
					return ESettingsStrings.DESC_GFXRelativePath;
					
				case ESettingsStrings.ImportOnStartup:
					return ESettingsStrings.DESC_ImportOnStartup;
					
				case ESettingsStrings.LastExportFiles:
					return ESettingsStrings.DESC_LastExportFiles;
					
				case ESettingsStrings.LastImportFiles:
					return ESettingsStrings.DESC_LastImportFiles;
					
				case ESettingsStrings.RCRelativePath:
					return ESettingsStrings.DESC_RCRelativePath;
					
				case ESettingsStrings.SB32bitRelativePath:
					return ESettingsStrings.DESC_SB32bitRelativePath;
					
				case ESettingsStrings.SB64bitRelativePath:
					return ESettingsStrings.DESC_SB64bitRelativePath;
					
				case ESettingsStrings.ScriptArguments:
					return ESettingsStrings.DESC_ScriptArguments;
					
				case ESettingsStrings.ScriptStartupFileAbsolutePath:
					return ESettingsStrings.DESC_ScriptStartupFileAbsolutePath;
					
				case ESettingsStrings.SourceTrackerWatchDirs:
					return ESettingsStrings.DESC_SourceTrackerWatchDirs;
					
				case ESettingsStrings.TemplateFolderName:
					return ESettingsStrings.DESC_TemplateFolderName;
					
				case ESettingsStrings.VerboseImportExport:
					return ESettingsStrings.DESC_VerboseImportExport;
					
				case ESettingsStrings.RootPath:
					return ESettingsStrings.DESC_RootPath;
					
				case ESettingsStrings.GFXExporterArguments:
					return ESettingsStrings.DESC_GFXExproterArguments;
					
				case ESettingsStrings.RC64bitRelativePath:
					return ESettingsStrings.DESC_RC64bitRelativePath;
					
				case ESettingsStrings.DragZoneLastPath:
					return ESettingsStrings.DESC_DragZoneLastPath;
					
				case ESettingsStrings.WarnNonCEConformPath:
					return ESettingsStrings.DESC_WarnNonCEConformPath;
					
				default:
					return "No description available";
					
			}
		}

	}
	/// <summary>
	/// Description of CApplicationSettings.
	/// </summary>
	public class CApplicationSettings : IApplicationSettings
	{
		#region Attributes
		/// <summary>
		/// Holds all settings that are simple key value pairs
		/// </summary>
		private Dictionary<string, CSetting> Settings { get; set;}
		/// <summary>
		/// Holds all registered DCC Programs
		/// </summary>
		private Dictionary<string, CDCCDefinition> DCCPrograms {get; set;}
		
		public Dictionary<string, CSetting> PublicSettings {get { return Settings; }}
		
		/// <summary>
		/// The path to the file storing the currently active profile
		/// </summary>
		private string m_sCurrentProfileFilePath;
		
		/// <summary>
		/// Path to the subdirectory where all profiles are stored
		/// </summary>
		private string m_sSettingsFilePath;

		public string SettingsFilePath
		{
			get {return m_sSettingsFilePath;}
		}

		/// <summary>
		/// Complete path including filename to ApplicationSettings.xml
		/// </summary>
		private string m_sFileSavePath;
		
		
		private string m_sProgramDefSavePath;
		
		private string m_sShortcutsSavePath;
		
		string m_sProfileHistoryFile;
		
		Dictionary<string, string> m_profileHistory = new Dictionary<string, string>();

		/// <summary>
		/// Path to the currently used profile's directory
		/// </summary>
		private string m_sCurrentProfileSavePath;
		
		/// <summary>
		/// Name of the folder that contains all the template files.
		/// </summary>
		public const string DefaultTemplateFolderName = "FileTemplates";
		
		public const string DefaultSB64bitRelativePath = "\\Bin64\\Editor.exe";
		
		public const string DefaultGame64bitRelativePath = "\\Bin64\\GameSDK.exe";
		
		public const string DefaultSB32bitRelativePath = "\\Bin32\\Editor.exe";
		
		public const string DefaultGame32bitRelativePath = "\\Bin32\\GameSDK.exe";
		
		public const string DefaultCodeSlnFileRelativePath = "\\Code\\Solutions\\CryEngine_GameCodeOnly.sln";
		public const string DefaultRCRelativePath = "\\Bin32\\rc\\rc.exe";
		public const string DefaultGFXExporterRelativePath = "\\Tools\\GFxExport\\gfxexport.exe";
		
		
		
		
		private static  CSetting TemlateFolderNameSetting;
		private static  CSetting RootPathSetting ;
		private static  CSetting GameFolderPathSetting;
		private static  CSetting SB64bitRelativePathSetting;
		private static  CSetting SB32bitRelativePathSetting ;
		private static  CSetting Game64bitRelativePathSetting;
		private static  CSetting Game32bitRelativePathSetting ;
		private static  CSetting CodeSlnRelativePathSetting ;
		private static  CSetting ScriptStartupSetting ;
		private static  CSetting RCRelativePathSetting ;
		private static  CSetting VerboseImportExportSetting ;
		private static  CSetting LastImportFilesSetting ;
		private static  CSetting LastExportFilesSetting ;
		private static  CSetting ImportOnStartupSetting ;
		private static  CSetting ExportOnExitSetting;
		private static  CSetting AskExportOnExitSetting;
		private static  CSetting AskImportOnStartupSetting;
		private static  CSetting SourceTrackerWatchDirsSetting ;
		private static  CSetting Game64bitArgumentsSetting ;
		private static  CSetting Game32bitArgumentsSetting ;
		private static  CSetting Editor64bitArgumentsSetting ;
		private static  CSetting Editor32bitArgumentsSetting ;
		private static  CSetting CodeArgumentsSetting ;
		private static  CSetting ScriptArgumentsSetting ;
		private static  CSetting CheckRegexOnStartupSetting ;
		private static  CSetting GFXExporterPathSetting ;
		private static  CSetting GFXExporterArgsSetting ;
		private static  CSetting RC64bitRelativePathSetting;
		private static CSetting WarnIfPathNotCEConfirmSetting;
		private static CSetting DragZoneLastPathSetting;
		
		private static CApplicationSettings _instance;
		public static CApplicationSettings Instance
		{
			get
			{
				if (_instance == null)
					_instance = new CApplicationSettings();
				return _instance;
			}
		}
		
		public List<SShortcut> Shortcuts {get; private set;}
		
		#endregion
		
		public CApplicationSettings()
		{
			SetupDefaultSettings();
			
			Settings = new Dictionary<string, CSetting>();
			DCCPrograms = new Dictionary<string, CDCCDefinition>();
			Shortcuts = new List<SShortcut>();
			
			m_sSettingsFilePath = ".\\Profiles";
			m_sCurrentProfileFilePath = "\\currentSettingsFile.txt";
			m_sProfileHistoryFile = m_sSettingsFilePath + "\\profileHistory.txt";
			
			m_sCurrentProfileSavePath = GetCurrentSettingsFolder();
			
			if (String.IsNullOrEmpty(m_sCurrentProfileSavePath))
				m_sCurrentProfileSavePath = m_sSettingsFilePath + "\\Default";
			
			AdjustSettingsPaths();
			
			if (!Directory.Exists(m_sCurrentProfileSavePath))
				Directory.CreateDirectory(m_sCurrentProfileSavePath);
			
			Reset(false);
		}
		
		
		#region IApplicationSettings
		public bool Init()
		{
			
			// TODO: Implement
			CLogfile.Instance.LogInfo("Initialising application settings...");
			Reset(false);
			
			
			if (!File.Exists(m_sShortcutsSavePath))
			{
				ResetShortcuts();
				SaveShortcutsToFile();
				
			}
			
			return LoadApplicationSettings();
		}

		
		public void Reset(bool bOnlyUserEditable)
		{
			// TODO: Implement
			if (!bOnlyUserEditable)
			{
				SetValue(RootPathSetting);
				SetValue(GameFolderPathSetting);
			}
			
			SetValue(TemlateFolderNameSetting);
			
			SetValue(SB64bitRelativePathSetting);
			SetValue(Editor64bitArgumentsSetting);
			
			SetValue(SB32bitRelativePathSetting);
			SetValue(Editor32bitArgumentsSetting);
			
			SetValue(Game64bitRelativePathSetting);
			SetValue(Game64bitArgumentsSetting);
			
			SetValue(Game32bitRelativePathSetting);
			SetValue(Game32bitArgumentsSetting);
			
			SetValue(CodeSlnRelativePathSetting);
			SetValue(CodeArgumentsSetting);
			
			SetValue(ScriptStartupSetting);
			SetValue(ScriptArgumentsSetting);
			
			SetValue(RCRelativePathSetting);
			SetValue(RC64bitRelativePathSetting);
			
			SetValue(GFXExporterPathSetting);
			SetValue(GFXExporterArgsSetting);
			SetValue(VerboseImportExportSetting);
			SetValue(LastImportFilesSetting);
			SetValue(LastExportFilesSetting);
			SetValue(ExportOnExitSetting);
			SetValue(ImportOnStartupSetting);
			SetValue(AskExportOnExitSetting);
			SetValue(AskImportOnStartupSetting);
			SetValue(SourceTrackerWatchDirsSetting);
			SetValue(CheckRegexOnStartupSetting);
			
			SetValue(WarnIfPathNotCEConfirmSetting);
			SetValue(DragZoneLastPathSetting);
			
			
			ResetShortcuts();
		}
		
		private void ResetShortcuts()
		{
			Shortcuts.Clear();

			for (int i = 0; i < 6; ++i)
			{
				SShortcut sc = new SShortcut();
				
				sc.Exec = "";
				sc.Args = "";
				sc.Name = "Shortcut" + i.ToString();
				sc.myID = i;
				Shortcuts.Add(sc);
			}
		}
		
		private void LoadShortcutsFromFile()
		{
			XmlTextReader reader = new XmlTextReader(m_sShortcutsSavePath);
			
			if (reader != null)
			{
				Shortcuts.Clear();
				while (reader.Read())
				{
					if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Shortcut")
					{
						
						reader.MoveToAttribute("id");
						int id = int.Parse(reader.Value);
						
						if (id >= 0 && id < 6)
						{
							SShortcut sc = new SShortcut();
							
							sc.myID = id;
							
							reader.MoveToAttribute("name");
							sc.Name = reader.Value;
							
							reader.MoveToAttribute("exec");
							sc.Exec = reader.Value;
							
							reader.MoveToAttribute("args");
							sc.Args = reader.Value;
							
							Shortcuts.Add(sc);
						}
						
					}
				}
				
				reader.Close();
			}
		}
		
		public void SaveShortcutsToFile()
		{
			XmlTextWriter writer = null;
			
			try
			{
				writer = new XmlTextWriter(File.Open(m_sShortcutsSavePath, FileMode.Create), System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				
				if (writer != null)
				{
					writer.WriteStartDocument();
					
					writer.WriteStartElement("Shortcuts");
					
					foreach (SShortcut sc in Shortcuts)
					{
						writer.WriteStartElement("Shortcut");
						
						writer.WriteAttributeString("id", sc.myID.ToString());
						writer.WriteAttributeString("name", sc.Name);
						writer.WriteAttributeString("exec", sc.Exec);
						writer.WriteAttributeString("args", sc.Args);
						
						writer.WriteEndElement();
					}
					
					writer.WriteEndElement();
					
					writer.WriteEndDocument();
					
					writer.Close();
				}
			}
			catch (Exception e)
			{
				if (writer != null)
					writer.Close();
				
				UserInteractionUtils.ShowErrorMessageBox(e.Message);
			}
		}
		
		public void Shutdown()
		{
			SaveApplicationSettings();
		}
		
		public bool IsRootValid(string sRootPath)
		{
			// TODO: Implement
			
			if (sRootPath == ESettingsStrings.Invalid)
				return false;
			
			if (!CPathUtils.IsStringCEConform(sRootPath))
			{
				var res = MessageBox.Show("Sorry, the directory contains non ASCII characters or whitespaces.\nThis will cause problems inside CE." +
				                          "\nPlease use a path containing only numbers or letters from A-Z (a-z) and don't use spaces in your foldernames!", "Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
				
				
				if (res != DialogResult.Ignore)				// LOCALIZE
					return false;
			}
			
			
			string rcpath = sRootPath + CApplicationSettings.Instance.GetValue(ESettingsStrings.RCRelativePath).GetValueString();//"\\Bin32\\rc\\rc.exe";
			string rc64bit = sRootPath + CApplicationSettings.Instance.GetValue(ESettingsStrings.RC64bitRelativePath).GetValueString();
			
			
			if (!File.Exists(rcpath) && !File.Exists(rc64bit))
			{
				MessageBox.Show("Sorry, no rc.exe found!\n Make sure you have selected a CE root dir (contains Bin32, Bin64 e.t.c)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				// LOCALIZE
				return false;
			}
			
			return true;
		}
		

		
		public bool SetRootPath(string sRootPath)
		{
			string final = ESettingsStrings.Invalid;
			
			if (sRootPath != ESettingsStrings.Invalid)
			{
				string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				
				if (sRootPath.Contains(userPath))
				{
					System.Windows.Forms.DialogResult res = MessageBox.Show("It seems that you are trying to run CE from within your user folder (Downloads, Documents e.t.c)" +
					                                                        "This is not recommended, are you sure you want to continue?", "Warning", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Exclamation);
					// LOCALIZE
					if (res != DialogResult.Ignore)
						return false;
					
				}
				
				
				if (IsRootValid(sRootPath))
				{
					final = sRootPath;
				}
			}

			if (Settings.ContainsKey(ESettingsStrings.RootPath))
			{
				Settings.Remove(ESettingsStrings.RootPath);
			}
			
			Settings.Add(ESettingsStrings.RootPath, new CSetting(ESettingsStrings.RootPath, final, ESettingsStrings.DESC_RootPath));
			
			bool validRoot = (final != ESettingsStrings.Invalid) ? true : false;
			
			return validRoot;
		}
		
		public bool TryInvalidateRootPath()
		{
			if (IsRootValid(GetValue(ESettingsStrings.RootPath).GetValueString()))
			{
				return false;
			}
			
			SetValue(ESettingsStrings.RootPath, ESettingsStrings.Invalid);
			
			return true;
		}
		
		public bool IsRootPathSet()
		{
			object root = GetValue(ESettingsStrings.RootPath);
			
			if(root == null)
			{
				return false;
			}
			else
			{
				return true;
			}
		}
		
		
		/*private object GetValue(string sKey)
		{
			object returned = new object();
			
			bool succes = Settings.TryGetValue(sKey, out returned);
					
			return succes ? returned : null;
			return true;
		}*/
		
		public CSetting GetValue(string sKey)
		{
			CSetting setting = null;
			
			bool sux = Settings.TryGetValue(sKey, out setting);
			
			return setting;
		}
		
		
		public bool SetValue(CSetting setting)
		{
			if (setting != null)
			{
				if (setting.Key == ESettingsStrings.RootPath)
				{
					return SetRootPath((string)setting.Value);
				}
				
				bool isNotOverride = true;
				
				if (Settings.ContainsKey(setting.Key))
				{
					Settings.Remove(setting.Key);
					isNotOverride = false;
				}
				
				Settings.Add(setting.Key, setting);
				
				return isNotOverride;
			}
			
			return false;
		}
		
		private bool SetValue(string sKey, object val)
		{
			/*// Rootpath needs special treatment
			if (sKey == ESettingsStrings.RootPath)
			{
				return SetRootPath((string)val);
			}
			
			bool isNotOverride = true;
			
			if (Settings.ContainsKey(sKey))
			{
				Settings.Remove(sKey);
				isNotOverride = false;
			}
			
			Settings.Add(sKey, val);
			
			return isNotOverride;*/
				return true;
		}
		
		public CDCCDefinition GetDCCProgram(string sName)
		{
			CDCCDefinition prog;
			
			bool succed = DCCPrograms.TryGetValue(sName, out prog);
			
			if (!succed)
				CLogfile.Instance.LogError(String.Format("Trying to get DCCDefinition {0}, which doesn't exist!",
				                                         sName));
			return succed ? prog : null;
		}
		
		public List<string> GetAllDCCProgramNames()
		{
			List<string> names = new List<string>();
			
			foreach (string key in DCCPrograms.Keys)
			{
				names.Add(key);
			}
			return names;
		}
		
		
		
		public bool SetDCCProgram(CDCCDefinition program)
		{
			bool isNotOverride = true;
			
			if (DCCPrograms.ContainsKey(program.Name))
			{
				DCCPrograms.Remove(program.Name);
				isNotOverride = false;
			}
			
			DCCPrograms.Add(program.Name, program);
			
			return isNotOverride;
		}
		
		public bool RemoveDCCProgram(string sName)
		{
			return DCCPrograms.Remove(sName);
		}
		
		
		
		#endregion
		
		#region Methods
		
		/// <summary>
		/// Loads all settings from ApplicationSettings.xml inside of UserAppDir
		/// </summary>
		/// <returns>True if loading succeded</returns>
		public bool LoadApplicationSettings()
		{
			Reset(false);
			
			if (!File.Exists(m_sFileSavePath))
				if (!SaveApplicationSettings())
					return false;
			
			
			XmlTextReader reader = null;
			try
			{
				CLogfile.Instance.LogInfo("Loading application settings from file...");
				
				reader = new XmlTextReader(m_sFileSavePath);
				
				while (reader.Read())
				{
					reader.MoveToElement();
					
					
					if (reader.LocalName == "Setting")
					{
						reader.MoveToNextAttribute();
						
						string name = reader.Name;
						string val = reader.Value;
						
						reader.MoveToNextAttribute();
						
						bool bUserEditable = bool.Parse(reader.Value);
						
						SetValue(new CSetting(name, val, bUserEditable));
					}
				}
				
				reader.Close();
				
				LoadProgramDefinitions();
				LoadShortcutsFromFile();
				LoadProfileHistory();
				
				CLogfile.Instance.LogInfo("Successfully loaded application settings!");
				
			}
			catch (Exception e )
			{
				if (reader != null)
					reader.Close();
				
				CLogfile.Instance.LogError(String.Format("Failed to load application settings! Error: {0}",
				                                         e.Message));	// LOCALIZE
				
				return false;
			}
			

			
			
			return true;
		}
		
		
		/// <summary>
		/// Saves all settings to ApplicationSettings.xml
		/// </summary>
		/// <returns>True on succes</returns>
		public bool SaveApplicationSettings()
		{
			try
			{
				File.Delete(m_sFileSavePath);
				File.Delete(m_sSettingsFilePath + m_sCurrentProfileFilePath);
			}
			catch (Exception e)
			{
				UserInteractionUtils.ShowErrorMessageBox(e.Message, Properties.Resources.CommonError);
			}
			
			XmlTextWriter writer = null;
			StreamWriter streamWriter = null;
			try
			{
				streamWriter = new StreamWriter(File.Open(m_sSettingsFilePath + m_sCurrentProfileFilePath, FileMode.CreateNew));
				
				streamWriter.WriteLine(m_sCurrentProfileSavePath);
				streamWriter.Close();
				
				
				//SaveProgramDefinitions();
				writer = new XmlTextWriter(m_sFileSavePath, System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				
				writer.WriteStartDocument();
				
				writer.WriteComment("Application settings for CEWSP");
				
				writer.WriteStartElement("ApplicationSettings");
				
				
				
				// single settings
				foreach (string key in Settings.Keys)
				{
					object val = GetValue(key);
					CSetting setting = val as CSetting;
					
					writer.WriteComment(setting.Descripition);
					
					writer.WriteStartElement("Setting");
					
					if (val != null)
					{
						writer.WriteAttributeString(key, setting.GetValueString());
					}
					else
					{
						writer.WriteAttributeString(key, ESettingsStrings.Invalid);
					}
					
					writer.WriteAttributeString("userEditable", setting.UserEditable.ToString());
					
					writer.WriteEndElement();
				}
				
				writer.WriteEndElement();
				
				writer.Flush();
				writer.Close();
				
			}
			catch (Exception)
			{
				MessageBox.Show("Failed to save application settings!", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Reset(false);
				if (writer != null)
					writer.Close();
				
				if (streamWriter != null)
					streamWriter.Close();
				
				return false;
			}
			
			SaveProfileHistory();
			SaveProgramDefinitions();
			return true;
		}
		
		/// <summary>
		/// Checks whether the source tracker should watch the current game directory
		/// </summary>
		/// <returns>True if the current game directory should be watched</returns>
		public bool SourceTrackerShouldWatchGame()
		{
			string val = GetValue(ESettingsStrings.SourceTrackerWatchDirs).GetValueString().ToLower();
			
			return (val == SourceFileTracking.ETrackedDirs.eTD_Both || val == SourceFileTracking.ETrackedDirs.eTD_Game);
		}
		
		/// <summary>
		/// Checks whether the source tracker should watch the current root directory
		/// </summary>
		/// <returns>True if the current root directory should be watched</returns>
		public bool SourceTrackerShouldWatchRoot()
		{
			string val = GetValue(ESettingsStrings.SourceTrackerWatchDirs).GetValueString().ToLower();
			
			return (val == SourceFileTracking.ETrackedDirs.eTD_Both || val == SourceFileTracking.ETrackedDirs.eTD_Root);
		}
		
		private void SaveProgramDefinitions()
		{
			XmlTextWriter writer = null;
			
			try
			{
				if (File.Exists(m_sProgramDefSavePath))
					File.Delete(m_sProgramDefSavePath);
				
				writer = new XmlTextWriter(File.Open(m_sProgramDefSavePath, FileMode.Create), System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				writer.WriteStartDocument();
				
				
				writer.WriteStartElement("ProgramDefs");
				
				
				
				foreach (var prog in DCCPrograms.Values)
				{

					if (prog != null)
					{
						writer.WriteStartElement("ProgramDef");
						writer.WriteAttributeString("name", prog.Name);
						//writer.WriteAttributeString("exec", prog.GetConcatenatedExecs());
						//writer.WriteAttributeString("file", prog.GetConcatenatedStartups());
						
						foreach (string progKey in prog.Programs.Keys)
						{
							SDCCProgram progele;
							prog.Programs.TryGetValue(progKey, out progele);
							
							writer.WriteStartElement("Program");
							writer.WriteAttributeString("name", progele.Name);
							writer.WriteAttributeString("Exec", progele.ExecutablePath);
							
							foreach (string  fileKey in progele.StartupFiles.Keys)
							{
								SStartupFile file;
								progele.StartupFiles.TryGetValue(fileKey, out file);
								
								writer.WriteStartElement("File");
								
								writer.WriteAttributeString("name", file.Name);
								writer.WriteAttributeString("path", file.FullName);
								writer.WriteAttributeString("copy", file.Copy.ToString());
								writer.WriteAttributeString("launch", file.LaunchWithProgram.ToString());
								
								writer.WriteEndElement();
							}
							
							writer.WriteEndElement();
						}
						
						writer.WriteEndElement();
					}
				}
				
				writer.WriteEndElement();
				writer.WriteEndDocument();
				
				writer.Close();
			}
			catch (Exception e)
			{
				if (writer != null)
					writer.Close();
				
				UserInteractionUtils.ShowErrorMessageBox(e.Message); // TODO: More info pls
			}
		}
		
		private void LoadProgramDefinitions()
		{
			XmlTextReader reader = null;
			
			try
			{
				if (!File.Exists(m_sProgramDefSavePath))
					SaveProgramDefinitions();
				
				
				reader = new XmlTextReader(m_sProgramDefSavePath);
				string currentDef = "";
				string currentProg = "";
				while (reader.Read())
				{
					reader.MoveToElement();
					
					if (reader.LocalName == "ProgramDef" && reader.AttributeCount > 0)
					{
						reader.MoveToNextAttribute();
						
						var def = new CDCCDefinition();
						
						def.Name = reader.Value;
						
						SetDCCProgram(def);
						currentDef = reader.Value;
					}
					else if (reader.LocalName == "Program" && reader.AttributeCount > 0)
					{
						reader.MoveToNextAttribute();
						
						SDCCProgram program = new SDCCProgram();
						
						program.Name = reader.Value;
						
						reader.MoveToNextAttribute();
						
						program.ExecutablePath = reader.Value;
						
						GetDCCProgram(currentDef).Programs.Add(program.Name, program);
						
						currentProg = program.Name;
					}
					else if (reader.LocalName == "File")
					{
						SDCCProgram prog = GetDCCProgram(currentDef).GetProgram(currentProg);
						
						SStartupFile file = new SStartupFile("", "");
						
						reader.MoveToNextAttribute();
						
						file.Name = reader.Value;
						
						reader.MoveToNextAttribute();
						
						file.SetFilePath(reader.Value);
						
						reader.MoveToNextAttribute();
						
						bool boolVal;
						Boolean.TryParse(reader.Value, out boolVal);
						file.Copy = boolVal;
						
						reader.MoveToNextAttribute();
						
						Boolean.TryParse(reader.Value, out boolVal);
						file.LaunchWithProgram = boolVal;
						
						prog.StartupFiles.Add(file.Name, file);
						
					}
					
					else
					{
						
					}
				}
			}
			catch (Exception e)
			{
				if (reader != null)
					reader.Close();
				
				UserInteractionUtils.ShowErrorMessageBox(e.Message); // TODO: More ionfo pls
				
			}
		}
		
		string GetCurrentSettingsFolder()
		{
			if (!File.Exists(m_sSettingsFilePath + m_sCurrentProfileFilePath))
				return "";
			
			FileStream fileStream = null;
			
			try
			{
				fileStream =  File.OpenRead(m_sSettingsFilePath + m_sCurrentProfileFilePath);
				
				StreamReader strReader = new StreamReader(fileStream);
				
				string sLine = strReader.ReadLine();
				
				strReader.Close();
				
				return sLine;
			}
			catch (Exception e)
			{
				if (fileStream != null)
					fileStream.Close();
				
				UserInteractionUtils.ShowErrorMessageBox(e.Message);
				
				return "";
			}
		}
		
		/// <summary>
		/// Saves old profile to file and load new one.
		/// </summary>
		/// <param name="sProfileFolderPath">Relative path to the folder containing the new profile</param>
		public void LoadNewProfile(string sProfileFolderPath)
		{
			SaveApplicationSettings();
			
			if (!Directory.Exists(sProfileFolderPath))
			{
				UserInteractionUtils.ShowErrorMessageBox("Could not find specified profile folder path, not loading profile!");  // LOCALIZE
				return;
			}
			
			
			m_sCurrentProfileSavePath = sProfileFolderPath;
			AdjustSettingsPaths();
			LoadApplicationSettings();
			LoadProgramDefinitions();
			LoadShortcutsFromFile();
			
			var dirInfo = new DirectoryInfo(sProfileFolderPath);
			if (dirInfo != null)
			{
				if (!m_profileHistory.ContainsKey(dirInfo.Name))
					m_profileHistory.Add(dirInfo.Name, sProfileFolderPath);
			}
		}
		
		/// <summary>
		/// Save current settings to file and sets m_sCurrentProfileSavePath accordingly
		/// </summary>
		/// <param name="sPathToFolder">Relative path to the folder where settings should be saved</param>
		public void SaveCurrentProfile(string sPathToFolder)
		{
			if (!Directory.Exists(sPathToFolder))
			{
				UserInteractionUtils.ShowErrorMessageBox("Could not find specified profile folder path, not loading profile!"); // LOCALIZE
				return;
			}
			
			m_sCurrentProfileSavePath = sPathToFolder;
			AdjustSettingsPaths();
			SaveApplicationSettings();
			SaveProgramDefinitions();
			SaveShortcutsToFile();
			
			var dirInfo = new DirectoryInfo(sPathToFolder);
			if (dirInfo != null)
			{
				if (!m_profileHistory.ContainsKey(dirInfo.Name))
					m_profileHistory.Add(dirInfo.Name, sPathToFolder);
			}
			
		}
		
		/// <summary>
		/// Adjust file paths according to m_sCurrentProfileSavePath
		/// </summary>
		void AdjustSettingsPaths()
		{
			m_sFileSavePath = m_sCurrentProfileSavePath + "\\ApplicationSettings.xml";
			m_sProgramDefSavePath = m_sCurrentProfileSavePath + "\\ProgramDefs.xml";
			m_sShortcutsSavePath = m_sCurrentProfileSavePath + "\\ProgramShortcuts.xml";
		}
		
		public Dictionary<string, string> GetProfileHistory()
		{
			return new Dictionary<string, string>(m_profileHistory);
		}
		
		void LoadProfileHistory()
		{
			if (!File.Exists(m_sProfileHistoryFile))
				return;
			
			m_profileHistory.Clear();
			
			StreamReader reader = null;
			
			try
			{
				reader = new StreamReader(File.Open(m_sProfileHistoryFile, FileMode.Open));
				
				while (!reader.EndOfStream)
				{
					string sEntry = reader.ReadLine();
					
					var dirInf = new DirectoryInfo(sEntry);
					
					m_profileHistory.Add(dirInf.Name, sEntry);
				}
				
				reader.Close();
			}
			catch (Exception e)
			{
				if (reader != null)
					reader.Close();
				
				UserInteractionUtils.ShowErrorMessageBox(e.Message);
			}
		}
		
		void SaveProfileHistory()
		{
			StreamWriter writer = null;
			
			if (File.Exists(m_sProfileHistoryFile))
				File.Delete(m_sProfileHistoryFile);
			
			try {
				writer = new StreamWriter(File.Open(m_sProfileHistoryFile, FileMode.OpenOrCreate));
				
				foreach (string pro in m_profileHistory.Values)
				{
					writer.WriteLine(pro);
				}
				
				writer.Close();
			}
			catch (Exception e)
			{
				
				if (writer != null)
					writer.Close();
				
				UserInteractionUtils.ShowErrorMessageBox(e.Message);
			}
		}
		
		public void DeleteProfile(string sKey, bool bHistoryOnly)
		{
			if (!bHistoryOnly)
			{
				if (m_profileHistory[sKey] == m_sCurrentProfileSavePath)
				{
					UserInteractionUtils.ShowErrorMessageBox("Trying to delete currently active profile, please switch to another one first!"); // LOCALIZE
					return;
				}
				
				try {
					Directory.Delete(m_profileHistory[sKey], true);
				}
				catch (DirectoryNotFoundException)
				{
					
					
				}
			}
			
			m_profileHistory.Remove(sKey);
		}
		
		
		public Dictionary<string, SDCCProgram> GetAllDCCProgramsDefined()
		{
			var returnList = new Dictionary<string, SDCCProgram>();
			foreach (CDCCDefinition def in DCCPrograms.Values)
			{
				foreach (SDCCProgram prog in def.Programs.Values)
				{
					if (!returnList.ContainsKey(prog.Name))
						returnList.Add(prog.Name, prog);
				}
			}
			
			return returnList;
		}
		
		/// <summary>
		/// Initalizes the default settings of CEWSP
		/// </summary>
		void SetupDefaultSettings()
		{
			TemlateFolderNameSetting = new CSetting(ESettingsStrings.TemplateFolderName, CApplicationSettings.DefaultTemplateFolderName, ESettingsStrings.DESC_TemplateFolderName, true);
			RootPathSetting = new CSetting(ESettingsStrings.RootPath, ESettingsStrings.Invalid, ESettingsStrings.DESC_RootPath, false);
		
			GameFolderPathSetting = new CSetting(ESettingsStrings.GameFolderPath, "GameSDK", ESettingsStrings.DESC_GameFolderPath, false);
			
			
			SB64bitRelativePathSetting = new CSetting(ESettingsStrings.SB64bitRelativePath, DefaultSB64bitRelativePath, ESettingsStrings.DESC_SB64bitRelativePath, true);
			SB32bitRelativePathSetting = new CSetting(ESettingsStrings.SB32bitRelativePath, DefaultSB32bitRelativePath, ESettingsStrings.DESC_SB32bitRelativePath, true);
			
			Game64bitRelativePathSetting = new CSetting(ESettingsStrings.Game64bitRelativePath, DefaultGame64bitRelativePath, ESettingsStrings.DESC_Game64bitRelativePath, true);
			Game32bitRelativePathSetting = new CSetting(ESettingsStrings.Game32bitRelativePath, DefaultGame32bitRelativePath, ESettingsStrings.DESC_Game32bitRelativePath, true);
			
			
			CodeSlnRelativePathSetting = new CSetting(ESettingsStrings.CodeSlnFileRelativePath, DefaultCodeSlnFileRelativePath, ESettingsStrings.DESC_CodeSlnFileRelativePath, true);
			ScriptStartupSetting = new CSetting(ESettingsStrings.ScriptStartupFileAbsolutePath, ESettingsStrings.Invalid, ESettingsStrings.DESC_ScriptStartupFileAbsolutePath, true);
			RCRelativePathSetting = new CSetting(ESettingsStrings.RCRelativePath, DefaultRCRelativePath, ESettingsStrings.DESC_RCRelativePath, true);
			VerboseImportExportSetting = new CSetting(ESettingsStrings.VerboseImportExport, false, ESettingsStrings.DESC_VerboseImportExport, true);
			LastImportFilesSetting = new CSetting(ESettingsStrings.LastImportFiles, "", ESettingsStrings.DESC_LastImportFiles, true);
			LastExportFilesSetting = new CSetting(ESettingsStrings.LastExportFiles, "", ESettingsStrings.DESC_LastExportFiles, true);
			ImportOnStartupSetting = new CSetting(ESettingsStrings.ImportOnStartup, false, ESettingsStrings.DESC_ImportOnStartup, true);
			ExportOnExitSetting = new CSetting(ESettingsStrings.ExportOnExit, false, ESettingsStrings.DESC_ExportOnExit, true);
			AskExportOnExitSetting = new CSetting(ESettingsStrings.AskExportOnExit, true, ESettingsStrings.DESC_AskExportOnExit, true);
			AskImportOnStartupSetting = new CSetting(ESettingsStrings.AskImportOnStartup, true, ESettingsStrings.DESC_AskImportOnStartup, true);
			SourceTrackerWatchDirsSetting = new CSetting(ESettingsStrings.SourceTrackerWatchDirs, "NONE", ESettingsStrings.DESC_SourceTrackerWatchDirs, true);
			Game64bitArgumentsSetting = new CSetting(ESettingsStrings.Game64bitArguments, "", ESettingsStrings.DESC_Game64bitArguments, true);
			Game32bitArgumentsSetting = new CSetting(ESettingsStrings.Game32bitArguments, "", ESettingsStrings.DESC_Game32bitArguments, true);
			Editor64bitArgumentsSetting = new CSetting(ESettingsStrings.Editor64bitArguments, "", ESettingsStrings.DESC_Editor64bitArguments, true);
			Editor32bitArgumentsSetting = new CSetting(ESettingsStrings.Editor32bitArguments, "", ESettingsStrings.DESC_Editor32bitArguments, true);
			CodeArgumentsSetting = new CSetting(ESettingsStrings.CodeArguments, "", ESettingsStrings.DESC_CodeArguments, true);
			ScriptArgumentsSetting = new CSetting(ESettingsStrings.ScriptArguments, "", ESettingsStrings.DESC_ScriptArguments, true);
			CheckRegexOnStartupSetting = new CSetting(ESettingsStrings.CheckIgnoredRegexSanityOnStartup, false, ESettingsStrings.DESC_CheckIngoredRegexSanityOnStartup, true);
			GFXExporterPathSetting = new CSetting(ESettingsStrings.GFXRelativePath, DefaultGFXExporterRelativePath, ESettingsStrings.DESC_GFXRelativePath, true);
			GFXExporterArgsSetting = new CSetting(ESettingsStrings.GFXExporterArguments, "", ESettingsStrings.DESC_GFXExproterArguments, true);
			RC64bitRelativePathSetting = new CSetting(ESettingsStrings.RC64bitRelativePath, "\\Bin64\\rc\\rc.exe", ESettingsStrings.DESC_RC64bitRelativePath, true);
			
			WarnIfPathNotCEConfirmSetting = new CSetting(ESettingsStrings.WarnNonCEConformPath, true, ESettingsStrings.DESC_WarnNonCEConformPath, true);
			DragZoneLastPathSetting = new CSetting(ESettingsStrings.DragZoneLastPath, "", ESettingsStrings.DESC_DragZoneLastPath);
			
		}
		#endregion
		
		
	}
}
