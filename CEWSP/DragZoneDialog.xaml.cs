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
using Microsoft.VisualBasic.FileIO;


using CEWSP.ApplicationSettings;
using CEWSP.Utils;

namespace CEWSP
{
	/// <summary>
	/// Interaction logic for DragZoneDialog.xaml
	/// </summary>
	public partial class DragZoneDialog : Window
	{
		private  List<FileInfo> m_fileInfoList;
		private  List<DirectoryInfo> m_dirInfoList;
		
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
		
		public  void ShowWindow(List<FileInfo> info)
		{
			m_fileInfoList = info;

		saveFileTextBox.Text = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			
			
			Show();
		}
		
		public  void ShowWindow(List<DirectoryInfo> info)
		{
			m_dirInfoList = info;
	
			saveFileTextBox.Text = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();		
			Show();;
		}
		
		private bool ProcessRequest(FileInfo info)
		{
			string sNewFile = saveFileTextBox.Text + "\\" + info.Name;
			
			if (copyCheckbox.IsChecked == true)
			{
				
				
				if (CPathUtils.IsStringCEConform(saveFileTextBox.Text))
				{
					CProcessUtils.CopyFile(info.FullName, sNewFile, false);
				}
				else
					return false;
				
				if (rcCheckbox.IsChecked == true)
				{
					FileInfo newFile = new FileInfo(sNewFile);
					CProcessUtils.RunRC(newFile);
				}
			}
			else
			{
				if (rcCheckbox.IsChecked == true)
				{
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
			CProcessUtils.CopyDirectory(info.FullName, sTargetPath, false);
		}
		
		private bool ProcessRequest(List<FileInfo> fileInfoList)
		{
			bool sux = true;
			foreach (FileInfo info in fileInfoList)
			{
				if (!ProcessRequest(info))
					sux = false;
			}
			
			return sux;
		}
		
		private bool ProcessRequest(List<DirectoryInfo> dirList, string sTargetPath)
		{
			bool sux = true;
			
			foreach (var dir in dirList)
			{
				if (!ProcessRequest(dir, sTargetPath))
					sux = false;
			}
			
			return sux;
		}
		
		
		void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			// We only need to know a folder now...
			
			var dialog = new FolderBrowserDialog();
			dialog.SelectedPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			dialog.Description = Properties.DragZoneResources.DragFolderHint;
			
			System.Windows.Forms.DialogResult res = dialog.ShowDialog();
			
			if (res == System.Windows.Forms.DialogResult.OK)
			{
				saveFileTextBox.Text = dialog.SelectedPath;
			}

		}
		
		void OnOkClicked(object sender, RoutedEventArgs e)
		{
			if (saveFileTextBox.Text.Contains("."))
			{
				CUserInteractionUtils.ShowErrorMessageBox(Properties.DragZoneResources.TextIsNoDirectory);
			}
			else
			{
				if (m_fileInfoList != null)
				{
					if (ProcessRequest(m_fileInfoList))
						Close();
				} 
				else 
				{
					if (ProcessRequest(m_dirInfoList, saveFileTextBox.Text))
						Close();
				}
			}
		}
		
		void OnCancelClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}