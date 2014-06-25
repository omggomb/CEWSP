/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 12.06.2014
 * Time: 15:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using CEWSP.ApplicationSettings;
using CEWSP.SourceFileTracking;
using CEWSP.Utils;
using ExplorerTreeView;
using OmgUtils.UserInteraction;

namespace CEWSP
{
	/// <summary>
	/// Class that sets up the explorer context menu and takes care of
	/// the resulting events
	/// </summary>
	public static class ExplorerSetup
	{
		/// <summary>
		/// The treeview that this class operates on
		/// </summary>
		static ExplorerTreeViewControl m_targetTreeView;
		
		/// <summary>
		/// Used with the NewAsset button.
		/// </summary>
		static string m_sRequestedDCCPackage;
		
		/// <summary>
		/// Adds CE specific context menu entries to the GlobalContextMenu of the ExplorerTreeViewControl
		/// </summary>
		/// <param name="target">The target ExplorerTreeViewControl instance</param>
		public static void SetupCESpecificEntries(ref ExplorerTreeViewControl target)
		{
			m_targetTreeView = target;
			
			
			var menu = target.GlobalContextMenu;
			
			Separator sep = new Separator();
			
			menu.Items.Add(sep);
			
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
			menu.Items.Add(item);
			
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
						var selectedItem = m_targetTreeView.SelectedItem as CustomTreeItem;
						if (selectedItem != null)
						{
							if (!selectedItem.IsDirectory)
							{
								Process proc = new Process();
								proc.StartInfo = new ProcessStartInfo(program.ExecutablePath);
								proc.StartInfo.Arguments = selectedItem.FullPathToReference;
								
								proc.Start();
							}
						}
						
					};
					
