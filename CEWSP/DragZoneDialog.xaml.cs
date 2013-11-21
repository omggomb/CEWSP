/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 23.10.2013
 * Time: 18:54
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
using System.Windows.Forms;


using CEWSP.ApplicationSettings;
using CEWSP.Utils;

namespace CEWSP
{
	/// <summary>
	/// Interaction logic for DragZoneDialog.xaml
	/// </summary>
	public partial class DragZoneDialog : Window
	{
		private static FileInfo m_fileInfo;
		private static DirectoryInfo m_dirInfo;
		
		public DragZoneDialog()
		{
			InitializeComponent();
			
			copyCheckbox.Click += new RoutedEventHandler(copyCheckbox_Checked);
			
			
		}

		void copyCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			saveFileTextBox.IsEnabled = (bool)copyCheckbox.IsChecked;
			browseButton.IsEnabled = (bool)copyCheckbox.IsChecked;
		}
		
		public static void ShowWindow(FileInfo info)
		{
			m_fileInfo = info;
			DragZoneDialog dialog = new DragZoneDialog();
			dialog.saveFileTextBox.Text = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString()
									+ "\\" + info.Name;
			
			
			dialog.Show();
		}
		
		public static void ShowWindow(DirectoryInfo info)
		{
			m_dirInfo = info;
			DragZoneDialog dialog = new DragZoneDialog();
			dialog.saveFileTextBox.Text = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString()
									+ "\\" + info.Name;			
			dialog.Show();
		}
		
		private bool ProcessRequest(FileInfo info)
		{
			if (copyCheckbox.IsChecked == true)
			{
				if (CPathUtils.IsStringCEConform(saveFileTextBox.Text))
				{
					info.CopyTo(saveFileTextBox.Text, true);
				}
				else
					return false;
				
				if (rcCheckbox.IsChecked == true)
				{
					FileInfo newFile = new FileInfo(saveFileTextBox.Text);
					CProcessUtils.RunRC(newFile);
				}
			}
			else
			{
				if (rcCheckbox.IsChecked == true)
				{
					FileInfo newFile = new FileInfo(saveFileTextBox.Text);
					CProcessUtils.RunRC(info);
				}
			}

			return true;			
			
		}
		
		private bool ProcessRequest(DirectoryInfo info, string sTargetPath)
		{
			try
			{
				CopyDirectory(info, sTargetPath);
 
			}
			catch (UnauthorizedAccessException e)
			{
				CUserInteractionUtils.ShowErrorMessageBox(e.Message);
				return false;
			}
			
			return true;
		}
		
		private void CopyDirectory(DirectoryInfo info, string sTargetPath)
		{
			string sTargetForFiles = sTargetPath + "\\" + info.Name;
			if (!Directory.Exists(sTargetForFiles))
				Directory.CreateDirectory(sTargetForFiles);


            foreach (DirectoryInfo subDir in info.GetDirectories())
            {
                CopyDirectory(subDir, sTargetForFiles);
            }

			foreach (FileInfo file in info.GetFiles())
			{
				File.Copy(file.FullName, sTargetForFiles + "\\" + file.Name, true);
			}

         
		}
		
		void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_fileInfo != null)
			{
				var dialog = new SaveFileDialog();
				
				dialog.InitialDirectory = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
				dialog.Filter = m_fileInfo.Extension + " Files | *" + m_fileInfo.Extension;
				dialog.FileName = m_fileInfo.Name;
				
				
				DialogResult res = dialog.ShowDialog();
				
				if (res != System.Windows.Forms.DialogResult.Cancel)
				{
					saveFileTextBox.Text = dialog.FileName;
				}
				
			}
			else 
			{
				var dialog = new FolderBrowserDialog();
				dialog.SelectedPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
				dialog.Description = Properties.DragZoneResources.DragFolderHint;
				
				System.Windows.Forms.DialogResult res = dialog.ShowDialog();
				
				if (res == System.Windows.Forms.DialogResult.OK)
				{
					saveFileTextBox.Text = dialog.SelectedPath;
				}
				
			}
			
		}
		
		void OnOkClicked(object sender, RoutedEventArgs e)
		{
			if (m_fileInfo != null)
			{
				if (ProcessRequest(m_fileInfo))
					Close();
			} 
			else 
			{
				if (ProcessRequest(m_dirInfo, saveFileTextBox.Text))
					Close();
			}
		}
		
		void OnCancelClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}