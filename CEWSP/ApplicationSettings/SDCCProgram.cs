/*
 * Created by SharpDevelop.
 * User: Ihatenames
 * Date: 18.10.2013
 * Time: 12:51
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using CEWSP.Utils;

namespace CEWSP.ApplicationSettings
{
	/// <summary>
	/// Represents a single startupfile. 
	/// </summary>
	public class SStartupFile
	{
		public string FullName {get; private set;}
		public string Extension {get; private set;}
		public string Name {get; set;}
		public bool Copy {get; set;}
		public bool LaunchWithProgram {get; set;}
		
		public SStartupFile(string name, string path)
		{
			Name = name;
			SetFilePath(path);
		}
		
		public void SetFilePath(string path)
		{
			FullName = path;
			int dotPos = path.LastIndexOf('.');
			
			if (dotPos != -1 && dotPos != path.Length - 1)
			{
				Extension = path.Substring(dotPos + 1, path.Length - dotPos - 1);
			}
			else
			{
				Extension = "";
			}
			
		}
		
		public bool IsIdentical(SStartupFile compareTo)
		{
			return (FullName == compareTo.FullName &&
			    	Name == compareTo.Name &&
			    	Copy == compareTo.Copy &&
			    	LaunchWithProgram == compareTo.LaunchWithProgram);
		}
	};
	
	/// <summary>
	/// Represents a single program. Can have multiple startup files. One instance is launched for each startup file.
	/// </summary>
	public class SDCCProgram
	{
		/// <summary>
		/// Full path to the executable.
		/// </summary>
		public string ExecutablePath {get; set;}
	

		/// <summary>
		/// List of all startup files to be used for this program
		/// </summary>
		public Dictionary<string, SStartupFile> StartupFiles {get; set;}
		
		public string Name {get; set;}
		
		
		public SDCCProgram()
		{
			StartupFiles = new Dictionary<string, SStartupFile>();
		}
		
		public SStartupFile GetFile(string name)
		{
			SStartupFile file;
			StartupFiles.TryGetValue(name, out file);
			return file;
		}
		
		public bool IsIdentical(SDCCProgram compareTo)
		{
			bool isIdentical = false;
			
			isIdentical = StartupFiles == compareTo.StartupFiles;
			
			
			
			
			return isIdentical;
		}
		

	};
	/// <summary>
	/// Represents a DCC package
	/// </summary>
	public class CDCCDefinition
	{
		
		public Dictionary<string, SDCCProgram> Programs = new Dictionary<string, SDCCProgram>();
		
		public string Name {get; set;}
		
		private const string m_sRootPathMacro = "$CEWSP_CE_ROOT_PATH";
		private const string m_sGameFolderMacro = "$CEWSP_CE_GAMEFOLDER_PATH";	
		private const string m_sFileNameMacro = "$CEWSP_CURRENT_FILE_NAME";
		private const string m_sFileMacro = "$CEWSP_CURRENT_FILE";
		private const string m_sFolderMacro = "$CEWSP_CURRENT_FOLDER";
		
		public CDCCDefinition()
		{
		}
		
		/// <summary>
		/// </summary>
		/// <param name="sName"></param>
		/// <param name="sExecutablePath">Can contain several execs, separated by semicolons</param>
		/// <param name="sStartupFileName">Can contain several startups, separated by semicolons</param>
		public CDCCDefinition(string sName, string sExecutablePath, string sStartupFileName)
		{
			// Add potentially missing semicolon at the end
			if (sExecutablePath[sExecutablePath.Length - 1] != ';')
				sExecutablePath += ';';
			
			if (sStartupFileName[sStartupFileName.Length - 1] != ';')
				sStartupFileName += ';';
			
		
			
			Name = sName;
			
			ParseExecutables(sExecutablePath);
			ParseStartupFiles(sStartupFileName);
		}
		
		public string GetConcatenatedExecs()
		{
			string execs = "";
			
			
			return execs;
		}
		
		public string GetConcatenatedStartups()
		{
			string startups = "";
			
			
			return startups;
		}
		
		public SDCCProgram GetProgram(string sName)
		{
			SDCCProgram program;
			Programs.TryGetValue(sName, out program);
			
			return program;
		}
		
		/// <summary>
		/// Copies the startup files and starts the programs with their specified startups
		/// </summary>
		/// <param name="sFileSavePath">Path containing one filename, which will be used for all programs startups. If no file is specified, the defaults will be used</param>
		public void Start(string sFileSavePath)
		{
			
			
			string fileName = "";
			string directory = "";
			int lastDir = sFileSavePath.LastIndexOf('\\');
			
			// Deduce the filename
			if (lastDir < sFileSavePath.Length - 1)
			{
				
				
				int dotPos = sFileSavePath.LastIndexOf('.');
				int nameEnd = sFileSavePath.Length - 1;
				
				if (dotPos > lastDir)
				{
					nameEnd = dotPos;
				}
				
				fileName = sFileSavePath.Substring(lastDir + 1, nameEnd - lastDir - 1);
				
				// Now construct directory path
				directory = sFileSavePath.Substring(0, lastDir);
			}
			// TODO: Account for no filename specified
			
			foreach (SDCCProgram program in Programs.Values)
			{
				
				foreach (SStartupFile file in program.StartupFiles.Values)
				{
					string newFile = file.FullName;
					if (file.Copy)
					{
						if (!Directory.Exists(directory))
							Directory.CreateDirectory(directory);
						
						newFile = directory + "\\" + fileName + "." + file.Extension;
						CProcessUtils.CopyFile(file.FullName, newFile);
						ExpandMacros(newFile);
					}
					
					if (file.LaunchWithProgram)
					{
						Process proc = new Process();
						ProcessStartInfo info = new ProcessStartInfo(program.ExecutablePath);
						info.Arguments += newFile;
						proc.StartInfo = info;
						proc.Start();
						
					}
				}
				
				/*if (hasProgramInstanceOpen == false)
				{
						Process proc = new Process();
						ProcessStartInfo info = new ProcessStartInfo(program.ExecutablePath);
						proc.StartInfo = info;
						proc.Start();						
				}*/
			}

		}
		
		private void ParseExecutables(string execs)
		{
			/*int semiPos = 0;
			int lastSemi = -1;
			int subProgCount = 0;
			
			while ((semiPos = execs.IndexOf(';', lastSemi + 1)) != -1)
			{
				string sSub = execs.Substring(lastSemi + 1, semiPos - lastSemi - 1);
				
				SDCCProgram sub = new SDCCProgram();
				
				sub.ExecutablePath = sSub;
				
				Programs.Add(sub);
				
				++subProgCount;
				lastSemi = semiPos;
			}*/
		}
		
		private void ParseStartupFiles(string startups)
		{
			/*
			int semiPos = 0;
			int lastSemi = -1;
			int startupFileCount = 0;
			
			while ((semiPos = startups.IndexOf(';', lastSemi + 1)) != -1)
			{
				string sSub = startups.Substring(lastSemi + 1, semiPos - lastSemi - 1);
				
				if (startupFileCount < Programs.Count)
				{
					Programs[startupFileCount].StartupFiles.Add(new SStartupFile(sSub));
				}
				else
				{
					Programs[Programs.Count - 1].StartupFiles.Add(new SStartupFile(sSub));
				}
				
				++startupFileCount;
				lastSemi = semiPos;
			}*/
		}
		
		private void ExpandMacros(string filePath)
		{
			ExpandMacrosByte(filePath);
		}
		
		private void ExpandMacrosByte(string filePath)
		{
			FileInfo info = new FileInfo(filePath);
		
			string fileContent = File.ReadAllText(filePath);
		
			if (fileContent.Contains(m_sRootPathMacro) ||
			    fileContent.Contains(m_sGameFolderMacro) ||
			    fileContent.Contains(m_sFileNameMacro) ||
			    fileContent.Contains(m_sFileMacro) ||
			    fileContent.Contains(m_sFolderMacro))
			{
				string ceRoot = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();			
				string ceGameFolder = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
				
				fileContent = fileContent.Replace(m_sRootPathMacro, ceRoot);
				fileContent = fileContent.Replace(m_sGameFolderMacro, ceGameFolder);	
				fileContent = fileContent.Replace(m_sFileNameMacro, CPathUtils.GetFilename(info.Name));
				fileContent = fileContent.Replace(m_sFileMacro, info.FullName);
				fileContent = fileContent.Replace(m_sFolderMacro, info.Directory.FullName);
				
				File.WriteAllText(filePath, fileContent);
			}
		}		
	};
}
