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
	public class CryEngineProcessUtils
	{
		public CryEngineProcessUtils()
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
				
				ProcessUtils.RunProcessWithRedirectedStdErrorStdOut(rcProcess, Properties.Resources.CommonNotice);
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
					
					ProcessUtils.RunProcessWithRedirectedStdErrorStdOut(rcProcess, Properties.Resources.CommonNotice);
				}
			}
		}
		
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
	}
}
