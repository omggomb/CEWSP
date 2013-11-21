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
using System.Diagnostics;

using CEWSP.Utils;
using CEWSP.Logging;

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
		                                                                    CApplicationSettings.Instance.GetValue(sKey).Descripition,
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
		/// Complete path including filename to ApplicationSettings.xml
		/// </summary>
		private string m_sFileSavePath;
		
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
		
		
		
		
		private static readonly CSetting TemlateFolderNameSetting = new CSetting(ESettingsStrings.TemplateFolderName, CApplicationSettings.DefaultTemplateFolderName, ESettingsStrings.DESC_TemplateFolderName, true);
		private static readonly CSetting RootPathSetting = new CSetting(ESettingsStrings.RootPath, ESettingsStrings.Invalid, ESettingsStrings.DESC_RootPath, false);
		private static readonly CSetting GameFolderPathSetting = new CSetting(ESettingsStrings.GameFolderPath, "GameSDK", ESettingsStrings.DESC_GameFolderPath, false);
		private static readonly CSetting SB64bitRelativePathSetting = new CSetting(ESettingsStrings.SB64bitRelativePath, DefaultSB64bitRelativePath, ESettingsStrings.DESC_SB64bitRelativePath, true);
		private static readonly CSetting SB32bitRelativePathSetting = new CSetting(ESettingsStrings.SB32bitRelativePath, DefaultSB32bitRelativePath, ESettingsStrings.DESC_SB32bitRelativePath, true);
		private static readonly CSetting Game64bitRelativePathSetting = new CSetting(ESettingsStrings.Game64bitRelativePath, DefaultGame64bitRelativePath, ESettingsStrings.DESC_Game64bitRelativePath, true);
		private static readonly CSetting Game32bitRelativePathSetting = new CSetting(ESettingsStrings.Game32bitRelativePath, DefaultGame32bitRelativePath, ESettingsStrings.DESC_Game32bitRelativePath, true);
		private static readonly CSetting CodeSlnRelativePathSetting = new CSetting(ESettingsStrings.CodeSlnFileRelativePath, DefaultCodeSlnFileRelativePath, ESettingsStrings.DESC_CodeSlnFileRelativePath, true);
		private static readonly CSetting ScriptStartupSetting = new CSetting(ESettingsStrings.ScriptStartupFileAbsolutePath, ESettingsStrings.Invalid, ESettingsStrings.DESC_ScriptStartupFileAbsolutePath, true);
		private static readonly CSetting RCRelativePathSetting = new CSetting(ESettingsStrings.RCRelativePath, DefaultRCRelativePath, ESettingsStrings.DESC_RCRelativePath, true);
		private static readonly CSetting VerboseImportExportSetting = new CSetting(ESettingsStrings.VerboseImportExport, false, ESettingsStrings.DESC_VerboseImportExport, true);
		private static readonly CSetting LastImportFilesSetting = new CSetting(ESettingsStrings.LastImportFiles, "", ESettingsStrings.DESC_LastImportFiles, true);
		private static readonly CSetting LastExportFilesSetting = new CSetting(ESettingsStrings.LastExportFiles, "", ESettingsStrings.DESC_LastExportFiles, true);
		private static readonly CSetting ImportOnStartupSetting = new CSetting(ESettingsStrings.ImportOnStartup, false, ESettingsStrings.DESC_ImportOnStartup, true);
		private static readonly CSetting ExportOnExitSetting = new CSetting(ESettingsStrings.ExportOnExit, false, ESettingsStrings.DESC_ExportOnExit, true);
		private static readonly CSetting AskExportOnExitSetting = new CSetting(ESettingsStrings.AskExportOnExit, true, ESettingsStrings.DESC_AskExportOnExit, true);
		private static readonly CSetting AskImportOnStartupSetting = new CSetting(ESettingsStrings.AskImportOnStartup, true, ESettingsStrings.DESC_AskImportOnStartup, true);
		
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
		
		#endregion
		
		public CApplicationSettings()
		{
			
			Settings = new Dictionary<string, CSetting>();
			DCCPrograms = new Dictionary<string, CDCCDefinition>();
			
			m_sFileSavePath = Application.UserAppDataPath + "\\ApplicationSettings.xml";
			
			Reset(false);
		}
	
		
		#region IApplicationSettings
		public bool Init()
		{
			
			// TODO: Implement
			CLogfile.Instance.LogInfo("Initialising application settings...");
			Reset(false);
			
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
			SetValue(SB32bitRelativePathSetting);
			SetValue(Game64bitRelativePathSetting);
			SetValue(Game32bitRelativePathSetting);
			SetValue(CodeSlnRelativePathSetting);
			SetValue(ScriptStartupSetting);
			SetValue(RCRelativePathSetting);
			SetValue(VerboseImportExportSetting);
			SetValue(LastImportFilesSetting);
			SetValue(LastExportFilesSetting);
			SetValue(ExportOnExitSetting);
			SetValue(ImportOnStartupSetting);
			SetValue(AskExportOnExitSetting);
			SetValue(AskImportOnStartupSetting);
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
				MessageBox.Show("Sorry, the directory contains non ASCII characters or whitespaces.\nThis will cause problems inside CE." +
				                "\nPlease use a path containing only numbers or letters from A-Z (a-z) and don't use spaces in your foldernames!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
								// LOCALIZE
				return false;
			}
				
			
			string rcpath = sRootPath + CApplicationSettings.Instance.GetValue(ESettingsStrings.RCRelativePath).GetValueString();//"\\Bin32\\rc\\rc.exe";
			
			if (!File.Exists(rcpath))
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
			
			string currentDef = "";
			string currentProg = "";
			
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
					else if (reader.LocalName == "ProgramDef" && reader.AttributeCount > 0)
					{
						reader.MoveToNextAttribute();
						
						/*string name = reader.Value;
						
						reader.MoveToNextAttribute();
						
						string exec = reader.Value;
						
						reader.MoveToNextAttribute();
						
						string file = reader.Value;
						
						SetDCCProgram(new CDCCDefinition(name, exec, file));*/
						
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
				
				reader.Close();
				CLogfile.Instance.LogInfo("Successfully loaded application settings!");
				
			}
			catch (Exception e )
			{
				if (reader != null)
					reader.Close();
				
				CLogfile.Instance.LogError(String.Format("Failed to load application settings! Error: {0}",
				                                         e.Message));
				
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
			} 
			catch (Exception)
			{
			}
			
			XmlTextWriter writer = null;
			try
			{
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
				
				// DCC programs
				
				writer.WriteStartElement("DCCPrograms");
				
				foreach (string key in DCCPrograms.Keys)
				{
					CDCCDefinition prog = GetDCCProgram(key);
					
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
				return false;
			}
			
			
			return true;
		}
		#endregion
		
	
	}
}