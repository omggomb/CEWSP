/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 24.10.2013
 * Time: 16:18
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

using CEWSP.ApplicationSettings;

namespace CEWSP.Utils
{
	/// <summary>
	/// Description of CProcessUtils.
	/// </summary>
	public class CProcessUtils
	{
        private static DispatcherTimer m_copyNoticeTimer;

		public CProcessUtils()
		{
		}
		
		public static void RunRCtif(FileInfo info)
		{
			if (CApplicationSettings.Instance.IsRootValid(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString()))
			{
				string rcPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString() + 
					CApplicationSettings.Instance.GetValue(ESettingsStrings.RCRelativePath).GetValueString();
				
			
				Process rcProcess = new Process();
				
				ProcessStartInfo startInfo = new ProcessStartInfo(rcPath);
				
				startInfo.Arguments = info.FullName + " /userdialog=1";
				rcProcess.StartInfo = startInfo;
				
				RunProcessWithRedirectedStdErrorStdOut(rcProcess);
			}
			
		}
		
		public static void RunRC(FileInfo info)
		{
			if (info.Extension == ".tif")
			{
				RunRCtif(info);
			}
			else
			{
				if (CApplicationSettings.Instance.IsRootValid(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString()))
				{
					string rcPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString() + 
						CApplicationSettings.Instance.GetValue(ESettingsStrings.RCRelativePath).GetValueString();
					Process rcProcess = new Process();
					
					ProcessStartInfo startInfo = new ProcessStartInfo(rcPath);
					
					startInfo.Arguments = info.FullName;
					rcProcess.StartInfo = startInfo;
					
					RunProcessWithRedirectedStdErrorStdOut(rcProcess);
				}
			}
		}
		
		public static void RunGFXExporter(FileInfo fileInfo)
		{
			string root = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			string gfxPath = root + CApplicationSettings.DefaultGFXExporterRelativePath;
			
			
			
			Process proc = new Process();
			
			ProcessStartInfo info = new ProcessStartInfo(gfxPath);
		
			info.Arguments = fileInfo.FullName; 
			proc.StartInfo = info;
			
			RunProcessWithRedirectedStdErrorStdOut(proc);
		}
		
		/// <summary>
		/// Runs the given process and redirects its sdtout and stderr to a textbox, which is shown afterwards.
		/// </summary>
		/// <param name="process"></param>
		public static void RunProcessWithRedirectedStdErrorStdOut(Process process)
		{
			ProcessStartInfo info = process.StartInfo;
			
			info.RedirectStandardOutput = true;
			info.RedirectStandardError = true;
			info.UseShellExecute = false;
			
			process.Start();
			
			StreamReader stdOut = process.StandardOutput;
			StreamReader stdError  = process.StandardError;
			
			string output = stdOut.ReadToEnd() + "\n" + stdError.ReadToEnd();
			
			CUserInteractionUtils.DisplayRichTextBox(output);
		}
		
		public static void CopyDirectory(string sPathToDir, string sTargetDir)
		{
			 m_copyNoticeTimer = new DispatcherTimer();
             m_copyNoticeTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
             m_copyNoticeTimer.IsEnabled = true;
			
			string sMessageBoxMessage = Properties.Resources.CommonCopying;
			
			Window window = new Window();
			
			window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			window.SizeToContent = SizeToContent.WidthAndHeight;
			
			window.Content = sMessageBoxMessage;

            m_copyNoticeTimer.Tick += delegate
			{
				sMessageBoxMessage += '.';
				
				if (sMessageBoxMessage.Length > 15)
					sMessageBoxMessage = sMessageBoxMessage.TrimEnd('.');
				
				window.Content = sMessageBoxMessage;
			};
			
			window.Show();
            m_copyNoticeTimer.Start();
			CopyDirectoryNoNotification(sPathToDir, sTargetDir);
            m_copyNoticeTimer.Stop();
			window.Close();
		}
		
		public  static void CopyDirectoryNoNotification(string sPathToDir, string sTargetDir)
		{
			if (!Directory.Exists(sPathToDir))
				return;
			
			if (!Directory.Exists(sTargetDir))
			{
				try 
				{
					Directory.CreateDirectory(sTargetDir);
				}
				catch (Exception e)
				{
					
					CUserInteractionUtils.ShowErrorMessageBox(e.Message);
					return;
				}
			}
			
			DirectoryInfo info = new DirectoryInfo(sPathToDir);
			
			
			
			foreach (DirectoryInfo dir in info.GetDirectories())
			{
				CopyDirectoryNoNotification(dir.FullName, sTargetDir + "\\" + dir.Name);
			}
			
			foreach (FileInfo file in info.GetFiles())
			{
				string sFileName = sTargetDir + "\\" + file.Name;
				
				try 
				{
					File.Copy(file.FullName, sFileName, true);
				}
				catch (Exception e)
				{
					CUserInteractionUtils.ShowErrorMessageBox(e.Message);
					
				}
			}
			
			
		}
	}
}
