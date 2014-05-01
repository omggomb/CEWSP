/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 15.10.2013
 * Time: 15:01
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

using CEWSP.ApplicationSettings;
using CEWSP.Utils;
using CEWSP.ApplicationManagement;

namespace CEWSP.ApplicationManagement
{
	/// <summary>
	/// Interaction logic for ManageAppllicationsWindow.xaml
	/// </summary>
	public partial class ManageAppllicationsWindow : Window
	{
		//#########################################################################################
		// Attributes
		//#########################################################################################
		private Dictionary<string, CDCCDefinition> m_dccDefCopy;
		
		//#########################################################################################
		// Methods
		//#########################################################################################
		public ManageAppllicationsWindow()
		{
			MakeLocalDCCDefCopy();
			InitializeComponent();
			SetupDCCDefDropdown();
			
		
		}
		
		private void MakeLocalDCCDefCopy()
		{
			m_dccDefCopy = new Dictionary<string, CDCCDefinition>();
			
			foreach (string  name in CApplicationSettings.Instance.GetAllDCCProgramNames()) 
			{
				m_dccDefCopy.Add(name, CApplicationSettings.Instance.GetDCCProgram(name));
			}
		}
		
		private void SetupDCCDefDropdown()
		{
			dccDefDropdown.SelectionChanged += OnDCCDropdownSelectedItemChanged;
			
			RefreshDCCDefDropdown();
		}
		
		private void RefreshDCCDefDropdown()
		{
			dccDefDropdown.Items.Clear();
			foreach (string  name in m_dccDefCopy.Keys) 
			{
				var item = new ComboBoxItem();
				item.Content = name;
				dccDefDropdown.Items.Add(item);
			}
			
			if (m_dccDefCopy.Count == 0)
			{
				ToggleProgramOptions(false, true);
				ToggleFilesOptions(false, true);
			}
			else
			{
				dccDefDropdown.SelectedIndex = 0;
			}
			
			UpdateListViews();
			
		}
		
		private void ApplyAllChanges()
		{
			// The copy becomes the actual one..
			
			// clean original
			foreach (string  name in CApplicationSettings.Instance.GetAllDCCProgramNames())
			{
				CApplicationSettings.Instance.RemoveDCCProgram(name);
			}
			
			// add new entries to original
			foreach (string  key in m_dccDefCopy.Keys)
			{
				CDCCDefinition def = null;
				m_dccDefCopy.TryGetValue(key, out def);
				
				if (def != null && def.Programs.Count > 0)
				{
					CApplicationSettings.Instance.SetDCCProgram(def);
				}
				
			}
		}
		
		private void SaveCurrentEntry()
		{
			ComboBoxItem selectedItem = dccDefDropdown.SelectedItem as ComboBoxItem;
			if (selectedItem != null)
			{
				string defName = (string)selectedItem.Content;
				
				var def = new CDCCDefinition();
				
				def.Name = defName;
				
		
			}
		}
		
		private void UpdateListViews()
		{
	
			if (dccDefDropdown.Items.Count == 0 || dccDefDropdown.SelectedItem == null)
				return;
			
			//if (programsListView.Items.Count != 0)
				programsListView.Items.Clear();
			
			//if (startupFileListView.Items != null)
				startupFileListView.Items.Clear();
			CDCCDefinition def = null;
			string defName = (string)(dccDefDropdown.SelectedItem as ComboBoxItem).Content;
			
			m_dccDefCopy.TryGetValue(defName, out def);
			
			foreach (string key in def.Programs.Keys)
			{
				var item = new ListViewItem();
				item.Content = def.GetProgram(key).Name;
				programsListView.Items.Add(item);
				
				foreach (string  filekey in def.GetProgram(key).StartupFiles.Keys)
				{
					item = new ListViewItem();
					
					item.Content = filekey;
					startupFileListView.Items.Add(item);
				}
			}
			
			if (programsListView.Items.Count > 0)
				programsListView.SelectedIndex = 0;
			
			if (startupFileListView.Items.Count > 0)
				startupFileListView.SelectedIndex = 0;
			
			UpdateDataGrid();
			
			
		}
		
		private void UpdateDataGrid()
		{
			if (GetCurrentProgram() != null)
			{
				programExeTextBox.Text = GetCurrentProgram().ExecutablePath;
				
				if (GetCurrentStartupFile() != null)
				{
					startupFileTextBox.Text = GetCurrentStartupFile().FullName;
					copyFileCheckbox.IsChecked = (bool)GetCurrentStartupFile().Copy;
					launchWithProgCheckbox.IsChecked = (bool)GetCurrentStartupFile().LaunchWithProgram;
				}
			}
			else
			{
				programExeTextBox.Text = "";
				startupFileTextBox.Text = "";
				copyFileCheckbox.IsChecked = false;
				launchWithProgCheckbox.IsChecked = false;
			}
		}
		
