/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 20.10.2013
 * Time: 15:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 * It's messy and has a lot of duplicated code, but I wrote it in one day, so... yea
 */
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;

using Microsoft.VisualBasic.FileIO;

using CEWSP.Utils;
using CEWSP.SourceFileTracking;

using OmgUtils.Path;
using OmgUtils.ProcessUt;
using OmgUtils.UserInteraction;




using CEWSP.ApplicationSettings;
namespace CEWSP
{
	
	public class DirectoryTreeItem : TreeViewItem
	{
		public DirectoryTreeItem()
		{
			
		}
		
		public void SetName(string sName)
		{
			ItemName = sName;
			Header = sName;
		}
		/// <summary>
		/// If no parent exists, returns itself.
		/// </summary>
		/// <returns></returns>
		public TreeViewItem GetParentSave()
		{
			TreeViewItem parent = Parent as TreeViewItem;
			
			if (parent == null)
				return this as TreeViewItem;
			else
				return Parent as TreeViewItem;
		}
		
		public bool IsDirectory {get; set;}
		public string ItemName {get; private set;}
		public string FullPath {get; set;}
	}
	
	/// <summary>
	/// Can represent a file or directory.
	/// Used for threading approach.
	/// </summary>
	public class CDirectoryEntry
	{
		public string Header {get; set;}
		public bool IsDirectory {get; set;}
		public string FullPath {get; set;}
		
		public List<CDirectoryEntry> SubEntries;
		
		public CDirectoryEntry()
		{
			SubEntries = new List<CDirectoryEntry>();
		}
	}
	
	public class CFillerThreadParam
	{
		public CDirectoryEntry rootEntry;
		public Action<CDirectoryEntry> callBack;
	}
	
	/// <summary>
	/// Represents the minimal exlorer like tree view used in the main window.
	/// </summary>
	public class FileExplorer
	{
		private static TreeView m_targetTreeView;
		private static ContextMenu m_viewItemContextMenu;
		private static DirectoryTreeItem m_lastSelectedItem;
		
		/// <summary>
		/// Indicated whether to cut or copy the file when pasting.
		/// </summary>
		private static  bool m_bIsFileCut = false;
		
	
		private volatile static bool m_bFillFinished;
		
		private static DispatcherTimer m_timer;
		
		private volatile static CDirectoryEntry m_filledEntry;
		
		private static DispatcherTimer m_hackyTimer;
		
		/// <summary>
		/// Used for the new asset option.
		/// </summary>
		private static string m_sRequesteDCCPackage;
		
		private static FileSystemWatcher m_fileWatcher;
		private static string m_sDirectoryToWatch;
		
