/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 24.10.2013
 * Time: 16:11
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CEWSP.Utils
{
	/// <summary>
	/// Description of CUserInteractionUtils.
	/// </summary>
	public class CUserInteractionUtils
	{
		public CUserInteractionUtils()
		{
		}
		
		public static void ShowErrorMessageBox(string message)
		{
			MessageBox.Show(message, Properties.Resources.CommonError, MessageBoxButton.OK, MessageBoxImage.Error);
		}
		
		public static void ShowInfoMessageBox(string message)
		{
			MessageBox.Show(message, Properties.Resources.CommonNotice, MessageBoxButton.OK, MessageBoxImage.Information);	
		}
		
		public static void DisplayRichTextBox(string content, string title = "Result") // LOCALIZE: 
		{
			RichTextBox box = new RichTextBox();
			
			box.AppendText(content);
			box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			box.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			
			Window window = new Window();
			
			window.Content = box;
			window.Title = title;
			window.Width = 400;
			window.Height = 200;
			
			window.Show();
		}
		
		public static void	AskUserToEnterString(string windowTitle, Func<TextBox, System.Windows.Forms.DialogResult, int> callbackOnFinished)
		{
			var window = new Window();
			
			window.Title = windowTitle;
			window.ResizeMode = ResizeMode.NoResize;
			window.SizeToContent = SizeToContent.WidthAndHeight;
			
			var box = new TextBox();
			box.Width = 300;
			box.KeyDown += delegate(object sender, KeyEventArgs e)
			{
				if (e.Key == Key.Enter)
				{
					window.Close();
					callbackOnFinished(box, System.Windows.Forms.DialogResult.OK);
					
				}
				else if (e.Key == Key.Escape)
				{
					window.Close();
					callbackOnFinished(box, System.Windows.Forms.DialogResult.Cancel);
					
				}
			};
			
			var buttonOK = new Button();
			buttonOK.HorizontalAlignment = HorizontalAlignment.Stretch;
			buttonOK.Content = Properties.Resources.CommonOK;
			buttonOK.Click += delegate
			{ 
				window.Close();
				callbackOnFinished(box, System.Windows.Forms.DialogResult.OK);
				
			};
			
			var buttonCancel = new Button();
			buttonCancel.Content = Properties.Resources.CommonCancel;
			buttonCancel.Click += delegate
			{
				window.Close();
				callbackOnFinished(box, System.Windows.Forms.DialogResult.Cancel);
				
			};
			
			Grid grid = new Grid();
			var rowDef = new RowDefinition();
			rowDef.Height = new GridLength(50, GridUnitType.Star);
			grid.RowDefinitions.Add(rowDef);
			rowDef = new RowDefinition();
			rowDef.Height = new GridLength(50, GridUnitType.Star);
			grid.RowDefinitions.Add(rowDef);
			
			var columnDef = new ColumnDefinition();
			columnDef.Width = new GridLength(50, GridUnitType.Star);
			grid.ColumnDefinitions.Add(columnDef);
			columnDef = new ColumnDefinition();
			columnDef.Width = new GridLength(50, GridUnitType.Star);
			grid.ColumnDefinitions.Add(columnDef);
			
			grid.Children.Add(box);
			grid.Children.Add(buttonOK);
			grid.Children.Add(buttonCancel);
			
			box.SetValue(Grid.RowProperty, 0);
			box.SetValue(Grid.ColumnSpanProperty, 2);
			
			buttonOK.SetValue(Grid.RowProperty, 1);
			buttonOK.SetValue(Grid.ColumnProperty, 0);
			
			buttonCancel.SetValue(Grid.RowProperty, 1);
			buttonCancel.SetValue(Grid.ColumnProperty, 1);
			
			window.Content = grid;
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			box.Focus();
			window.ShowDialog();
			
		}
		
	}
}
