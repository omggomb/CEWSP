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

using OmgUtils.ProcessUt;

namespace CEWSP.Utils
{
	/// <summary>
	/// Description of CProcessUtils.
	/// </summary>
	public static class CryEngineProcessUtils
	{
		
		
		/// <summary>
		/// Runs the crytiff converter on the given file and display a result window afterwards
		/// </summary>
		/// <param name="info"></param>
		public static void RunRCtif(FileInfo info)
		{
			if (CApplicationSettings.Instance.IsRootValid(CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString()))
			{
				string rcPath = GetValidRCPath();
				
				
				Process rcProcess = new Process();
				
				ProcessStartInfo startInfo = new ProcessStartInfo(rcPath);
				
				startInfo.Arguments = "\"" + info.FullName + "\"" + " /userdialog";
				rcProcess.StartInfo = startInfo;
				
				
				
				ProcessUtils.RunProcessWithRedirectedStdErrorStdOut(rcProcess, Properties.Resources.CommonNotice);
			}
			
		}
		
		/// <summary>
		/// Runs the resource compiler on the given file and displays a result window afterwards
		/// </summary>
		/// <param name="info"></param>
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
					string rcPath = GetValidRCPath();
					
					
					Process rcProcess = new Process();
					
					ProcessStartInfo startInfo = new ProcessStartInfo(rcPath);
					
					startInfo.Arguments = "\"" + info.FullName + "\"";
					rcProcess.StartInfo = startInfo;
					
					ProcessUtils.RunProcessWithRedirectedStdErrorStdOut(rcProcess, Properties.Resources.CommonNotice);
				}
			}
		}
		
		/// <summary>
		/// Runs the gfx exproter on the given file and displays a result window afterwards
		/// </summary>
		/// <param name="fileInfo"></param>
		public static void RunGFXExporter(FileInfo fileInfo)
		{
			string root = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			string gfxPath = root + CApplicationSettings.Instance.GetValue(ESettingsStrings.GFXRelativePath).GetValueString();
			
			
			
			Process proc = new Process();
			
			ProcessStartInfo info = new ProcessStartInfo(gfxPath);
			
			info.Arguments = fileInfo.FullName + " " + CApplicationSettings.Instance.GetValue(ESettingsStrings.GFXExporterArguments).Value;
			proc.StartInfo = info;
			
			ProcessUtils.RunProcessWithRedirectedStdErrorStdOut(proc, Properties.Resources.CommonNotice);
		}
		
		/// <summary>
		/// Decides whether to use 32bit or 64bit rc and returns its path.
		/// There are issues with the 64bit rc, so it will always return 32bit, if present
		/// </summary>
		/// <returns></returns>
		public static string GetValidRCPath()
		{
			string rcPath = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
					
			string 	rc64Path  = rcPath + CApplicationSettings.Instance.GetValue(ESettingsStrings.RC64bitRelativePath).GetValueString();
			string 	rc32Path = rcPath + CApplicationSettings.Instance.GetValue(ESettingsStrings.RCRelativePath).GetValueString();
			
			return File.Exists(rc32Path) ? rc32Path : rc64Path;
			
		}
	}
}
