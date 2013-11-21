/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 19.11.2013
 * Time: 12:25
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

namespace CEWSP.ApplicationSettings
{
	/// <summary>
	/// Interaction logic for ApplicationSettingsDialog.xaml
	/// </summary>
	public partial class ApplicationSettingsDialog : Window
	{
		private Dictionary<string, CSetting> m_settingCopy;
		
		public ApplicationSettingsDialog()
		{
			InitializeComponent();
			m_settingCopy = new Dictionary<string, CSetting>();
		}
		
		void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			MakeLocalCopy();
			LoadSettings();
			settingsListView.SelectionChanged += OnItemClicked;
		}
		
		
		void OnItemClicked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (settingsListView.SelectedItem == null)
				return;
			string sSelectedItem = (settingsListView.SelectedItem as ListViewItem).Content  as string;
			
			CSetting selectedSetting;
			if (!m_settingCopy.TryGetValue(sSelectedItem, out selectedSetting))
				return;
			
			ESettingsType type = selectedSetting.GetSettingType();
			
			switch (type)
			{
				case ESettingsType.eST_Boolean:
					DisplayBoolTemplate(selectedSetting);
					break;
				case ESettingsType.eST_String:
					DisplayStringTemplate(selectedSetting);
					break;
			}
		}
		
		void OnOKClicked(object sender, RoutedEventArgs e)
		{
			ApplyChanes();
			Close();
		}
			
		void OnCancelClicked(object sender, RoutedEventArgs e)
		{
			Close();
		}
		void OnApplyClicked(object sender, RoutedEventArgs e)
		{
			ApplyChanes();
		}
		
		void OnResetDefaultClicked(object sender, RoutedEventArgs e)
		{
			CApplicationSettings.Instance.Reset(true);
			
			
			MakeLocalCopy();
			
			LoadSettings();
		}
		
		private void MakeLocalCopy()
		{
			m_settingCopy.Clear();
			foreach (CSetting setting in CApplicationSettings.Instance.PublicSettings.Values)
			{
				m_settingCopy.Add(setting.Key, new CSetting(setting.Key, setting.Value, setting.Descripition, setting.UserEditable));
			}
		}
		
		private void LoadSettings()
		{
			int selectedIndex = -1;
		
			if (settingsListView.HasItems)
			{
				selectedIndex = settingsListView.SelectedIndex;
				settingsListView.Items.Clear();
				
			}
			
			foreach (CSetting setting in m_settingCopy.Values)
			{			
				if (setting.UserEditable)
				{
					var listItem = new ListViewItem();
					listItem.Content = setting.Key;
					listItem.ToolTip = setting.Descripition;
					settingsListView.Items.Add(listItem);
				}
			}
			
			if (settingsListView.HasItems)
			{
				settingsListView.SelectedIndex = selectedIndex == -1 ? 0 : selectedIndex;
			}
		}
	
		private void DisplayBoolTemplate(CSetting setting)
		{
			StackPanel panel = new StackPanel();
			panel.Orientation = Orientation.Vertical;
			panel.SetValue(Grid.ColumnProperty, 1);
			
			TextBlock label = new TextBlock();
			label.Text = setting.Descripition;
			label.TextWrapping = TextWrapping.WrapWithOverflow;
			panel.Children.Add(label);
			
			Separator sep = new Separator();
			panel.Children.Add(sep);
			
			RadioButton falseButton = new RadioButton();
			RadioButton trueButton = new RadioButton();
			
			trueButton.Content = Properties.Resources.CommonTrue;
			trueButton.IsChecked = setting.GetValueBool() == true;
			trueButton.Click += delegate
			{
				falseButton.IsChecked = false;
				setting.Value = trueButton.IsChecked.ToString();
			};
			panel.Children.Add(trueButton);
			
			
			falseButton.Content = Properties.Resources.CommonFalse;
			falseButton.IsChecked = (setting.GetValueBool() == false);
			falseButton.Click += delegate
			{
				trueButton.IsChecked = false;
				setting.Value = (!falseButton.IsChecked).ToString();
			};
			panel.Children.Add(falseButton);
			
			settingsGrid.Children.Clear();
			settingsGrid.Children.Add(panel);
		}
		
		private void DisplayStringTemplate(CSetting setting)
		{
			StackPanel panel = new StackPanel();
			panel.Orientation = Orientation.Vertical;
			panel.SetValue(Grid.ColumnProperty, 1);
			
			TextBlock label = new TextBlock();
			label.Text	 = setting.Descripition;
			label.TextWrapping = TextWrapping.WrapWithOverflow;
			panel.Children.Add(label);
			
			Separator sep = new Separator();
			panel.Children.Add(sep);
			
			TextBox box = new TextBox();
			box.Text = setting.GetValueString();
			box.TextChanged += delegate
			{
				setting.Value = box.Text;
			};
			panel.Children.Add(box);
			
			settingsGrid.Children.Clear();
			settingsGrid.Children.Add(panel);
		
		}
		
		private void ApplyChanes()
		{
			foreach (CSetting setting in m_settingCopy.Values)
			{
				CApplicationSettings.Instance.SetValue(setting);
			}
		}
		
	}
}