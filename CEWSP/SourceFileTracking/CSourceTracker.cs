/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 03.11.2013
 * Time: 14:54
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

using CEWSP.Utils;
using CEWSP.ApplicationSettings;
using CEWSP.Logging;

using OmgUtils.Path;
using OmgUtils.ProcessUt;
using OmgUtils.UserInteraction;

namespace CEWSP.SourceFileTracking
{
	public class ETrackedDirs
	{
		public const string eTD_None = "none";
		public const string eTD_Game = "game";
		public const string eTD_Root = "root";
		public const string eTD_Both = "both";
	}
	public enum EFileRoot
	{
		/// <summary>
		/// The given path should be stored relative to CE root
		/// </summary>
		eFR_CERoot,
		/// <summary>
		/// The given path should be stored relative to the game folder
		/// </summary>
		eFR_GameFolder
	}
	
	/// <summary>
	/// Description of CSourceTracker.
	/// </summary>
	public class CSourceTracker
	{
		
		// TODO: this needs its own filewatcher...
		private List<string> m_trackedCERootFiles;
		private List<string> m_trackedGameFolderFiles;
		private List<string> m_ignoredFiles;
		private const string m_sIgnoredFilesListPath = ".\\SourceTracker\\Ignorefiles.txt";
		private string m_sCurrentTrackingList;
		private const string m_sCurrentRootTrackingListPath = ".\\SourceTracker\\currentRootTrackingList" + m_sFileExtension;
		private const string m_sCurrentGameTrackingListPath = ".\\SourceTracker\\currentGameTrackingList" + m_sFileExtension;
		private const string m_sRootTrackingListSignature = "# ROOT";
		private const string m_sGameTrackingListSignature = "# GAME";
		private const string m_sFileExtension = ".txt";
		private FileSystemWatcher m_rootFilesWatcher;
		private FileSystemWatcher m_gameFilesWatcher;
		private FileSystemWatcher m_ignoredFilesWacher;
		private Object m_lockingObject;
		private List<Regex> m_ignoredRegexList;
		private List<Regex> m_ignoredNegatedRegexList;
		
		/// <summary>
		/// Extension of tracker files includig the leading dot
		/// </summary>
		public static string FileExtension { get { return m_sFileExtension; } }
		public static string GameFilesListPath { get { return m_sCurrentGameTrackingListPath; } }
		public static string RootFilesListPath { get { return m_sCurrentRootTrackingListPath; } }
		public static string IngoredFilesPath { get { return m_sIgnoredFilesListPath; } }
		
		private static CSourceTracker _instance;
		public static CSourceTracker Instance
		{
			get
			{
				if (_instance == null)
					_instance = new CSourceTracker();
				return _instance;
			}
		}
		
		
		public CSourceTracker()
		{
		}
		
		public void Init()
		{
			if (!Directory.Exists(".\\SourceTracker"))
				Directory.CreateDirectory(".\\SourceTracker");
			
			m_trackedCERootFiles = new List<string>();
			m_trackedGameFolderFiles = new List<string>();
			m_ignoredFiles = new List<string>();
			m_ignoredFilesWacher = new FileSystemWatcher();
			m_ignoredRegexList = new List<Regex>();
			m_ignoredNegatedRegexList = new List<Regex>();
		
			m_ignoredFilesWacher.Path = PathUtils.GetFilePath(m_sIgnoredFilesListPath);
			m_ignoredFilesWacher.Filter = "Ignorefiles.txt";
			m_ignoredFilesWacher.NotifyFilter = NotifyFilters.LastWrite;
			//m_ignoredFilesWacher.Changed += delegate { LoadIgnoredFilesList(); };
			
			m_lockingObject  = new object();
			
			
		
			
			LoadIgnoredFilesList(CApplicationSettings.Instance.GetValue(ESettingsStrings.CheckIgnoredRegexSanityOnStartup).GetValueBool());
			LoadFileTrackingList(m_sCurrentRootTrackingListPath, EFileRoot.eFR_CERoot);
			LoadFileTrackingList(m_sCurrentGameTrackingListPath, EFileRoot.eFR_GameFolder);
			
		}

