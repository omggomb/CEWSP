/*
 * Created by SharpDevelop.
 * User: Ihatenames
 * Date: 12.06.2014
 * Time: 16:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Windows.Controls;
using CEWSP;
using CEWSP.SourceFileTracking;
using CEWSP.Utils;
using ExplorerTreeView;

namespace CEWSP
{
	/// <summary>
	/// Description of CewspTreeViewItem.
	/// </summary>
	public class CewspTreeViewItem : CustomTreeItem
	{
		public CewspTreeViewItem() : base()
		{
			
		}
		
		public override void MakeHeader(System.Windows.Controls.Image oEntryImage)
		{
			base.MakeHeader(oEntryImage);
			
			string sRelPath = CPathUtils.ExtractRelativeToGameFolder(FullPathToReference);
			
			if (sRelPath == FullPathToReference)
				sRelPath = CPathUtils.ExtractRelativeToRoot(FullPathToReference);
			
			sRelPath += "\\";
			
			if (CSourceTracker.Instance.IsFileTracked(FullPathToReference))
			{
				MarkTracked();
			}
			
			if (IsDirectory && sRelPath != ".\\")
			{
				if (CSourceTracker.Instance.DoesDirectoryContainTrackedFile(sRelPath))
					MarkTracked(false);
			}
		}
		
		public void MarkTracked(bool bIncludeSubDirs = true)
		{
			if (IsDirectory && bIncludeSubDirs)
			{
				foreach (CewspTreeViewItem element in Items)
				{
					element.MarkTracked();
				}
			}
			
			var stack = Header as StackPanel;
			
			if (stack != null)
			{
				var label = stack.Children[1] as Label;
				
				if (label  != null)
				{
					string sOldName = label.Content as string;
					
					if (!sOldName.Contains("[Tracked]"))
					{
						sOldName += " [Tracked]";
						label.Content = sOldName;
					}
				}
			}
			
			
		}
		
		public void UnmarkTracked(bool bIncludeSubDirs = true)
		{
			if (IsDirectory && bIncludeSubDirs)
			{
				foreach (CewspTreeViewItem element in Items)
				{
					element.UnmarkTracked();
				}
			}
			
			var stack = Header as StackPanel;
			
			if (stack != null)
			{
				var label = stack.Children[1] as Label;
				
				if (label  != null)
				{
					string sOldName = label.Content as string;
					
					if (sOldName.Contains("[Tracked]"))
					{
						sOldName = IdentificationName;
						label.Content = sOldName;
					}
				}
			}
			
			
		}
		
		void RefreshParentTrackingMark(CewspTreeViewItem item)
		{
			var par = item.GetParentSave() as CewspTreeViewItem;
			
			string sRelPath = CPathUtils.ExtractRelativeToGameFolder(FullPathToReference);
			
			if (sRelPath == FullPathToReference)
				sRelPath = CPathUtils.ExtractRelativeToRoot(FullPathToReference);
			
			sRelPath += "\\";
			
		
		}	
	}
}
