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
	}
}