        public void Reset(EFileRoot root, bool bSaveCurrent, string sSaveFilePath = "")
        {
            if (bSaveCurrent)
            {
                switch (root)
                {
                    case EFileRoot.eFR_CERoot:
                        DumpFilesToDisk(sSaveFilePath, EFileRoot.eFR_CERoot);
                        break;
                    case EFileRoot.eFR_GameFolder:
                        DumpFilesToDisk(sSaveFilePath, EFileRoot.eFR_GameFolder);
                        break;
                    default:
                        break;
                }


            }

            switch (root)
            {
                case EFileRoot.eFR_CERoot:
            		if (m_trackedCERootFiles != null) 
            			m_trackedCERootFiles.Clear();
                    break;
                case EFileRoot.eFR_GameFolder:
                    if (m_trackedGameFolderFiles != null)
                    	m_trackedGameFolderFiles.Clear();
                    break;
                default:
                    break;
            }
        }
		
		
		
		public void Shutdown()
		{
			if (m_rootFilesWatcher != null)
				m_rootFilesWatcher.EnableRaisingEvents = false;
			if (m_gameFilesWatcher != null)
				m_gameFilesWatcher.EnableRaisingEvents = false;
			
			UpdateTrackedFiles();
			DumpFilesToDisk(m_sCurrentRootTrackingListPath, EFileRoot.eFR_CERoot);
			DumpFilesToDisk(m_sCurrentGameTrackingListPath, EFileRoot.eFR_GameFolder);
		}
		
		public bool AddFileToTracking(string sFilePath, EFileRoot root, bool bBypassIgnoreList = false)
		{
				bool bIsAdded = false;
				
				
			lock (m_trackedCERootFiles)
			{
				
				if (root == EFileRoot.eFR_CERoot)
				{
					// if we can extract the gamefolder from the root folder, add to game folder tracking
					// this means that the game folder is inside the root folder and doing 
					// so avoid tracking one file with both lists
					if (CPathUtils.ExtractRelativeToGameFolder(sFilePath) != sFilePath)
					{
						root = EFileRoot.eFR_GameFolder;
					}
				}
				
			
				switch (root) 
				{
					case EFileRoot.eFR_CERoot:
						{
							string relativePath = CPathUtils.ExtractRelativeToRoot(sFilePath);
							if (!m_trackedCERootFiles.Contains(relativePath) && (bBypassIgnoreList || !ShouldIgnorePath(sFilePath)))
							{	
							
								m_trackedCERootFiles.Add(relativePath);
								bIsAdded = true;
							}
						}break;
					case EFileRoot.eFR_GameFolder:
						{
							string relativePath = CPathUtils.ExtractRelativeToGameFolder(sFilePath);
							if (!m_trackedGameFolderFiles.Contains(relativePath) && (bBypassIgnoreList || !ShouldIgnorePath(sFilePath)))
							{	

								m_trackedGameFolderFiles.Add(relativePath);
								bIsAdded = true;
							}
						}break;
					default:
						throw new Exception("Invalid value for EFileRoot");
				}
			}
			
			if (bIsAdded)
			{
				switch (root)
				{
					case EFileRoot.eFR_CERoot:
						{
							DumpFilesToDisk(m_sCurrentRootTrackingListPath, EFileRoot.eFR_CERoot);
							
						}break;
					case EFileRoot.eFR_GameFolder:
						{
							DumpFilesToDisk(m_sCurrentGameTrackingListPath, EFileRoot.eFR_GameFolder);
						}break;
				}
			}
			return bIsAdded;
		}
		
		public void RemoveFileFromTracking(string sFilePath, EFileRoot root)
		{
			if (root == EFileRoot.eFR_CERoot)
			{
				// if we can extract the gamefolder from the root folder, add to game folder tracking
				// this means that the game folder is inside the root folder and doing 
				// so avoid tracking one file with both lists
				if (CPathUtils.ExtractRelativeToGameFolder(sFilePath) != sFilePath)
				{
					root = EFileRoot.eFR_GameFolder;
				}
			}
			switch (root) 
			{
				case EFileRoot.eFR_CERoot:
					{
						m_trackedCERootFiles.Remove(CPathUtils.ExtractRelativeToRoot(sFilePath));
						DumpFilesToDisk(m_sCurrentRootTrackingListPath, EFileRoot.eFR_CERoot);
					}break;
				case EFileRoot.eFR_GameFolder:
					{
						m_trackedGameFolderFiles.Remove(CPathUtils.ExtractRelativeToGameFolder(sFilePath));
						DumpFilesToDisk(m_sCurrentGameTrackingListPath, EFileRoot.eFR_GameFolder);
					}break;
				default:
					throw new Exception("Invalid value for EFileRoot");
			}
			
			
			
		}
		
