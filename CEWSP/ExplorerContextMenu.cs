/*
 * Created by SharpDevelop.
 * User: Ihatenames
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
	/// Description of ExplorerContextMenu.
	/// </summary>
	public class ExplorerContextMenu
	{
		static ExplorerTreeViewControl m_targetTreeView;
		
		static string m_sRequestedDCCPackage;
		
		public ExplorerContextMenu()
		{
		}
		
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
		
		static void OnContextRunRCClicked(object sender, RoutedEventArgs args)
		{
			var selectedItem = m_targetTreeView.SelectedItem as CustomTreeItem;
			
			if (!selectedItem.IsDirectory)
			{
				FileInfo info = new FileInfo(selectedItem.FullPathToReference);
				CryEngineProcessUtils.RunRC(info);
			}
		}
		
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
		
		static void OnContextTrackClicked(object sender, RoutedEventArgs args)
		{
			var selectedItem = m_targetTreeView.SelectedItem as CustomTreeItem;
			
			// TODO: If its a directory, add all files in it ( not recursive)
			if (selectedItem != null)
			{
				if (!selectedItem.IsDirectory)
				{
					if (CSourceTracker.Instance.AddFileToTracking(selectedItem.FullPathToReference, EFileRoot.eFR_CERoot, true))
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
			var selectedItem = m_targetTreeView.SelectedItem as CustomTreeItem;
			
			// TODO: If its a directory, remove all files in it ( not recursive)
			if (selectedItem != null)
			{
				if (!selectedItem.IsDirectory)
				{
					CSourceTracker.Instance.RemoveFileFromTracking(selectedItem.FullPathToReference, EFileRoot.eFR_CERoot);
					UnmarkTracked(selectedItem);
				}
				else
				{				
					UntrackDirectory(selectedItem);
				}
			}
		}
		
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
		
		private static void MarkAsTracked( CustomTreeItem item)
		{
			var stack = item.Header as StackPanel;
			
			if (stack != null)
			{
				var label = stack.Children[1] as Label;
				
				if (label  != null)
				{
					string sOldName = label.Content as string;
					
					if (!sOldName.Contains("[Tracked]"))
					{
						sOldName += " [Tracked]";
						label.Content = sOldName;
					}
				}
			}
		}
		
		private static void UnmarkTracked( CustomTreeItem item)
		{
			var stack = item.Header as StackPanel;
			
			if (stack != null)
			{
				var label = stack.Children[1] as Label;
				
				if (label  != null)
				{
					string sOldName = label.Content as string;
					
					if (sOldName.Contains("[Tracked]"))
					{
						sOldName = item.IdentificationName;
						label.Content = sOldName;
					}
				}
			}
		}
		
		private static void TrackDirectory(CustomTreeItem directory)
		{
			foreach (CustomTreeItem item in directory.Items)
			{
				if (item.IsDirectory)
				{
					TrackDirectory(item);
				}
				else
				{
					CSourceTracker.Instance.AddFileToTracking(item.FullPathToReference, EFileRoot.eFR_CERoot);
					
					MarkAsTracked(item);
				}
			}
		}
		
		private static void UntrackDirectory(CustomTreeItem directory)
		{
			foreach (CustomTreeItem item in directory.Items)
			{
				if (item.IsDirectory)
				{
					UntrackDirectory(item);
				}
				else
				{
					CSourceTracker.Instance.RemoveFileFromTracking(item.FullPathToReference, EFileRoot.eFR_CERoot);
					UnmarkTracked(item);
				}
			}
		}
		
	}
}
