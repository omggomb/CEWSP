﻿/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 23.09.2013
 * Time: 20:06
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using System.Drawing.Imaging;

using CEWSP.Utils;
using CEWSP.ApplicationSettings;
using CEWSP.ApplicationManagement;
using CEWSP.SourceFileTracking;
using CEWSP.Logging;
using CEWSP.Shortcuts;

using OmgUtils.ProcessUt;
using OmgUtils.UserInteraction;



namespace CEWSP
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		#region Attributes
		// Attributes
		
		private ContextMenu m_ToolsContextMenu;
		MenuItem m_profileHistoryContextMenu;
		public const string m_sGameTempDirName = "TempGame";
		
		CewspTreeItemFactory m_treeItemFactory;
		#endregion
		
		
		#region Methods
		// Methods
		public Window1()
		{
			InitializeComponent();
			Application.Current.Exit += new ExitEventHandler(OnAppClosing);
			
			CLogfile.Instance.Initialise("CEWSP_log.txt");
			
			if (!CApplicationSettings.Instance.Init())
			{
				CApplicationSettings.Instance.Reset(false);
			}
			
			
			
			setRootDirButton.ToolTip = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			setGamefolderButton.ToolTip = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			
			
			
			
			ValidateRootPath();
			ValidateGameFolder();
			
			SetUpToolsContextMenu();
			ValidateQuickAccessButtons();
			
			//OnShowRootFolderClicked(null, null);
			
			CSourceTracker.Instance.Init();
			//string root = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).ToString();
			//string rc = root + CApplicationSettings.Instance.GetValue(ESettingsStrings.RCRelativePath).ToString();
			
			
			CSourceTracker.Instance.StartWatching(CApplicationSettings.Instance.SourceTrackerShouldWatchRoot(),
			                                      CApplicationSettings.Instance.SourceTrackerShouldWatchGame());
			//CSourceTracker.Instance.ExportTrackingFile("G:\\Dropbox\\CEWorkspace\\CEWSP\\CEWSP\\bin\\Debug\\SourceTrackerTest\\tracker.txt");
			
			//CSourceTracker.Instance.ImportTrackingList("G:\\Dropbox\\CEWorkspace\\CEWSP\\CEWSP\\bin\\Debug\\SourceTrackerTest\\tracker.txt");
			
			if (CApplicationSettings.Instance.GetValue(ESettingsStrings.ImportOnStartup).GetValueBool())
			{
				CSourceTracker.Instance.ImportLastExported();
			}
			else
			{
				if (CApplicationSettings.Instance.GetValue(ESettingsStrings.AskImportOnStartup).GetValueBool())
				{
					MessageBoxResult res = MessageBox.Show(Properties.Resources.ImportOnStartup,
					                                       Properties.Resources.CommonNotice,
					                                       MessageBoxButton.YesNo,
					                                       MessageBoxImage.Question);
					
					if (res == MessageBoxResult.Yes)
					{
						CSourceTracker.Instance.ImportLastExported();
					}
				}
			}
			
			RefreshShortcuts();
			
			m_treeItemFactory = new CewspTreeItemFactory();
			
			explorerTreeView.WatchDir = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			explorerTreeView.IsWatching = true;
			
			explorerTreeView.InitializeTree(m_treeItemFactory);
			
			ExplorerSetup.SetupCESpecificEntries(ref explorerTreeView);
			
		}
		
		
		
		
		private void SetUpToolsContextMenu()
		{
			m_ToolsContextMenu = new ContextMenu();
			m_profileHistoryContextMenu = new MenuItem();
			m_profileHistoryContextMenu.Header = Properties.Resources.ToolsProfileHistory;
			
			MenuItem item = new MenuItem();
			item.Header = "Settings"; // LOCALIZE
			item.Click += ToolContextSettingsClicked;
			m_ToolsContextMenu.Items.Add(item);
			
			item = new MenuItem();
			item.Header = "Restore Sandbox Layout"; // LOCALIZE
			item.Click += ToolContextRestoreSBLayoutClicked;
			m_ToolsContextMenu.Items.Add(item);
			
			item = new MenuItem();
			item.Header = "Run..."; // LOCALIZE
			List<string> defNames = CApplicationSettings.Instance.GetAllDCCProgramNames();
			MenuItem progItem;
			
			List<string> addedPrograms = new List<string>();
			
			foreach (string defName in defNames)
			{
				CDCCDefinition def = CApplicationSettings.Instance.GetDCCProgram(defName);
				
				foreach (SDCCProgram program in def.Programs.Values)
				{
					if (addedPrograms.Contains(program.Name))
						continue;
					
					progItem = new MenuItem();
					progItem.Header = program.Name;
					
					progItem.Click += delegate(object sender, RoutedEventArgs e)
					{
						Process proc = new Process();
						ProcessStartInfo info = new ProcessStartInfo(program.ExecutablePath);
						proc.StartInfo = info;
						proc.Start();
					};
					
					item.Items.Add(progItem);
					addedPrograms.Add(program.Name);
				}
			}
			
			if (item.Items.Count == 0)
				item.IsEnabled = false;
			m_ToolsContextMenu.Items.Add(item);
			
			item = new MenuItem();
			item.Header = Properties.Resources.ToolsSourceTrackingSubmenu;
			SetupSourceFileContextMenu(ref item);
			m_ToolsContextMenu.Items.Add(item);
			
			//============================================================================
			
			var sep = new Separator();
			m_ToolsContextMenu.Items.Add(sep);
			
			item = new MenuItem();
			item.Header = Properties.Resources.ToolsLoadProfile;
			item.Click += OnLoadProfileClicked;
			m_ToolsContextMenu.Items.Add(item);
			
			item = new MenuItem();
			item.Header = Properties.Resources.ToolsSaveProfile;
			item.Click += OnSaveProfileClicked;
			m_ToolsContextMenu.Items.Add(item);
			
			
			
			RefreshProfileHistory();
			if (m_profileHistoryContextMenu.Items.Count <= 0)
			{
				m_profileHistoryContextMenu.IsEnabled = false;
			}
			
			m_ToolsContextMenu.Items.Add(m_profileHistoryContextMenu);
			
			
			
		}
		/// <summary>
		/// Opens up a folder browser dialog and lets the user select a CE root.
		/// </summary>
		/// <returns>True if setting succeded.</returns>
		public bool DemandUserSetRoot()
		{
			MessageBoxResult nres = MessageBox.Show("Please set the CE root path!", "Important", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation); // LOCALIZE
			
			if (nres != MessageBoxResult.Cancel)
			{
				
				System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
				fbd.Description = "Choose a valid CE root directory (contains Bin32 and others)"; // LOCALIZE
				
				System.Windows.Forms.DialogResult res = System.Windows.Forms.DialogResult.Cancel;
				
				
				string path = "";
				
				string sOldPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
				do
				{
					res = fbd.ShowDialog();
					
					path = fbd.SelectedPath;
					
					
					
				}while ( res != System.Windows.Forms.DialogResult.Cancel && !CApplicationSettings.Instance.SetRootPath(path) );
				
				if (res == System.Windows.Forms.DialogResult.Cancel)
				{
					CApplicationSettings.Instance.TryInvalidateRootPath();
					ValidateQuickAccessButtons();
					return false;
				}

				//OnSetGameFolderClicked(null, null);
				HintRootFolderValid(true);
				CApplicationSettings.Instance.SaveApplicationSettings();
				OnRootFolderChanged(fbd.SelectedPath, sOldPath);
				return true;
			}
			ValidateQuickAccessButtons();
			return false;
		}
		
		private void RefreshShortcuts()
		{
			shortButton1.Content = CApplicationSettings.Instance.Shortcuts[0].Name;
			shortButton2.Content = CApplicationSettings.Instance.Shortcuts[1].Name;
			shortButton3.Content = CApplicationSettings.Instance.Shortcuts[2].Name;
			shortButton4.Content = CApplicationSettings.Instance.Shortcuts[3].Name;
			shortButton5.Content = CApplicationSettings.Instance.Shortcuts[4].Name;
			shortButton6.Content = CApplicationSettings.Instance.Shortcuts[5].Name;
		}
		
		
		#region Event handlers
		// Event handlers
		
		void DragZoneDrop(object sender, DragEventArgs e)
		{
			string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
			
			if (filenames.Length > 0)
			{
				var dialog = new DragZoneDialog();
				dialog.ShowWindow(filenames);
			}
			
			
		}
		
		void OnSetRootDirClicked(object sender, RoutedEventArgs e)
		{
			DemandUserSetRoot();
		}
		
		void OnShortcutRightClick(object sender, RoutedEventArgs e)
		{
			if (sender == shortButton1)
				ShortcutDialog.ShowMe(0);
			else if (sender == shortButton2)
				ShortcutDialog.ShowMe(1);
			else if (sender == shortButton3)
				ShortcutDialog.ShowMe(2);
			else if (sender == shortButton4)
				ShortcutDialog.ShowMe(3);
			else if (sender == shortButton5)
				ShortcutDialog.ShowMe(4);
			else if (sender == shortButton6)
				ShortcutDialog.ShowMe(5);
			
			
			CApplicationSettings.Instance.SaveShortcutsToFile();
			RefreshShortcuts();
			
		}
		
		void OnShortCutClicked(object sender, RoutedEventArgs e)
		{
			if (sender == shortButton1)
				CApplicationSettings.Instance.Shortcuts[0].Start();
			else if (sender == shortButton2)
				CApplicationSettings.Instance.Shortcuts[1].Start();
			else if (sender == shortButton3)
				CApplicationSettings.Instance.Shortcuts[2].Start();
			else if (sender == shortButton4)
				CApplicationSettings.Instance.Shortcuts[3].Start();
			else if (sender == shortButton5)
				CApplicationSettings.Instance.Shortcuts[4].Start();
			else if (sender == shortButton6)
				CApplicationSettings.Instance.Shortcuts[5].Start();
		}
		
		void OnAppClosing(object sender, EventArgs e)
		{
			if (CApplicationSettings.Instance.GetValue(ESettingsStrings.ExportOnExit).GetValueBool())
			{
				CSourceTracker.Instance.ExportCurrentToLastExported();
			}
			else
			{
				if (CApplicationSettings.Instance.GetValue(ESettingsStrings.AskExportOnExit).GetValueBool())
				{
					MessageBoxResult res = MessageBox.Show(Properties.Resources.ExportOnClose,
					                                       Properties.Resources.CommonNotice,
					                                       MessageBoxButton.YesNo,
					                                       MessageBoxImage.Question);
					
					if (res == MessageBoxResult.Yes)
					{
						CSourceTracker.Instance.ExportCurrentToLastExported();
					}
				}
			}
			CSourceTracker.Instance.Shutdown();
			CApplicationSettings.Instance.Shutdown();
		}
		
		void OnNew3dSceneClicked(object sender, RoutedEventArgs e)
		{
			ThreeDSceneDialog.ThreeDSceneDialog dia = new CEWSP.ThreeDSceneDialog.ThreeDSceneDialog();
			
			dia.ShowDialog();
		}
		
		void ManageProgramDefinitionsClicked(object sender, RoutedEventArgs e)
		{
			ManageAppllicationsWindow window = new ManageAppllicationsWindow();
			
			window.ShowDialog();
			
			// Update the file explorer context menu
			
			SetUpToolsContextMenu();
			
			// DONE_FIXME: Do this inside the Management window and check whether the changes are to be changed
			// FIXED: Application management dialog either doesn't touch or completely
			// overrides CApplicationSettings.Instance.
			CApplicationSettings.Instance.SaveApplicationSettings();
			
		}
		
		
		void OnSetGameFolderClicked(object sender, RoutedEventArgs e)
		{
			// DONE_FIXME: Don't override when previous valid and canceled!
			//ValidateGameFolder();
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.Description = "Please specify the game folder to be used for the current CE config (can be anywhere, must be CE conform)"; // LOCALIZE
			
			if (CApplicationSettings.Instance.IsRootPathSet() == true)
			{
				dialog.SelectedPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString() + "\\Bin32";
			}
			
			System.Windows.Forms.DialogResult res;
			
			do
			{
				res = dialog.ShowDialog();
				
			}while (!CPathUtils.IsStringCEConform(dialog.SelectedPath) && res != System.Windows.Forms.DialogResult.Cancel);
			
			
			if (res != System.Windows.Forms.DialogResult.Cancel)
			{
				string sOldPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
				CApplicationSettings.Instance.SetValue(new CSetting (ESettingsStrings.GameFolderPath, dialog.SelectedPath));
				CApplicationSettings.Instance.SaveApplicationSettings();
				OnGameFolderChanged(dialog.SelectedPath, sOldPath);
				HintGameFolderValid(true);
			}
			else
			{
				// TODO: Revisit, if path doesn't exist, set to invalid
				if (CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString() != ESettingsStrings.Invalid)
					return;
				
				MessageBox.Show("Setting game folder to the default (the current root's GameSDK folder!", // LOCALIZE
				                "Aborted!", MessageBoxButton.OK, MessageBoxImage.Exclamation );
				string root = (string)CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).Value;
				
				string path = root + "\\GameSDK";
				
				HintGameFolderValid(Directory.Exists(path));
				
				CApplicationSettings.Instance.SetValue(new CSetting(ESettingsStrings.GameFolderPath, path));
			}
		}
		
		void OnShowRootFolderClicked(object sender, RoutedEventArgs e)
		{
			ValidateRootPath();
			string sRootPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			explorerTreeView.WatchDir = sRootPath;
			explorerTreeView.InitializeTree(m_treeItemFactory);
		}
		
		void OnShowGameFolderClicked(object sender, RoutedEventArgs e)
		{
			ValidateGameFolder();
			string sRootPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			//folderTreeView.Items.Clear();
			
			
			
			
			explorerTreeView.WatchDir = sRootPath;
			explorerTreeView.InitializeTree(m_treeItemFactory);
		}
		
		
		
		
		void OnLaunchSB64bitClicked(object sender, RoutedEventArgs e)
		{
			ValidateRootPath();
			string root =  CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			if (CApplicationSettings.Instance.IsRootValid(root))
			{
				string path = root + CApplicationSettings.Instance.GetValue(ESettingsStrings.SB64bitRelativePath).GetValueString();
				string args = CApplicationSettings.Instance.GetValue(ESettingsStrings.Editor64bitArguments).GetValueString();
				StartProcess(path, args, sandbox64bitbutton);
			}
		}
		
		private void StartProcess(string path, string args = "", Button startingButton = null)
		{
			
			if (!File.Exists(path))
			{
				UserInteractionUtils.ShowErrorMessageBox(Properties.Resources.QuickAccesPathNonexistent);
				return;
			}
			
			Process programProcess = new Process();
			programProcess.Exited += OnProcessTerminated;
			programProcess.ErrorDataReceived += OnProcessTerminated;
			programProcess.EnableRaisingEvents = true;
			
			ProcessStartInfo startInfo = new ProcessStartInfo(path, args);
			
			programProcess.StartInfo = startInfo;
			
			if (startingButton != null)
			{
				var menu = new ContextMenu();
				
				var entry = new MenuItem();
				
				entry.Click += delegate 
				{
					try
					{
						programProcess.Kill();
					}  
					catch (Exception e) 
					{
						UserInteractionUtils.ShowErrorMessageBox(e.Message, Properties.Resources.CommonError);
					}
				};
				entry.Header = Properties.Resources.EndTask;
				
				menu.Items.Clear();
				menu.Items.Add(entry);
				
				startingButton.ContextMenu = menu;
			}
			
			programProcess.Start();
		}
		
		private void OnProcessTerminated(object sender, EventArgs args)
		{
			var process = sender as Process;
			
			if (process != null)
			{
				string sGame64bit = CApplicationSettings.Instance.GetValue(ESettingsStrings.Game64bitRelativePath).GetValueString();
				string sGame32bit = CApplicationSettings.Instance.GetValue(ESettingsStrings.Game32bitRelativePath).GetValueString();
				
				string sProcessExePath = process.StartInfo.FileName;
				
				// if the game was launched, the game folder was copied over, so restore the previous folder in sys cfg
				// and delete the temp game folder.
				if (sProcessExePath.Contains(sGame32bit) || sProcessExePath.Contains(sGame64bit))
				{
					SetGameFolderInSysCFG(CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString());
					
					string sTempGameFolder = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString() + "\\" + m_sGameTempDirName;
					
					if (Directory.Exists(sTempGameFolder))
						Directory.Delete(sTempGameFolder, true);
				}
			}
		}
		
		
		public void ValidateRootPath()
		{
			CSetting setting = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath) as CSetting;
			
			if (!CApplicationSettings.Instance.IsRootValid((string)setting.Value))
			{
				HintRootFolderValid(DemandUserSetRoot());
			}
			else
				HintRootFolderValid(true);
			
			
			//ValidateGameFolder();
		}
		
		public void ValidateGameFolder()
		{
			string  gameFolder = (string)(CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).Value);
			
			bool ok = CPathUtils.IsStringCEConform(gameFolder);
			bool exists = Directory.Exists(gameFolder);
			
			while (CPathUtils.IsStringCEConform(gameFolder) == false || Directory.Exists(gameFolder) == false)
			{
				
				MessageBoxResult res = MessageBox.Show("Please specify a gamefolder!", "Important", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation); // LOCALIZE
				if (res != MessageBoxResult.Cancel)
				{
					OnSetGameFolderClicked(null, null);
					gameFolder = (string)(CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).Value);
					
					if (gameFolder.Contains(ESettingsStrings.Invalid))
					{
						HintGameFolderValid(true);
						return;
					}
					
					
				}
				else
				{
					MessageBox.Show("Setting game folder to the default (the current root's GameSDK) folder!", // LOCALIZE
					                "Aborted!", MessageBoxButton.OK, MessageBoxImage.Exclamation );
					string root = (string)CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).Value;
					
					string path = root + "\\GameSDK";
					
					HintGameFolderValid(Directory.Exists(path));
					CApplicationSettings.Instance.SetValue(new CSetting(ESettingsStrings.GameFolderPath, path));
					return;
				}
				
			}
			
			HintGameFolderValid(true);
		}
		
		private void HintGameFolderValid(bool bIsValid)
		{
			if (bIsValid)
			{
				setGamefolderButton.Background = new SolidColorBrush(Color.FromRgb(38, 127,0));
				setGamefolderButton.ToolTip = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			}
			else
			{
				setGamefolderButton.Background = new SolidColorBrush(Color.FromRgb(127, 0, 0));
			}
		}
		
		private void HintRootFolderValid(bool bIsValid)
		{
			if (bIsValid)
			{
				setRootDirButton.Background = new SolidColorBrush(Color.FromRgb(38, 127,0));
			}
			else
			{
				setRootDirButton.Background = new SolidColorBrush(Color.FromRgb(127, 0, 0));
			}
		}
		
		/// <summary>
		/// Checks whether the paths set for the quick acces buttons actually exist
		/// </summary>
		private void ValidateQuickAccessButtons()
		{
			string root = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			
			sandbox64bitbutton.IsEnabled = File.Exists(root + CApplicationSettings.Instance.GetValue(ESettingsStrings.SB64bitRelativePath).GetValueString());
			sandbox32bitbutton.IsEnabled = File.Exists(root + CApplicationSettings.Instance.GetValue(ESettingsStrings.SB32bitRelativePath).GetValueString());
			game64bitbutton.IsEnabled = File.Exists(root + CApplicationSettings.Instance.GetValue(ESettingsStrings.Game64bitRelativePath).GetValueString());
			game32bitbutton.IsEnabled = File.Exists(root + CApplicationSettings.Instance.GetValue(ESettingsStrings.Game32bitRelativePath).GetValueString());
			codeSLNButton.IsEnabled = File.Exists(root + CApplicationSettings.Instance.GetValue(ESettingsStrings.CodeSlnFileRelativePath).GetValueString());
			openScriptButton.IsEnabled = File.Exists(root + CApplicationSettings.Instance.GetValue(ESettingsStrings.ScriptStartupFileAbsolutePath).GetValueString());
			
			if (!System.Environment.Is64BitOperatingSystem)
			{
				sandbox64bitbutton.IsEnabled = false;
				game64bitbutton.IsEnabled = false;
			}
		}
		
		private void SetupSourceFileContextMenu(ref MenuItem rootItem)
		{
			if (rootItem != null)
			{
				MenuItem item = new MenuItem();
				item.Header = Properties.Resources.ToolsImportTrackingList;
				item.Click += OnImportTrackingListClicked;
				rootItem.Items.Add(item);
				
				item = new MenuItem();
				item.Header = Properties.Resources.ToolsExportTrackedFiles;
				item.Click += OnExportTrackedFilesClicked;
				rootItem.Items.Add(item);
				
				item = new MenuItem();
				item.Header = Properties.Resources.ToolsMoveSources;
				item.Click += OnMoveSourceFilesClicked;
				rootItem.Items.Add(item);
				
				item = new MenuItem();
				item.Header = Properties.Resources.ToolsClearProcedFromSources;
				item.Click += OnClearProjectFromSourcesClicked;
				rootItem.Items.Add(item);
				
				item = new MenuItem();
				item.Header = Properties.Resources.ToolsViewTrackedRoot;
				item.Click += delegate
				{
					CSourceTracker.Instance.DumpFilesToDisk(CSourceTracker.RootFilesListPath, EFileRoot.eFR_CERoot);
					Process proc = new Process();
					proc.StartInfo = new ProcessStartInfo(CSourceTracker.RootFilesListPath);
					proc.Start();
				};
				rootItem.Items.Add(item);
				
				item = new MenuItem();
				item.Header = Properties.Resources.ToolsViewTrackedGameFolder;
				item.Click += delegate
				{
					CSourceTracker.Instance.DumpFilesToDisk(CSourceTracker.GameFilesListPath, EFileRoot.eFR_GameFolder);
					Process proc = new Process();
					proc.StartInfo = new ProcessStartInfo(CSourceTracker.GameFilesListPath);
					proc.Start();
				};
				rootItem.Items.Add(item);
				
				item = new MenuItem();
				item.Header = Properties.Resources.ToolsViewTrackerIgnoreFiles;
				item.Click += delegate
				{
					Process proc = new Process();
					proc.StartInfo = new ProcessStartInfo(CSourceTracker.IngoredFilesPath);
					proc.Exited += delegate { CSourceTracker.Instance.LoadIgnoredFilesList(); };
					proc.EnableRaisingEvents = true;
					proc.Start();
				};
				rootItem.Items.Add(item);
				
			}
		}

		private bool PrepareGameLaunch()
		{
			string sRootFolder = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			string sGameFolder = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();

			// Check if game folder is not inside root folder
			if (CPathUtils.ExtractRelativeToRoot(sGameFolder) == sGameFolder)
			{
				MessageBoxResult res =  MessageBox.Show(Properties.Resources.GameLauchCopyToRoot,
				                                        Properties.Resources.CommonNotice,
				                                        MessageBoxButton.OKCancel,
				                                        MessageBoxImage.Exclamation);
				
				if (res == MessageBoxResult.OK)
				{
					string sFullTempDirPath = sRootFolder + "\\" + m_sGameTempDirName;
					if (Directory.Exists(sFullTempDirPath))
						Directory.Delete(sFullTempDirPath, true);
					
					Directory.CreateDirectory(sFullTempDirPath);
					
					ProcessUtils.CopyDirectory(sGameFolder, sFullTempDirPath);
					SetGameFolderInSysCFG(sFullTempDirPath);
					return true;
				}
				else
					return false;
				
			}
			
			return true;
		}
		
		private void SetGameFolderInSysCFG(string sGameFolderPath)
		{
			FileStream file = File.Open(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString() + "\\system.cfg", FileMode.Open);
			
			var streamReader = new StreamReader(file);
			string fileString = streamReader.ReadToEnd();
			streamReader.Close();
			
			var stringWriter = new StringWriter();
			var stringReader = new StringReader(fileString);
			
			string currentLine;
			while ((currentLine = stringReader.ReadLine()) != null)
			{
				if (currentLine.Contains("sys_game_folder = "))
					continue;
				else
					stringWriter.WriteLine(currentLine);
			}
			
			
			string gameFolderPath = "";
			
			// DONE_TODO CONFIRMED: If redirecting game folder for launcher is deprechiated, make workaround
			// by copying the game folder to the ce root and setting the copy as sys_game_folder
			if (sGameFolderPath.Contains(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString()))
			{
				DirectoryInfo info = new DirectoryInfo(sGameFolderPath);
				gameFolderPath = info.Name;
			}
			else
				gameFolderPath = sGameFolderPath;
			
			stringWriter.WriteLine("sys_game_folder = \"" + gameFolderPath + "\"");
			
			File.Delete(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString() + "\\system.cfg");
			
			string sFinalString = stringWriter.GetStringBuilder().ToString();
			
			File.WriteAllText(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString() + "\\system.cfg", sFinalString);
		}
		
		private void PostApplicationSettingsDialog()
		{
			ValidateQuickAccessButtons();
			CSourceTracker.Instance.StartWatching(CApplicationSettings.Instance.SourceTrackerShouldWatchRoot(),
			                                      CApplicationSettings.Instance.SourceTrackerShouldWatchGame());
			
		}
		
		/// <summary>
		/// Must only be triggered if the game folder path is actually valid!
		/// </summary>
		/// <param name="sNewPath"></param>
		private void OnGameFolderChanged(string sNewPath, string sOldPath)
		{
			TryAlterSystemCFG(sNewPath);
			CSourceTracker.Instance.Reset(EFileRoot.eFR_GameFolder, false);
			CSourceTracker.Instance.ClearCurrentTrackingList(EFileRoot.eFR_GameFolder);
			if (explorerTreeView.WatchDir == sOldPath)
				OnShowGameFolderClicked(null, null);
			setGamefolderButton.ToolTip = sNewPath;
		}
		
		/// <summary>
		/// Must only be triggered if the root folder path is actually valid!
		/// </summary>
		/// <param name="sNewPath"></param>
		private void OnRootFolderChanged(string sNewPath, string sOldPath)
		{
			// Since the location of root has changed, the game folder may not be inside it anymore
			TryAlterSystemCFG(CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString());
			CSourceTracker.Instance.Reset(EFileRoot.eFR_CERoot, false);
			CSourceTracker.Instance.ClearCurrentTrackingList(EFileRoot.eFR_CERoot);
			AskChangeGameFolderToCurrentRoot();
			
			if (explorerTreeView.WatchDir == sOldPath)
				OnShowRootFolderClicked(null, null);
			else
				OnShowGameFolderClicked(null, null);
			
			setRootDirButton.ToolTip = sNewPath;
			ValidateQuickAccessButtons();
			
		}
		
		/// <summary>
		/// Searches the current root folder for the first folder that contains a game.cfg file and 
		/// asks the user whether the game folder should be set to the detected one.
		/// </summary>
		void AskChangeGameFolderToCurrentRoot()
		{
			var appSettings = CApplicationSettings.Instance;
			string sCurrentRoot = appSettings.GetValue(ESettingsStrings.RootPath).GetValueString();
			
			// No valid root, no use
			if (!appSettings.IsRootValid(sCurrentRoot))
				return;
			
			
			string sCurrentGameFolder = appSettings.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			
			// Game folder is already inside root folder
			if (sCurrentRoot.Contains(sCurrentGameFolder))
				return;
			
			// Travel through root and see if there is a folder containing game.cfg
			var rootInfo = new DirectoryInfo(sCurrentRoot);
			DirectoryInfo possibleGameFolder = null;
			
			foreach (var dir in rootInfo.GetDirectories())
			{
				if (CPathUtils.CanNameBeGameFolder(dir.Name))
				{
					foreach (var file in dir.GetFiles())
					{
						if (file.Name.ToLower() == "game.cfg")
						{
							possibleGameFolder = dir;
							break;
						}
							
					}
				}
			}
			
			// If we've found one ask the user if he wants to change the game folder to the found one
			if (possibleGameFolder != null)
			{
				var res = MessageBox.Show(Properties.Resources.AskChangeGameToCurRoot, Properties.Resources.CommonNotice, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				
				// Change game folder to detected one
				if (res == MessageBoxResult.Yes)
				{
					if (CPathUtils.IsStringCEConform(possibleGameFolder.FullName))
						appSettings.SetValue(new CSetting(ESettingsStrings.GameFolderPath, possibleGameFolder.FullName, ESettingsStrings.DESC_GameFolderPath));
					
					ValidateGameFolder();
				}
				
				// Let the user select a new game folder
				if (res == MessageBoxResult.No)
				{
					OnSetGameFolderClicked(null, null);
				}
			}
			
			
			
			
		}
		
		private void TryAlterSystemCFG(string sNewGameFolderPath)
		{
			MessageBoxResult res = MessageBox.Show(Properties.Resources.AskGameFolderUpdateCFG,
			                                       Properties.Resources.CommonNotice,
			                                       MessageBoxButton.YesNo,
			                                       MessageBoxImage.Question);
			
			
			if (res == MessageBoxResult.Yes)
			{
				if (CApplicationSettings.Instance.IsRootValid(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString()))
				{
					SetGameFolderInSysCFG(CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString());
				}
				else
				{
					UserInteractionUtils.ShowErrorMessageBox(Properties.Resources.RootNotValidNoSaveChanges);
				}
			}
		}
		
		void OnLaunchSB32bitClicked(object sender, RoutedEventArgs e)
		{
			ValidateRootPath();
			string root =  CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			if (CApplicationSettings.Instance.IsRootValid(root))
			{
				string path = root + CApplicationSettings.Instance.GetValue(ESettingsStrings.SB32bitRelativePath).GetValueString();
				string args = CApplicationSettings.Instance.GetValue(ESettingsStrings.Editor32bitArguments).GetValueString();
				StartProcess(path, args, sandbox32bitbutton);
			}
		}
		
		void OnLaunchGame64bitClicked(object sender, RoutedEventArgs e)
		{
			ValidateRootPath();
			string root =  CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			if (CApplicationSettings.Instance.IsRootValid(root))
			{
				if (PrepareGameLaunch())
				{
					string path = root + CApplicationSettings.Instance.GetValue(ESettingsStrings.Game64bitRelativePath).GetValueString();
					string arguments = CApplicationSettings.Instance.GetValue(ESettingsStrings.Game64bitArguments).GetValueString();
					StartProcess(path, arguments, game64bitbutton);
				}
			}
		}
		
		void OnLaunchGame32bitClicked(object sender, RoutedEventArgs e)
		{
			ValidateRootPath();
			string root =  CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			if (CApplicationSettings.Instance.IsRootValid(root))
			{
				if (PrepareGameLaunch())
				{
					string path = root + CApplicationSettings.Instance.GetValue(ESettingsStrings.Game32bitRelativePath).GetValueString();
					string args = CApplicationSettings.Instance.GetValue(ESettingsStrings.Game32bitArguments).GetValueString();
					StartProcess(path, args, game32bitbutton);
				}
			}
		}
		
		void OnOpenCodeClicked(object sender, RoutedEventArgs e)
		{
			ValidateRootPath();
			string root =  CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			if (CApplicationSettings.Instance.IsRootValid(root))
			{
				string path = root + CApplicationSettings.Instance.GetValue(ESettingsStrings.CodeSlnFileRelativePath).GetValueString();
				StartProcess(path, CApplicationSettings.Instance.GetValue(ESettingsStrings.CodeArguments).GetValueString(), codeSLNButton);
			}
		}
		
		void OnOpenScriptsClicked(object sender, RoutedEventArgs e)
		{
			string startupFile = CApplicationSettings.Instance.GetValue(ESettingsStrings.ScriptStartupFileAbsolutePath).GetValueString();
			
			if (startupFile != ESettingsStrings.Invalid)
			{
				
				StartProcess(startupFile, CApplicationSettings.Instance.GetValue(ESettingsStrings.ScriptArguments).GetValueString(), openScriptButton);
			}
		}
		
		void OnToolsClicked(object sender, RoutedEventArgs e)
		{
			m_ToolsContextMenu.IsOpen = true;
		}
		
		void ToolContextSettingsClicked(object sender, RoutedEventArgs e)
		{
			/*Process proc = new Process();
			proc.Exited += delegate { CApplicationSettings.Instance.Init(); };
			
			ProcessStartInfo info = new ProcessStartInfo(System.Windows.Forms.Application.UserAppDataPath + "\\ApplicationSettings.xml");
			proc.StartInfo = info;
			proc.Start();*/
			
			ApplicationSettingsDialog dialog = new ApplicationSettingsDialog();
			dialog.ShowDialog();
			PostApplicationSettingsDialog();
		}
		
		
		
		void ToolContextRestoreSBLayoutClicked(object sender, RoutedEventArgs e)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
			
			key.DeleteSubKeyTree("Crytek", false);
			
			key.Close();
		}
		
		void OnExportTrackedFilesClicked(object sender, RoutedEventArgs e)
		{
			ImExFiles.ShowWindow(EMode.eMO_Export);
		}
		
		void OnImportTrackingListClicked(object sender, RoutedEventArgs e)
		{
			ImExFiles.ShowWindow(EMode.eMO_Import);
		}
		
		void OnClearProjectFromSourcesClicked(object sender, RoutedEventArgs e)
		{
			CSourceTracker.Instance.CleanProject();
		}
		
		void OnMoveSourceFilesClicked(object sender, RoutedEventArgs e)
		{
			ImExFiles.ShowWindow(EMode.eMO_Move);
		}
		
		void OnLoadProfileClicked(object sender, RoutedEventArgs e)
		{
			bool bOK = false;
			
			
			System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
			dialog.CheckFileExists = true;
			dialog.CheckPathExists = true;
			dialog.InitialDirectory = new DirectoryInfo(CApplicationSettings.Instance.SettingsFilePath).FullName;
			
			FileInfo fileInf = null;
			
			while (bOK != true)
			{
				System.Windows.Forms.DialogResult res = dialog.ShowDialog();
				
				if (res == System.Windows.Forms.DialogResult.OK)
				{
					fileInf = new FileInfo(dialog.FileName);
					
					if (fileInf.Directory.Parent.Name != "Profiles")
					{
						UserInteractionUtils.ShowErrorMessageBox("Please specify a folder inside the " +  // LOCALIZE
						                                         CApplicationSettings.Instance.SettingsFilePath +
						                                         "subdirectory of CEWSP!");
					}
					else
					{
						bOK = true;
					}
					
					
				}
				else
				{
					return;
				}
			}
			
			
			string sRelPath	= CApplicationSettings.Instance.SettingsFilePath + "\\" + fileInf.Directory.Name;
			CApplicationSettings.Instance.LoadNewProfile(sRelPath);
			PostLoadProfile();
		}
		
		void OnSaveProfileClicked(object sender, RoutedEventArgs e)
		{
			UserInteractionUtils.AskUserToEnterString(Properties.Resources.CommonUserEntry,
			                                          Properties.Resources.AskEntryProfileName,
			                                          new UserInteractionUtils.UserFinishedEnteringStringDelegate(OnFinishedEnteringProfileName),
			                                          Properties.Resources.CommonOK, Properties.Resources.CommonCancel);
		}
		
		void OnFinishedEnteringProfileName(TextBox box, MessageBoxResult res)
		{
			if (res == MessageBoxResult.Cancel)
				return;
			
			string sRelPath = CApplicationSettings.Instance.SettingsFilePath + "\\" + box.Text;
			if (!Directory.Exists(sRelPath))
				Directory.CreateDirectory(sRelPath);
			CApplicationSettings.Instance.SaveCurrentProfile(sRelPath);
			SetUpToolsContextMenu();
		}
		
		void RefreshProfileHistory()
		{
			var dict = CApplicationSettings.Instance.GetProfileHistory();
			
			m_profileHistoryContextMenu.Items.Clear();
			
			foreach (var entry in dict)
			{
				MenuItem item = new MenuItem();
				item.Header = entry.Key;
				item.Click += delegate
				{
					CApplicationSettings.Instance.LoadNewProfile(entry.Value);
					PostLoadProfile();
				};
				item.KeyDown += delegate(object sender, KeyEventArgs e)
				{
					if (e.Key == Key.Delete)
					{
						var res = System.Windows.Forms.MessageBox.Show(Properties.Resources.ToolsDeleteProfileFromHistoryOnly,
						                                               Properties.Resources.CommonNotice,
						                                               System.Windows.Forms.MessageBoxButtons.YesNoCancel,
						                                               System.Windows.Forms.MessageBoxIcon.Question);
						
						if (res == System.Windows.Forms.DialogResult.Yes)
							CApplicationSettings.Instance.DeleteProfile(entry.Key, true);
						else if (res == System.Windows.Forms.DialogResult.No)
							CApplicationSettings.Instance.DeleteProfile(entry.Key, false);
						else
						{
							
						}
						
						RefreshProfileHistory();
						
					}
				};
				m_profileHistoryContextMenu.Items.Add(item);
			}
		}
		
		void PostLoadProfile()
		{
			
			RefreshShortcuts();
			ValidateQuickAccessButtons();
			ValidateRootPath();
			ValidateGameFolder();
			SetUpToolsContextMenu();
			
			OnShowRootFolderClicked(null, null);
		}
		#endregion
		
		
		#endregion
		
		
		
		
		
		
		
		
	}
}