		private SDCCProgram GetCurrentProgram()
		{
			if (programsListView.Items.Count == 0 || GetCurrentDefinition() == null)
				return null;
			
			string currentProgramName = (programsListView.SelectedItem as ListViewItem).Content as string;
			return GetCurrentDefinition().GetProgram(currentProgramName);
	
		}
		
		private CDCCDefinition GetCurrentDefinition()
		{
			if (dccDefDropdown.Items.Count == 0)
				return null;
			string defName = (dccDefDropdown.SelectedItem as ComboBoxItem).Content as string;
			
			CDCCDefinition def;
			m_dccDefCopy.TryGetValue(defName, out def);
			
			return def;
		}
		
		private SStartupFile GetCurrentStartupFile()
		{
			if (startupFileListView.Items.Count == 0 || GetCurrentProgram() == null)
				return null;
			
			string selectedFile = (startupFileListView.SelectedItem as ListViewItem).Content as string;
			return	GetCurrentProgram().GetFile(selectedFile);
		}
		
		private void TrySelectDCCDef(string defName)
		{
			for (int i = 0; i < dccDefDropdown.Items.Count; ++i)
			{
				ComboBoxItem comboItem = dccDefDropdown.Items[i] as ComboBoxItem;
				
				if (comboItem.Content as string == defName)
				{
					dccDefDropdown.SelectedItem = comboItem;
				}
			}
		}
	
		private bool ApplyCurrentSettings()
		{
			if (!ValidateDefs())
			{
				CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.DCCDefChangesNotSaved);
				return false;
			}
			
			if (GetCurrentProgram() != null)
			{
				if (File.Exists(programExeTextBox.Text))
				{
					SDCCProgram pro = GetCurrentProgram();
				
					pro.ExecutablePath = programExeTextBox.Text;
				}
				else
				{
					CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.CommonPathDoesntExist + " (" + programExeTextBox.Text + ")");
				}
				
				if (GetCurrentStartupFile() != null)
				{
					if (File.Exists(startupFileTextBox.Text))
					{
						SStartupFile file = GetCurrentStartupFile();
						
						file.SetFilePath(startupFileTextBox.Text);
						
						file.Copy = (bool)copyFileCheckbox.IsChecked;
						file.LaunchWithProgram = (bool)launchWithProgCheckbox.IsChecked;
					}
					else
					{
						CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.CommonPathDoesntExist + " (" + startupFileTextBox.Text + ")");
					}
				}
				