		public void DumpFilesToDisk(string sSaveFilePath, EFileRoot root)
		{
			
			lock (m_lockingObject)
			{
				List<string> listToDump;
				switch (root)
				{
					case EFileRoot.eFR_CERoot:
						listToDump = m_trackedCERootFiles;
						break;
					case EFileRoot.eFR_GameFolder:
						listToDump = m_trackedGameFolderFiles;
						break;
					default:
						throw new Exception("Invalid value for EFileRoot");
				}
				
				if (!File.Exists(sSaveFilePath))
				{
					FileInfo info = new FileInfo(sSaveFilePath);
					
					try
					{
						if (!Directory.Exists(info.DirectoryName))
							Directory.CreateDirectory(info.DirectoryName);
					} 
					catch (Exception e)
					{
						UserInteractionUtils.ShowErrorMessageBox(e.Message);
						
					}
				}
				else
				{
					try
					{
						File.Delete(sSaveFilePath);
					} 
					catch (Exception e)
					{
						
						if (e is IOException)
						{
							// Rescedule... 
							System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
							timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
							timer.Tick += delegate { timer.Stop(); DumpFilesToDisk(sSaveFilePath, root); };
							timer.Start();
							return;
						}
						else
						{
							UserInteractionUtils.ShowErrorMessageBox(e.Message);
							return;
						}
					}
				}
					
				FileStream stream = new FileStream(sSaveFilePath, FileMode.Create);
				
				StreamWriter writer = new StreamWriter(stream);
				
				if (writer != null)
				{
					switch (root)
					{
						case EFileRoot.eFR_CERoot:
							writer.WriteLine(m_sRootTrackingListSignature);
							break;
						case EFileRoot.eFR_GameFolder:
							writer.WriteLine(m_sGameTrackingListSignature);
							break;
					}
					
					foreach (string entry in listToDump)
					{
						writer.WriteLine(entry);
					}
					
					writer.Close();
				}		
			}
		}
		
		public void LoadFileTrackingList(string sFilePath, EFileRoot root)
		{
			try
			{
				List<string> listToAddTo;
				
				switch (root) 
				{
					case EFileRoot.eFR_CERoot:
						listToAddTo = m_trackedCERootFiles;
						break;
					case EFileRoot.eFR_GameFolder:
						listToAddTo = m_trackedGameFolderFiles;
						break;
					default:
						throw new Exception("Invalid value for EFileRoot");
				}
				FileStream stream = File.Open(sFilePath, FileMode.Open);
				StreamReader reader = new StreamReader(stream);
				
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (!listToAddTo.Contains(line) && !String.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
						listToAddTo.Add(line);
				}
			} 
			catch (Exception)
			{
			}		
		}
		
		
		public EFileRoot GetTrackingFileAffection(string sFilePath)
		{
			StreamReader reader = null;
			try 
			{
				var stream = File.Open(sFilePath, FileMode.Open);
				reader = new StreamReader(stream);
				
				string signature = reader.ReadLine();
				
				EFileRoot root;
				signature = signature.Trim();
				if (signature == m_sRootTrackingListSignature)
					root = EFileRoot.eFR_CERoot;
				else if (signature == m_sGameTrackingListSignature)
					root = EFileRoot.eFR_GameFolder;
				else
					root = EFileRoot.eFR_CERoot;
				
				reader.Close();
				
				return root;
				
			}
			catch (Exception)
			{
				if (reader != null)
				{
					reader.Close();
				}
				return EFileRoot.eFR_CERoot;
			
			}
		}
		
