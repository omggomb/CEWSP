/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 09/27/2013
 * Time: 21:59
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

using CEWSP.ApplicationSettings;
using CEWSP.Utils;

namespace CEWSP.ThreeDSceneDialog
{
	/// <summary>
	/// Interaction logic for ThreeDSceneDialog.xaml
	/// </summary>
	public partial class ThreeDSceneDialog : Window
	{
		public ThreeDSceneDialog()
		{
			InitializeComponent();
			
			
		}
		
		#region Attributes
		private string DesiredSavePath {get; set;}
		#endregion
		
		#region Eventhandlers
		
		void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Need to use event for this so the window can actually be closed if something is wrong 
			saveFileTextBox.Text = (string)CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			
			List<string> dccs = CApplicationSettings.Instance.GetAllDCCProgramNames();
			
			if (dccs.Count <= 0)
			{
				MessageBox.Show("There are no DCC Programs specified! Please add at least one via \"Settings\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // LOCALIZE
				Close();
			}
			
			foreach (string  name in dccs)
			{
				
				
				ComboBoxItem item = new ComboBoxItem();
				
				item.Content = name;
				
				dccProgramDropdown.Items.Add(item);
			}

			dccProgramDropdown.SelectedIndex = 0;			
		}
		
		void OnOKClicked(object sender, RoutedEventArgs e)
		{
			// Check if path in textbox is valid
			if (!ValidateFilePath(DesiredSavePath))
			{
				return;
			}
			
			ComboBoxItem item = (ComboBoxItem)dccProgramDropdown.SelectedItem;
			string progName = (string)item.Content;
			
			CDCCDefinition prog = CApplicationSettings.Instance.GetDCCProgram(progName);
			
			prog.Start(DesiredSavePath);

			// TODO: Implement prog.Start();
			
			Close();
		}
		
		void OnCancelClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
		
		void OnBrowseDirClicked(object sender, RoutedEventArgs e)
		{
			// TODO: Remimplement
			System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
			
			// Error checking can be omitted since there are only valid entries to begin with
			ComboBoxItem item = (ComboBoxItem)dccProgramDropdown.SelectedItem;
			string progName = (string)item.Content;
			
			CDCCDefinition prog = CApplicationSettings.Instance.GetDCCProgram(progName);
			
			if (prog != null)
			{
				string filter = "";
				int j = 0;
				
				foreach (string key in prog.Programs.Keys)
				{
					SDCCProgram program = prog.GetProgram(key);
					
					int i = 0;
					
					foreach (string fileKey in program.StartupFiles.Keys)
					{
						SStartupFile file = program.GetFile(fileKey);
						
						if (i == 0)
							filter += program.Name + " Files|";
							
						filter += "*." + file.Extension;
						
						if (i < program.StartupFiles.Count - 1)
							filter += ";";
						++i;
					
						
					}
						
						
						// TODO: What if there are two progs but only one startup?
						
					if (j < prog.Programs.Count - 1)
							filter += "|";
					
					++j;
				}
			
				dialog.Filter = filter;
			}
	
			dialog.InitialDirectory = (string)CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			
			System.Windows.Forms.DialogResult res = dialog.ShowDialog();
			
			if (res == System.Windows.Forms.DialogResult.Cancel)
			{
				if (saveFileTextBox.Text != CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString())
				{
					return;
				}
				string sRootPath = (string)CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
				string sGameFolder = (string)CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
				
				sGameFolder = sGameFolder == null ? "" : sGameFolder;
				SetDesiredSavePath(sGameFolder + "\\Objects\\");
				
			}
			else
			{
				SetDesiredSavePath(dialog.FileName);
			}
			
		}
		void OnTextBoxTextEntered(object sender, TextChangedEventArgs e)
		{
			SetDesiredSavePath(saveFileTextBox.Text);
		}
		
		#endregion
		
		#region Methods
		
		/// <summary>
		/// Checks whether sPath is CE conform and actually contains a file name
		/// </summary>
		/// <param name="sPath"></param>
		/// <returns>True if valid, else false</returns>
		private bool ValidateFilePath(string sPath)
		{
			// Need to check for a valid filename because user can enter stuff in the textbox itself
			
			int dotPos = DesiredSavePath.LastIndexOf('.');
			int dirPos = DesiredSavePath.LastIndexOf('\\');
				ComboBoxItem item = dccProgramDropdown.SelectedItem as ComboBoxItem;
			CDCCDefinition prog = CApplicationSettings.Instance.GetDCCProgram(item.Content as string);
			
			if ((dotPos < dirPos && dotPos != -1) || dirPos == DesiredSavePath.Length - 1)
			{
				MessageBox.Show("Please specify a valid filname with extension", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			
			// It's valid but doesn't have an extension
			// Irrelevant since the name is only used for naming anyway
			/*if (dotPos == -1)
			{
			

				DesiredSavePath += "." + prog.FileExtension;
			}
			else
			{
				// Check for the correct extension
				string extension = DesiredSavePath.Substring(dotPos + 1, DesiredSavePath.Length - dotPos -1);
				
				if (extension != prog.FileExtension)
				{
					string newPath = DesiredSavePath.Substring(0, dotPos + 1) + prog.FileExtension;
				}
			}
			*/
			
			
			return CPathUtils.IsStringCEConform(sPath);
		}
		
		
		
		/// <summary>
		/// Sets the save path to the soecified location. Does NOT perform error checking.
		/// </summary>
		/// <param name="sPath"></param>
		private void SetDesiredSavePath(string sPath)
		{
			DesiredSavePath = sPath;
			saveFileTextBox.Text = sPath;
		}
		
		#endregion
		
		
	}
}