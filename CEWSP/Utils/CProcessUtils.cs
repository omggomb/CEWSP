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
using Microsoft.VisualBasic.FileIO;

using CEWSP.ApplicationSettings;

namespace CEWSP.Utils
{
	/// <summary>
	/// Description of CProcessUtils.
	/// </summary>
	public class CProcessUtils
	{
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
		
		/// <summary>
		/// Copies a directory.
		/// </summary>
		/// <param name="sPathToDir">Full path to the directory to be copied</param>
		/// <param name="sTargetDir">Full path to the target directory</param>
		/// <param name="bOverwrite">Overwrite existing directory?</param>
		/// <param name="bSilent">Hide progress bar? Still shows errors</param>
		public static void CopyDirectory(string sPathToDir, string sTargetDir, bool bOverwrite = true, bool bSilent = false)
		{
			if (bOverwrite)
			{
				if (Directory.Exists(sTargetDir))
					Directory.Delete(sTargetDir, true);
			}
			
			DirectoryInfo info = new DirectoryInfo(sPathToDir);
			
			FileSystem.CopyDirectory(sPathToDir, sTargetDir + "\\" + info.Name,
			                         bSilent ? UIOption.OnlyErrorDialogs : UIOption.AllDialogs,
			                         UICancelOption.DoNothing);
		}
		
		/// <summary>
		/// Copies a file
		/// </summary>
		/// <param name="sSourceFile">Full path to the file to be copied</param>
		/// <param name="sTargetFile">Full path to the destination file</param>
		/// <param name="bOverwrite">Overwrite exsting file?</param>
		/// <param name="bSilent">Hide progress bar? Still shows errors</param>
		public static void CopyFile(string sSourceFile, string sTargetFile, bool bOverwrite = true, bool bSilent = false)
		{
			if (bOverwrite)
			{
				if (File.Exists(sTargetFile))
					File.Delete(sTargetFile);
			}
			
			FileSystem.CopyFile(sSourceFile, sTargetFile,
			                         bSilent ? UIOption.OnlyErrorDialogs : UIOption.AllDialogs,
			                         UICancelOption.DoNothing);
		}
		
		/// <summary>
		/// Moves a directory.
		/// </summary>
		/// <param name="sPathToDir">Full path to the directory to be moved</param>
		/// <param name="sTargetDir">Full path to the target directory</param>
		/// <param name="bOverwrite">Overwrite existing directory?</param>
		/// <param name="bSilent">Hide progress bar? Still shows errors</param>
		public static void MoveDirectory(string sPathToDir, string sTargetDir, bool bOverwrite = true, bool bSilent = false)
		{
			if (bOverwrite)
			{
				if (Directory.Exists(sTargetDir))
					Directory.Delete(sTargetDir, true);
			}
			
			FileSystem.MoveDirectory(sPathToDir, sTargetDir,
			                         bSilent ? UIOption.OnlyErrorDialogs : UIOption.AllDialogs,
			                         UICancelOption.DoNothing);
		}
		
		/// <summary>
		/// Moves a file
		/// </summary>
		/// <param name="sSourceFile">Full path to the file to be moved</param>
		/// <param name="sTargetFile">Full path to the destination file</param>
		/// <param name="bOverwrite">Overwrite exsting file?</param>
		/// <param name="bSilent">Hide progress bar? Still shows errors</param>
		public static void MoveFile(string sSourceFile, string sTargetFile, bool bOverwrite = true, bool bSilent = false)
		{
			if (bOverwrite)
			{
				if (File.Exists(sTargetFile))
					File.Delete(sTargetFile);
			}
			
			FileSystem.MoveFile(sSourceFile, sTargetFile,
			                         bSilent ? UIOption.OnlyErrorDialogs : UIOption.AllDialogs,
			                         UICancelOption.DoNothing);
		}
	}
}