		public void ImportTrackingList(string sFilePath, EFileRoot root)
		{
			Reset(root, false);
			
			LoadFileTrackingList(sFilePath, root);
			
			FileInfo trackingFileInfo = new FileInfo(sFilePath);
			
			string sTrackingDir = trackingFileInfo.DirectoryName;
			string sDestinationRoot = "";
			List<string> importList;
			
			switch (root) 
			{
				case EFileRoot.eFR_CERoot:
					{
						sDestinationRoot = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
						importList = m_trackedCERootFiles;
					}break;
				case EFileRoot.eFR_GameFolder:
					{
						sDestinationRoot = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
						importList = m_trackedGameFolderFiles;
					}break;
				default:
					throw new Exception("Invalid value for EFileRoot");
			}
			
			CSetting verboseImport = CApplicationSettings.Instance.GetValue(ESettingsStrings.VerboseImportExport);
			bool bVerboseImport = verboseImport.GetValueBool();
			
			
			foreach (string relativePath in importList)
			{
				string sDestinationPath = sDestinationRoot + relativePath;
				string sSourcePath = sTrackingDir + relativePath;
				
				FileInfo sourceFile = new FileInfo(sSourcePath);
				FileInfo targetFile = new FileInfo(sDestinationPath);
				
				
				int compareResult = sourceFile.LastWriteTime.CompareTo(targetFile.LastWriteTime);
				
				if (compareResult > 0)
				{
					try
					{
						
						
						// Using CopyFile here too in case of large files
						// DONE_TODO: see if this gets troublesome if file already exists
						// Using custom function instead
						ProcessUtils.CopyFile(sSourcePath, sDestinationPath, true);
					}
					catch (Exception)
					{
						
					}
					
				}
				else if (compareResult == 0)
				{
					string message = String.Format("Source file {1} is not different " +
												"from destination file {0}, skipping import for it", sDestinationPath,
												sSourcePath);// LOCALIZE
					if (bVerboseImport)
						UserInteractionUtils.ShowInfoMessageBox(message);	
					
					 message = String.Format("Source file {1} is not different " +
												"from destination file {0}, skipping import for it", sDestinationPath,
												sSourcePath);// DONOTLOCALIZE
					// Always log to file...
					CLogfile.Instance.LogWarning("[Source tracker]" + message);
					
					
				}
				else
				{
					string message = String.Format("Destination file {0} is more recent than " +
												"source file {1}, skipping import for it", sDestinationPath,
												sSourcePath);// LOCALIZE
					if (bVerboseImport)
						UserInteractionUtils.ShowInfoMessageBox(message);	
					message = String.Format("Destination file {0} is more recent than " +
												"source file {1}, skipping import for it", sDestinationPath,
												sSourcePath); // DONOTLOCALIZE
					// Always log to file...
					CLogfile.Instance.LogWarning("[Source tracker]" + message);
				}
			}
			
			
			string sLastImportFiles = CApplicationSettings.Instance.GetValue(ESettingsStrings.LastImportFiles).GetValueString();
			string sNewLastImportFiles = "";
			
			char[] splitter = { ';' };
			
			string[] singleFiles = sLastImportFiles.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
			
			// If there are two files then look for the obsolete one
			
				foreach (string lastSave in singleFiles)
				{
					if (lastSave != m_sCurrentTrackingList)
						sNewLastImportFiles += lastSave + ";";
				}
			
			
			m_sCurrentTrackingList = sFilePath;
			sNewLastImportFiles += sFilePath;
			
			CApplicationSettings.Instance.SetValue(new CSetting(ESettingsStrings.LastImportFiles,
			                                                    sNewLastImportFiles));
		}
			
