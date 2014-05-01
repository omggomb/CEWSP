/*
 * Created by SharpDevelop.
 * User: Ihatenames
 * Date: 04/06/2014
 * Time: 10:44
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
using System.Windows.Forms;

using CEWSP.ApplicationSettings;

namespace CEWSP.Shortcuts
{
	/// <summary>
	/// Interaction logic for ShortcutDialog.xaml
	/// </summary>
	public partial class ShortcutDialog : Window
	{
		
		private SShortcut m_currentSC = new	SShortcut();
		
		private ShortcutDialog()
		{
			InitializeComponent();
			
		}
		
		public static void ShowMe(int id)
		{
			ShortcutDialog dia = new ShortcutDialog();
			dia.LoadInfo(id);
			dia.ShowDialog();
		}
		
		private void LoadInfo(int id)
		{
			m_currentSC.myID = id;
			m_currentSC.Name = nameTextBox.Text = CApplicationSettings.Instance.Shortcuts[id].Name;
			m_currentSC.Exec = execTextBox.Text = CApplicationSettings.Instance.Shortcuts[id].Exec;
			m_currentSC.Args = argsTextBox.Text = CApplicationSettings.Instance.Shortcuts[id].Args;
			
			var definedPrograms = CApplicationSettings.Instance.GetAllDCCProgramsDefined();
			foreach (var program in definedPrograms) 
			{
				var item = new ComboBoxItem();
				item.Content = program.Key;
				existingProgComboBox.Items.Add(item);
			}
		}
		
		void FileBrowseButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dia = new OpenFileDialog();
			dia.CheckFileExists = true;
			
			System.Windows.Forms.DialogResult res = dia.ShowDialog();
			
			if (res == System.Windows.Forms.DialogResult.OK)
			{
				execTextBox.Text = dia.FileName;
				nameTextBox.Text = dia.SafeFileName;
			}	
		}
		
		void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
		
		void OkButton_Click(object sender, RoutedEventArgs e)
		{
			
				m_currentSC.Name = nameTextBox.Text;
				m_currentSC.Exec = execTextBox.Text;
				m_currentSC.Args = argsTextBox.Text;
			
		
			
			
			CApplicationSettings.Instance.Shortcuts.RemoveAt(m_currentSC.myID);
			CApplicationSettings.Instance.Shortcuts.Insert(m_currentSC.myID, m_currentSC);
			
			Close();
		}
		
		void ToggleInputFields(bool bEnabled)
		{
			nameTextBox.IsEnabled = bEnabled;
			execTextBox.IsEnabled = bEnabled;
			//argsTextBox.IsEnabled = bEnabled;
			fileBrowseButton.IsEnabled = bEnabled;
		}
		void ExistingProgComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var programs = CApplicationSettings.Instance.GetAllDCCProgramsDefined();
			SDCCProgram prog = null;
			string sSelected = (existingProgComboBox.SelectedItem as ComboBoxItem).Content as string; 
			
			if (programs.TryGetValue(sSelected, out prog))
			{
				nameTextBox.Text = prog.Name;
				execTextBox.Text = prog.ExecutablePath;
			}
			
		}
	}
}