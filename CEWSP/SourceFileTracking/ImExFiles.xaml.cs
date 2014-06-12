/*
 * Created by SharpDevelop.
 * User: Ihatenames
 * Date: 11/06/2013
 * Time: 19:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using OmgUtils.Path;
using OmgUtils.UserInteraction;

namespace CEWSP.SourceFileTracking
{
	public enum EMode
	{
		eMO_Import,
		eMO_Export,
		eMO_Move
	}
	/// <summary>
	/// Interaction logic for ImExFiles.xaml
	/// </summary>
	public partial class ImExFiles : Window
	{
		
		private static EMode m_mode;
		public static string WindowTitle {get; set;}
		private ImExFiles()
		{
			InitializeComponent();
		}
		
		public static void ShowWindow(EMode mode)
		{
			m_mode = mode;
			
			switch (m_mode) 
			{
				case EMode.eMO_Import:
					WindowTitle = Properties.ImportExportResources.WindowTitleImport;
					break;
				case EMode.eMO_Export:
					WindowTitle = Properties.ImportExportResources.WindowTitleExport;
					break;
				case EMode.eMO_Move:
					WindowTitle = Properties.ImportExportResources.WindowTitleMove;
					break;
				default:
					throw new Exception("Invalid value for EMode");
			}
			
			ImExFiles window = new ImExFiles();
			window.SetupAffectionComboBox();
			window.ShowDialog();
			
		}
		
		void OnBrowseClicked(object sender, RoutedEventArgs e)
		{
			if (m_mode == EMode.eMO_Move)
			{
				FolderBrowserDialog dialog = new FolderBrowserDialog();
				System.Windows.Forms.DialogResult res = dialog.ShowDialog();
				
				if (res == System.Windows.Forms.DialogResult.OK)
				{
					fileTextBox.Text = dialog.SelectedPath;
				}
			}
			else if (m_mode == EMode.eMO_Export)
			{
				SaveFileDialog dialog = new SaveFileDialog();
				dialog.Filter = "Tracker files | *" + CSourceTracker.FileExtension;
				dialog.AddExtension = true;
				dialog.CheckPathExists = true;
				
				
				
				System.Windows.Forms.DialogResult res = dialog.ShowDialog();
				
				if (res == System.Windows.Forms.DialogResult.OK)
				{
					fileTextBox.Text = dialog.FileName;
				}
				
			}
			else
			{
				OpenFileDialog dialog = new OpenFileDialog();
				dialog.Filter = "Tracker files | *" + CSourceTracker.FileExtension;
				
				dialog.CheckPathExists = true;
				dialog.Multiselect = true;
				
				System.Windows.Forms.DialogResult res = dialog.ShowDialog();
				
				if (res == System.Windows.Forms.DialogResult.OK)
				{
					foreach (string file in dialog.FileNames)
					{
						fileTextBox.Text += file + ";";
					}
				}
			}
		}
		
		void OnOKClicked(object sender, RoutedEventArgs e)
		{
			string path = fileTextBox.Text;
			string[] paths = path.Split(';');
			foreach ( string filePath in paths)
			{
				if (String.IsNullOrWhiteSpace(filePath))
					continue;
				
				string finalPath = filePath;
				if (!finalPath.Contains("."))
				{
					UserInteractionUtils.ShowErrorMessageBox(Properties.Resources.CommonNoFileNameSpecified);
					return;
				}
				
				FileInfo info = new FileInfo(finalPath);
				
				if (info.Extension != CSourceTracker.FileExtension)
				{
					finalPath = PathUtils.ChangeExtension(finalPath, CSourceTracker.FileExtension);
				}
				
				if (!info.Directory.Exists)
					Directory.CreateDirectory(info.DirectoryName);
				
				EFileRoot root;
				bool bDoBoth = false;
				
				if (affectionComboBox.SelectedIndex == 0)
					root = EFileRoot.eFR_CERoot;
				else if (affectionComboBox.SelectedIndex == 1)
					root = EFileRoot.eFR_GameFolder;
				else
				{
					root = EFileRoot.eFR_CERoot;
					bDoBoth = true;
				}
				
				switch (m_mode) 
				{
					case EMode.eMO_Import:
						{
							
							EFileRoot targetRoot = CSourceTracker.Instance.GetTrackingFileAffection(finalPath);
							if (!bDoBoth && targetRoot != root)
							{
								UserInteractionUtils.ShowErrorMessageBox(Properties.ImportExportResources.AffectionMissmatch);
								return;
							}
							
							CSourceTracker.Instance.ImportTrackingList(finalPath, CSourceTracker.Instance.GetTrackingFileAffection(finalPath));
							
							
						}					
						break;
					case EMode.eMO_Export:
						{
							if (!bDoBoth)
								CSourceTracker.Instance.ExportTrackingFile(finalPath, root);
							else
							{
								string name = finalPath;
								string noExtension = PathUtils.RemoveExtension(name);
								string rootFile = noExtension;
								string gameFile = noExtension;
								if (!rootFile.Contains("_Root"))
									rootFile += "_Root";
								if (!gameFile.Contains("_Game"))
									gameFile += "_Game";
							
								
								
								CSourceTracker.Instance.ExportTrackingFile(rootFile + CSourceTracker.FileExtension, EFileRoot.eFR_CERoot);
								CSourceTracker.Instance.ExportTrackingFile(gameFile + CSourceTracker.FileExtension, EFileRoot.eFR_GameFolder);
							}
						}
						break;
					case EMode.eMO_Move:
						{
							if (!bDoBoth)
								CSourceTracker.Instance.MoveTrackedFiles(finalPath, root);
							else
							{
								CSourceTracker.Instance.MoveTrackedFiles(finalPath, EFileRoot.eFR_CERoot);
								CSourceTracker.Instance.MoveTrackedFiles(finalPath, EFileRoot.eFR_GameFolder);
							}
						}
						break;
					default:
						throw new Exception("Invalid value for EMode");
				}
			
			}
			
			Close();
		}
		
		void OnCancelClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
		
		private void SetupAffectionComboBox()
		{
			ComboBoxItem item = new ComboBoxItem();
			item.Content = Properties.ImportExportResources.AffectRootFiles; 
			affectionComboBox.Items.Add(item);
			
			item = new ComboBoxItem();
			item.Content = Properties.ImportExportResources.AffectGameFiles; 
			affectionComboBox.Items.Add(item);
			
			item = new ComboBoxItem();
			item.Content = Properties.ImportExportResources.AffectBoth;
			affectionComboBox.Items.Add(item);
			
			affectionComboBox.SelectedIndex = 0;
			
		}
	}
}