		public void ExportTrackingFile(string sSaveFilePath, EFileRoot root)
		{
			FileInfo dirinfo = new FileInfo(sSaveFilePath);
			
			/*if (Directory.Exists(dirinfo.DirectoryName))
			{
				Directory.Delete(dirinfo.DirectoryName, true);
				Directory.CreateDirectory(dirinfo.DirectoryName);
			}*/
			
			DumpFilesToDisk(sSaveFilePath, root);
			FileInfo trackingFile = new FileInfo(sSaveFilePath);
			
			
			string sDestinationDir = trackingFile.DirectoryName;
			string sSourceDir = "";
			List<string> exportList;
			
			switch (root) 
			{
				case EFileRoot.eFR_CERoot:
					{
						sSourceDir = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
						exportList = m_trackedCERootFiles;
					}
					break;
				case EFileRoot.eFR_GameFolder:
					{
						sSourceDir = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
						exportList = m_trackedGameFolderFiles;
					}
					break;
				default:
					throw new Exception("Invalid value for EFileRoot");
			}
			
			bool bVerboseExport = CApplicationSettings.Instance.GetValue(ESettingsStrings.VerboseImportExport).GetValueBool();
			
			foreach (string relativePath in exportList)
			{
				string sTargetFilePath = sDestinationDir + relativePath;
				string sSourceFilePath = sSourceDir + relativePath;
				if (File.Exists(sTargetFilePath))
				{
					FileInfo destinationInfo = new FileInfo(sTargetFilePath);
					FileInfo sourceInfo = new FileInfo(sSourceFilePath);
					
					int compareResult = destinationInfo.LastWriteTime.CompareTo(sourceInfo.LastWriteTime);
					
					if (compareResult > 0)
					{
						string message = String.Format("Destination file {0} is more recent than " +
											"source file {1}, skipping export for it", sTargetFilePath,
											sSourceFilePath);	// LOCALIZE
						if (bVerboseExport)
							UserInteractionUtils.ShowInfoMessageBox(message);
						
						message = String.Format("Destination file {0} is more recent than " +
											"source file {1}, skipping export for it", sTargetFilePath,
											sSourceFilePath); // DONOTLOCALIZE
						
						CLogfile.Instance.LogWarning("[Source tracker]" + message);
						continue;
						
					}
					else if (compareResult == 0)
					{
						string message = String.Format("Source file {1} is not different " +
												"from destination file {0}, skipping export for it", sTargetFilePath,
												sSourceFilePath);// LOCALIZE
					if (bVerboseExport)
						UserInteractionUtils.ShowInfoMessageBox(message);	
					
					message = String.Format("Source file {1} is not different " +
												"from destination file {0}, skipping export for it", sTargetFilePath,
												sSourceFilePath);// DONOTLOCALIZE
					// Always log to file...
					CLogfile.Instance.LogWarning("[Source tracker]" + message);
						continue;
					}
				}
				
				try
				{
					FileInfo info = new FileInfo(sTargetFilePath);
					
					if (!Directory.Exists(info.DirectoryName))
						Directory.CreateDirectory(info.DirectoryName);
					ProcessUtils.CopyFile(sSourceFilePath, sTargetFilePath);
				} 
				catch (Exception e)
				{
					string message = "Failed to export file! Error: " + e.Message; // LOCALIZE
					
					UserInteractionUtils.ShowErrorMessageBox(message);
					
					message = "Failed to export file! Error: " + e.Message; // DONOTLOCALIZE
					CLogfile.Instance.LogError(message);
					return;
				}
			}
			
			string sLastExportFiles = CApplicationSettings.Instance.GetValue(ESettingsStrings.LastExportFiles).GetValueString();
			string sNewLastExportFiles = "";
			
			char[] splitter = { ';' };
			
			string[] singleFiles = sLastExportFiles.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
			
			// If there are two files then look for the obsolete one
			
				foreach (string lastSave in singleFiles)
				{
					if (sSaveFilePath != lastSave)
						sNewLastExportFiles += lastSave + ";";
				}
			
			
			
			sNewLastExportFiles += sSaveFilePath + ";";
			
			CApplicationSettings.Instance.SetValue(new CSetting(ESettingsStrings.LastExportFiles,
			                                                    sNewLastExportFiles));
		}
		
		public void CleanProject()
		{
			
		}
		
		public void MoveTrackedFiles(string sDestinationPath, EFileRoot root)
		{
			
		}
		
