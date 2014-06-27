/*
 * Created by SharpDevelop.
 * User: omggomb
 * Date: 09/24/2013
 * Time: 18:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;

using CEWSP.ApplicationSettings;

namespace CEWSP.Utils
{
	/// <summary>
	/// Path utilities.
	/// </summary>
	public static class CPathUtils
	{
		
		/// <summary>
		/// Checks whether a given string will cause problems inside CE (e.g is neither a letter nor digit
		/// nor a path specific letter such as : . \ _).
		/// </summary>
		/// <param name="sString">The string to be checked.</param>
		/// <returns>True if the string won't cause any problems.</returns>
		/// <param name = "bForce">If true, user will definately be warned about the path being non CE conform</param>
		public static bool IsStringCEConform(string sString, bool bForce = false)
		{
			if (!CApplicationSettings.Instance.GetValue(ESettingsStrings.WarnNonCEConformPath).GetValueBool() && !bForce)
				return true;
			
			char currentChar = '?';	
			int charValue = 0;
		
			List<char> allowedSpecials = new List<char>();
			allowedSpecials.Add('.');
			allowedSpecials.Add(':');
			allowedSpecials.Add('\\');
			allowedSpecials.Add('_');
			allowedSpecials.Add('(');
			allowedSpecials.Add(')');
			allowedSpecials.Add('-');

			for (int i = 0; i < sString.Length; ++i)
			{
				currentChar = sString[i];
				charValue = (int)currentChar;
				
				// It' not a digit...
				if (charValue < 48 || charValue > 57)
				{
					// It's not a letter A-Z or a-z
					if ((charValue < 65 || charValue > 90) && (charValue < 97 || charValue > 122))
					{
						// It's a whitspace character or none of \ . : _
						if (Char.IsWhiteSpace(currentChar) || (!allowedSpecials.Contains(currentChar)))
						{
							string errorMessage = "";
							
							if (Char.IsWhiteSpace(currentChar))
							{
								errorMessage = "Your path contains a whitespace, try using '_' instead of spaces"; 
							}
							else
							{
								errorMessage = "Your path contains an invalid character (" + currentChar + "). Please only use ASCII characters!";
							}
							
							var res = MessageBox.Show("Sorry, but the path you specified (" + sString + ") is not CE conform, it will cause problems." + "\n" + errorMessage + "\n" +
							                "Do you still want to use this path?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
							if (res == MessageBoxResult.No)
								return false;
						}
					}
				}
			}
			
			return true;
		}
		
		
		/// <summary>
		/// Crops the path so it only contains folders and files inside the current root.
		/// </summary>
		/// <param name="sPath"></param>
		/// <returns></returns>
		public static string ExtractRelativeToRoot(string sPath)
		{
			// TODO: Implement
			
			string root = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath).GetValueString();
			
			if (sPath.Contains(root))
			{
				sPath = sPath.Remove(0, root.Length);
				sPath = "." + sPath;
			}
			
			return sPath;
		}
		
		/// <summary>
		/// Tries to make the given path relative to the currently set game folder
		/// </summary>
		/// <param name="sPath">Path that should be made relative</param>
		/// <returns>If succede the relative path, else the unchanged given path (sPath)</returns>
		public static string ExtractRelativeToGameFolder(string sPath)
		{
			string gameFolder = CApplicationSettings.Instance.GetValue(ESettingsStrings.GameFolderPath).GetValueString();
			
			if (sPath.Contains(gameFolder))
			{
				sPath = sPath.Remove(0, gameFolder.Length);
				sPath = "." + sPath;
			}
			
			return sPath;
		}
		
		
		/// <summary>
		/// Looks up the version number of the CE Editor executable
		/// </summary>
		/// <returns>Null if failed</returns>
		public static FileVersionInfo GetCEVersion()
		{
			CSetting root = CApplicationSettings.Instance.GetValue(ESettingsStrings.RootPath);
			
			if (root != null)
			{
				string edito64bitPath = root.GetValueString() + CApplicationSettings.Instance.GetValue(ESettingsStrings.SB64bitRelativePath).GetValueString();
				FileVersionInfo ceInfo = FileVersionInfo.GetVersionInfo(edito64bitPath);
				
				return ceInfo;
			}
			
			return null;
		}
		
		/// <summary>
		/// Creates a string from the CE Editor executable version number
		/// </summary>
		/// <returns>Empty string if failed</returns>
		public static string GetCEVersionAsString()
		{
			FileVersionInfo info = GetCEVersion();
			
			
			return (info != null) ? String.Format("{0}.{1}.{2}.{3}", info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart) : "";
			                            
		}
		
		/// <summary>
		/// Tries to make the given path relative to either the game or the root folder.
		/// </summary>
		/// <param name="sPath"></param>
		/// <returns></returns>
		public static string MakeRelative(string sPath)
		{
			string sRelPath = CPathUtils.ExtractRelativeToGameFolder(sPath);
			
			if (sRelPath == sPath)
				sRelPath = CPathUtils.ExtractRelativeToRoot(sPath);
			
			return sRelPath;
		}
		
		/// <summary>
		/// Makes path relative
		/// </summary>
		/// <param name="sPath">Path to be made relative</param>
		/// <param name="detectedRoot">The file root that was detected for this path</param>
		/// <returns></returns>
		public static string MakeRelative(string sPath, out SourceFileTracking.EFileRoot detectedRoot)
		{
			detectedRoot = GetRoot(sPath);
			
			return MakeRelative(sPath);
		}
		
		/// <summary>
		/// Detects whether the given path is inside or outside the game folder
		/// </summary>
		/// <param name="sPath"></param>
		/// <returns></returns>
		public static SourceFileTracking.EFileRoot GetRoot(string sPath)
		{
			string sRelPath = CPathUtils.ExtractRelativeToGameFolder(sPath);
			
			if (sRelPath == sPath)
			{
				sRelPath = CPathUtils.ExtractRelativeToRoot(sPath);
				return SourceFileTracking.EFileRoot.eFR_CERoot;
			}
			else
			{
				return SourceFileTracking.EFileRoot.eFR_GameFolder;
			}
		}
		
		/// <summary>
		/// Checks whether the given name can be used as a game folder name, meaning it is not one of the engine folders.
		/// </summary>
		/// <param name="sNameToCheck"></param>
		/// <returns></returns>
		public static bool CanNameBeGameFolder(string sNameToCheck)
		{
			if (sNameToCheck != "Bin32" &&
			    sNameToCheck != "Bin64" &&
			    sNameToCheck != "Code" &&
			    sNameToCheck != "Bin32_Dedicated" &&
			    sNameToCheck != "Bin64_Dedicated" &&
			    sNameToCheck != "Editor" &&
			    sNameToCheck != "Engine" &&
			    sNameToCheck != "Localization" &&
			    sNameToCheck != "LogBackups" &&
			    sNameToCheck != "statoscope" &&
			    sNameToCheck != "TestResults" &&
			    sNameToCheck != "Tools" &&
			    !sNameToCheck.Contains("USER"))
				return true;
			
			return false;
			    
		}
	}
}