					runWithItem.Items.Add(programItem);
					addedPrograms.Add(program.Name);
				}
			}
			
			menu.Items.Add(runWithItem);
			//----------------------------------
			
			sep = new Separator();
			menu.Items.Add(sep);
			
			
			sep = new Separator();
			menu.Items.Add(sep);
			
			item = new MenuItem();
			item.Header = "Track file"; // LOCALIZE
			item.Click += OnContextTrackClicked;
			menu.Items.Add(item);
			
			item = new MenuItem();
			item.Header = "Stop tracking file"; // LOCALIZE
			item.Click += OnContextStopTrackClicked;
			menu.Items.Add(item);
			
		}
		
		/// <summary>
		/// Spawns a textbox for the user to enter the desired asset name and then does the usual thing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		static void OnContextNewAssetClicked(object sender, RoutedEventArgs args)
		{
			MenuItem senderItem = sender as MenuItem;
			m_sRequestedDCCPackage = senderItem.Header as string;
			ShowAdHocMessageBox(TriggerNewAssetSave);
		}
		
		/// <summary>
		/// Runs the resource compiler on the selected file if any
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		static void OnContextRunRCClicked(object sender, RoutedEventArgs args)
		{
			var selectedItem = m_targetTreeView.SelectedItem as CustomTreeItem;
			
			if (!selectedItem.IsDirectory)
			{
				FileInfo info = new FileInfo(selectedItem.FullPathToReference);
				
				
					CryEngineProcessUtils.RunRC(info);
			}
		}
		
		/// <summary>
		/// Runs the gfx exporter on the selected file, if any
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		static void OnContextRunGFXClicked(object sender, RoutedEventArgs args)
		{
			
			var selectedItem = m_targetTreeView.SelectedItem as CustomTreeItem;
			
			if (selectedItem.IsDirectory)
			{
				UserInteractionUtils.ShowErrorMessageBox("You can only run the gfxexporter on files (directory was selected)"); // LOCALIZE
				return;
			}
			else
			{
				FileInfo fileInf = new FileInfo(selectedItem.FullPathToReference);
				
				if (fileInf.Extension != ".swf")
				{
					UserInteractionUtils.ShowErrorMessageBox("You can only run the gfxexporter on .swf files"); // LOCALIZE
					return;
				}
				
				CryEngineProcessUtils.RunGFXExporter(fileInf);
			}
			
			
		}
		
		/// <summary>
		/// Adds the selected file or directory to the source tracker and marks the tree items as "Tracked"
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		static void OnContextTrackClicked(object sender, RoutedEventArgs args)
		{
			var selectedItem = m_targetTreeView.SelectedItem as CewspTreeViewItem;
			
			// TODO: If its a directory, add all files in it ( not recursive)
			if (selectedItem != null)
			{
				if (!selectedItem.IsDirectory)
				{
					if (CSourceTracker.Instance.AddFileToTracking(selectedItem.FullPathToReference, EFileRoot.eFR_CERoot, true))
					{
						RefreshParentTrackingMark(selectedItem);
					}
				}
				else
				{
					TrackDirectory(new DirectoryInfo(selectedItem.FullPathToReference));
					RefreshParentTrackingMark(selectedItem);
				}
				
				selectedItem.MarkTracked();
			}
		}
		
		/// <summary>
		/// Removes the selected file or directory from the source tracker and removes the "Tracked" mark.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		static void OnContextStopTrackClicked(object sender, RoutedEventArgs args)
		{
			var selectedItem = m_targetTreeView.SelectedItem as CewspTreeViewItem;
			
			// TODO: If its a directory, remove all files in it ( not recursive)
			if (selectedItem != null)
			{
				if (!selectedItem.IsDirectory)
				{
					CSourceTracker.Instance.RemoveFileFromTracking(selectedItem.FullPathToReference, EFileRoot.eFR_CERoot);
					
					RefreshParentTrackingMark(selectedItem);
					
				}
				else
				{
					UntrackDirectory(new DirectoryInfo(selectedItem.FullPathToReference));
					
					RefreshParentTrackingMark(selectedItem);
				}
				
				selectedItem.UnmarkTracked();
			}
		}
		
		/// <summary>
		/// Called after the user has entered a valid name for the new asset. See
		/// <see cref="ShowAdHocMessageBox"/>
		/// </summary>
		/// <param name="box">The TextBox into which the user entered the desired name</param>
		/// <returns>Return value does not inidicate anything, legacy of using Func instead of delegate</returns>
		static int TriggerNewAssetSave(TextBox box)
		{
			var selectedItem = m_targetTreeView.SelectedItem as CustomTreeItem;
			
			if (selectedItem.IsDirectory == false)
			{
				selectedItem = selectedItem.GetParentSave() as CustomTreeItem;
			}
			
			
			CDCCDefinition def = CApplicationSettings.Instance.GetDCCProgram(m_sRequestedDCCPackage);
			
			
			string savepath = selectedItem.FullPathToReference + "\\" +  box.Text;
			
			
			if (savepath.LastIndexOf('.') == -1)
			{
				// HACK: Add a dot if there is none, so def.Start won't crop the last letter
				savepath += '.';
			}
			
			
			def.Start(savepath);
			return 0;
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
		
		/// <summary>
		/// Adds the specified directory to the source tracker. Include subdirs recursively.
		/// Potentially very slow!
		/// </summary>
		/// <param name="directory"></param>
		static void TrackDirectory(DirectoryInfo directory)
		{
			foreach (var element in directory.GetDirectories())
			{
				TrackDirectory(element);
			}
			
			foreach (var element in directory.GetFiles())
			{
				CSourceTracker.Instance.AddFileToTracking(element.FullName, CSourceTracker.Instance.GetTrackingFileAffection(element.FullName), true);
			}
		}
		
		/// <summary>
		/// Removes a directory and it subcontents from the source tracker.
		/// Potentially very slow!
		/// </summary>
		/// <param name="directory"></param>
		static void UntrackDirectory(DirectoryInfo directory)
		{
			foreach (var element in directory.GetDirectories())
			{
				UntrackDirectory(element);
			}
			
			foreach (var element in directory.GetFiles())
			{
				CSourceTracker.Instance.RemoveFileFromTracking(element.FullName, CSourceTracker.Instance.GetTrackingFileAffection(element.FullName));
			}
		}
		
		/// <summary>
		/// Travels through all the parent of the specified tree item and checks whether they
		/// need to be marked as tracked or not
		/// </summary>
		/// <param name="item"></param>
		static void RefreshParentTrackingMark(CewspTreeViewItem item)
		{
			var parent = item;
			
			while ((parent = parent.Parent as CewspTreeViewItem) != null)
			{
				string sRelPath = CPathUtils.MakeRelative(parent.FullPathToReference);
				
				sRelPath += "\\";
				
				if (CSourceTracker.Instance.DoesDirectoryContainTrackedFile(sRelPath))
				{
					parent.MarkTracked(false);
				}
				else
				{
					parent.UnmarkTracked(false);
				}
			}
			
		}
		
	}
}