		/// <summary>
		/// Starts (or stops) watching dirs
		/// </summary>
		/// <param name="bWatchCERoot"></param>
		/// <param name="bWatchGameFolder"></param>
		public void StartWatching(bool bWatchCERoot, bool bWatchGameFolder)
		{
			//BeginWatching("");
			
			if (m_gameFilesWatcher == null)
				m_gameFilesWatcher = new FileSystemWatcher();
			if (m_rootFilesWatcher == null)
				m_rootFilesWatcher = new FileSystemWatcher();
			
			m_rootFilesWatcher.Path = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			m_rootFilesWatcher.Filter = "*.*";
			m_rootFilesWatcher.IncludeSubdirectories = true;
			m_rootFilesWatcher.NotifyFilter = NotifyFilters.FileName;
			m_rootFilesWatcher.Created += OnRootFileChanged;
			m_rootFilesWatcher.Renamed += OnRootFileChanged;
			m_rootFilesWatcher.Deleted += OnRootFileChanged;
			m_rootFilesWatcher.EnableRaisingEvents = bWatchCERoot;
			
			CLogfile.Instance.LogInfo(String.Format("[Source tracker] Watching root directory: {0}", bWatchCERoot.ToString()));
			
			
			m_gameFilesWatcher.Path = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			m_gameFilesWatcher.Filter = "*.*";
			m_gameFilesWatcher.IncludeSubdirectories = true;
			m_gameFilesWatcher.NotifyFilter = NotifyFilters.FileName;
			m_gameFilesWatcher.Created += OnGameFileChanged;
			m_gameFilesWatcher.Renamed += OnGameFileChanged;
			m_gameFilesWatcher.Deleted += OnGameFileChanged;
			m_gameFilesWatcher.EnableRaisingEvents = bWatchGameFolder;
			
			CLogfile.Instance.LogInfo(String.Format("[Source tracker] Watching game directory: {0}", bWatchGameFolder.ToString()));
			
		}
		
		
		public bool IsFileTracked(string sFilePath, out EFileRoot root)
		{
			if (CPathUtils.ExtractRelativeToGameFolder(sFilePath) != sFilePath)
			{
				root = EFileRoot.eFR_GameFolder;
				return m_trackedGameFolderFiles.Contains(CPathUtils.ExtractRelativeToGameFolder(sFilePath));
				
			}
			else
			{
				root = EFileRoot.eFR_CERoot;
				return m_trackedCERootFiles.Contains(CPathUtils.ExtractRelativeToRoot(sFilePath));
				
			}
		}
		
		public bool IsFileTracked(string sFilePath)
		{
			EFileRoot root;
			return IsFileTracked(sFilePath, out root);
		}
		
		public void ImportLastExported()
		{
			CLogfile.Instance.LogInfo("[Source tracker import] Importing last exported file(s)...");
			string sLastExport = CApplicationSettings.Instance.GetValue(ESettingsStrings.LastExportFiles).GetValueString();
			
			char[] splitter = { ';' };
			string[] dirs = sLastExport.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
			
			if (dirs.Length == 0)
			{
				CLogfile.Instance.LogWarning("[Source tracker import] Can't import last export, because there was none");
			}
			
			foreach (string file in dirs)
			{
				if (!File.Exists(file))
				{
					CLogfile.Instance.LogWarning(String.Format("[Source tracker import] File: {0} doesn't exist anymore! Skipping...", file));
					continue;
				}
				EFileRoot root = GetTrackingFileAffection(file);
				ImportTrackingList(file, root);
			}
			CLogfile.Instance.LogInfo("[Source tracker import] Importing last exported file(s) done!");
		}
		
		public void ExportCurrentToLastExported()
		{
			CLogfile.Instance.LogInfo("[Source tracker export] Exporting current to last exported file(s)...");
			string sLastExport = CApplicationSettings.Instance.GetValue(ESettingsStrings.LastExportFiles).GetValueString();
			
			char[] splitter = { ';' };
			string[] dirs = sLastExport.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
			
			if (dirs.Length == 0)
			{
				CLogfile.Instance.LogWarning("[Source tracker export] Can't export to last export, because there was none");
			}
			
			foreach (string file in dirs)
			{
				EFileRoot root = GetTrackingFileAffection(file);
				ExportTrackingFile(file, root);
			}
			
			CLogfile.Instance.LogInfo("[Source tracker export] Exporting current to last exported file(s) done!");
		}
		
		public void ClearCurrentTrackingList(EFileRoot root)
		{
			switch (root) 
			{
				case EFileRoot.eFR_CERoot:
					{
						if (File.Exists(m_sCurrentRootTrackingListPath))
							File.Delete(m_sCurrentRootTrackingListPath);
					}break;
				case EFileRoot.eFR_GameFolder:
					{
						if (File.Exists(m_sCurrentGameTrackingListPath))
							File.Delete(m_sCurrentGameTrackingListPath);
					}break;
			}
		}
		