		private static List<string> m_incomingFileChanges;
		
		
		public static string CurrentDirectoryWatched
		{
			get
			{
				return m_sDirectoryToWatch;
			}
		}
		private FileExplorer() {}
		
		
		public static void Init(ref TreeView targetView)
		{
			SetupContextMenu();
			m_timer = new DispatcherTimer();
			
			m_timer.Interval = new TimeSpan(0,0,0,0,30);
			m_timer.Tick += new EventHandler(TimerTick);
			
			#region Really awfull hacking hacky timer
			// Needed due to FileSystemWatcher firing events more than once per file at times
			// When OnFile is called when the timer is running, the event will be discarded
			// by setting the period to something really small we make sure that only the doubled event is ignored
			m_hackyTimer = new DispatcherTimer();
			m_timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
			
			m_hackyTimer.Tick += delegate 
			{ 
				m_hackyTimer.Stop();
				m_targetTreeView.Dispatcher.Invoke(delegate
				                                   {
				                                   	PopulateTreeView(m_sDirectoryToWatch);
				                                   });
			};
			
			#endregion
			
		
			if (targetView == null)
			{
				throw new ArgumentNullException("targetView", "Must assign an existing view, else it won't work");
			}
			m_targetTreeView = targetView;
			
			m_targetTreeView.KeyDown += delegate(object sender, System.Windows.Input.KeyEventArgs e) 
			{
				if (e.Key == System.Windows.Input.Key.Delete)
				{
					OnContextRecycleClicked(null, null);
				}
				else if (e.Key == System.Windows.Input.Key.F2)
				{
					OnContextRenameClicked(null, null);
				}
				else if ((e.Key == System.Windows.Input.Key.C) && 
				         ((System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl)) ||
				          (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))))
				{
					OnContextCopyClicked(null, null);
				}
				else if ((e.Key == System.Windows.Input.Key.X) && 
				         ((System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl)) ||
				          (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))))
				{
					OnContextCutClicked(null, null);
				}
				else if ((e.Key == System.Windows.Input.Key.V) && 
				         ((System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl)) ||
				          (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))))
				{
					OnContextPasteClicked(null, null);
				}
			};
			
		
		}
		
		public static void BeginWatching(string dirToWatch)
		{
			//if (m_fileWatcher != null)
				//return;
			//if (m_fileWatcher.EnableRaisingEvents == true)
				//return;
				if (!Directory.Exists(dirToWatch))
				{
					if (m_fileWatcher != null)
						m_fileWatcher.EnableRaisingEvents = false;
					
					return;
				}
				
			m_sDirectoryToWatch = dirToWatch;
			
			if (m_fileWatcher == null)
				m_fileWatcher = new FileSystemWatcher();
			m_fileWatcher.Path = dirToWatch;
			m_fileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
			m_fileWatcher.Filter = "*.*";
			m_fileWatcher.IncludeSubdirectories = true;
			
			//m_fileWatcher.Changed += OnFileChanged;
			m_fileWatcher.Created += OnFileChanged;
			m_fileWatcher.Deleted += OnFileChanged;
			m_fileWatcher.Renamed += OnFileChanged;

			m_fileWatcher.EnableRaisingEvents = true;	
			m_incomingFileChanges = new List<string>();
		}
		
		public static EFileRoot GetCurrentlyDisplayedDirectoryType()
		{
			// FIXME: if dir has changed already, new dir != old dir (game != game)
			if (m_sDirectoryToWatch == CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString())
				return EFileRoot.eFR_CERoot;
			else
				return EFileRoot.eFR_GameFolder;
		}
		
		
		
		private static void OnFileChanged(object sender, FileSystemEventArgs args)
		{
			// Not needed as FileSystemWatcher doesn't fire events for the folder itself
			// Handle when renamed or deleted inside context menu's events
			/*
			var renamedArgs = args as RenamedEventArgs;
			string sAffectedPath = renamedArgs == null ? args.FullPath : renamedArgs.OldFullPath;
			
			if (args.ChangeType == WatcherChangeTypes.Deleted || renamedArgs != null)
			{
				
				if (sAffectedPath == CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString())
				{
					var mainWindow = Application.Current.MainWindow as Window1;
					
					if (mainWindow != null)
					{
						mainWindow.ValidateRootPath();
					}
				}
				else
				{
					if (sAffectedPath == CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString())
					{
						var mainWindow = Application.Current.MainWindow as Window1;
						
						if (mainWindow != null)
						{
							mainWindow.ValidateGameFolder();
						}
					}
				}
			}*/
			
			
			
			if (!m_hackyTimer.IsEnabled)
			{
				m_hackyTimer.Start();
			}
		}

		static void TimerTick(object sender, EventArgs e)
		{
			if (m_bFillFinished)
			{
				m_timer.Stop();
				m_bFillFinished = false;
			
				
				var rootItem = new DirectoryTreeItem();
				rootItem.Header = m_filledEntry.Header;
				rootItem.FullPath = m_filledEntry.FullPath;
				rootItem.IsDirectory = m_filledEntry.IsDirectory;
				
				foreach (CDirectoryEntry entry in m_filledEntry.SubEntries)
				{
					FillTreeItem(entry, ref rootItem);
				}
				
				m_targetTreeView.Items.Clear();
				m_targetTreeView.Items.Add(rootItem);
				
				// Expand the treeview to the last selected item
				if (m_lastSelectedItem != null)
				{
					List<string> selectedPath = new List<string>();
					DirectoryTreeItem parentItem = m_lastSelectedItem;
					
					// Construct a relative tree view path to the selection
					while (parentItem != null)
					{
						
						var temp = parentItem.Parent as DirectoryTreeItem;
						if (temp == null)
							break;
						
						string sheader = parentItem.Header as string;
						
						// TODO: Revisit when not using label in header for tracking indicator
						if (sheader.Contains(" [Tracked]"))
									sheader = sheader.Remove(sheader.IndexOf(" [Tracked"), sheader.Length - sheader.IndexOf(" [Tracked]"));
						
						selectedPath.Add(sheader as string);
						parentItem = temp;
					}
					
					selectedPath.Reverse();
					
					// Construct  the filesystem path to the selected item 
					string path = rootItem.FullPath;
					foreach (string dir in selectedPath)
					{
						path += "\\" + dir;
					} 
					
					// If the selection was deleted, remove it from the relative path
					if (!Directory.Exists(path) && !File.Exists(path))
						selectedPath.RemoveAt(selectedPath.Count - 1);
					
					
					// Traverse the tree view until the path has been expanded
					Queue<string> selQ = new Queue<string>(selectedPath);
					
					if (m_targetTreeView.Items.Count > 0)
					{
						
						rootItem.IsExpanded = true;
						
						while (rootItem.HasItems)
						{
							for (int i = 0; i < rootItem.Items.Count; ++i) 
							{
								var temp = rootItem.Items[i] as DirectoryTreeItem;
								string currentName = temp.Header as string;
								// TODO: Revisit when not using label in header for tracking indicator
								if (currentName.Contains(" [Tracked]"))
									currentName = currentName.Remove(currentName.IndexOf(" [Tracked"), currentName.Length - currentName.IndexOf(" [Tracked]"));
								
								if (selQ.Count == 0)
								{
									rootItem = temp;
									rootItem.IsSelected = true;
									rootItem.IsExpanded = true;
									break;
								}
									
								string queueEnd = selQ.Peek();
								if (currentName == queueEnd)
								{
									rootItem = temp;
									rootItem.IsExpanded = true;
									selQ.Dequeue();
									
								}
							}
						}
					
					}
					
				
					
				}
			}
			else
			{
				DirectoryTreeItem item = m_targetTreeView.Items[0] as DirectoryTreeItem;
				string name = item.Header as string;
				if (name.Length < 15)
				{
					name += ".";	
					item.Header = name;				
				}
				else
				{
					name = name.TrimEnd('.');
				}
			}
		}
		
		static void FillTreeItem(CDirectoryEntry entry, ref DirectoryTreeItem item)
		{
			
			var newDir = new DirectoryTreeItem();
			newDir.Header = entry.Header;
			newDir.IsDirectory = entry.IsDirectory;
			newDir.FullPath = entry.FullPath;			
			if (!newDir.IsDirectory)
			{
				if (CSourceTracker.Instance.IsFileTracked(newDir.FullPath))
				{
					// TODO: Load some fancy icon here!
					// Also see revisits
					newDir.Header += " [Tracked]";
				}
			}
				
			if (entry.IsDirectory)
			{
				
				foreach (CDirectoryEntry subEntry in entry.SubEntries)
				{
					FillTreeItem(subEntry, ref newDir);
				}
	
			}
			else
			{
				newDir.MouseDoubleClick += delegate
				{
					
					System.Diagnostics.Process proc = new System.Diagnostics.Process();
					
					System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(newDir.FullPath);
					
					proc.StartInfo = info;
					
					proc.Start();
				};
						
			}
			
			item.ToolTip = item.FullPath;
			item.Items.Add(newDir);
			item.ContextMenu = m_viewItemContextMenu;
			
			
		}
		
		public static  void PopulateTreeView(string sRootPath)
		{
			if (!Directory.Exists(sRootPath))
			{
				var empty = new DirectoryTreeItem();
				empty.Header = "Specified path doesn't exist!"; // LOCALIZE
				
				m_targetTreeView.Items.Clear();
				m_targetTreeView.Items.Add(empty);
				return;
			}
			
			DirectoryInfo dirInfo = new DirectoryInfo(sRootPath);
			DirectoryTreeItem item = new DirectoryTreeItem();
			
	
			
			/*item.SetName(dirInfo.Name);
			item.FullPath = dirInfo.FullName;
			item.IsDirectory = true;
			
			item.ContextMenu = m_viewItemContextMenu;
			
			//item.MouseRightButtonDown += delegate { item.IsSelected = true; };
			
			
			TraverseDirectory(dirInfo, ref item);
			
			m_targetTreeView.Items.Add(item);*/
			
			if (sRootPath == m_sDirectoryToWatch)
				m_lastSelectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			else
				m_lastSelectedItem = null;
			
			m_bFillFinished = false;
			
			CDirectoryEntry rootEntry = new CDirectoryEntry();
			
			rootEntry.Header = dirInfo.Name;
			rootEntry.FullPath = dirInfo.FullName;
			rootEntry.IsDirectory = true;
			
			CFillerThreadParam param = new CFillerThreadParam();
			
			param.rootEntry = rootEntry;
			param.callBack = FillerCallback;
			m_timer.Start();
			
			m_targetTreeView.Items.Clear();
		
			item.Header = "Loading";
			m_targetTreeView.Items.Add(item);
			
			Thread filler = new Thread(TraverseDirectory);
			
			filler.Start(param as object);
			
			
			
		}
		
		private static void TraverseDirectory(object fillOut)
		{
			lock (fillOut)
			{
				CFillerThreadParam param = fillOut as CFillerThreadParam;
				
				CDirectoryEntry fill = param.rootEntry;
				
				if (fill != null)
				{
					DirectoryInfo info = new DirectoryInfo(fill.FullPath);
					
					TraverseInternal(info, ref fill.SubEntries);
				}
				
				param.callBack(fill);
			}
		}
		
		private static void TraverseInternal(DirectoryInfo dir, ref List<CDirectoryEntry> entry)
		{
			DirectoryInfo[] subDirs = dir.GetDirectories();
			FileInfo[] subFiles = dir.GetFiles();
			
			foreach (DirectoryInfo directory in subDirs)
			{
				var item = new CDirectoryEntry();
				
				item.Header = directory.Name;
				item.FullPath = directory.FullName;
				item.IsDirectory = true;
				
				 TraverseInternal(directory, ref item.SubEntries);
				
				
				entry.Add(item);
			}
			
			foreach (FileInfo file in subFiles)
			{
				var item = new CDirectoryEntry();
			
				item.Header = file.Name;
				item.FullPath = file.FullName;
				item.IsDirectory = false;
				
				
				entry.Add(item);
			}
		}
		
		private static void FillerCallback(CDirectoryEntry rootEntry)
		{
			m_bFillFinished = true;
			m_filledEntry = rootEntry;
			//m_timer.Start();
			// This is where tree gets filled
		}
		public static void SetupContextMenu()
		{
			if (m_viewItemContextMenu == null)
				m_viewItemContextMenu = new ContextMenu();
			
			m_viewItemContextMenu.Items.Clear();
			
			Separator sep = new Separator();
			
			// Setup all the different DCCPackages
			var item = new MenuItem();
			
			item.Header = "New asset"; // LOCALIZE
			
			List<string> dccdefs = CApplicationSettings.Instance.GetAllDCCProgramNames();
		
			
			foreach (string dccdef in dccdefs)
			{
				MenuItem notherItem = new MenuItem();
				
				notherItem.Header = dccdef;
				notherItem.Click += OnContextNewAssetClicked;
				
				item.Items.Add(notherItem);
			}
			
			if (item.Items.Count == 0)
				item.IsEnabled = false;
			m_viewItemContextMenu.Items.Add(item);
			
			//---------------------------------
			MenuItem runWithItem = new MenuItem();
			runWithItem.Header = "Run with..."; // LOCALIZE
			
			item = new MenuItem();			
			item.Header = "Run with rc"; // LOCALIZE
			item.Click += OnContextRunRCClicked;
			runWithItem.Items.Add(item);
			
			item = new MenuItem();			
			item.Header = "Run with gfxExporter"; // LOCALIZE
			item.Click += OnContextRunGFXClicked;
			runWithItem.Items.Add(item);
			
			List<string> defs = CApplicationSettings.Instance.GetAllDCCProgramNames();
			
			if (defs.Count > 0)
			{
				sep = new Separator();
				runWithItem.Items.Add(sep);
			}
			List<string> addedPrograms = new List<string>();
			foreach (string defName in defs)
			{
				CDCCDefinition def = CApplicationSettings.Instance.GetDCCProgram(defName);
				
				foreach (SDCCProgram program in def.Programs.Values) 
				{
					if (addedPrograms.Contains(program.Name))
						continue;
					
					MenuItem programItem = new MenuItem();
					
					programItem.Header = program.Name;
					
					programItem.Click += delegate 
					{
						DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
						if (selectedItem != null)
						{
							if (!selectedItem.IsDirectory)
							{
								Process proc = new Process();
								proc.StartInfo = new ProcessStartInfo(program.ExecutablePath);
								proc.StartInfo.Arguments = selectedItem.FullPath;
								
								proc.Start();
							}
						}
						
					};
					
					runWithItem.Items.Add(programItem);
					addedPrograms.Add(program.Name);
				}
			}
			
			m_viewItemContextMenu.Items.Add(runWithItem);
			//----------------------------------
			
			sep = new Separator();
			m_viewItemContextMenu.Items.Add(sep);
			
			item = new MenuItem();			
			item.Header = "Copy"; // LOCALIZE
			item.Click += OnContextCopyClicked;
			m_viewItemContextMenu.Items.Add(item);
			
			item = new MenuItem();
			
			item.Header = "Cut"; // LOCALIZE
			item.Click +=  OnContextCutClicked;
			
			m_viewItemContextMenu.Items.Add(item);
			
			item = new MenuItem();
			
			item.Header = "Paste"; // LOCALIZE
			item.Click +=  OnContextPasteClicked;
			
			m_viewItemContextMenu.Items.Add(item);
			
		
			item = new MenuItem();
			
			item.Header = "Move to Recycle Bin"; // LOCALIZE
			item.Click +=  OnContextRecycleClicked;
			
			m_viewItemContextMenu.Items.Add(item);

			item = new MenuItem();
			item.Header = "Delete"; // LOCALIZE
			item.Click += OnContextDeleteClicked;
			m_viewItemContextMenu.Items.Add(item);
			
			item = new MenuItem();
			
			item.Header = "Rename"; // LOCALIZE
			item.Click +=  OnContextRenameClicked;
			
			m_viewItemContextMenu.Items.Add(item);	
			
			
			sep = new Separator();
			m_viewItemContextMenu.Items.Add(sep);
			
			item = new MenuItem();
		
			
			item.Header = "New Directory"; // LOCALIZE
			item.Click +=  OnContextNewDirectoryClicked;
			
			m_viewItemContextMenu.Items.Add(item);

			item = new MenuItem();
			item.Header = "Open in explorer"; // LOCALIZE
			item.Click += OnContextOpenInExplorerClicked;
			m_viewItemContextMenu.Items.Add(item);
			
			sep = new Separator();
			m_viewItemContextMenu.Items.Add(sep);
			
			item = new MenuItem();
			item.Header = "Track file"; // LOCALIZE
			item.Click += OnContextTrackClicked;
			m_viewItemContextMenu.Items.Add(item);
			
			item = new MenuItem();
			item.Header = "Stop tracking file"; // LOCALIZE
			item.Click += OnContextStopTrackClicked;
			m_viewItemContextMenu.Items.Add(item);
			

		}
		
		/// <summary>
		/// Traverses the specified directory recursively and adds subitem for each entry to addto.
		/// Based off the MSDN article.
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="addto"></param>
		private static  void TraverseDirectory(DirectoryInfo dir, ref DirectoryTreeItem addto)
		{
			DirectoryInfo[] subDirs = dir.GetDirectories();
			FileInfo[] subFiles = dir.GetFiles();
			
			m_lastSelectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			foreach (DirectoryInfo directory in subDirs)
			{
				DirectoryTreeItem item = new DirectoryTreeItem();
				
				item.SetName(directory.Name);
				item.FullPath = directory.FullName;
				item.IsDirectory = true;
				
				 TraverseDirectory(directory, ref item);
				
				
				addto.Items.Add(item);
			}
			
			foreach (FileInfo file in subFiles)
			{
				DirectoryTreeItem item = new DirectoryTreeItem();
			
				item.SetName(file.Name);
				item.FullPath = file.FullName;
				item.IsDirectory = false;
				item.ContextMenu = m_viewItemContextMenu;
				
					
				item.MouseDoubleClick += delegate
				{
					
					System.Diagnostics.Process proc = new System.Diagnostics.Process();
					
					System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(file.FullName);
					
					proc.StartInfo = info;
					
					proc.Start();
				};
				
				
				addto.Items.Add(item);
				
				if (m_lastSelectedItem != null)
				{
					Stack<string> selectedPath = new Stack<string>();
					DirectoryTreeItem parentItem = m_lastSelectedItem;
					
					while (parentItem != null)
					{
						
						var temp = parentItem.Parent as DirectoryTreeItem;
						if (temp == null)
							break;
						
						selectedPath.Push(parentItem.Header as string);
						parentItem = temp;
					}
					
					if (m_targetTreeView.Items.Count > 0)
					{
						var rootItem = m_targetTreeView.Items[0] as DirectoryTreeItem;
						rootItem.IsExpanded = true;
						
						while (rootItem.HasItems)
						{
							for (int i = 0; i < rootItem.Items.Count; ++i) 
							{
								var temp = rootItem.Items[i] as DirectoryTreeItem;
								string currentName = temp.Header as string;
								
								if (selectedPath.Count == 0)
								{
									rootItem = temp;
									rootItem.IsSelected = true;
									break;
								}
									
								
								if (currentName == selectedPath.Peek())
								{
									rootItem = temp;
									rootItem.IsExpanded = true;
									selectedPath.Pop();
									break;
								}
							}
						}
					}
				}
			}
		}
		
		
		static void OnContextCopyClicked(object sender, EventArgs args)
		{
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			if (selectedItem != null)
			{
				StringCollection coll = new StringCollection();
				
				coll.Add(selectedItem.FullPath);
				Clipboard.SetFileDropList(coll);
			}
		}
		
		static void OnContextPasteClicked(object sender, EventArgs args)
		{
			// DONE_TODO: Implement directory copying
			if (!Clipboard.ContainsFileDropList())
				return;
			
			DirectoryTreeItem targetItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			if (!targetItem.IsDirectory)
			{
				targetItem = targetItem.GetParentSave() as DirectoryTreeItem;
			}
			
			//string fileSource = Clipboard.GetFileDropList()[0];
			
			foreach (string fileSource in Clipboard.GetFileDropList())
			{
				FileInfo fileInfo = new FileInfo(fileSource);
				
				if (fileInfo.Exists)
				{
				
					string fileTarget = targetItem.FullPath + "\\" + fileInfo.Name;
					
					try
					{
						//File.Copy(fileSource, fileTarget, true);
						
						
						if (m_bIsFileCut)
						{
							FileSystem.MoveFile(fileSource, fileTarget, UIOption.AllDialogs, UICancelOption.DoNothing);
						}
						else
						{
							ProcessUtils.CopyFile(fileSource, fileTarget, false);
						}
					} 
					catch (Exception e)
					{
						
						UserInteractionUtils.ShowErrorMessageBox(e.Message);
						m_bIsFileCut = false;
					}
				}
				else
				{
					// It's a direcory?
					DirectoryInfo dirInf = new DirectoryInfo(fileSource);
					if (dirInf.Exists)
					{
						string targetDir = targetItem.FullPath + "\\" + dirInf.Name;
						
						if (m_bIsFileCut)
						{
							ProcessUtils.MoveDirectory(dirInf.FullName, targetDir, false);
						}
						else
						{
							ProcessUtils.CopyDirectory(dirInf.FullName, targetDir, false);
						}
					}
				}
			}
			
			TraverseDirectory(new DirectoryInfo(targetItem.FullPath), ref targetItem);
		}
		static void OnContextCutClicked(object sender, EventArgs args)
		{
			OnContextCopyClicked(null, null);
			m_bIsFileCut = true;
		}
		static void OnContextRecycleClicked(object sender, EventArgs args)
		{
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			DirectoryTreeItem parent = selectedItem.GetParentSave() as DirectoryTreeItem;
			
			try 
			{
				
				if (selectedItem.IsDirectory)
				{
							
					//Directory.Delete(selectedItem.FullPath, true);
					FileSystem.DeleteDirectory(selectedItem.FullPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
					var mainWindow = Application.Current.MainWindow as Window1;
					if (mainWindow != null)
					{
						mainWindow.ValidateRootPath();
						mainWindow.ValidateGameFolder();
					}			
				}
				else
				{					
					//File.Delete(selectedItem.FullPath);		
					FileSystem.DeleteFile(selectedItem.FullPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);					
				}
				
			}
			catch (Exception e)
			{
				if (e is ArgumentException ||
				    e is UnauthorizedAccessException ||
				    e is IOException)
				{
					UserInteractionUtils.ShowErrorMessageBox(e.Message);
				}
				else
					throw;
			}
		}
		
		static void OnContextDeleteClicked(object sender, EventArgs args)
		{
			var selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			if (selectedItem != null)
			{
				
				try
				{
					if (selectedItem.IsDirectory)
					{						
						FileSystem.DeleteDirectory(selectedItem.FullPath, UIOption.AllDialogs, RecycleOption.DeletePermanently, UICancelOption.DoNothing);
						var mainWindow = Application.Current.MainWindow as Window1;
						if (mainWindow != null)
						{
							mainWindow.ValidateRootPath();
							mainWindow.ValidateGameFolder();
						}			
					}
					else
					{
						FileSystem.DeleteFile(selectedItem.FullPath, UIOption.AllDialogs, RecycleOption.DeletePermanently, UICancelOption.DoNothing);					
					}
				} 
				catch (Exception e)
				{
					
					UserInteractionUtils.ShowErrorMessageBox(e.Message);
				}
			}
		}
		
		static void OnContextRenameClicked(object sender, EventArgs args)
		{
			ShowAdHocMessageBox(RenameTreeEntry);
		}
		
		static int RenameTreeEntry(TextBox box)
		{
			// DONE_TODO: Account for extension being typed already...
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			try
			{
				if (selectedItem.IsDirectory)
				{
					string newPath = selectedItem.FullPath.Substring(0, selectedItem.FullPath.LastIndexOf('\\')) + '\\' + box.Text;
					//Directory.Move(selectedItem.FullPath, newPath);
					FileSystem.RenameDirectory(selectedItem.FullPath, newPath);
				}
				else
				{
					var fileInfo = new FileInfo(selectedItem.FullPath);
					
					string sNewExtension = PathUtils.GetExtension(box.Text);
					
					if (sNewExtension != box.Text && sNewExtension != fileInfo.Extension.TrimStart('.'))
					{
						var res = MessageBox.Show(Properties.Resources.FEConfirmChangeExtension,
						                          Properties.Resources.CommonNotice,
						                          MessageBoxButton.YesNo,
						                          MessageBoxImage.Question);
						
						if (res != MessageBoxResult.Yes)
						{
							sNewExtension = fileInfo.Extension.TrimStart('.');
						}
						                 
					}
					else
					{
						if (sNewExtension == box.Text)
							sNewExtension = fileInfo.Extension.TrimStart('.');
					}
					string filename = PathUtils.RemoveExtension(PathUtils.GetFilename(box.Text));
					FileSystem.RenameFile(selectedItem.FullPath, filename + "." + sNewExtension);
				}
			} 
			catch (Exception e)
			{
				if (e is ArgumentException ||
				    e is UnauthorizedAccessException ||
				    e is IOException)
				{
					UserInteractionUtils.ShowErrorMessageBox(e.Message);
				}
				else
					throw;
			}
			
		
			var mainWindow = Application.Current.MainWindow as Window1;
			if (mainWindow != null)
			{
				// Not elegant but all the error checking is done in these methods
				// no sense in writing them again				
				mainWindow.ValidateRootPath();
				mainWindow.ValidateGameFolder();
			}
			
			//selectedItem = selectedItem.GetParentSave() as DirectoryTreeItem;
			//selectedItem.Items.Clear();
			//TraverseDirectory(new DirectoryInfo(selectedItem.FullPath), ref selectedItem);
			
			return 0;
		}
		
		/// <summary>
		/// Spawns a textbox for the user to enter the desired asset name and then does the usual thing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		static void OnContextNewAssetClicked(object sender, RoutedEventArgs args)
		{
			MenuItem senderItem = sender as MenuItem;
			m_sRequesteDCCPackage = senderItem.Header as string;
			ShowAdHocMessageBox(TriggerNewAssetSave);
		}
		
		static int TriggerNewAssetSave(TextBox box)
		{
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			if (selectedItem.IsDirectory == false)
			{
				selectedItem = selectedItem.GetParentSave() as DirectoryTreeItem;
			}
			
			
			CDCCDefinition def = CApplicationSettings.Instance.GetDCCProgram(m_sRequesteDCCPackage);
			
			
			string savepath = selectedItem.FullPath + "\\" +  box.Text;
			
			
			if (savepath.LastIndexOf('.') == -1)
			{
				// HACK: Add a dot if there is none, so def.Start won't crop the last letter
				savepath += '.';
			}
			
			
			def.Start(savepath);
					
			
			selectedItem.Items.Clear();
			
			DirectoryInfo info = new DirectoryInfo(selectedItem.FullPath);
			
			TraverseDirectory(info, ref selectedItem);
			
			selectedItem.IsExpanded = true;	
			return 0;
		}
		
		static void OnContextNewDirectoryClicked(object sender, RoutedEventArgs args)
		{
			ShowAdHocMessageBox(CreateNewDirectoryFromTextEntry);
		}
		
		static int CreateNewDirectoryFromTextEntry(TextBox box)
		{
			
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			if (!selectedItem.IsDirectory)
				selectedItem = selectedItem.GetParentSave() as DirectoryTreeItem;
		
			
			string newdir = selectedItem.FullPath + "\\" + box.Text;
		
			Directory.CreateDirectory(newdir);
			
			//selectedItem.Items.Clear();
			//TraverseDirectory(new DirectoryInfo(selectedItem.FullPath), ref selectedItem);
			
			//selectedItem.IsExpanded = true;
			
			m_targetTreeView.Focus();
			
		
			
			return 0;
		}
		
		static void OnContextRunRCClicked(object sender, RoutedEventArgs args)
		{
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			if (!selectedItem.IsDirectory)
			{
				FileInfo info = new FileInfo(selectedItem.FullPath);
				CryEngineProcessUtils.RunRC(info);
			}
		}
		
		
		
		/// <summary>
		/// Displays a text box at the currently selected tree item's position.
		/// </summary>
		/// <param name="callbackOnPressedEnter">Called when enter is pressed while the text box has focus</param>
		private static void ShowAdHocMessageBox(Func<TextBox, int> callbackOnPressedEnter)
		{			
			TextBox box = new TextBox();
			
			
			
			box.SetValue(Grid.ColumnProperty, 1);
			box.Height = 20;
			
			
			TreeViewItem selected = m_targetTreeView.SelectedItem as TreeViewItem;;
			
			Point translation = selected.TranslatePoint(new Point(0,0), m_targetTreeView);
			Point extend = selected.TranslatePoint(new Point(selected.Width, selected.Height), m_targetTreeView);
			
			box.RenderTransform = new System.Windows.Media.TranslateTransform(translation.X, translation.Y);
			box.Width = selected.Width;
			
			
			
			
			
			Grid parentGrid = m_targetTreeView.Parent as Grid;
		
				
			if (parentGrid != null)
			{
			
				box.KeyDown += delegate(object sender, System.Windows.Input.KeyEventArgs e) 
				{ 
					if (e.Key == System.Windows.Input.Key.Enter)
					{
	
						if (CPathUtils.IsStringCEConform(box.Text))
						{
							parentGrid.Children.Remove(box);
							callbackOnPressedEnter(box);
						}	
						else
							box.Text = "";
					}
					else if (e.Key == System.Windows.Input.Key.Escape)
					{
						parentGrid.Children.Remove(box);
					}
				};
				
				box.LostFocus += delegate
				{
					parentGrid.Children.Remove(box);
				};
				
				parentGrid.Children.Add(box);
				
				box.Focus();
			}
		}
		
		static void OnContextRunGFXClicked(object sender, RoutedEventArgs args)
		{
			
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			if (selectedItem.IsDirectory)
			{
				UserInteractionUtils.ShowErrorMessageBox("You can only run the gfxexporter on files (directory was selected)"); // LOCALIZE
				return;
			}
			else
			{
				FileInfo fileInf = new FileInfo(selectedItem.FullPath);
				
				if (fileInf.Extension != ".swf")
				{
					UserInteractionUtils.ShowErrorMessageBox("You can only run the gfxexporter on .swf files"); // LOCALIZE
					return;
				}
				
				CryEngineProcessUtils.RunGFXExporter(fileInf);
			}
			
	
		}
		
		static void OnContextOpenInExplorerClicked(object sender, RoutedEventArgs args)
		{
			if (m_targetTreeView.SelectedItem != null)
			{
				DirectoryTreeItem item = m_targetTreeView.SelectedItem as DirectoryTreeItem;
				string dirPath = "";
				
				if (item.IsDirectory)
					dirPath = item.FullPath;
				else
				{
					dirPath = PathUtils.GetFilePath(item.FullPath);
				}
				
				Process proc = new Process();
				proc.StartInfo = new ProcessStartInfo("explorer", dirPath);
				proc.Start();
			}
		}
		
		static void OnContextTrackClicked(object sender, RoutedEventArgs args)
		{
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			// TODO: If its a directory, add all files in it ( not recursive)
			if (selectedItem != null)
			{
				if (!selectedItem.IsDirectory)
				{
					if (CSourceTracker.Instance.AddFileToTracking(selectedItem.FullPath, EFileRoot.eFR_CERoot, true))
					{
						MarkAsTracked(selectedItem);
					}
				}
				else
				{
					TrackDirectory(selectedItem);
				}
			}
		}
		
		static void OnContextStopTrackClicked(object sender, RoutedEventArgs args)
		{
			DirectoryTreeItem selectedItem = m_targetTreeView.SelectedItem as DirectoryTreeItem;
			
			// TODO: If its a directory, remove all files in it ( not recursive)
			if (selectedItem != null)
			{
				if (!selectedItem.IsDirectory)
				{
					CSourceTracker.Instance.RemoveFileFromTracking(selectedItem.FullPath, EFileRoot.eFR_CERoot);
					UnmarkTracked(selectedItem);
				}
				else
				{				
					UntrackDirectory(selectedItem);
				}
			}
		}
		
		private static void MarkAsTracked( DirectoryTreeItem item)
		{
			string sItemHeader = item.Header as string;
			
			if (!sItemHeader.Contains("[Tracked]"))
			{
				sItemHeader = sItemHeader.Trim();
				
				sItemHeader += " [Tracked]";
				
				item.Header = sItemHeader;
			}
		}
		
		private static void UnmarkTracked( DirectoryTreeItem item)
		{
			string sItemHeader = item.Header as string;
			
			if (sItemHeader.Contains("[Tracked]"))
			{
				int pos = sItemHeader.IndexOf("[Tracked]");
				
				sItemHeader = sItemHeader.Substring(0, pos);
				sItemHeader = sItemHeader.Trim();
				item.Header = sItemHeader;
			}
		}
		
		private static void TrackDirectory(DirectoryTreeItem directory)
		{
			foreach (DirectoryTreeItem item in directory.Items)
			{
				if (item.IsDirectory)
				{
					TrackDirectory(item);
				}
				else
				{
					CSourceTracker.Instance.AddFileToTracking(item.FullPath, EFileRoot.eFR_CERoot);
					
					MarkAsTracked(item);
				}
			}
		}
		
		private static void UntrackDirectory(DirectoryTreeItem directory)
		{
			foreach (DirectoryTreeItem item in directory.Items)
			{
				if (item.IsDirectory)
				{
					UntrackDirectory(item);
				}
				else
				{
					CSourceTracker.Instance.RemoveFileFromTracking(item.FullPath, EFileRoot.eFR_CERoot);
					UnmarkTracked(item);
				}
			}
		}
		
	}
}
