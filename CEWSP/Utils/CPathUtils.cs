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
	public class CPathUtils
	{
		public CPathUtils()
		{
		}
		
		/// <summary>
		/// Checks whether a given string will cause problems inside CE (e.g is neither a letter nor digit
		/// nor a path specific letter such as : . \ _).
		/// </summary>
		/// <param name="sString">The string to be checked.</param>
		/// <returns>True if the string won't cause any problems.</returns>
		public static bool IsStringCEConform(string sString)
		{
			char currentChar = '?';	
			int charValue = 0;
		
			List<char> allowedSpecials = new List<char>();
			allowedSpecials.Add('.');
			allowedSpecials.Add(':');
			allowedSpecials.Add('\\');
			allowedSpecials.Add('_');
			allowedSpecials.Add('(');
			allowedSpecials.Add(')');

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
							
							MessageBox.Show("Sorry, but the path you specified (" + sString + ") is not CE conform, it will cause problems." + "\n" + errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
		
		public static string GetFilename(string path)
		{
			int dirPos = path.LastIndexOf('\\');
			
			dirPos = (dirPos == -1) ? 0 : dirPos;
			
			int dotPos = path.LastIndexOf('.');
			
			dotPos = (dotPos == -1) ? (path.Length) : dotPos;
			
			int dirAdjust = (dirPos == 0) ? 0 : (dirPos + 1);
			
			return path.Substring(dirPos + dirAdjust, dotPos - dirPos - dirAdjust);
		}
		
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
		
		public static string GetCEVersionAsString()
		{
			FileVersionInfo info = GetCEVersion();
			
			
			return (info != null) ? String.Format("{0}.{1}.{2}.{3}", info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart) : "";
			                            
		}
		
		public static string ChangeExtension(string sTargetPath, string sNewExtensionWithDot)
		{
			string noExtension = RemoveExtension(sTargetPath);
			
			if (sTargetPath != noExtension)
			{
				sTargetPath = noExtension + sNewExtensionWithDot;
			}
			
			return sTargetPath;
		}
		
		public static string GetFilePath(string fullFilePath)
		{
			int lastDirPos = fullFilePath.LastIndexOf('\\');
			
			if (lastDirPos != -1)
			{
				fullFilePath = fullFilePath.Substring(0, lastDirPos);
			}
			
			return fullFilePath;
		}
		
		public static string RemoveExtension(string sFilePath)
		{
			int dotPos = sFilePath.LastIndexOf('.');
			
			if (dotPos > 0) // meaning != -1 and != 0 (0 would mean it's a relative path)
			{
				sFilePath =  sFilePath.Substring(0, dotPos);
			}
			
			return sFilePath;
		}
		
		/// <summary>
		/// Returns a file's extension (if any) WITHOUT the dot.
		/// </summary>
		/// <param name="sFilePath"></param>
		/// <returns></returns>
		public static string GetExtension(string sFilePath)
		{
			int lastDir = sFilePath.LastIndexOf('\\');
			int lastDot = sFilePath.LastIndexOf('.');
			
			if (lastDot > lastDir && lastDot != -1)
			{
				string sExtension = sFilePath.Substring(lastDot);
				
				sExtension = sExtension.TrimStart('.');
				
				if (sExtension != "")
					return sExtension;
			}
			
			return sFilePath;
		}
	}
}