		public void LoadIgnoredFilesList(bool bCheckForSanity = true)
		{
			m_ignoredFilesWacher.EnableRaisingEvents = false;
			try
			{
				m_ignoredFiles.Clear();
				m_ignoredNegatedRegexList.Clear();
				m_ignoredRegexList.Clear();
				FileStream stream = File.Open(m_sIgnoredFilesListPath, FileMode.Open);
				StreamReader reader = new StreamReader(stream);
				
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					line = line.Trim();
					
					if (line.StartsWith("#") || String.IsNullOrWhiteSpace(line))
						continue;
					
					if (line.IndexOf('#') != -1)
					{
						line = line.Substring(0, line.IndexOf('#'));
						line = line.Trim();
					}
					
					m_ignoredFiles.Add(line);
				}
				
				reader.Close();
			} 
			catch (FileNotFoundException )
			{
				File.Create(m_sIgnoredFilesListPath);
				
				try
				{
					LoadIgnoredFilesList();
					
				}
				catch(Exception)
				{
				}
			}

            m_ignoredFiles.Add(Window1.m_sGameTempDirName);
            
            foreach (string ignoredPath in m_ignoredFiles)
            {
            	try
            	{
            		if (!ignoredPath.StartsWith("!"))
	            	{
	            		m_ignoredRegexList.Add(new Regex(ignoredPath));
	            	}
	            	else
	            	{
	            		m_ignoredNegatedRegexList.Add(new Regex(ignoredPath.TrimStart('!')));
	            	}
            	} 
            	catch (ArgumentException e)
            	{
            		UserInteractionUtils.ShowErrorMessageBox(Properties.Resources.IgnoredRegexWrong + '\n'
            		                                          + e.Message);
            		continue;
            		
            	}
            }
            
          
            if (bCheckForSanity)
            {
            	if (!CheckIgnoredFilesSanity())
	            {
	            	UserInteractionUtils.ShowInfoMessageBox(Properties.Resources.IngoredRegexDoesntExist);
	            }
            }
            else
            {
            	CLogfile.Instance.LogWarning("[Source tracker] Loading ignored files file without sanity check! Performance and logic issues inbound!");
            }
			
			m_ignoredFilesWacher.EnableRaisingEvents = true;
		}

		private bool CheckIgnoredFilesSanity()
		{
			var filesList = new List<string>();
			
			
			string sRoot = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			string sGame = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			
			GetFilesInDirectory(sRoot, filesList);
			
			// If we can't extract the game folder from the root folder, it is in a different place
			// and needs to be checked, too.
			if (CPathUtils.ExtractRelativeToRoot(sGame) == sGame)
			{
				GetFilesInDirectory(sGame, filesList);
			}
			
			bool[] oIgnoredBool = new bool[m_ignoredRegexList.Count];
			bool[] oNotIgnoredBool = new bool[m_ignoredNegatedRegexList.Count];
			
			for (int i = 0; i < m_ignoredNegatedRegexList.Count; ++i)
			{
				var currentRegex = m_ignoredNegatedRegexList[i];
				
				foreach (string file in filesList)
				{
					if (currentRegex.IsMatch(file))
					{
						oNotIgnoredBool[i] = true;
						break;
					}
				}
			}
			
			for (int i = 0; i < m_ignoredRegexList.Count; ++i)
			{
				var currentRegex = m_ignoredRegexList[i];
				
				foreach (string file in filesList)
				{
					if (currentRegex.IsMatch(file))
					{
						oIgnoredBool[i] = true;
						break;
					}
				}
			}
			
			bool bIsSane = true;
			
		
			for (int i = 0; i < oIgnoredBool.Length; ++i)
			{
				var ignoredSane = oIgnoredBool[i];
				if (!ignoredSane)
				{
					LogSanityWarning(m_ignoredRegexList[i].ToString());
					bIsSane = false;
				}
			}
		

			for (int i = 0; i < oNotIgnoredBool.Length; ++i)
			{
				var notIgnoredSane = oNotIgnoredBool[i];
				
				if (!notIgnoredSane)
				{
					LogSanityWarning(m_ignoredNegatedRegexList[i].ToString());
					bIsSane = false;
				}
			}
			
			return bIsSane;
		}
		