				UpdateListViews();
			}
			
			ApplyAllChanges();
			
			return true;
		}
		
		private void ToggleProgramOptions(bool bActivated, bool bToggleProgramAddButton)
		{
			programsListView.IsEnabled = bActivated;
			programExeTextBox.IsEnabled = bActivated;
			programBrowseButton.IsEnabled = bActivated;
			addProgramButton.IsEnabled = bToggleProgramAddButton ? bActivated : addProgramButton.IsEnabled;
			removeProgramButton.IsEnabled = bActivated;
			
			
		}
		
		private void ToggleFilesOptions(bool bActivated, bool bToggleFileAddButton)
		{
			startupFileListView.IsEnabled = bActivated;
			startupFileTextBox.IsEnabled = bActivated;
			copyFileCheckbox.IsEnabled = bActivated;
			launchWithProgCheckbox.IsEnabled = bActivated;
			filesAddButton.IsEnabled = bToggleFileAddButton ? bActivated : filesAddButton.IsEnabled;
			filesRemoveButton.IsEnabled = bActivated;
			fileBrowseButton.IsEnabled = bActivated;
		}
		
		private void RefreshFilesListView()
		{
			
			// TODO: Performance due to function calls....
			if (GetCurrentProgram() != null)
			{
				startupFileListView.Items.Clear();
				
				foreach (string key in GetCurrentProgram().StartupFiles.Keys)
				{
				
					var item = new ListViewItem();
					item.Content = GetCurrentProgram().GetFile(key).Name;
					
					startupFileListView.Items.Add(item);
				}
				
				if (startupFileListView.Items.Count > 0)
				{
					startupFileListView.SelectedIndex = 0;
				}
			}
		}
		
		private void CheckSaveChanges()
		{
			MessageBoxResult res = MessageBox.Show(Properties.Resources.CommonAskApplyChanges, 
			                					Properties.Resources.CommonApply, 
			                					MessageBoxButton.YesNo,
			                					MessageBoxImage.Question);
			
			if (res == MessageBoxResult.Yes)
			{
				ApplyCurrentSettings();
			}
		}
		
		//#########################################################################################
		// Callbacks
		//#########################################################################################
		int ProgramDefAddCallback(TextBox box, System.Windows.Forms.DialogResult res)
		{
			
			if (res != System.Windows.Forms.DialogResult.Cancel)
			{
				if (m_dccDefCopy.ContainsKey(box.Text))
				{
					CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.DCCDefDuplicateEntry + " " + box.Text);
					return 0;
				}
				
				CDCCDefinition newDef = new CDCCDefinition();
				newDef.Name = box.Text;
				
				m_dccDefCopy.Add(newDef.Name, newDef);
				
				RefreshDCCDefDropdown();
				
				TrySelectDCCDef(newDef.Name);
			}
			return 0;
		}
		
		bool AddProgramEntryCallback(TextBox box, System.Windows.Forms.DialogResult res)
		{
			if (GetCurrentDefinition() != null)
			{
				CDCCDefinition def = GetCurrentDefinition();
				
				if (def.Programs.ContainsKey(box.Text))
				{
					CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.DCCDefDuplicateEntry + " " + box.Text);
					return false;
				}
				
				SDCCProgram prog = new SDCCProgram();
				prog.Name = box.Text;
				
				def.Programs.Add(prog.Name, prog);
				
				
				UpdateListViews();
			}
				
			return true;
		}
		
		int AddFileEntryCallback(TextBox box, System.Windows.Forms.DialogResult res)
		{
			if (GetCurrentProgram() != null)
			{
				SDCCProgram prog = GetCurrentProgram();
				
				if (prog.StartupFiles.ContainsKey(box.Text))
				{
					CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.DCCDefDuplicateEntry + " " + box.Text);
					return 0;
				}
				
				SStartupFile newFile = new SStartupFile(box.Text, "");
				prog.StartupFiles.Add(newFile.Name, newFile);
				
				RefreshFilesListView();
			}
			
			return 0;
		}
		
		private bool ValidateDefs()
		{
			foreach (CDCCDefinition def in m_dccDefCopy.Values)
			{
				foreach (SDCCProgram prog in def.Programs.Values)
				{
					foreach (SStartupFile file in prog.StartupFiles.Values)
					{
						if (!File.Exists(file.FullName))
						{
							CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.CommonPathDoesntExist +
							                                          " :" + file.FullName +
							                                          " (" + file.Name + ")");
							return false;
						}
					}
					
					if (!File.Exists(prog.ExecutablePath))
					{
						CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.CommonPathDoesntExist +
							                                          " :" + prog.ExecutablePath +
							                                          " (" + prog.Name + ")");
							return false;
					}
				}
				
				if (def.Programs.Count <= 0)
				{
					MessageBoxResult res = MessageBox.Show(Properties.Resources.DCCDefEmptyDef, 
					                                       Properties.Resources.CommonError, 
					                                       MessageBoxButton.OKCancel,
					                                       MessageBoxImage.Error);
					
					if (res == MessageBoxResult.Cancel)
						return false;
				}
			}
			
			return true;
		}
				
		//#########################################################################################
		// Event handlers
		//#########################################################################################
		
		// Selection changes
		void OnDCCDropdownSelectedItemChanged(object sender, RoutedEventArgs args)
		{
			if (m_dccDefCopy.Count > 0)
			{
				ToggleProgramOptions(true, true);
			}
			else
			{
				ToggleProgramOptions(false, false);
			}
			UpdateListViews();
		}
		
		void OnFilesViewSelectionChanged(object sender, RoutedEventArgs args)
		{
			
			
			if (startupFileListView.Items.Count == 0)
			{
				if (GetCurrentProgram() != null)
				{
					ToggleFilesOptions(true, true);
					ToggleFilesOptions(false, false);
				}
				else
				{
					ToggleFilesOptions(false, false);
				}
				return;
			}		
			if (GetCurrentProgram().StartupFiles.Count > 0)
			{
				ToggleFilesOptions(true, true);
			}
			else
			{
				ToggleFilesOptions(false, false);
			}
			
			SStartupFile currentFile = GetCurrentProgram().GetFile((startupFileListView.SelectedItem as ListViewItem).Content as string);
			
			startupFileTextBox.Text = currentFile.FullName;
			copyFileCheckbox.IsChecked = (bool)currentFile.Copy;
			launchWithProgCheckbox.IsChecked = (bool)currentFile.LaunchWithProgram;
			
		}
		
		void OnProgramListViesSelctionChanged(object sender, RoutedEventArgs args)
		{
			if (GetCurrentProgram() == null)
			{
				if (GetCurrentDefinition() != null)
				{
					ToggleProgramOptions(true, true);
					ToggleProgramOptions(false, false);
				}
				else
				{
					ToggleProgramOptions(false, true);
				}
				ToggleFilesOptions(false, true);
				return;
			}
			else
			{
				ToggleProgramOptions(true, true);
				programExeTextBox.Text = GetCurrentProgram().ExecutablePath;
			
			}
			
			if (GetCurrentProgram().StartupFiles.Count > 0)
			{
				ToggleFilesOptions(true, true);
			}
			else
			{
				// Make sure add button is enabled
				ToggleFilesOptions(true, true);
				// deativate rest
				ToggleFilesOptions(false, false);
			}
			
			RefreshFilesListView();
		}
		
		// Button interaction
		void OnAddProgramDefinitionClicked(object sender, RoutedEventArgs args)
		{	
			CUserInteractionUtils.AskUserToEnterString(Properties.Resources.DCCDefEnterDefName, ProgramDefAddCallback);
		}
		
		void OnAddNewProgramClicked(object sender, RoutedEventArgs args)
		{
			//CUserInteractionUtils.AskUserToEnterString(Properties.Resources.DCCDefEnterProgName, AddProgramEntryCallback);

			var grid = new Grid();
			for (int i = 0; i < 5; i++)
			{
				var rowDef = new RowDefinition();
				rowDef.Height = new GridLength(12, GridUnitType.Star);
				grid.RowDefinitions.Add(rowDef);
			}
			
			// Label
			var label = new Label();
			label.Content = "Either enter name or choose existing"; // LOCALIZE
			label.SetValue(Grid.RowProperty, 0);
			grid.Children.Add(label);
			
			// Combobox
			var comboBox = new ComboBox();
			comboBox.SetValue(Grid.RowProperty, 1);
			// items in combobox
			var progs = CApplicationSettings.Instance.GetAllDCCProgramsDefined();
			
			foreach (var prog in progs.Keys) 
			{
				var item = new ComboBoxItem();
				item.Content = prog;
				comboBox.Items.Add(item);
			}
			grid.Children.Add(comboBox);
			
			// Name label
			label = new Label();
			label.Content = "Name"; // LOCALIZE
			label.SetValue(Grid.RowProperty, 2); 
			grid.Children.Add(label);
			
			// text box
			var textBox = new TextBox();
			textBox.Text = "New Program";
			textBox.SetValue(Grid.RowProperty, 3);
			grid.Children.Add(textBox);
			
			
			// Ok Cancel
			var okCancGrid = new Grid();
			okCancGrid.SetValue(Grid.RowProperty, 4);
			
			var colDef = new ColumnDefinition();
			colDef.Width = new GridLength(50, GridUnitType.Star);
			okCancGrid.ColumnDefinitions.Add(colDef);
			colDef = new ColumnDefinition();
			colDef.Width = new GridLength(50, GridUnitType.Star);
			okCancGrid.ColumnDefinitions.Add(colDef);

			
			var okBtn = new Button();
			okBtn.Content = Properties.Resources.CommonOK;
			okBtn.SetValue(Grid.ColumnProperty, 0);
			okCancGrid.Children.Add(okBtn);
			
			var cancButton = new Button();
			cancButton.Content = Properties.Resources.CommonCancel;
			cancButton.SetValue(Grid.ColumnProperty, 1);
			okCancGrid.Children.Add(cancButton);
			
			grid.Children.Add(okCancGrid);
			
			var window = new Window();
			window.SizeToContent = SizeToContent.WidthAndHeight;
			window.Content = grid;
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			window.Title = "Add new program"; // LOCALIZE
			
			comboBox.SelectionChanged += delegate
			{
				textBox.IsEnabled = false;
			};
			
			okBtn.Click += delegate
			{
				// If there's a wrong entry, keep the window open
				bool bCloseWindow = false;
				
				if (comboBox.SelectedIndex != -1)
				{
					var programs = CApplicationSettings.Instance.GetAllDCCProgramsDefined();
					string sSelected = (comboBox.SelectedItem as ComboBoxItem).Content as string;
					
					var curDef = GetCurrentDefinition();		
					
					if (curDef == null)
						return;
					
					if (!curDef.Programs.ContainsKey(sSelected))
					{
						SDCCProgram prog = null;
						if (programs.TryGetValue(sSelected, out prog))
						{
							curDef.Programs.Add(prog.Name, prog);
							UpdateDataGrid();
							UpdateListViews();
							bCloseWindow = true;
						}
					}
					else
					{
						CUserInteractionUtils.ShowErrorMessageBox(Properties.Resources.DCCDefDuplicateEntry + " " + sSelected);
					}
					
				}
				else
				{
					if (textBox.Text.Length > 0)
					{
						bCloseWindow = AddProgramEntryCallback(textBox, System.Windows.Forms.DialogResult.OK);
					}
					else
					{
						CUserInteractionUtils.ShowErrorMessageBox("Please enter a name or choose an existing program from the dropdown!"); // LOCALIZE
					}
				}
				
				if (bCloseWindow)
					window.Close();
				else
				{
					comboBox.SelectedIndex = -1;
					textBox.IsEnabled = true;
				}
			};
			
			cancButton.Click += delegate 
			{
				window.Close();
			};
			
			
			
			window.ShowDialog();
						
		}
			
		void OnRemoveFileClicked(object sender, RoutedEventArgs args)
		{
			SDCCProgram currentProgram = GetCurrentProgram();
			
			string selectedItemName = (startupFileListView.SelectedItem as ListViewItem).Content as string;
			
			if (currentProgram.StartupFiles.ContainsKey(selectedItemName))
			{
			    currentProgram.StartupFiles.Remove(selectedItemName);
			}
			
			UpdateListViews();
			    
		}
		
		void OnAddFileClicked(object sender, RoutedEventArgs args)
		{			
			CUserInteractionUtils.AskUserToEnterString(Properties.Resources.DCCDefEnterFileName, AddFileEntryCallback);
		}
		
		void OnApplyChangesClicked(object sender, RoutedEventArgs args)
		{
			ApplyCurrentSettings();
		}
		
		void OnBrowseStartupFileClicked(object sender, RoutedEventArgs args)
		{
			var dialog = new System.Windows.Forms.OpenFileDialog();
			
			dialog.InitialDirectory = System.Environment.CurrentDirectory  + "\\" + CApplicationSettings.Instance.GetValue(ESettingsStrings.TemplateFolderName).GetValueString() + "\\";
			
			dialog.ShowDialog();
			
			startupFileTextBox.Text = dialog.FileName;
		}
		
		void OnBrowseProgramExeClicked(object sender, RoutedEventArgs args)
		{
			var dialog = new System.Windows.Forms.OpenFileDialog();
			
			
			dialog.ShowDialog();
			
			programExeTextBox.Text = dialog.FileName;
		}
		
		void OnCancelClicked(object sender, RoutedEventArgs args)
		{
			Close();
		}
		
		void OnOKClicked(object sender, RoutedEventArgs args)
		{
			if (ApplyCurrentSettings())
				Close();
		}
		
		void OnRemoveDefClicked(object sender, RoutedEventArgs args)
		{
			CDCCDefinition def = GetCurrentDefinition();
			
			if (def != null)
			{
				m_dccDefCopy.Remove(def.Name);
			}
			
			RefreshDCCDefDropdown();
		}
		
		void OnRemoveProgramClicked(object sender, RoutedEventArgs args)
		{
			SDCCProgram prog = GetCurrentProgram();
			
			if (prog != null)
			{
				CDCCDefinition def = GetCurrentDefinition();
				
				if (def != null)
				{
					def.Programs.Remove(prog.Name);
					UpdateListViews();
				}
			}
		}
		
		void OnFileTextChanged(object sender, RoutedEventArgs args)
		{
			SStartupFile file = GetCurrentStartupFile();
			
			if (file != null)
			{
				file.SetFilePath(startupFileTextBox.Text);
			}
		}
		
		void OnCopyFileCheckboxClicked(object sender, RoutedEventArgs args)
		{
			SStartupFile file = GetCurrentStartupFile();
			
			if (file != null)
			{
				file.Copy = (bool)copyFileCheckbox.IsChecked;
			}
		}
		
		void OnLaunchProgramCheckboxChecked(object sender, RoutedEventArgs args)
		{
			SStartupFile file = GetCurrentStartupFile();
			
			if (file != null)
			{
				file.LaunchWithProgram = (bool)launchWithProgCheckbox.IsChecked;
			}
		}
		
		void OnProgramExeTextChanged(object sender, RoutedEventArgs args)
		{
			SDCCProgram prog = GetCurrentProgram();
			
			if (prog != null)
			{
				prog.ExecutablePath = programExeTextBox.Text;
			}
		}
	}
}