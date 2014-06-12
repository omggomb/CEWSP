/*
 * Created by SharpDevelop.
 * User: Ihatenames
 * Date: 04/06/2014
 * Time: 15:36
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Diagnostics;
using CEWSP.Utils;


using OmgUtils.UserInteraction;

namespace CEWSP.Shortcuts
{
	/// <summary>
	/// Description of SShortcut.
	/// </summary>
	public struct SShortcut
	{
		public string Name {get; set;}
		public string Exec {get; set;}
		public string Args {get; set;}
		public int 		myID {get; set;}
		
		
		/*public SShortcut(int nID, string sName, string sExec, string sArgs)
		{
			myID = nID;
			Name = sName;
			Exec = sExec;
			Args = sArgs;
		}*/
		
		public void Start()
		{
			if (!File.Exists(Exec))
			{
				UserInteractionUtils.ShowErrorMessageBox(Properties.Resources.CommonPathDoesntExist + "(" + Exec + ")");
				return;
			}
			
			Process p = new Process();
			ProcessStartInfo info = new ProcessStartInfo(Exec);
			info.Arguments = Args;
			
			p.StartInfo = info;
			
			p.Start();
	
		}
	}
}