		private void GetFilesInDirectory(string sDirPath, List<string> fillOut)
		{
			foreach (string dir in Directory.GetDirectories(sDirPath))
			{
				GetFilesInDirectory(dir, fillOut);
			}
			
			foreach (string file in Directory.GetFiles(sDirPath))
			{
				fillOut.Add(file);
			}
		}
		
		private void LogSanityWarning(string sRegex)
		{
			Logging.CLogfile.Instance.LogWarning(String.Format("[Source tracker ignore list] Specified regex pattern [{0}] has" +
			                                                   " no match! This is a performance issue and might lead to unexpected behaviour!", sRegex));
			                                                   
		}
		
		
		private bool ShouldIgnorePath(string sPathToTest)
		{
			bool bShouldIgnore = true;
			
			foreach (Regex  allButThem in m_ignoredNegatedRegexList)
			{
				if (allButThem.IsMatch(sPathToTest))
					bShouldIgnore = false;
			}
			
			foreach (Regex ignoredRegex in m_ignoredRegexList)
			{
				if (ignoredRegex.IsMatch(sPathToTest))
					return bShouldIgnore = true;		// If it's an explicitly ignored path, no need to do further checking
			}
			
			
			
			return bShouldIgnore;
		}
		
		private void UpdateTrackedFiles()
		{
			UpdateTrackedRootFiles();
			UpdateTrackedGameFolderFiles();
		}
		
		private void UpdateTrackedRootFiles()
		{
			string root = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			List<int> removeShedule = new List<int>(m_trackedCERootFiles.Count);
			
			for (int i = 0; i < m_trackedCERootFiles.Count; ++i)
			{
				if (!File.Exists(root + m_trackedCERootFiles[i]))
					removeShedule.Add(i);
			}
			
			foreach (int toRemove in removeShedule)
			{
				//m_trackedCERootFiles.RemoveAt(toRemove);
				m_trackedCERootFiles[toRemove] = null;
			}
		}
		
		private void UpdateTrackedGameFolderFiles()
		{
			string root = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			List<int> removeScedule = new List<int>(m_trackedGameFolderFiles.Count);
			
			for (int i = 0; i < m_trackedGameFolderFiles.Count; ++i)
			{
				if (!File.Exists(root + m_trackedGameFolderFiles[i]))
					removeScedule.Add(i);
			}
			
			foreach (int toRemove in removeScedule)
			{
				//m_trackedGameFolderFiles.RemoveAt(toRemove);
				m_trackedGameFolderFiles[toRemove] = null;
			}
		}
		
		
		
		private void OnRootFileChanged(object sender, FileSystemEventArgs args)
		{
			if (args.ChangeType == WatcherChangeTypes.Created)
				AddFileToTracking(args.FullPath, EFileRoot.eFR_CERoot);
			else if (args.ChangeType == WatcherChangeTypes.Deleted)
				RemoveFileFromTracking(args.FullPath, EFileRoot.eFR_CERoot);
			else if (args.ChangeType == WatcherChangeTypes.Renamed)
			{
				RenamedEventArgs rargs = args as RenamedEventArgs;
				
				RemoveFileFromTracking(rargs.OldFullPath, EFileRoot.eFR_CERoot);
				AddFileToTracking(rargs.FullPath, EFileRoot.eFR_CERoot);
			}
		}
		
		private void OnGameFileChanged(object sender, FileSystemEventArgs args)
		{
			if (args.ChangeType == WatcherChangeTypes.Created)
				AddFileToTracking(args.FullPath, EFileRoot.eFR_GameFolder);
			else if (args.ChangeType == WatcherChangeTypes.Deleted)
				RemoveFileFromTracking(args.FullPath, EFileRoot.eFR_GameFolder);
			else if (args.ChangeType == WatcherChangeTypes.Renamed)
			{
				RenamedEventArgs rargs = args as RenamedEventArgs;
				
				RemoveFileFromTracking(rargs.OldFullPath, EFileRoot.eFR_GameFolder);
				AddFileToTracking(rargs.FullPath, EFileRoot.eFR_GameFolder);
			}
		}
	}
}
