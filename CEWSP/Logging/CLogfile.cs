/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 19.11.2013
 * Time: 00:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text;
using System.IO;

namespace CEWSP.Logging
{
	public enum ESeverity
	{
		eSev_Info,
		eSev_Warning,
		eSev_Error,
	}
	/// <summary>
	/// Description of CLogfile.
	/// </summary>
	public class CLogfile
	{
		private static CLogfile _instance;
		
		private string m_sSaveFilePath;
		
		private StringBuilder m_sLogBuffer;
		
		private const string m_sInfoTag = "[Info]";
		private const string m_sWarningTag = "[Warning]";
		private const string m_sErrorTag = "[Error]";
		
		public static CLogfile Instance
		{
			get
			{
				if (_instance == null)
					_instance = new CLogfile();
				
				return _instance;
			}
		}
		
		public CLogfile()
		{
		}
		
		/// <summary>
		/// Initialises this logfile
		/// </summary>
		/// <param name="sFilePath">The path where the logfile should be saved to.</param>
		/// <returns>True on succes</returns>
		public bool Initialise(string sFilePath)
		{
			m_sSaveFilePath = sFilePath;
			
			m_sLogBuffer = new StringBuilder();
			
			
			return true;
		}
		
		/// <summary>
		/// Saves the log to a file.
		/// </summary>
		/// <returns>True on succes</returns>
		public bool Shutdown()
		{
			return true;
		}
		
		public void LogMessage(string sMessage)
		{
			LogInfo(sMessage);
		}
		
		public void LogMessage(ESeverity severity, string sMessage)
		{
			switch (severity) 
			{
				case ESeverity.eSev_Info:
					LogInfo(sMessage);
					break;
				case ESeverity.eSev_Warning:
					LogWarning(sMessage);
					break;
				case ESeverity.eSev_Error:
					LogError(sMessage);
					break;
				default:
					throw new Exception("Invalid value for ESeverity");
			}
		}
		
		public void LogInfo(string sMessage)
		{
			AppendLine(m_sInfoTag + " " + sMessage);
		}
		
		public void LogWarning(string sMessage)
		{
			AppendLine(m_sWarningTag + " " + sMessage);
		}
		
		public void LogError(string sMessage)
		{
			AppendLine(m_sErrorTag + " " + sMessage);
		}
		
		private void AppendLine(string sLine)
		{
			m_sLogBuffer.AppendLine(sLine);
			
			if (!String.IsNullOrWhiteSpace(m_sSaveFilePath))
			{
				StreamWriter writer = null;
				try
				{
					writer = File.CreateText(m_sSaveFilePath);
					
					writer.Write(m_sLogBuffer.ToString());
					
					writer.Close();
					
				} 
				catch (Exception)
				{
					
					if (writer != null)
						writer.Close();
				}
			}
		}
	}
}
